namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using System.Text.RegularExpressions;

	using Skyline.DataMiner.Net.Helper;
	using Skyline.DataMiner.Net.Jobs;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.ResourceManager.Objects;
	using Skyline.DataMiner.Net.ResponseErrorData;
	using Skyline.DataMiner.Net.SRM.Capabilities;
	using Skyline.DataMiner.Net.SRM.Capacities;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;

	using CoreReservation = Net.ResourceManager.Objects.ReservationInstance;
	using DomJob = Storage.DOM.SlcWorkflow.JobsInstance;
	using DomResource = Storage.DOM.SlcResource_Studio.ResourceInstance;

	internal class CoreJobHandler : DomInstanceApiObjectValidator<DomJob>
	{
		private readonly MediaOpsPlanApi planApi;

		private CoreJobHandler(MediaOpsPlanApi planApi)
		{
			this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
		}

		public static bool TryCreateOrUpdate(MediaOpsPlanApi planApi, ICollection<DomJob> domJobs, out DomInstanceBulkOperationResult<DomJob> result)
		{
			var handler = new CoreJobHandler(planApi);
			handler.CreateOrUpdate(domJobs);

			result = new DomInstanceBulkOperationResult<DomJob>(handler.successfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);
			return !result.HasFailures;
		}

		public static bool TryConfirm(MediaOpsPlanApi planApi, ICollection<DomJob> domJobs, out DomInstanceBulkOperationResult<DomJob> result)
		{
			var handler = new CoreJobHandler(planApi);
			handler.Confirm(domJobs);

			result = new DomInstanceBulkOperationResult<DomJob>(handler.successfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);
			return !result.HasFailures;
		}

		public static bool TryReturnToPending(MediaOpsPlanApi planApi, ICollection<DomJob> domJobs, out DomInstanceBulkOperationResult<DomJob> result)
		{
			var handler = new CoreJobHandler(planApi);
			handler.ReturnToPending(domJobs);

			result = new DomInstanceBulkOperationResult<DomJob>(handler.successfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);
			return !result.HasFailures;
		}

		public static bool TryCancel(MediaOpsPlanApi planApi, ICollection<DomJob> domJobs, out DomInstanceBulkOperationResult<DomJob> result)
		{
			var handler = new CoreJobHandler(planApi);
			handler.Cancel(domJobs);

			result = new DomInstanceBulkOperationResult<DomJob>(handler.successfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);
			return !result.HasFailures;
		}

		public static bool TryDelete(MediaOpsPlanApi planApi, ICollection<DomJob> domJobs, out DomInstanceBulkOperationResult<DomJob> result)
		{
			var handler = new CoreJobHandler(planApi);
			handler.Delete(domJobs);

			result = new DomInstanceBulkOperationResult<DomJob>(handler.successfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);
			return !result.HasFailures;
		}

		private static string ComposeReservationActionScriptConfig(Guid reservationId, string action)
		{
			return $"Script:MediaOps_SRM_Scheduling Actions||Reservation ID={reservationId};Action={action}|||NoConfirmation,NoSetCheck,Asynchronous";
		}

		private void CreateOrUpdate(ICollection<DomJob> domJobs)
		{
			if (domJobs == null)
			{
				throw new ArgumentNullException(nameof(domJobs));
			}

			if (domJobs.Count == 0)
			{
				return;
			}

			var jobByReservationId = new Dictionary<Guid, DomJob>();

			var reservationsToCreateOrUpdate = new List<CoreReservation>();
			foreach (var mapping in JobReservationMapping.GetMappings(planApi, domJobs))
			{
				var job = mapping.Job;
				var reservation = mapping.Reservation;

				if (!SyncJobWithReservation(job, ref reservation))
				{
					planApi.Logger.Information(this, $"No update required for Job with ID {job.ID.Id} and Reservation with ID {reservation.ID}.");
					continue;
				}

				reservationsToCreateOrUpdate.Add(reservation);

				jobByReservationId.Add(reservation.ID, job);
			}

			if (reservationsToCreateOrUpdate.Count == 0)
			{
				return;
			}

			planApi.CoreHelpers.ResourceManagerHelper.TryCreateOrUpdateReservationInstancesInBatches(reservationsToCreateOrUpdate, out var result, new ResourceManagerTraceDataHandler(planApi));

			foreach (var id in result.UnsuccessfulIds)
			{
				if (!jobByReservationId.TryGetValue(id, out var domJob))
				{
					planApi.Logger.Error(this, $"Failed to find DOM ID for Reservation ID {id}.");
					continue;
				}

				ReportError(domJob.ID.Id);

				if (result.TraceDataPerItem.TryGetValue(id, out var traceData))
				{
					PassTraceData(domJob.ID.Id, traceData);
				}
			}

			foreach (var linkableObject in result.SuccessfulItems)
			{
				if (!jobByReservationId.TryGetValue(linkableObject.ID, out var domJob))
				{
					planApi.Logger.Error(this, $"Failed to find DOM ID for Reservation ID {linkableObject.ID}.");
					continue;
				}

				var reservation = linkableObject as CoreReservation;
				if (reservation == null)
				{
					planApi.Logger.Error(this, $"Linkable object with ID {linkableObject.ID} is not of type CoreReservation.");
					continue;
				}

				ReportSuccess(domJob);
			}
		}

		private void Confirm(ICollection<DomJob> domJobs)
		{
			UpdateStatus(domJobs, Skyline.DataMiner.Net.Messages.ReservationStatus.Confirmed);
		}

		private void ReturnToPending(ICollection<DomJob> domJobs)
		{
			UpdateStatus(domJobs, Skyline.DataMiner.Net.Messages.ReservationStatus.Pending);
		}

		private void Cancel(ICollection<DomJob> domJobs)
		{
			UpdateStatus(domJobs, Skyline.DataMiner.Net.Messages.ReservationStatus.Canceled);
		}

		private void Delete(ICollection<DomJob> domJobs)
		{
			if (domJobs == null)
			{
				throw new ArgumentNullException(nameof(domJobs));
			}

			if (domJobs.Count == 0)
			{
				return;
			}

			var domJobsByReservationId = new Dictionary<Guid, DomJob>();
			var toDelete = new List<CoreReservation>();

			foreach (var mapping in JobReservationMapping.GetMappings(planApi, domJobs))
			{
				if (mapping.IsNew)
				{
					ReportSuccess(mapping.Job);
					continue;
				}

				toDelete.Add(mapping.Reservation);
				domJobsByReservationId[mapping.Reservation.ID] = mapping.Job;
			}

			planApi.CoreHelpers.ResourceManagerHelper.TryDeleteReservationInstancesInBatches(toDelete, out var result);

			foreach (var id in result.UnsuccessfulIds)
			{
				if (!domJobsByReservationId.TryGetValue(id, out var domJob))
				{
					planApi.Logger.Error(this, $"Failed to find DOM ID for Reservation ID {id}.");
					continue;
				}

				ReportError(domJob.ID.Id);

				if (result.TraceDataPerItem.TryGetValue(id, out var traceData))
				{
					PassTraceData(domJob.ID.Id, traceData);
				}
			}

			foreach (var id in result.SuccessfulIds)
			{
				if (!domJobsByReservationId.TryGetValue(id, out var domJob))
				{
					planApi.Logger.Error(this, $"Failed to find DOM ID for Reservation ID {id}.");
					continue;
				}

				ReportSuccess(domJob);
			}
		}

		private void UpdateStatus(ICollection<DomJob> domJobs, Skyline.DataMiner.Net.Messages.ReservationStatus reservationStatus)
		{
			if (domJobs == null)
			{
				throw new ArgumentNullException(nameof(domJobs));
			}

			if (domJobs.Count == 0)
			{
				return;
			}

			var domJobsByReservationId = new Dictionary<Guid, DomJob>();
			var toUpdate = new List<CoreReservation>();

			foreach (var mapping in JobReservationMapping.GetMappings(planApi, domJobs))
			{
				if (mapping.IsNew)
				{
					ReportError(mapping.Job.ID.Id);
					continue;
				}

				mapping.Reservation.Status = reservationStatus;

				toUpdate.Add(mapping.Reservation);
				domJobsByReservationId[mapping.Reservation.ID] = mapping.Job;
			}

			planApi.CoreHelpers.ResourceManagerHelper.TryCreateOrUpdateReservationInstancesInBatches(toUpdate, out var result);

			foreach (var id in result.UnsuccessfulIds)
			{
				if (!domJobsByReservationId.TryGetValue(id, out var domJob))
				{
					planApi.Logger.Error(this, $"Failed to find DOM ID for Reservation ID {id}.");
					continue;
				}

				ReportError(domJob.ID.Id);

				if (result.TraceDataPerItem.TryGetValue(id, out var traceData))
				{
					PassTraceData(domJob.ID.Id, traceData);
				}
			}

			foreach (var id in result.SuccessfulIds)
			{
				if (!domJobsByReservationId.TryGetValue(id, out var domJob))
				{
					planApi.Logger.Error(this, $"Failed to find DOM ID for Reservation ID {id}.");
					continue;
				}

				ReportSuccess(domJob);
			}
		}

		private bool SyncJobWithReservation(DomJob job, ref CoreReservation reservation)
		{
			bool updateRequired = false;

			updateRequired |= SyncName(job, reservation);
			updateRequired |= SyncStatus(job, reservation);
			updateRequired |= SyncQuarantineHandlingScript(reservation);
			updateRequired |= SyncProperties(job, reservation);
			updateRequired |= SyncTime(job, ref reservation);
			updateRequired |= SyncEvents(reservation);
			updateRequired |= SyncResources(job, reservation);

			return updateRequired;
		}

		private bool SyncName(DomJob job, CoreReservation reservation)
		{
			var reservationName = ReservationNameComposer.Compose(planApi, job);
			if (String.Equals(reservation.Name, reservationName))
			{
				return false;
			}

			reservation.Name = reservationName;
			return true;
		}

		private bool SyncStatus(DomJob job, CoreReservation reservation)
		{
			var expected = job.Status == Storage.DOM.SlcWorkflow.SlcWorkflowIds.Behaviors.Job_Behavior.StatusesEnum.Confirmed
				? Skyline.DataMiner.Net.Messages.ReservationStatus.Confirmed
				: Skyline.DataMiner.Net.Messages.ReservationStatus.Pending;

			if (reservation.Status.Equals(expected))
			{
				return false;
			}

			reservation.Status = expected;
			return true;
		}

		private bool SyncQuarantineHandlingScript(CoreReservation reservation)
		{
			var expected = "MediaOps_SRM_QuarantineHandling";

			if (reservation.QuarantineHandlingScriptName.Equals(expected))
			{
				return false;
			}

			reservation.QuarantineHandlingScriptName = expected;
			return true;
		}

		private bool SyncProperties(DomJob job, CoreReservation reservation)
		{
			bool updateRequired = false;

			updateRequired |= SyncProperty(reservation, "Job ID", Convert.ToString(job.ID.Id));

			return updateRequired;
		}

		private bool SyncProperty(CoreReservation reservation, string propertyName, string expectedValue)
		{
			if (!reservation.Properties.Dictionary.TryGetValue(propertyName, out var existingValue))
			{
				reservation.Properties.Add(new KeyValuePair<string, object>(propertyName, expectedValue));
				return true;
			}

			var existingPropertyValue = Convert.ToString(existingValue);
			if (existingPropertyValue.Equals(expectedValue))
			{
				return false;
			}

			reservation.Properties.AddOrUpdate(propertyName, expectedValue);
			return true;
		}

		private bool SyncTime(DomJob job, ref CoreReservation reservation)
		{
			var timeRange = new Skyline.DataMiner.Net.Time.TimeRangeUtc(job.JobInfo.Preroll.Value, job.JobInfo.Postroll.Value);
			if (reservation.TimeRange.Equals(timeRange))
			{
				return false;
			}

			var existingEvents = reservation.Events;
			foreach (var existingEvent in existingEvents)
			{
				reservation.RemoveEvent(existingEvent.Key, existingEvent.Value);
			}

			reservation = reservation.NewTimeRange(timeRange);

			foreach (var existingEvent in existingEvents)
			{
				DateTime time;

				switch (existingEvent.Value.Name)
				{
					case JobEvent.Start:
						time = timeRange.Start;
						break;

					case JobEvent.End:
						time = timeRange.Stop;
						break;

					default:
						time = existingEvent.Key;
						break;
				}

				reservation.AddEvent(time, existingEvent.Value);
			}

			return true;
		}

		private bool SyncEvents(CoreReservation reservation)
		{
			bool updateRequired = false;

			var eventNames = reservation.Events.Select(x => x.Value.Name).ToHashSet();
			if (!eventNames.Contains(JobEvent.Start))
			{
				reservation.AddEvent(reservation.TimeRange.Start, new Skyline.DataMiner.Net.Messages.ReservationEvent(JobEvent.Start, ComposeReservationActionScriptConfig(reservation.ID, JobEvent.Start)));
				updateRequired = true;
			}

			if (!eventNames.Contains(JobEvent.End))
			{
				reservation.AddEvent(reservation.TimeRange.Stop, new Skyline.DataMiner.Net.Messages.ReservationEvent(JobEvent.End, ComposeReservationActionScriptConfig(reservation.ID, JobEvent.End)));
				updateRequired = true;
			}

			return updateRequired;
		}

		private bool SyncResources(DomJob job, CoreReservation reservation)
		{
			var expectedUsages = ResourceUsageBuilder.BuildUsages(planApi, job);

			if (reservation.ResourcesInReservationInstance.ScrambledEquals(expectedUsages))
			{
				return false;
			}

			reservation.ResourcesInReservationInstance.Clear();
			if (!reservation.IsQuarantined)
			{
				reservation.ResourcesInReservationInstance.AddRange(expectedUsages);
				return true;
			}

			reservation.QuarantinedResources.RemoveAll(x =>
			{
				// Can be removed if not present in expected usages
				var coreResourceUsages = expectedUsages.Where(y => y.GUID == x.QuarantinedResourceUsage.GUID).ToList();
				if (coreResourceUsages.Count == 0)
				{
					return true;
				}

				// Cannot be removed if the corresponding ServiceDefinitionNodeID is still present in expected usages, even if other details differ
				if (coreResourceUsages.Select(y => y.ServiceDefinitionNodeID).Contains(((ServiceResourceUsageDefinition)x.QuarantinedResourceUsage).ServiceDefinitionNodeID))
				{
					return false;
				}

				return true;
			});

			reservation.ResourcesInReservationInstance.AddRange(expectedUsages.Where(x =>
			{
				var coreResourcesInQuarantine = reservation.QuarantinedResources.Where(y => y.QuarantinedResourceUsage.GUID == x.GUID).ToList();
				if (coreResourcesInQuarantine.Count == 0)
				{
					return true;
				}

				if (coreResourcesInQuarantine.Select(y => ((ServiceResourceUsageDefinition)y.QuarantinedResourceUsage).ServiceDefinitionNodeID).Contains(x.ServiceDefinitionNodeID))
				{
					return false;
				}

				return true;
			}));

			if (reservation.QuarantinedResources.Count == 0)
			{
				reservation.IsQuarantined = false;
				reservation.Status = job.Status == Storage.DOM.SlcWorkflow.SlcWorkflowIds.Behaviors.Job_Behavior.StatusesEnum.Tentative
					? Net.Messages.ReservationStatus.Pending
					: Net.Messages.ReservationStatus.Confirmed;
			}

			return true;
		}

		private static class ReservationNameComposer
		{
			private const string ReplaceCharacter = "-";

			private static readonly char[] ForbiddenCharacters = new[] { '/', '\\', ':', ';', '*', '?', '"', '<', '>', '|', '°' };

			public static string Compose(MediaOpsPlanApi planApi, DomJob job)
			{
				if (planApi == null)
				{
					throw new ArgumentNullException(nameof(planApi));
				}

				if (job == null)
				{
					throw new ArgumentNullException(nameof(job));
				}

				var reservationName = $"{job.JobInfo.JobName} [{job.JobInfo.JobID}]";
				reservationName = CleanName(reservationName);

				return reservationName;
			}

			private static string CleanName(string name)
			{
				// Removes leading '.' or 'space' characters using regex
				name = Regex.Replace(name, @"^[.\s]+", string.Empty);

				// Replace forbidden characters with '-'
				string forbiddenPattern = $"[{Regex.Escape(new string(ForbiddenCharacters))}]";
				name = Regex.Replace(name, forbiddenPattern, ReplaceCharacter);

				// Ensures that '%' does not exist more than once
				int firstPercentIndex = name.IndexOf('%');
				if (firstPercentIndex != -1)
				{
					// Replace all subsequent '%' characters with '-'
					name = name.Substring(0, firstPercentIndex + 1) +
							  name.Substring(firstPercentIndex + 1).Replace("%", ReplaceCharacter);
				}

				return name;
			}
		}

		private sealed class ResourceUsageBuilder
		{
			public static IReadOnlyCollection<ServiceResourceUsageDefinition> BuildUsages(MediaOpsPlanApi planApi, DomJob job)
			{
				var resourceNodes = job.Nodes.Where(x => x.NodeType == Storage.DOM.SlcWorkflow.SlcWorkflowIds.Enums.Nodetype.Resource && x.NodeEndTime > DateTimeOffset.UtcNow).ToList();
				if (resourceNodes.Count == 0)
				{
					return new List<ServiceResourceUsageDefinition>();
				}

				var domResourceIds = resourceNodes.Select(x => x.ReferenceId).Distinct().ToList();
				var cachedDomResourcesById = job.DomInstanceCache.GetFromCache<DomResource>().ToDictionary(x => x.ID.Id);

				var missingResourceIds = domResourceIds.Where(x => !cachedDomResourcesById.ContainsKey(x));
				var domResources = planApi.Resources.Read(missingResourceIds).Select(x => x.OriginalInstance).ToList();
				domResources.AddRange(cachedDomResourcesById.Values);

				var domResourcesById = domResources.ToDictionary(x => x.ID.Id);

				// All node orchestration settings are parsed earlier in the pipeline and cached on the DOM job by
				// DomJobHandler (in UpdateOrchestrationSettingsCache) immediately before this handler runs, and every
				// node section references its orchestration setting by ID. The settings of every resource node are
				// therefore available here without re-parsing the configuration or reading the profile parameters again.
				var orchestrationSettingsCache = job.OrchestrationSettingsCache;

				// The resolved references were cached earlier in the pipeline, so referenced values can be applied here.
				var resolvedReferences = job.ResolvedReferenceCache;

				var result = new List<ServiceResourceUsageDefinition>();
				foreach (var node in resourceNodes)
				{
					if (!domResourcesById.TryGetValue(node.ReferenceId, out var domResource))
					{
						// This situation should not happen as the validation in DomJobHandler should have prevented it, but in case it does, we log an error and skip this resource.
						planApi.Logger.Error($"Failed to find DOM Resource with ID {node.ReferenceId} for Job with ID {job.ID.Id}. This resource will be skipped.");
						continue;
					}

					var resourceUsage = new ServiceResourceUsageDefinition
					{
						GUID = domResource.ResourceInternalProperties.Resource_Id.GetValueOrDefault(),
						ServiceDefinitionNodeID = (int)node.CoreReservationNodeID.GetValueOrDefault(),
					};

					if (node.NodeConfiguration.HasValue
						&& node.NodeConfiguration.Value != Guid.Empty
						&& orchestrationSettingsCache.TryGetValue(node.NodeConfiguration.Value, out var orchestrationSettings))
					{
						resourceUsage.RequiredCapabilities = BuildCapabilities(orchestrationSettings.Capabilities, resolvedReferences).ToList();
						resourceUsage.RequiredCapacities = BuildCapacities(orchestrationSettings.Capacities, resolvedReferences).ToList();
					}

					result.Add(resourceUsage);
				}

				return result;
			}

			private static IEnumerable<ResourceCapabilityUsage> BuildCapabilities(IReadOnlyCollection<CapabilitySetting> capabilities, ResolvedReferenceCache resolvedReferences)
			{
				foreach (var capability in capabilities)
				{
					// Capabilities are discrete in this model, so the value is always applied as the required discrete value.
					if (!TryGetCapabilityValue(capability, resolvedReferences, out var value))
					{
						continue;
					}

					yield return new ResourceCapabilityUsage
					{
						CapabilityProfileID = capability.Id,
						RequiredDiscreet = value,
					};
				}
			}

			private static IEnumerable<MultiResourceCapacityUsage> BuildCapacities(IReadOnlyCollection<CapacitySetting> capacities, ResolvedReferenceCache resolvedReferences)
			{
				foreach (var capacity in capacities)
				{
					switch (capacity)
					{
						case NumberCapacitySetting numberCapacity:
							if (TryGetCapacityQuantity(numberCapacity, resolvedReferences, out var quantity))
							{
								yield return new MultiResourceCapacityUsage
								{
									CapacityProfileID = numberCapacity.Id,
									DecimalQuantity = quantity,
								};
							}

							break;

						case RangeCapacitySetting rangeCapacity:
							// References are not yet supported for range capacities, so only literal min/max values are applied.
							if (rangeCapacity.HasValue)
							{
								yield return new MultiResourceCapacityUsage
								{
									CapacityProfileID = rangeCapacity.Id,
									RangeStart = rangeCapacity.MinValue.Value,
									DecimalQuantity = rangeCapacity.MaxValue.Value - rangeCapacity.MinValue.Value,
								};
							}

							break;
					}
				}
			}

			private static bool TryGetCapabilityValue(CapabilitySetting capability, ResolvedReferenceCache resolvedReferences, out string value)
			{
				if (capability.HasValue)
				{
					value = capability.Value;
					return true;
				}

				if (TryGetResolvedRawValue(capability, resolvedReferences, out var rawValue))
				{
					value = Convert.ToString(rawValue, CultureInfo.InvariantCulture);
					return value != null;
				}

				value = null;
				return false;
			}

			private static bool TryGetCapacityQuantity(NumberCapacitySetting capacity, ResolvedReferenceCache resolvedReferences, out decimal quantity)
			{
				if (capacity.Value.HasValue)
				{
					quantity = capacity.Value.Value;
					return true;
				}

				if (TryGetResolvedRawValue(capacity, resolvedReferences, out var rawValue) && TryConvertToDecimal(rawValue, out quantity))
				{
					return true;
				}

				quantity = default;
				return false;
			}

			private static bool TryGetResolvedRawValue(Setting setting, ResolvedReferenceCache resolvedReferences, out object rawValue)
			{
				rawValue = null;

				if (!setting.HasReference || resolvedReferences == null)
				{
					return false;
				}

				if (!resolvedReferences.TryGetValue(setting.Reference, out var resolvedValue) || !resolvedValue.IsResolved)
				{
					return false;
				}

				rawValue = resolvedValue.GetRawValue();
				return rawValue != null;
			}

			private static bool TryConvertToDecimal(object rawValue, out decimal value)
			{
				switch (rawValue)
				{
					case decimal decimalValue:
						value = decimalValue;
						return true;
					case double doubleValue:
						value = (decimal)doubleValue;
						return true;
					case string stringValue when decimal.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed):
						value = parsed;
						return true;
					default:
						value = default;
						return false;
				}
			}
		}

		private sealed class JobReservationMapping
		{
			private JobReservationMapping(DomJob domJob)
				: this(domJob, BuildCoreReservation())
			{
				IsNew = true;
			}

			private JobReservationMapping(DomJob domJob, CoreReservation coreReservation)
			{
				Job = domJob ?? throw new ArgumentNullException(nameof(domJob));
				Reservation = coreReservation ?? throw new ArgumentNullException(nameof(coreReservation));
			}

			public DomJob Job { get; }

			public CoreReservation Reservation { get; }

			/// <summary>
			/// Indicates whether this mapping represents a new reservation that needs to be created, or an existing reservation that may need to be updated.
			/// </summary>
			public bool IsNew { get; }

			public static IEnumerable<JobReservationMapping> GetMappings(MediaOpsPlanApi planApi, ICollection<DomJob> domJobs)
			{
				if (planApi == null)
				{
					throw new ArgumentNullException(nameof(planApi));
				}

				if (domJobs == null)
				{
					throw new ArgumentNullException(nameof(domJobs));
				}

				if (domJobs.Count == 0)
				{
					return [];
				}

				return GetMappingsIterator(planApi, domJobs);
			}

			private static IEnumerable<JobReservationMapping> GetMappingsIterator(MediaOpsPlanApi planApi, ICollection<DomJob> domJobs)
			{
				var jobIds = domJobs.Select(x => x.ID.Id).ToList();
				FilterElement<CoreReservation> Filter(Guid id) => ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(id));
				var reservationsByJobId = planApi.CoreHelpers.ResourceManagerHelper.GetReservationInstances(jobIds, Filter)
					.GroupBy(x => Guid.Parse(Convert.ToString(x.Properties
						.First(y => y.Key == "Job ID").Value)))
					.ToDictionary(g => g.Key, g => g.ToList());

				foreach (var domJob in domJobs)
				{
					if (!reservationsByJobId.TryGetValue(domJob.ID.Id, out var coreReservations))
					{
						yield return new JobReservationMapping(domJob);
						continue;
					}

					var mapping = new JobReservationMapping(domJob, coreReservations.First());
					if (coreReservations.Count > 1)
					{
						planApi.Logger.Error(mapping, $"Multiple reservations found for Job with ID {domJob.ID.Id}. This should not happen. Job Name: {domJob.Name}. Number of reservations found: {coreReservations.Count}. First reservation will be used.");
					}

					yield return mapping;
				}
			}

			private static CoreReservation BuildCoreReservation()
			{
				return new CoreReservation
				{
					ID = Guid.NewGuid(),
				};
			}
		}

		private sealed class ResourceManagerTraceDataHandler : ITraceDataHandler<ResourceManagerErrorData>
		{
			private readonly MediaOpsPlanApi planApi;

			private readonly Dictionary<Guid, MediaOpsTraceData> traceDataPerReservationId = new Dictionary<Guid, MediaOpsTraceData>();

			public ResourceManagerTraceDataHandler(MediaOpsPlanApi planApi)
			{
				this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
			}

			public IReadOnlyDictionary<Guid, MediaOpsTraceData> Translate(ICollection<ResourceManagerErrorData> resourceManagerErrors)
			{
				if (resourceManagerErrors == null)
				{
					throw new ArgumentNullException(nameof(resourceManagerErrors));
				}

				if (resourceManagerErrors.Count == 0)
				{
					return new Dictionary<Guid, MediaOpsTraceData>();
				}

				var reservationUpdateCausedReservationsToGoToQuarantineErrors = resourceManagerErrors.Where(x => x.ErrorReason == ResourceManagerErrorData.Reason.ReservationUpdateCausedReservationsToGoToQuarantine).ToList();
				var resourceCapacityInvalidErrors = resourceManagerErrors.Where(x => x.ErrorReason == ResourceManagerErrorData.Reason.ResourceCapacityInvalid).ToList();
				var resourceCapabilityInvalidErrors = resourceManagerErrors.Where(x => x.ErrorReason == ResourceManagerErrorData.Reason.ResourceCapabilityInvalid).ToList();
				if ((reservationUpdateCausedReservationsToGoToQuarantineErrors.Count + resourceCapacityInvalidErrors.Count + resourceCapabilityInvalidErrors.Count) != resourceManagerErrors.Count)
				{
					return ReturnDefaultTraceData(resourceManagerErrors);
				}

				throw new NotImplementedException();
			}

			private MediaOpsTraceData GetOrCreateTraceData(Guid id)
			{
				if (!traceDataPerReservationId.TryGetValue(id, out var traceData))
				{
					traceData = new MediaOpsTraceData();
					traceDataPerReservationId[id] = traceData;
				}

				return traceData;
			}

			private Dictionary<Guid, MediaOpsTraceData> ReturnDefaultTraceData(ICollection<ResourceManagerErrorData> resourceManagerErrors)
			{
				foreach (var error in resourceManagerErrors)
				{
					if (error.SubjectId == Guid.Empty)
					{
						planApi.Logger.Error(this, $"Error with reason {error.ErrorReason} has empty SubjectId. This should not happen. Error message: {error.Message}");
						continue;
					}

					var traceData = GetOrCreateTraceData(error.SubjectId.Value);
					traceData.Add(new MediaOpsErrorData
					{
						ErrorMessage = error.ToString(),
					});
				}

				return traceDataPerReservationId;
			}
		}
	}
}
