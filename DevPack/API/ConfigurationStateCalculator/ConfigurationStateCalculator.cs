namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API;

	/// <summary>
	/// Calculates the <see cref="ConfigurationState"/> of the orchestration settings of a job, a recurring job or their nodes.
	/// </summary>
	/// <remarks>
	/// This calculator determines the configuration state on the fly, based on the orchestration settings of the given object. All required parameter definitions and orchestration script definitions
	/// are read in batch and cached for the lifetime of the calculator, so a single instance should be reused as long as the
	/// underlying job (or recurring job) is not modified.
	/// </remarks>
	internal sealed class ConfigurationStateCalculator
	{
		private readonly IMediaOpsPlanApi _planApi;
		private readonly IMediaOpsLiveApi _liveApi;

		private readonly OrchestrationSettings _rootSettings;
		private readonly IReadOnlyDictionary<string, OrchestrationSettings> _settingsByNodeId;

		private readonly Lazy<IReadOnlyDictionary<Guid, Capability>> _capabilities;
		private readonly Lazy<IReadOnlyDictionary<Guid, Capacity>> _capacities;
		private readonly Lazy<IReadOnlyDictionary<Guid, Configuration>> _configurations;
		private readonly Lazy<bool> _isLiveInstalled;

		private readonly Dictionary<string, ScriptInputRequirements> _requirementsByScriptName = new Dictionary<string, ScriptInputRequirements>();
		private readonly Dictionary<string, ConfigurationState> _stateByNodeId = new Dictionary<string, ConfigurationState>();

		private ConfigurationState? _rootState;

		/// <summary>
		/// Initializes a new instance of the <see cref="ConfigurationStateCalculator"/> class for the specified job.
		/// </summary>
		/// <param name="planApi">The MediaOps Plan API.</param>
		/// <param name="liveApi">The MediaOps Live API.</param>
		/// <param name="job">The job for which the configuration states are calculated.</param>
		/// <exception cref="ArgumentNullException">When one of the arguments is <c>null</c>.</exception>
		public ConfigurationStateCalculator(IMediaOpsPlanApi planApi, IMediaOpsLiveApi liveApi, Job job)
			: this(planApi, liveApi, job?.OrchestrationSettings, GetSettingsByNodeId(job?.NodeGraph))
		{
			if (job == null)
			{
				throw new ArgumentNullException(nameof(job));
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ConfigurationStateCalculator"/> class for the specified recurring job.
		/// </summary>
		/// <param name="planApi">The MediaOps Plan API.</param>
		/// <param name="liveApi">The MediaOps Live API.</param>
		/// <param name="recurringJob">The recurring job for which the configuration states are calculated.</param>
		/// <exception cref="ArgumentNullException">When one of the arguments is <c>null</c>.</exception>
		public ConfigurationStateCalculator(IMediaOpsPlanApi planApi, IMediaOpsLiveApi liveApi, RecurringJob recurringJob)
			: this(planApi, liveApi, recurringJob?.OrchestrationSettings, GetSettingsByNodeId(recurringJob?.NodeGraph))
		{
			if (recurringJob == null)
			{
				throw new ArgumentNullException(nameof(recurringJob));
			}
		}

		private ConfigurationStateCalculator(IMediaOpsPlanApi planApi, IMediaOpsLiveApi liveApi, OrchestrationSettings rootSettings, IReadOnlyDictionary<string, OrchestrationSettings> settingsByNodeId)
		{
			_planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
			_liveApi = liveApi ?? throw new ArgumentNullException(nameof(liveApi));

			_rootSettings = rootSettings;
			_settingsByNodeId = settingsByNodeId ?? new Dictionary<string, OrchestrationSettings>();

			_capabilities = new Lazy<IReadOnlyDictionary<Guid, Capability>>(() => _planApi.Capabilities.Read(GetReferencedIds(x => x.Capabilities.Select(y => y.Id))).ToDictionary(x => x.Id));
			_capacities = new Lazy<IReadOnlyDictionary<Guid, Capacity>>(() => _planApi.Capacities.Read(GetReferencedIds(x => x.Capacities.Select(y => y.Id))).ToDictionary(x => x.Id));
			_configurations = new Lazy<IReadOnlyDictionary<Guid, Configuration>>(() => _planApi.Configurations.Read(GetReferencedIds(x => x.Configurations.Select(y => y.Id))).ToDictionary(x => x.Id));
			_isLiveInstalled = new Lazy<bool>(() => _liveApi.IsInstalled());
		}

		/// <summary>
		/// Creates a calculator for the nodes of the specified node graph. No job level state can be calculated with the returned instance.
		/// </summary>
		/// <typeparam name="TNode">The type of the nodes in the graph.</typeparam>
		/// <param name="planApi">The MediaOps Plan API.</param>
		/// <param name="liveApi">The MediaOps Live API.</param>
		/// <param name="nodeGraph">The node graph for which the node configuration states are calculated.</param>
		/// <returns>A calculator for the nodes of the specified node graph.</returns>
		/// <exception cref="ArgumentNullException">When one of the arguments is <c>null</c>.</exception>
		public static ConfigurationStateCalculator ForNodes<TNode>(IMediaOpsPlanApi planApi, IMediaOpsLiveApi liveApi, NodeGraph<TNode> nodeGraph)
			where TNode : NodeBase
		{
			if (nodeGraph == null)
			{
				throw new ArgumentNullException(nameof(nodeGraph));
			}

			return new ConfigurationStateCalculator(planApi, liveApi, null, GetSettingsByNodeId(nodeGraph));
		}

		/// <summary>
		/// Gets the configuration state of the job (or recurring job) itself, based on its job level orchestration settings.
		/// </summary>
		/// <returns>The configuration state of the job.</returns>
		/// <exception cref="InvalidOperationException">When the calculator was not created for a job or recurring job.</exception>
		public ConfigurationState GetJobConfigurationState()
		{
			if (_rootSettings == null)
			{
				throw new InvalidOperationException("No job level orchestration settings are available. Create the calculator with a job or recurring job to calculate the job configuration state.");
			}

			if (!_rootState.HasValue)
			{
				_rootState = GetConfigurationState(_rootSettings);
			}

			return _rootState.Value;
		}

		/// <summary>
		/// Gets the configuration state of the node with the specified ID.
		/// </summary>
		/// <param name="nodeId">The ID of the node.</param>
		/// <returns>The configuration state of the node.</returns>
		/// <exception cref="InvalidOperationException">When the node is not part of the job.</exception>
		public ConfigurationState GetNodeConfigurationState(string nodeId)
		{
			if (!TryGetNodeConfigurationState(nodeId, out var state))
			{
				throw new InvalidOperationException($"The node with ID {nodeId} is not part of the job.");
			}

			return state;
		}

		/// <summary>
		/// Tries to get the configuration state of the node with the specified ID.
		/// </summary>
		/// <param name="nodeId">The ID of the node.</param>
		/// <param name="state">When this method returns <c>true</c>, contains the configuration state of the node.</param>
		/// <returns><c>true</c> when the node is part of the job; otherwise, <c>false</c>.</returns>
		public bool TryGetNodeConfigurationState(string nodeId, out ConfigurationState state)
		{
			state = default;

			if (String.IsNullOrEmpty(nodeId))
			{
				return false;
			}

			if (_stateByNodeId.TryGetValue(nodeId, out state))
			{
				return true;
			}

			if (!_settingsByNodeId.TryGetValue(nodeId, out var settings))
			{
				return false;
			}

			state = GetConfigurationState(settings);
			_stateByNodeId[nodeId] = state;

			return true;
		}

		/// <summary>
		/// Gets the configuration state of every node of the job.
		/// </summary>
		/// <returns>The configuration state per node ID.</returns>
		public IReadOnlyDictionary<string, ConfigurationState> GetNodeConfigurationStates()
		{
			foreach (var nodeId in _settingsByNodeId.Keys)
			{
				TryGetNodeConfigurationState(nodeId, out _);
			}

			return _stateByNodeId;
		}

		/// <summary>
		/// Determines whether the job or one of its nodes is missing mandatory values.
		/// </summary>
		/// <returns><c>true</c> when the job or one of its nodes has the state <see cref="ConfigurationState.MandatoryValuesMissing"/>; otherwise, <c>false</c>.</returns>
		public bool HasMissingMandatoryValues()
		{
			if (_rootSettings != null && GetJobConfigurationState() == ConfigurationState.MandatoryValuesMissing)
			{
				return true;
			}

			return GetNodeConfigurationStates().Values.Any(x => x == ConfigurationState.MandatoryValuesMissing);
		}

		/// <summary>
		/// Calculates the configuration state of the specified orchestration settings.
		/// </summary>
		/// <param name="settings">The orchestration settings.</param>
		/// <returns>The configuration state of the specified orchestration settings.</returns>
		/// <exception cref="ArgumentNullException">When <paramref name="settings"/> is <c>null</c>.</exception>
		public ConfigurationState GetConfigurationState(OrchestrationSettings settings)
		{
			if (settings == null)
			{
				throw new ArgumentNullException(nameof(settings));
			}

			if (settings.OrchestrationEvents.Any(x => !IsEventFullyDefined(x)))
			{
				return ConfigurationState.MandatoryValuesMissing;
			}

			if (MandatoryParametersMissingValues(settings))
			{
				return ConfigurationState.MandatoryValuesMissing;
			}

			if (ParametersMissingValues(settings))
			{
				return ConfigurationState.NonMandatoryValuesMissing;
			}

			if (IsEmpty(settings))
			{
				return ConfigurationState.NoParametersDefined;
			}

			return ConfigurationState.AllValuesProvided;
		}

		private static IReadOnlyDictionary<string, OrchestrationSettings> GetSettingsByNodeId<TNode>(NodeGraph<TNode> nodeGraph)
			where TNode : NodeBase
		{
			var settingsByNodeId = new Dictionary<string, OrchestrationSettings>();
			if (nodeGraph == null)
			{
				return settingsByNodeId;
			}

			foreach (var node in nodeGraph.Nodes)
			{
				if (node?.Id == null || node.OrchestrationSettings == null)
				{
					continue;
				}

				settingsByNodeId[node.Id] = node.OrchestrationSettings;
			}

			return settingsByNodeId;
		}

		private IReadOnlyCollection<Guid> GetReferencedIds(Func<OrchestrationSettings, IEnumerable<Guid>> idSelector)
		{
			var ids = new HashSet<Guid>();

			if (_rootSettings != null)
			{
				ids.UnionWith(idSelector(_rootSettings));
			}

			foreach (var settings in _settingsByNodeId.Values)
			{
				ids.UnionWith(idSelector(settings));
			}

			return ids;
		}

		private bool MandatoryParametersMissingValues(OrchestrationSettings settings)
		{
			if (settings.Capabilities.Any(x => !x.HasValue && !x.HasReference && IsMandatory(_capabilities.Value, x.Id, y => y.IsMandatory)))
			{
				return true;
			}

			if (settings.Capacities.Any(x => !x.HasValue && !x.HasReference && IsMandatory(_capacities.Value, x.Id, y => y.IsMandatory)))
			{
				return true;
			}

			if (settings.Configurations.Any(x => !x.HasValue && !x.HasReference && IsMandatory(_configurations.Value, x.Id, y => y.IsMandatory)))
			{
				return true;
			}

			return false;
		}

		private static bool IsMandatory<TParameter>(IReadOnlyDictionary<Guid, TParameter> parametersById, Guid id, Func<TParameter, bool> isMandatorySelector)
		{
			// A parameter definition that no longer exists cannot be considered mandatory.
			return parametersById.TryGetValue(id, out var parameter) && isMandatorySelector(parameter);
		}

		private bool ParametersMissingValues(OrchestrationSettings settings)
		{
			if (settings.Capabilities.Any(x => !x.HasValue && !x.HasReference))
			{
				return true;
			}

			if (settings.Capacities.Any(x => !x.HasValue && !x.HasReference))
			{
				return true;
			}

			if (settings.Configurations.Any(x => !x.HasValue && !x.HasReference))
			{
				return true;
			}

			return false;
		}

		private bool IsEmpty(OrchestrationSettings settings)
		{
			if (settings.Capabilities.Any())
			{
				return false;
			}

			if (settings.Capacities.Any())
			{
				return false;
			}

			if (settings.Configurations.Any())
			{
				return false;
			}

			if (settings.OrchestrationEvents.Any(x => !String.IsNullOrEmpty(x.ExecutionDetails.ScriptName)))
			{
				return false;
			}

			return true;
		}

		private bool IsEventFullyDefined(OrchestrationEvent orchestrationEvent)
		{
			if (!_isLiveInstalled.Value)
			{
				return true;
			}

			if (orchestrationEvent?.ExecutionDetails == null || String.IsNullOrWhiteSpace(orchestrationEvent.ExecutionDetails.ScriptName))
			{
				return true;
			}

			var requirements = GetScriptInputRequirements(orchestrationEvent.ExecutionDetails.ScriptName);

			foreach (var elementName in requirements.ElementNames)
			{
				var elementSetting = orchestrationEvent.ExecutionDetails.ScriptElements.FirstOrDefault(x => x.Name == elementName);
				if (elementSetting == null)
				{
					return false;
				}

				if (String.IsNullOrWhiteSpace(elementSetting.ElementName) && !elementSetting.HasReference)
				{
					return false;
				}
			}

			foreach (var parameterName in requirements.ParameterNames)
			{
				var parameterSetting = orchestrationEvent.ExecutionDetails.ScriptParameters.FirstOrDefault(x => x.Name == parameterName);
				if (parameterSetting == null)
				{
					return false;
				}

				if (String.IsNullOrWhiteSpace(parameterSetting.Value) && !parameterSetting.HasReference)
				{
					return false;
				}
			}

			return true;
		}

		private ScriptInputRequirements GetScriptInputRequirements(string scriptName)
		{
			if (_requirementsByScriptName.TryGetValue(scriptName, out var requirements))
			{
				return requirements;
			}

			var scriptInputInfo = _liveApi.Orchestration.Scripts.GetOrchestrationScriptInputInfo(scriptName);
			if (scriptInputInfo == null)
			{
				requirements = ScriptInputRequirements.Empty;
			}
			else
			{
				requirements = new ScriptInputRequirements(
					scriptInputInfo.Elements.Select(x => x.Name).ToList(),
					scriptInputInfo.Parameters.Select(x => x.Name).ToList());
			}

			_requirementsByScriptName[scriptName] = requirements;

			return requirements;
		}

		private sealed class ScriptInputRequirements
		{
			public static readonly ScriptInputRequirements Empty = new ScriptInputRequirements(new List<string>(), new List<string>());

			public ScriptInputRequirements(IReadOnlyCollection<string> elementNames, IReadOnlyCollection<string> parameterNames)
			{
				ElementNames = elementNames;
				ParameterNames = parameterNames;
			}

			public IReadOnlyCollection<string> ElementNames { get; }

			public IReadOnlyCollection<string> ParameterNames { get; }
		}
	}
}
