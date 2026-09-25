namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.ResourceManager.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core;

	using CoreResource = Skyline.DataMiner.Net.Messages.Resource;
	using Live = Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;
	using LiveEnums = Skyline.DataMiner.Solutions.MediaOps.Live.API.Enums;

	/// <summary>Validates jobs against their Resource Studio, core DataMiner, and MediaOps Live dependencies.</summary>
	internal sealed class JobValidator
	{
		private const int MaxErrorMessageLength = 300;

		private readonly IMediaOpsLiveApi liveApi;
		private readonly MediaOpsPlanApi planApi;

		/// <summary>Initializes a new instance of the <see cref="JobValidator"/> class.</summary>
		/// <param name="planApi">The MediaOps Plan API used to retrieve validation data.</param>
		public JobValidator(MediaOpsPlanApi planApi)
		{
			this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
			this.liveApi = planApi.LiveApi;
		}

		/// <summary>Validates jobs using shared bulk reads for all referenced objects.</summary>
		public IReadOnlyCollection<JobValidationResult> Validate(IEnumerable<Job> jobs)
		{
			if (jobs == null)
			{
				throw new ArgumentNullException(nameof(jobs));
			}

			var jobList = jobs.ToList();
			if (jobList.Any(job => job == null))
			{
				throw new ArgumentException("The jobs collection cannot contain null values.", nameof(jobs));
			}

			var results = jobList.ToDictionary(job => job, job => new JobValidationResult(job));
			if (jobList.Count == 0)
			{
				return Array.Empty<JobValidationResult>();
			}

			try
			{
				var context = BuildContext(jobList);
				foreach (var job in jobList)
				{
					Validate(job, results[job], context);
				}
			}
			catch (Exception exception)
			{
				foreach (var result in results.Values)
				{
					result.SetError(new GenericJobValidationError(exception));
				}
			}

			return jobList.Select(job => results[job]).ToList().AsReadOnly();
		}

		private ValidationContext BuildContext(IReadOnlyCollection<Job> jobs)
		{
			var resourceIds = jobs.SelectMany(GetResourceNodes).Select(node => node.ResourceId).Distinct().ToList();
			var resources = planApi.Resources.Read(resourceIds).ToDictionary(resource => resource.Id);

			var coreResourceIds = resources.Values.Select(resource => resource.CoreResourceId).Where(id => id != Guid.Empty).Distinct().ToList();
			var coreResources = planApi.CoreHelpers.ResourceManagerHelper
				.GetResources(coreResourceIds, id => Skyline.DataMiner.Net.Messages.ResourceExposers.ID.Equal(id))
				.ToDictionary(resource => resource.ID);

			var reservationsByJob = GetReservationsByJob(jobs);

			var liveIsInstalled = liveApi.IsInstalled();
			var virtualSignalGroupIds = resources.Values
				.SelectMany(resource => new[] { resource.VirtualSignalGroupInputId, resource.VirtualSignalGroupOutputId })
				.Where(id => id != Guid.Empty)
				.Distinct()
				.ToList();
			var virtualSignalGroupIdsFound = liveIsInstalled
				? new HashSet<Guid>(liveApi.VirtualSignalGroups.Read(virtualSignalGroupIds).Keys)
				: new HashSet<Guid>();

			return new ValidationContext(resources, coreResources, reservationsByJob, virtualSignalGroupIdsFound, liveIsInstalled, new ReferenceDefinitionCache(planApi));
		}

		/// <summary>Retrieves the reservation of each job. A job is linked to at most one reservation, which holds the job ID in the 'Job ID' property.</summary>
		private IReadOnlyDictionary<Job, ReservationInstance> GetReservationsByJob(IReadOnlyCollection<Job> jobs)
		{
			var jobIds = jobs.Select(job => job.Id).Distinct().ToList();

			FilterElement<ReservationInstance> Filter(Guid id) => ReservationInstanceExposers.Properties.StringField(CoreJobHandler.JobIdPropertyName).Equal(Convert.ToString(id));

			var reservationsByJobId = planApi.CoreHelpers.ResourceManagerHelper
				.GetReservationInstances(jobIds, Filter)
				.Select(reservation => (Reservation: reservation, JobId: GetJobId(reservation)))
				.Where(item => item.JobId != Guid.Empty)
				.GroupBy(item => item.JobId)
				.ToDictionary(group => group.Key, group => group.First().Reservation);

			var result = new Dictionary<Job, ReservationInstance>();
			foreach (var job in jobs)
			{
				if (reservationsByJobId.TryGetValue(job.Id, out var reservation))
				{
					result[job] = reservation;
				}
			}

			return result;
		}

		private static Guid GetJobId(ReservationInstance reservation)
		{
			if (reservation.Properties?.Dictionary == null || !reservation.Properties.Dictionary.TryGetValue(CoreJobHandler.JobIdPropertyName, out var value))
			{
				return Guid.Empty;
			}

			return Guid.TryParse(Convert.ToString(value), out var jobId) ? jobId : Guid.Empty;
		}

		private void Validate(Job job, JobValidationResult result, ValidationContext context)
		{
			try
			{
				var resources = ValidateResources(job, result, context);
				context.ReservationsByJob.TryGetValue(job, out var reservation);

				ValidateReservation(reservation, result);
				ValidateVirtualSignalGroups(resources, result, context);
				ValidateReferences(job, reservation, result, context);
			}
			catch (Exception exception)
			{
				result.SetError(new GenericJobValidationError(exception));
			}
		}

		private static IReadOnlyCollection<Resource> ValidateResources(Job job, JobValidationResult result, ValidationContext context)
		{
			var resources = new Dictionary<Guid, Resource>();
			foreach (var node in GetResourceNodes(job))
			{
				if (!context.Resources.TryGetValue(node.ResourceId, out var resource))
				{
					result.SetError(new DomResourceNotFoundJobValidationError(node.ResourceId, node.Id));
					continue;
				}

				resources[resource.Id] = resource;
				if (resource.State != ResourceState.Complete)
				{
					result.SetError(new DomResourceInvalidJobValidationError($"Resource '{resource.Name}' is not in complete state"));
				}

				if (resource.OriginalInstance?.Errors.Count > 0)
				{
					result.SetError(new DomResourceInvalidJobValidationError($"Resource '{resource.Name}' has active errors"));
				}

				if (resource.CoreResourceId == Guid.Empty)
				{
					continue;
				}

				if (!context.CoreResources.TryGetValue(resource.CoreResourceId, out var coreResource))
				{
					result.SetError(new CoreResourceNotFoundJobValidationError(resource.CoreResourceId, resource.Name));
				}
				else if (coreResource.Mode != ResourceMode.Available)
				{
					result.SetError(new CoreResourceUnavailableJobValidationError(coreResource.Name, Convert.ToString(coreResource.Mode)));
				}
			}

			return resources.Values.ToList().AsReadOnly();
		}

		private static void ValidateReservation(ReservationInstance reservation, JobValidationResult result)
		{
			if (reservation == null || !reservation.IsQuarantined)
			{
				return;
			}

			result.SetError(new QuarantinedReservationJobValidationError(ComposeQuarantineMessage(reservation, result)));
		}

		private static void ValidateVirtualSignalGroups(IEnumerable<Resource> resources, JobValidationResult result, ValidationContext context)
		{
			if (!context.LiveIsInstalled)
			{
				return;
			}

			foreach (var resource in resources)
			{
				if (resource.VirtualSignalGroupInputId != Guid.Empty && !context.VirtualSignalGroupIds.Contains(resource.VirtualSignalGroupInputId))
				{
					result.SetError(new VirtualSignalGroupNotFoundJobValidationError("input", resource.VirtualSignalGroupInputId, resource.Name));
				}

				if (resource.VirtualSignalGroupOutputId != Guid.Empty && !context.VirtualSignalGroupIds.Contains(resource.VirtualSignalGroupOutputId))
				{
					result.SetError(new VirtualSignalGroupNotFoundJobValidationError("output", resource.VirtualSignalGroupOutputId, resource.Name));
				}
			}
		}

		private void ValidateReferences(Job job, ReservationInstance reservation, JobValidationResult result, ValidationContext context)
		{
			if (job.State == JobState.Draft || job.State == JobState.Tentative)
			{
				return;
			}

			var resolver = new JobReferenceResolver(planApi, job, context.ReferenceDefinitions);
			var resolution = new JobReferenceValidator(resolver, context.ReferenceDefinitions).Resolve(job);
			if (!resolution.IsValid)
			{
				result.SetError(new UnresolvedReferencesJobValidationError(ComposeUnresolvedReferencesMessage(resolution.UnresolvedReferences)));
			}

			ValidateReservationRequirementsStillMatch(job, reservation, resolution, result);
			ValidateLiveEventsStillMatch(job, resolver, context.ReferenceDefinitions, result, context.LiveIsInstalled);
		}

		private static void ValidateReservationRequirementsStillMatch(Job job, ReservationInstance reservation, JobReferenceResolution resolution, JobValidationResult result)
		{
			if (reservation == null)
			{
				return;
			}

			var usagesByNodeId = (reservation.ResourcesInReservationInstance ?? new List<ResourceUsageDefinition>())
				.OfType<ServiceResourceUsageDefinition>()
				.GroupBy(usage => usage.ServiceDefinitionNodeID)
				.ToDictionary(group => group.Key, group => group.Last());
			var mismatches = new List<string>();

			foreach (var node in GetResourceNodes(job))
			{
				if (!node.CoreReservationNodeId.HasValue || !usagesByNodeId.TryGetValue(node.CoreReservationNodeId.Value, out var usage))
				{
					continue;
				}

				foreach (var capability in node.OrchestrationSettings.Capabilities.Where(setting => setting.HasReference))
				{
					if (TryGetResolvedValue(resolution, node.Id, capability, out var resolvedValue))
					{
						var booked = usage.RequiredCapabilities?.FirstOrDefault(required => required.CapabilityProfileID == capability.Id)?.RequiredDiscreet;

						// A capability is booked on the display value, since its options have no separate display name.
						var resolved = resolvedValue.DisplayValue;
						if (booked != null && !String.Equals(booked, resolved, StringComparison.Ordinal))
						{
							mismatches.Add($"Capability '{capability.Id}' of {GetNodeDisplayName(node)} resolves to '{resolved}' but the reservation was booked with '{booked}'");
						}
					}
				}

				foreach (var capacity in node.OrchestrationSettings.Capacities.OfType<NumberCapacitySetting>().Where(setting => setting.HasReference))
				{
					if (TryGetResolvedValue(resolution, node.Id, capacity, out var resolvedValue) && ResolvedValueConverter.TryGetNumber(resolvedValue, out var resolved))
					{
						var booked = usage.RequiredCapacities?.FirstOrDefault(required => required.CapacityProfileID == capacity.Id);
						if (booked != null && booked.DecimalQuantity != resolved)
						{
							mismatches.Add($"Capacity '{capacity.Id}' of {GetNodeDisplayName(node)} resolves to '{FormatValue(resolvedValue.GetRawValue())}' but the reservation was booked with '{booked.DecimalQuantity.ToString(CultureInfo.InvariantCulture)}'");
						}
					}
				}
			}

			if (mismatches.Count > 0)
			{
				result.SetError(new ReservationRequirementsMismatchJobValidationError(ComposeReservationMismatchMessage(mismatches)));
			}
		}

		private void ValidateLiveEventsStillMatch(Job job, JobReferenceResolver resolver, ReferenceDefinitionCache definitions, JobValidationResult result, bool liveIsInstalled)
		{
			if (!liveIsInstalled || job.OrchestrationSettings?.OrchestrationEvents == null)
			{
				return;
			}

			var scheduledConfiguration = liveApi.Orchestration.GetOrCreateNewOrchestrationJobConfiguration(job.Id.ToString());
			if (scheduledConfiguration?.OrchestrationEvents == null || scheduledConfiguration.OrchestrationEvents.Count == 0)
			{
				return;
			}

			var mismatches = new List<string>();
			foreach (var orchestrationEvent in job.OrchestrationSettings.OrchestrationEvents.Where(item => item.ExecutionDetails != null))
			{
				if (!TryMapEventType(orchestrationEvent.EventType, out var liveEventType))
				{
					continue;
				}

				var scheduledEvent = scheduledConfiguration.OrchestrationEvents.FirstOrDefault(item => item.EventType == liveEventType);
				if (scheduledEvent == null)
				{
					continue;
				}

				CompareArguments(orchestrationEvent.ExecutionDetails.ScriptParameters.Where(item => item.HasReference).Select(item => (item.Name, item.Reference)), LiveEnums.OrchestrationScriptArgumentType.Parameter, liveEventType, scheduledEvent.GlobalOrchestrationScriptArguments, resolver, mismatches);
				CompareArguments(orchestrationEvent.ExecutionDetails.ScriptElements.Where(item => item.HasReference).Select(item => (item.Name, item.Reference)), LiveEnums.OrchestrationScriptArgumentType.Element, liveEventType, scheduledEvent.GlobalOrchestrationScriptArguments, resolver, mismatches);
				CompareProfileSettings(orchestrationEvent.ExecutionDetails.Capabilities.Cast<Setting>().Concat(orchestrationEvent.ExecutionDetails.Capacities).Concat(orchestrationEvent.ExecutionDetails.Configurations), liveEventType, scheduledEvent.Profile, resolver, definitions, mismatches);
			}

			if (mismatches.Count > 0)
			{
				result.SetError(new LiveEventsMismatchJobValidationError(ComposeLiveEventMismatchMessage(mismatches)));
			}
		}

		private static void CompareArguments(IEnumerable<(string Name, DataReference Reference)> references, LiveEnums.OrchestrationScriptArgumentType type, LiveEnums.EventType eventType, IEnumerable<Live.OrchestrationScriptArgument> scheduledArguments, JobReferenceResolver resolver, ICollection<string> mismatches)
		{
			if (scheduledArguments == null)
			{
				return;
			}

			foreach (var reference in references)
			{
				if (!TryResolveReference(resolver, reference.Reference, out var value))
				{
					continue;
				}

				var scheduled = scheduledArguments.FirstOrDefault(argument => argument.Type == type && String.Equals(argument.Name, reference.Name, StringComparison.Ordinal));
				var expected = FormatValue(value.GetRawValue());
				if (scheduled != null && !String.Equals(scheduled.Value, expected, StringComparison.Ordinal))
				{
					mismatches.Add($"The '{reference.Name}' value of the '{eventType}' live event resolves to '{expected}' but was scheduled with '{scheduled.Value}'");
				}
			}
		}

		private static void CompareProfileSettings(IEnumerable<Setting> settings, LiveEnums.EventType eventType, Live.OrchestrationProfile scheduledProfile, JobReferenceResolver resolver, ReferenceDefinitionCache definitions, ICollection<string> mismatches)
		{
			if (scheduledProfile?.Values == null)
			{
				return;
			}

			foreach (var setting in settings.Where(item => item.HasReference))
			{
				// The live event was scheduled with the value as the target parameter takes it, so compare it the same way.
				if (!TryResolveReference(resolver, setting.Reference, out var value)
					|| !ResolvedValueConverter.TryConvert(value, definitions.GetParameterDefinition(setting.Id), out var converted))
				{
					continue;
				}

				var scheduled = scheduledProfile.Values.FirstOrDefault(item => String.Equals(item.Name, setting.Id.ToString(), StringComparison.Ordinal));
				var expected = FormatValue(converted.GetRawValue());
				var actual = scheduled == null ? null : FormatScheduledProfileValue(scheduled.Value);
				if (scheduled != null && !String.Equals(actual, expected, StringComparison.Ordinal))
				{
					mismatches.Add($"The profile parameter '{setting.Id}' of the '{eventType}' live event resolves to '{expected}' but was scheduled with '{actual}'");
				}
			}
		}

		private static bool TryResolveReference(JobReferenceResolver resolver, DataReference reference, out ResolvedValue value)
		{
			value = null;
			try
			{
				var resolved = resolver.ResolveValue(reference);
				if (resolved == null || !resolved.IsResolved)
				{
					return false;
				}

				value = resolved;
				return true;
			}
			catch (CircularReferenceException)
			{
				return false;
			}
		}

		private static bool TryGetResolvedValue(JobReferenceResolution resolution, string nodeId, Setting setting, out ResolvedValue value)
		{
			if (!resolution.ResolvedReferences.TryGetValue(nodeId, setting.Reference, out value) || !value.IsResolved)
			{
				value = null;
				return false;
			}

			return true;
		}

		private static IReadOnlyCollection<JobResourceNode> GetResourceNodes(Job job)
		{
			return job.NodeGraph.Nodes.OfType<JobResourceNode>().ToList().AsReadOnly();
		}

		private static string ComposeQuarantineMessage(ReservationInstance reservation, JobValidationResult result)
		{
			var resourceNames = new List<string>();
			var quarantinedIds = reservation.QuarantinedResources.Select(item => item.QuarantinedResourceUsage.GUID).ToList();
			foreach (var quarantined in reservation.QuarantinedResources)
			{
				var resourceName = String.Empty;
				foreach (var trigger in quarantined.QuarantineTriggers ?? [])
				{
					resourceName = trigger.UpdateTrigger?.NewResource?.Name ?? trigger.UpdateTrigger?.OldResource?.Name ?? resourceName;
				}

				var nodeId = quarantined.QuarantinedResourceUsage is ServiceResourceUsageDefinition usage
					? usage.ServiceDefinitionNodeID.ToString(CultureInfo.InvariantCulture)
					: String.Empty;
				result.AddQuarantinedNodeId(nodeId);
				if (quarantinedIds.Count(id => id == quarantined.QuarantinedResourceUsage.GUID) > 1 && !String.IsNullOrEmpty(nodeId))
				{
					resourceName += $" ({nodeId})";
				}

				resourceNames.Add(resourceName);
			}

			var message = $"Reservation contains quarantined resources: {String.Join(", ", resourceNames.Distinct())}. Consider swapping the overbooked resources.";
			return message.Length <= MaxErrorMessageLength ? message : $"Reservation contains {reservation.QuarantinedResources.Count} quarantined resources. Consider swapping the overbooked resources.";
		}

		private static string ComposeUnresolvedReferencesMessage(IReadOnlyCollection<(DataReference Reference, string Reason)> references)
		{
			var message = $"Unresolved references: {String.Join("; ", references.Select(x => $"{x.Reference} {x.Reason}"))}.";
			return message.Length <= MaxErrorMessageLength ? message : $"{references.Count} references cannot be resolved. Please verify the job configuration.";
		}

		private static string ComposeReservationMismatchMessage(IReadOnlyCollection<string> mismatches)
		{
			var message = $"Resolving the references resulted in different capabilities/capacities than the ones on the reservation. The selected resources may be invalid: {String.Join("; ", mismatches)}.";
			return message.Length <= MaxErrorMessageLength ? message : $"Resolving the references resulted in {mismatches.Count} capability/capacity value(s) that do not match the reservation. The selected resources may be invalid.";
		}

		private static string ComposeLiveEventMismatchMessage(IReadOnlyCollection<string> mismatches)
		{
			var message = $"Resolving the references resulted in different values than the ones used by the scheduled live events: {String.Join("; ", mismatches)}.";
			return message.Length <= MaxErrorMessageLength ? message : $"Resolving the references resulted in {mismatches.Count} value(s) that do not match the scheduled live events.";
		}

		private static bool TryMapEventType(OrchestrationEventType eventType, out LiveEnums.EventType liveEventType)
		{
			switch (eventType)
			{
				case OrchestrationEventType.PrerollStart: liveEventType = LiveEnums.EventType.PrerollStart; return true;
				case OrchestrationEventType.PrerollStop: liveEventType = LiveEnums.EventType.PrerollStop; return true;
				case OrchestrationEventType.PostrollStart: liveEventType = LiveEnums.EventType.PostrollStart; return true;
				case OrchestrationEventType.PostrollStop: liveEventType = LiveEnums.EventType.PostrollStop; return true;
				default: liveEventType = LiveEnums.EventType.Other; return false;
			}
		}

		private static string FormatScheduledProfileValue(Skyline.DataMiner.Net.Profiles.ParameterValue value)
		{
			if (value == null)
			{
				return String.Empty;
			}

			switch (value.Type)
			{
				case Skyline.DataMiner.Net.Profiles.ParameterValue.ValueType.Double: return FormatValue(value.DoubleValue);
				case Skyline.DataMiner.Net.Profiles.ParameterValue.ValueType.Range: return FormatValue(value.RangeEnd);
				default: return value.StringValue ?? String.Empty;
			}
		}

		private static string GetNodeDisplayName(JobNode node)
		{
			return String.IsNullOrWhiteSpace(node.Alias) ? $"node {node.Id}" : $"node '{node.Alias.Trim()}'";
		}

		private static string FormatValue(object value)
		{
			return Convert.ToString(value, CultureInfo.InvariantCulture);
		}

		private sealed class ValidationContext
		{
			public ValidationContext(
				IReadOnlyDictionary<Guid, Resource> resources,
				IReadOnlyDictionary<Guid, CoreResource> coreResources,
				IReadOnlyDictionary<Job, ReservationInstance> reservationsByJob,
				ISet<Guid> virtualSignalGroupIds,
				bool liveIsInstalled,
				ReferenceDefinitionCache referenceDefinitions)
			{
				Resources = resources;
				CoreResources = coreResources;
				ReservationsByJob = reservationsByJob;
				VirtualSignalGroupIds = virtualSignalGroupIds;
				LiveIsInstalled = liveIsInstalled;
				ReferenceDefinitions = referenceDefinitions;
			}

			public IReadOnlyDictionary<Guid, Resource> Resources { get; }
			public IReadOnlyDictionary<Guid, CoreResource> CoreResources { get; }
			public IReadOnlyDictionary<Job, ReservationInstance> ReservationsByJob { get; }
			public ISet<Guid> VirtualSignalGroupIds { get; }
			public bool LiveIsInstalled { get; }
			public ReferenceDefinitionCache ReferenceDefinitions { get; }
		}
	}
}