namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.ResourceManager.Objects;
	using Skyline.DataMiner.Net.SRM.Capabilities;
	using Skyline.DataMiner.Net.SRM.Capacities;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.ActivityHelper;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;

	using CoreResource = Net.Messages.Resource;

	/// <summary>
	/// Translates a DevPack eligibility request into a core <see cref="EligibleResourceContext"/>, executes it and maps the
	/// eligible core resources back to their Resource Studio counterparts.
	/// </summary>
	internal class CoreEligibleResourceHandler
	{
		private readonly MediaOpsPlanApi planApi;

		private CoreEligibleResourceHandler(MediaOpsPlanApi planApi)
		{
			this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
		}

		public static EligibleResourcesResult GetEligibleResources(MediaOpsPlanApi planApi, EligibleResourcesContext context)
		{
			if (context == null)
			{
				throw new ArgumentNullException(nameof(context));
			}

			return GetEligibleResources(planApi, new[] { context }).Single();
		}

		public static IReadOnlyList<EligibleResourcesResult> GetEligibleResources(MediaOpsPlanApi planApi, IReadOnlyCollection<EligibleResourcesContext> contexts)
		{
			if (contexts == null)
			{
				throw new ArgumentNullException(nameof(contexts));
			}

			if (contexts.Any(x => x == null))
			{
				throw new ArgumentException("The contexts cannot contain null values.", nameof(contexts));
			}

			if (contexts.Select(x => x.Id).Distinct().Count() != contexts.Count)
			{
				throw new ArgumentException("Every context can only be requested once.", nameof(contexts));
			}

			var handler = new CoreEligibleResourceHandler(planApi);

			return ActivityHelper.Track(
				nameof(CoreEligibleResourceHandler),
				nameof(GetEligibleResources),
				act => handler.GetEligibleResources(contexts));
		}

		private static IEnumerable<ResourceCapabilityUsage> BuildCapabilities(IReadOnlyCollection<CapabilitySetting> capabilitySettings)
		{
			if (capabilitySettings == null)
			{
				yield break;
			}

			foreach (var capability in capabilitySettings)
			{
				// Capabilities are discrete in this model, so the value is always applied as the required discrete value.
				if (capability == null || !capability.HasValue)
				{
					continue;
				}

				yield return new ResourceCapabilityUsage
				{
					CapabilityProfileID = capability.Id,
					RequiredDiscreet = capability.Value,
				};
			}
		}

		private static IEnumerable<MultiResourceCapacityUsage> BuildCapacities(IReadOnlyCollection<CapacitySetting> capacitySettings)
		{
			if (capacitySettings == null)
			{
				yield break;
			}

			foreach (var capacity in capacitySettings)
			{
				switch (capacity)
				{
					case NumberCapacitySetting numberCapacity when numberCapacity.HasValue:
						yield return new MultiResourceCapacityUsage
						{
							CapacityProfileID = numberCapacity.Id,
							DecimalQuantity = numberCapacity.Value.Value,
						};

						break;

					case RangeCapacitySetting rangeCapacity when rangeCapacity.HasValue:
						yield return new MultiResourceCapacityUsage
						{
							CapacityProfileID = rangeCapacity.Id,
							RangeStart = rangeCapacity.MinValue.Value,
							DecimalQuantity = rangeCapacity.MaxValue.Value - rangeCapacity.MinValue.Value,
						};

						break;
				}
			}
		}

		private IReadOnlyList<EligibleResourcesResult> GetEligibleResources(IReadOnlyCollection<EligibleResourcesContext> contexts)
		{
			var nodesToIgnore = ResolveNodesToIgnore(contexts);

			var requests = new List<(EligibleResourcesContext Context, EligibleResourceContext CoreContext, Dictionary<Guid, Resource> FilteredResourcesByCoreId)>();
			var resultsByContextId = new Dictionary<Guid, EligibleResourcesResult>();
			foreach (var context in contexts)
			{
				var coreContext = new EligibleResourceContext(context.Id)
				{
					TimeRange = new Net.Time.TimeRangeUtc(context.Start.UtcDateTime, context.End.UtcDateTime),
					RequiredCapabilities = BuildCapabilities(context.CapabilitySettings).ToList(),
					RequiredCapacities = BuildCapacities(context.CapacitySettings).ToList(),
					CalculateAllCapacities = context.IncludeAllCapacityUsages,
					CalculateAllCapabilities = context.IncludeAllCapabilityUsages,
					FillLinkerTableEntries = false,
				};

				if (nodesToIgnore.TryGetValue(context.Id, out var nodeToIgnore))
				{
					coreContext.ReservationIdToIgnore = new Net.ReservationInstanceID(nodeToIgnore.ReservationId);
					coreContext.NodeIdToIgnore = nodeToIgnore.CoreNodeId;
				}

				var filter = context.Filter;

				Dictionary<Guid, Resource> filteredResourcesByCoreId = null;
				if (filter != null && !filter.isEmpty())
				{
					// The DevPack filter is defined on the Resource Studio resources, so it is resolved first and the matching
					// core resources are then used to restrict the eligibility request.
					filteredResourcesByCoreId = planApi.Resources.Read(filter)
						.Where(x => x.CoreResourceId != Guid.Empty)
						.GroupBy(x => x.CoreResourceId)
						.ToDictionary(x => x.Key, x => x.First());

					if (filteredResourcesByCoreId.Count == 0)
					{
						// No Resource Studio resource matches the filter, so no resource can be eligible.
						resultsByContextId[context.Id] = new EligibleResourcesResult(context.Id, Array.Empty<EligibleResource>());
						continue;
					}

					coreContext.ResourceFilter = new ORFilterElement<CoreResource>(filteredResourcesByCoreId.Keys.Select(x => Net.Messages.ResourceExposers.ID.Equal(x)).ToArray());
				}

				requests.Add((context, coreContext, filteredResourcesByCoreId));
			}

			if (requests.Count > 0)
			{
				var coreResults = planApi.CoreHelpers.ResourceManagerHelper.GetEligibleResourcesForContexts(requests.Select(x => x.CoreContext).ToList());
				var capabilityIdResolver = new CapabilityIdResolver(planApi);

				foreach (var request in requests)
				{
					if (!coreResults.TryGetValue(request.Context.Id, out var coreResult))
					{
						throw new InvalidOperationException($"No eligibility result was returned for context {request.Context.Id}.");
					}

					resultsByContextId[request.Context.Id] = MapResult(request.Context.Id, coreResult, request.FilteredResourcesByCoreId, capabilityIdResolver);
				}
			}

			return contexts.Select(x => resultsByContextId[x.Id]).ToList().AsReadOnly();
		}

		private EligibleResourcesResult MapResult(Guid contextId, EligibleResourceResult coreResult, IReadOnlyDictionary<Guid, Resource> filteredResourcesByCoreId, CapabilityIdResolver capabilityIdResolver)
		{
			var resources = MapToResourceStudioResources(coreResult.EligibleResources, filteredResourcesByCoreId);
			var usageByCoreResourceId = (coreResult.UsageDetails ?? new List<ResourceUsageDetails>()).ToDictionary(usage => usage.ResourceId);

			var eligibleResources = resources.Select(resource =>
			{
				if (!usageByCoreResourceId.TryGetValue(resource.CoreResourceId, out var usageDetails))
				{
					throw new InvalidOperationException($"No usage details were returned for eligible resource {resource.Id}.");
				}

				return new EligibleResource(resource, MapUsage(resource, usageDetails, capabilityIdResolver));
			});

			return new EligibleResourcesResult(contextId, eligibleResources);
		}

		private IReadOnlyDictionary<Guid, (Guid ReservationId, int CoreNodeId)> ResolveNodesToIgnore(IReadOnlyCollection<EligibleResourcesContext> contexts)
		{
			var result = new Dictionary<Guid, (Guid ReservationId, int CoreNodeId)>();

			var contextsWithNodeToIgnore = contexts.Where(x => x.HasNodeToIgnore).ToList();
			if (contextsWithNodeToIgnore.Count == 0)
			{
				return result;
			}

			if (contextsWithNodeToIgnore.Any(x => x.JobIdToIgnore == Guid.Empty || String.IsNullOrEmpty(x.NodeIdToIgnore)))
			{
				throw new ArgumentException($"{nameof(EligibleResourcesContext.JobIdToIgnore)} and {nameof(EligibleResourcesContext.NodeIdToIgnore)} must be set together.", nameof(contexts));
			}

			var jobIds = contextsWithNodeToIgnore.Select(x => x.JobIdToIgnore).Distinct().ToList();
			var jobsById = planApi.Jobs.Read(jobIds).ToDictionary(x => x.Id);
			var reservationsByJobId = JobBookingReader.GetReservationsByJobId(planApi, jobIds);

			foreach (var context in contextsWithNodeToIgnore)
			{
				if (!jobsById.TryGetValue(context.JobIdToIgnore, out var job) || !reservationsByJobId.TryGetValue(context.JobIdToIgnore, out var reservation))
				{
					throw new ArgumentException($"Job {context.JobIdToIgnore} has no booking.", nameof(contexts));
				}

				var node = job.NodeGraph.Nodes.FirstOrDefault(x => String.Equals(x.Id, context.NodeIdToIgnore, StringComparison.OrdinalIgnoreCase));
				var coreNodeId = node?.CoreReservationNodeId;
				if (!coreNodeId.HasValue || !reservation.ResourcesInReservationInstance.OfType<ServiceResourceUsageDefinition>().Any(x => x.ServiceDefinitionNodeID == coreNodeId.Value))
				{
					throw new ArgumentException($"Node {context.NodeIdToIgnore} of job {context.JobIdToIgnore} is not booked.", nameof(contexts));
				}

				result[context.Id] = (reservation.ID, coreNodeId.Value);
			}

			return result;
		}

		private static ResourceUsage MapUsage(Resource resource, ResourceUsageDetails usageDetails, CapabilityIdResolver capabilityIdResolver)
		{
			var capacitySettingsById = resource.Capacities.ToDictionary(capacity => capacity.Id);
			var capacityUsages = (usageDetails.CapacityUsageDetails ?? new List<CapacityUsageDetails>())
				.Where(usage => capacitySettingsById.ContainsKey(usage.CapacityParameterId))
				.Select(usage => MapCapacityUsage(capacitySettingsById[usage.CapacityParameterId], usage))
				.ToList();

			var capabilityUsages = (usageDetails.CapabilityUsageDetails ?? new List<CapabilityUsageDetails>())
				.Select(usage => MapCapabilityUsage(resource, usage, capabilityIdResolver))
				.Where(usage => usage != null)
				.ToList();

			var remainingConcurrency = Math.Max(0, usageDetails.ConcurrencyLeft);
			return new ResourceUsage(Math.Max(0, resource.Concurrency - remainingConcurrency), remainingConcurrency, capacityUsages, capabilityUsages);
		}

		private static CapabilityUsage MapCapabilityUsage(Resource resource, CapabilityUsageDetails usageDetails, CapabilityIdResolver capabilityIdResolver)
		{
			if (!capabilityIdResolver.TryGetCapabilityId(resource, usageDetails.CapabilityParameterId, out var capabilityId))
			{
				return null;
			}

			var bookings = (usageDetails.BookedCapabilities ?? new List<BookedCapability>())
				.Where(booked => booked?.BookedTimeRange != null)
				.Select(booked => new CapabilityBooking(
					new DateTimeOffset(DateTime.SpecifyKind(booked.BookedTimeRange.Start, DateTimeKind.Utc)),
					new DateTimeOffset(DateTime.SpecifyKind(booked.BookedTimeRange.Stop, DateTimeKind.Utc)),
					booked.Value?.RequiredDiscreet ?? booked.Value?.RequiredString));

			return new CapabilityUsage(capabilityId, usageDetails.HasExistingBookings.HasValue, bookings);
		}

		private static CapacityUsage MapCapacityUsage(CapacitySetting capacitySetting, CapacityUsageDetails usageDetails)
		{
			if (capacitySetting is NumberCapacitySetting numberCapacity)
			{
				var remainingCapacity = usageDetails.CapacityLeft;
				return new NumberCapacityUsage(capacitySetting.Id, Math.Max(0, numberCapacity.Value.GetValueOrDefault() - remainingCapacity), remainingCapacity);
			}

			var rangeCapacity = (RangeCapacitySetting)capacitySetting;
			var minimum = rangeCapacity.MinValue.GetValueOrDefault();
			var maximum = rangeCapacity.MaxValue.GetValueOrDefault();
			var remainingRanges = (usageDetails.RangesLeft ?? [])
				.Select(range => new CapacityRange(range.RangeStart, range.RangeEnd))
				.OrderBy(range => range.Start)
				.ToList();

			return new RangeCapacityUsage(capacitySetting.Id, BuildConsumedRanges(minimum, maximum, remainingRanges), remainingRanges);
		}

		private static IReadOnlyCollection<CapacityRange> BuildConsumedRanges(decimal minimum, decimal maximum, IReadOnlyCollection<CapacityRange> remainingRanges)
		{
			var consumed = new List<CapacityRange>();
			var cursor = minimum;
			foreach (var range in remainingRanges)
			{
				if (range.Start > cursor)
				{
					consumed.Add(new CapacityRange(cursor, range.Start));
				}

				cursor = Math.Max(cursor, range.End);
			}

			if (cursor < maximum)
			{
				consumed.Add(new CapacityRange(cursor, maximum));
			}

			return consumed;
		}

		private ICollection<Resource> MapToResourceStudioResources(IReadOnlyCollection<CoreResource> coreResources, IReadOnlyDictionary<Guid, Resource> knownResourcesByCoreId)
		{
			if (coreResources == null || coreResources.Count == 0)
			{
				return Array.Empty<Resource>();
			}

			var coreResourceIds = coreResources.Select(x => x.ID).Where(x => x != Guid.Empty).Distinct().ToList();
			if (coreResourceIds.Count == 0)
			{
				return Array.Empty<Resource>();
			}

			if (knownResourcesByCoreId != null)
			{
				// The Resource Studio resources were already read to build the core resource filter, so they can be reused here.
				return coreResourceIds
					.Where(knownResourcesByCoreId.ContainsKey)
					.Select(x => knownResourcesByCoreId[x])
					.ToList();
			}

			FilterElement<DomInstance> Filter(Guid coreResourceId) =>
				DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resource.Id)
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInternalProperties.Resource_Id).Equal(coreResourceId));

			// Core resources without a Resource Studio counterpart cannot be represented as a DevPack resource and are skipped.
			var domResources = planApi.DomHelpers.SlcResourceStudioHelper.GetResources(coreResourceIds, Filter);

			return Resource.InstantiateResources(planApi, domResources).ToList();
		}

		// Core reports a time-dependent capability on its linked parameter, which is mapped back to the capability it belongs to.
		private sealed class CapabilityIdResolver
		{
			private readonly MediaOpsPlanApi planApi;
			private readonly HashSet<Guid> readCapabilityIds = new HashSet<Guid>();
			private readonly Dictionary<Guid, Guid> capabilityIdByLinkedId = new Dictionary<Guid, Guid>();

			public CapabilityIdResolver(MediaOpsPlanApi planApi)
			{
				this.planApi = planApi;
			}

			public bool TryGetCapabilityId(Resource resource, Guid coreParameterId, out Guid capabilityId)
			{
				var resourceCapabilityIds = resource.Capabilities.Select(x => x.Id).ToList();
				if (resourceCapabilityIds.Contains(coreParameterId))
				{
					capabilityId = coreParameterId;
					return true;
				}

				var unreadIds = resourceCapabilityIds.Where(x => !readCapabilityIds.Contains(x)).ToList();
				if (unreadIds.Count > 0)
				{
					readCapabilityIds.UnionWith(unreadIds);
					foreach (var capability in planApi.Capabilities.Read(unreadIds).Where(x => x.IsTimeDependent && x.LinkedTimeDependentCapabilityId != Guid.Empty))
					{
						capabilityIdByLinkedId[capability.LinkedTimeDependentCapabilityId] = capability.Id;
					}
				}

				return capabilityIdByLinkedId.TryGetValue(coreParameterId, out capabilityId);
			}
		}
	}
}
