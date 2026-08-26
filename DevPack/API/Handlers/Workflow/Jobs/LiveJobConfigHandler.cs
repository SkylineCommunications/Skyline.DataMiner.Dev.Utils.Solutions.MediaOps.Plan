namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;
	using Skyline.DataMiner.Utils.SecureCoding.SecureSerialization.Json.Newtonsoft;

	using ConnectivityLevel = Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.ConnectivityManagement.Level;
	using Live = Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;
	using LiveEnums = Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;

	/// <summary>
	/// Provides functionality to handle the orchestration configuration for a job in MediaOps Live.
	/// </summary>
	internal sealed class LiveJobConfigHandler
	{
		private static readonly TimeSpan EventMinSchTime = TimeSpan.FromSeconds(5);

		private readonly MediaOpsPlanApi _planApi;
		private readonly Job _job;
		private readonly JobState _targetState;
		private readonly DateTimeOffset _currentTime;
		private readonly JobReferenceResolver _referenceResolver;
		private readonly Live.OrchestrationJobConfiguration _liveConfiguration;
		private readonly Lazy<Dictionary<long, ConnectivityLevel>> _lazyLevelsByNumber;
		private readonly Lazy<Dictionary<Guid, Resource>> _lazyResourcesById;

		private LiveJobConfigHandler(MediaOpsPlanApi planApi, Job job, JobState targetState, ReferenceDefinitionCache referenceDefinitions, DateTimeOffset currentTime)
		{
			_planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
			_job = job ?? throw new ArgumentNullException(nameof(job));
			_targetState = targetState;
			_currentTime = currentTime;

			_referenceResolver = new JobReferenceResolver(planApi, job, referenceDefinitions);
			_liveConfiguration = planApi.LiveApi.Orchestration.GetOrCreateNewOrchestrationJobConfiguration(job.Id.ToString());

			_lazyLevelsByNumber = new Lazy<Dictionary<long, ConnectivityLevel>>(BuildLevelsByNumber);
			_lazyResourcesById = new Lazy<Dictionary<Guid, Resource>>(BuildResourcesById);
		}

		private Dictionary<long, ConnectivityLevel> LevelsByNumber => _lazyLevelsByNumber.Value;

		private Dictionary<Guid, Resource> ResourcesById => _lazyResourcesById.Value;

		internal static void SetLiveJobConfigForJob(MediaOpsPlanApi planApi, Job job, JobState targetState, ReferenceDefinitionCache referenceDefinitions, DateTimeOffset currentTime)
		{
			if (planApi == null)
			{
				throw new ArgumentNullException(nameof(planApi));
			}

			if (!planApi.LiveApi.IsInstalled())
			{
				// MediaOps Live is not installed, so no orchestration events can be created.
				return;
			}

			new LiveJobConfigHandler(planApi, job, targetState, referenceDefinitions, currentTime).ScheduleOrTriggerEvents();
		}

		internal static void DeleteLiveJobConfigForJob(MediaOpsPlanApi planApi, Job job)
		{
			if (planApi == null)
			{
				throw new ArgumentNullException(nameof(planApi));
			}

			if (job == null)
			{
				throw new ArgumentNullException(nameof(job));
			}

			if (!planApi.LiveApi.IsInstalled())
			{
				// MediaOps Live is not installed, so there is nothing to delete.
				return;
			}

			var orchestrationJob = planApi.LiveApi.Orchestration.GetOrCreateNewOrchestrationJob(job.Id.ToString());
			if (orchestrationJob.OrchestrationEvents.Count == 0)
			{
				return;
			}

			planApi.LiveApi.Orchestration.DeleteJob(orchestrationJob);
		}

		private static LiveEnums.EventType MapEventType(OrchestrationEventType eventType)
		{
			switch (eventType)
			{
				case OrchestrationEventType.PrerollStart: return LiveEnums.EventType.PrerollStart;
				case OrchestrationEventType.PrerollStop: return LiveEnums.EventType.PrerollStop;
				case OrchestrationEventType.PostrollStart: return LiveEnums.EventType.PostrollStart;
				case OrchestrationEventType.PostrollStop: return LiveEnums.EventType.PostrollStop;
				default: throw new InvalidCastException("Cannot convert orchestration event type to a Live event type: " + eventType);
			}
		}

		private static Skyline.DataMiner.Net.Profiles.ParameterValue ToParameterValue(ResolvedValue resolvedValue)
		{
			var parameterValue = new Skyline.DataMiner.Net.Profiles.ParameterValue();

			switch (resolvedValue)
			{
				case DoubleResolvedValue doubleResolved:
					parameterValue.Type = Skyline.DataMiner.Net.Profiles.ParameterValue.ValueType.Double;
					parameterValue.DoubleValue = doubleResolved.Value;
					break;

				case DecimalResolvedValue decimalResolved:
					parameterValue.Type = Skyline.DataMiner.Net.Profiles.ParameterValue.ValueType.Double;
					parameterValue.DoubleValue = (double)decimalResolved.Value;
					break;

				default:
					parameterValue.Type = Skyline.DataMiner.Net.Profiles.ParameterValue.ValueType.String;
					parameterValue.StringValue = Convert.ToString(resolvedValue.GetRawValue(), CultureInfo.InvariantCulture);
					break;
			}

			return parameterValue;
		}

		private void ScheduleOrTriggerEvents()
		{
			if (_targetState == JobState.Draft)
			{
				// A draft job has no orchestration events.
				return;
			}

			var eventsToTrigger = new List<Live.OrchestrationEvent>();
			_liveConfiguration.JobInfo.JobName = _job.Name;
			_liveConfiguration.JobInfo.JobDescription = _job.Description;

			if (_targetState == JobState.Tentative)
			{
				// Orchestration events are only created once the job is confirmed. Remove any events that were created
				// previously (e.g. when returning from confirmed to tentative), but only when there is something to remove.
				if (_liveConfiguration.OrchestrationEvents.Any())
				{
					DeleteLiveJobConfigForJob(_planApi, _job);
				}

				return;
			}
			else if (_targetState == JobState.Confirmed || _targetState == JobState.Running)
			{
				// Schedule events in the future and trigger events in the past that did not trigger yet.
				eventsToTrigger = GetEventsToTriggerAndScheduleEvents();
			}
			else if (_targetState == JobState.Canceled)
			{
				if (!_liveConfiguration.OrchestrationEvents.Any())
				{
					// A job that was canceled before it was ever confirmed (e.g. a tentative job) has no orchestration events.
					return;
				}

				CancelEventsIfNotAlreadyTriggered();
			}
			else if (_targetState == JobState.Completed)
			{
				// A completed job keeps its orchestration events as-is.
			}
			else
			{
				throw new NotSupportedException("Unexpected job state: " + _targetState);
			}

			_planApi.LiveApi.Orchestration.SaveOrchestrationJobConfiguration(_liveConfiguration);

			if (eventsToTrigger.Any())
			{
				_planApi.LiveApi.Orchestration.ExecuteEventsNowInBackground(eventsToTrigger);
			}
		}

		private List<Live.OrchestrationEvent> GetEventsToTriggerAndScheduleEvents()
		{
			var eventsToTrigger = new List<Live.OrchestrationEvent>();

			// When the job is not already stopping, keep the start events up to date.
			if (!IsEventInThePast(LiveEnums.EventType.PostrollStart))
			{
				if (NeedsImmediateTriggeringAndSetEvent(LiveEnums.EventType.PrerollStart, out var livePreRollStartEvent))
				{
					eventsToTrigger.Add(livePreRollStartEvent);
				}

				if (NeedsImmediateTriggeringAndSetEvent(LiveEnums.EventType.PrerollStop, out var livePreRollStopEvent))
				{
					eventsToTrigger.Add(livePreRollStopEvent);
				}
			}

			if (NeedsImmediateTriggeringAndSetEvent(LiveEnums.EventType.PostrollStart, out var livePostRollStartEvent))
			{
				eventsToTrigger.Add(livePostRollStartEvent);
			}

			if (NeedsImmediateTriggeringAndSetEvent(LiveEnums.EventType.PostrollStop, out var livePostRollStopEvent))
			{
				eventsToTrigger.Add(livePostRollStopEvent);
			}

			return eventsToTrigger;
		}

		private void CancelEventsIfNotAlreadyTriggered()
		{
			CancelEventIfNotAlreadyTriggered(LiveEnums.EventType.PrerollStart);
			CancelEventIfNotAlreadyTriggered(LiveEnums.EventType.PrerollStop);
			CancelEventIfNotAlreadyTriggered(LiveEnums.EventType.PostrollStart);
			CancelEventIfNotAlreadyTriggered(LiveEnums.EventType.PostrollStop);
		}

		private void CancelEventIfNotAlreadyTriggered(LiveEnums.EventType eventType)
		{
			if (!DidEventAlreadyTrigger(eventType))
			{
				SetEventConfig(eventType, LiveEnums.EventState.Cancelled);
			}
		}

		private bool NeedsImmediateTriggeringAndSetEvent(LiveEnums.EventType eventType, out Live.OrchestrationEvent liveEvent)
		{
			liveEvent = null;

			if (IsEventInThePast(eventType))
			{
				if (DidEventAlreadyTrigger(eventType))
				{
					return false;
				}

				// The event is in the past and did not trigger yet, so it is put in Draft state to trigger it immediately.
				liveEvent = SetEventConfig(eventType, LiveEnums.EventState.Draft);
				return true;
			}

			SetEventConfig(eventType, LiveEnums.EventState.Confirmed);
			return false;
		}

		private Live.OrchestrationEvent SetEventConfig(LiveEnums.EventType eventType, LiveEnums.EventState eventState)
		{
			var liveEventConfig = _liveConfiguration.OrchestrationEvents.SingleOrDefault(e => e.EventType == eventType);
			if (liveEventConfig == null)
			{
				liveEventConfig = new Live.OrchestrationEventConfiguration
				{
					EventType = eventType,
					Name = $"{_job.Name}_{eventType}",
					EventTime = GetEventTimeForEventType(eventType),
				};

				_liveConfiguration.OrchestrationEvents.Add(liveEventConfig);
			}
			else
			{
				liveEventConfig.Name = $"{_job.Name}_{eventType}";
				liveEventConfig.EventTime = GetEventTimeForEventType(eventType);
			}

			liveEventConfig.EventState = eventState;

			SetLiveEventsForJob(liveEventConfig, eventType);

			return liveEventConfig;
		}

		private void SetLiveEventsForJob(Live.OrchestrationEventConfiguration liveEventConfig, LiveEnums.EventType eventType)
		{
			var jobEventSettings = _job.OrchestrationSettings?.OrchestrationEvents
				.FirstOrDefault(e => MapEventType(e.EventType) == eventType);

			BuildScriptConfiguration(jobEventSettings, null, out var scriptName, out var arguments, out var profile);

			liveEventConfig.GlobalOrchestrationScript = scriptName;
			liveEventConfig.GlobalOrchestrationScriptArguments = arguments;
			liveEventConfig.Profile = profile;

			// Hidden nodes are soft-deleted (removed or swapped-out) nodes that are kept for history but must not be orchestrated.
			var visibleNodes = _job.NodeGraph.Nodes.Where(n => !n.Hidden).ToList();

			// Remove node configurations for nodes that no longer exist in the job or that are hidden.
			var jobNodeIds = visibleNodes.Select(n => n.Id).ToHashSet();
			var nodeConfigs = liveEventConfig.Configuration.NodeConfigurations;

			foreach (var nodeConfig in nodeConfigs.Where(n => !jobNodeIds.Contains(n.NodeId)).ToList())
			{
				nodeConfigs.Remove(nodeConfig);
			}

			foreach (var node in visibleNodes)
			{
				SetLiveEventForNodeConfig(liveEventConfig, node, eventType);
			}

			liveEventConfig.Configuration.Connections = BuildConnections().ToList();
		}

		private void SetLiveEventForNodeConfig(Live.OrchestrationEventConfiguration liveEventConfig, JobNode node, LiveEnums.EventType eventType)
		{
			var nodeConfigs = liveEventConfig.Configuration.NodeConfigurations;
			var nodeEventConfig = nodeConfigs.SingleOrDefault(n => n.NodeId == node.Id);

			if (nodeEventConfig == null)
			{
				nodeEventConfig = new Live.NodeConfiguration
				{
					NodeId = node.Id,
					NodeLabel = node.Alias,
				};

				nodeConfigs.Add(nodeEventConfig);
			}
			else
			{
				nodeEventConfig.NodeLabel = node.Alias;
			}

			var nodeEventSettings = node.OrchestrationSettings?.OrchestrationEvents
				.FirstOrDefault(e => MapEventType(e.EventType) == eventType);

			BuildScriptConfiguration(nodeEventSettings, node.Id, out var scriptName, out var arguments, out var profile);

			nodeEventConfig.OrchestrationScriptName = scriptName;
			nodeEventConfig.OrchestrationScriptArguments = arguments;
			nodeEventConfig.Profile = profile;
		}

		private void BuildScriptConfiguration(OrchestrationEvent eventSettings, string owningNodeId, out string scriptName, out IList<Live.OrchestrationScriptArgument> arguments, out Live.OrchestrationProfile profile)
		{
			var executionDetails = eventSettings?.ExecutionDetails;

			if (executionDetails == null || String.IsNullOrWhiteSpace(executionDetails.ScriptName))
			{
				scriptName = null;
				arguments = new List<Live.OrchestrationScriptArgument>();
				profile = new Live.OrchestrationProfile();
				return;
			}

			var storage = executionDetails.ToStorage();

			scriptName = executionDetails.ScriptName;
			arguments = GetScriptArgumentsFromExecutionDetails(storage, eventSettings.Metadata, owningNodeId);
			profile = GetScriptProfilesFromExecutionDetails(storage, owningNodeId);
		}

		private IList<Live.OrchestrationScriptArgument> GetScriptArgumentsFromExecutionDetails(Storage.DOM.ScriptExecutionDetails executionDetails, string metadata, string owningNodeId)
		{
			var arguments = new List<Live.OrchestrationScriptArgument>();

			foreach (var dummy in executionDetails.Dummies)
			{
				arguments.Add(new Live.OrchestrationScriptArgument(LiveEnums.OrchestrationScriptArgumentType.Element, dummy.Key, dummy.Value));
			}

			foreach (var dummyReference in executionDetails.DummyReferences.Where(x => x.Value != null))
			{
				if (TryResolveReference(dummyReference.Value, owningNodeId, out var resolved))
				{
					var stringValue = Convert.ToString(resolved.GetRawValue(), CultureInfo.InvariantCulture);
					arguments.Add(new Live.OrchestrationScriptArgument(LiveEnums.OrchestrationScriptArgumentType.Element, dummyReference.Key, stringValue));
				}
			}

			foreach (var parameter in executionDetails.Parameters)
			{
				arguments.Add(new Live.OrchestrationScriptArgument(LiveEnums.OrchestrationScriptArgumentType.Parameter, parameter.Key, parameter.Value));
			}

			foreach (var parameterReference in executionDetails.ParameterReferences.Where(x => x.Value != null))
			{
				if (TryResolveReference(parameterReference.Value, owningNodeId, out var resolved))
				{
					var stringValue = Convert.ToString(resolved.GetRawValue(), CultureInfo.InvariantCulture);
					arguments.Add(new Live.OrchestrationScriptArgument(LiveEnums.OrchestrationScriptArgumentType.Parameter, parameterReference.Key, stringValue));
				}
			}

			if (!String.IsNullOrWhiteSpace(metadata))
			{
				try
				{
					var deserializedMetadata = SecureNewtonsoftDeserialization.DeserializeObject<Dictionary<string, string>>(metadata);

					foreach (var kvp in deserializedMetadata)
					{
						arguments.Add(new Live.OrchestrationScriptArgument(LiveEnums.OrchestrationScriptArgumentType.Metadata, kvp.Key, kvp.Value));
					}
				}
				catch
				{
					// Ignore malformed metadata; it should not block the orchestration events.
				}
			}

			return arguments;
		}

		private Live.OrchestrationProfile GetScriptProfilesFromExecutionDetails(Storage.DOM.ScriptExecutionDetails executionDetails, string owningNodeId)
		{
			var profile = new Live.OrchestrationProfile();

			foreach (var profileParameterValue in executionDetails.ProfileParameterValues)
			{
				if (profileParameterValue.Reference != null)
				{
					if (TryResolveReference(profileParameterValue.Reference, owningNodeId, out var resolved))
					{
						profile.Values.Add(new Live.OrchestrationProfileValue
						{
							Name = profileParameterValue.ProfileParameterId.ToString(),
							Value = ToParameterValue(resolved),
						});
					}

					continue;
				}

				var parameterValue = new Skyline.DataMiner.Net.Profiles.ParameterValue
				{
					Type = Skyline.DataMiner.Net.Profiles.ParameterValue.ValueType.String,
					StringValue = profileParameterValue.StringValue,
				};

				if (profileParameterValue.DoubleMinValue.HasValue && profileParameterValue.DoubleMaxValue.HasValue)
				{
					parameterValue.Type = Skyline.DataMiner.Net.Profiles.ParameterValue.ValueType.Range;
					parameterValue.RangeStart = profileParameterValue.DoubleMinValue.Value;
					parameterValue.RangeEnd = profileParameterValue.DoubleMaxValue.Value;
				}
				else if (profileParameterValue.DoubleMaxValue.HasValue)
				{
					parameterValue.Type = Skyline.DataMiner.Net.Profiles.ParameterValue.ValueType.Double;
					parameterValue.DoubleValue = profileParameterValue.DoubleMaxValue.Value;
				}

				profile.Values.Add(new Live.OrchestrationProfileValue
				{
					Name = profileParameterValue.ProfileParameterId.ToString(),
					Value = parameterValue,
				});
			}

			return profile;
		}

		private IEnumerable<Live.Connection> BuildConnections()
		{
			var resourcesById = ResourcesById;

			// Hidden nodes are soft-deleted (removed or swapped-out) nodes that are excluded from orchestration, so any
			// connection touching a hidden node must be skipped as well.
			foreach (var connection in _job.NodeGraph.Connections.Where(c => !c.From.Hidden && !c.To.Hidden))
			{
				if (!connection.From.IsResourceNode(out var sourceResourceNode) ||
					!resourcesById.TryGetValue(sourceResourceNode.ResourceId, out var sourceResource) ||
					sourceResource.VirtualSignalGroupOutputId == Guid.Empty)
				{
					// A connection should not be created without a valid source virtual signal group.
					continue;
				}

				if (!connection.To.IsResourceNode(out var destinationResourceNode) ||
					!resourcesById.TryGetValue(destinationResourceNode.ResourceId, out var destinationResource) ||
					destinationResource.VirtualSignalGroupInputId == Guid.Empty)
				{
					// A connection should not be created without a valid destination virtual signal group.
					continue;
				}

				var liveConnection = new Live.Connection
				{
					SourceNodeId = connection.From.Id,
					DestinationNodeId = connection.To.Id,
					SourceVsg = sourceResource.VirtualSignalGroupOutputId,
					DestinationVsg = destinationResource.VirtualSignalGroupInputId,
				};

				if (TryBuildLevelMapping(connection.Configuration, out var levelMappings))
				{
					if (levelMappings.Count == 0)
					{
						// A shuffle configuration without any resolvable level mappings would be interpreted by MediaOps Live
						// as a default full connection, which would incorrectly broaden access. Omit the connection instead.
						continue;
					}

					liveConnection.LevelMappings = levelMappings;
				}

				yield return liveConnection;
			}
		}

		private bool TryBuildLevelMapping(ConnectionConfiguration configuration, out IList<Live.LevelMapping> levelMappings)
		{
			levelMappings = new List<Live.LevelMapping>();

			// Only a shuffle configuration provides explicit level mappings. Any other configuration uses the default
			// behavior that connects each matching level.
			if (configuration is not ShuffleLevelBasedConnectionConfiguration shuffle)
			{
				return false;
			}

			foreach (var mapping in shuffle.LevelMappings)
			{
				// The dictionary maps a destination level number to a source level number.
				if (!LevelsByNumber.TryGetValue(mapping.Value, out var sourceLevel) ||
					!LevelsByNumber.TryGetValue(mapping.Key, out var destinationLevel))
				{
					continue;
				}

				levelMappings.Add(new Live.LevelMapping(
					new Live.Level(sourceLevel.Name, Convert.ToInt32(sourceLevel.Number)),
					new Live.Level(destinationLevel.Name, Convert.ToInt32(destinationLevel.Number))));
			}

			return true;
		}

		private bool TryResolveReference(Storage.DOM.DataReferenceStorage referenceStorage, string owningNodeId, out ResolvedValue resolvedValue)
		{
			resolvedValue = null;

			try
			{
				resolvedValue = _referenceResolver.ResolveValue(referenceStorage.ToDataReference(), owningNodeId);
			}
			catch (CircularReferenceException)
			{
				return false;
			}

			return resolvedValue != null && resolvedValue.IsResolved;
		}

		private bool DidEventAlreadyTrigger(LiveEnums.EventType eventType)
		{
			var liveEventConfig = _liveConfiguration.OrchestrationEvents.SingleOrDefault(e => e.EventType == eventType);
			if (liveEventConfig == null)
			{
				return false;
			}

			if (liveEventConfig.EventState == LiveEnums.EventState.Completed
				|| liveEventConfig.EventState == LiveEnums.EventState.Failed
				|| liveEventConfig.EventState == LiveEnums.EventState.Configuring)
			{
				return true;
			}

			// Execution is handed off to a deferred script, so an event that was already handed off is still in Draft
			// state for a while. It only counts as triggered when the hand-off happened at or after the time it is
			// currently scheduled for, so an event that is rescheduled to a later time is triggered again.
			return liveEventConfig.ActualStartTime.HasValue
				&& liveEventConfig.EventTime.HasValue
				&& liveEventConfig.ActualStartTime.Value >= liveEventConfig.EventTime.Value;
		}

		private bool IsEventInThePast(LiveEnums.EventType eventType)
		{
			return GetEventTimeForEventType(eventType) < (_currentTime + EventMinSchTime);
		}

		private DateTimeOffset GetEventTimeForEventType(LiveEnums.EventType eventType)
		{
			switch (eventType)
			{
				case LiveEnums.EventType.PrerollStart: return _job.PreRollStart;
				case LiveEnums.EventType.PrerollStop: return _job.Start;
				case LiveEnums.EventType.PostrollStart: return _job.End;
				case LiveEnums.EventType.PostrollStop: return _job.PostRollEnd;
				default: throw new InvalidOperationException("Unexpected event type: " + eventType);
			}
		}

		private Dictionary<long, ConnectivityLevel> BuildLevelsByNumber()
		{
			return _planApi.LiveApi.Levels.ReadAll().SafeToDictionary(x => x.Number);
		}

		private Dictionary<Guid, Resource> BuildResourcesById()
		{
			var resourceIds = new HashSet<Guid>();

			foreach (var node in _job.NodeGraph.Nodes)
			{
				if (node.IsResourceNode(out var resourceNode))
				{
					resourceIds.Add(resourceNode.ResourceId);
				}
			}

			return _planApi.Resources.Read(resourceIds).SafeToDictionary(x => x.Id);
		}
	}
}
