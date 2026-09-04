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

			var handler = new CoreEligibleResourceHandler(planApi);

			return ActivityHelper.Track(
				nameof(CoreEligibleResourceHandler),
				nameof(GetEligibleResources),
				act => handler.GetEligibleResources(context));
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

		private EligibleResourcesResult GetEligibleResources(EligibleResourcesContext context)
		{
			var coreContext = new EligibleResourceContext(new Net.Time.TimeRangeUtc(context.Start.UtcDateTime, context.End.UtcDateTime))
			{
				RequiredCapabilities = BuildCapabilities(context.CapabilitySettings).ToList(),
				RequiredCapacities = BuildCapacities(context.CapacitySettings).ToList(),
			};

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
					return new EligibleResourcesResult(Array.Empty<EligibleResource>());
				}

				coreContext.ResourceFilter = new ORFilterElement<CoreResource>(filteredResourcesByCoreId.Keys.Select(x => Net.Messages.ResourceExposers.ID.Equal(x)).ToArray());
			}

			if (context.JobIdToIgnore != Guid.Empty)
			{
				var reservation = planApi.CoreHelpers.ResourceManagerHelper
					.GetReservationInstances(
						new[] { context.JobIdToIgnore },
						jobId => ReservationInstanceExposers.Properties.StringField(CoreJobHandler.JobIdPropertyName).Equal(Convert.ToString(jobId)))
					.FirstOrDefault();

				if (reservation != null)
				{
					coreContext.ReservationIdToIgnore = new Net.ReservationInstanceID(reservation.ID);
				}
			}

			var coreResult = planApi.CoreHelpers.ResourceManagerHelper.GetEligibleResourcesForContext(coreContext);
			var resources = MapToResourceStudioResources(coreResult.EligibleResources, filteredResourcesByCoreId);
			var usageByCoreResourceId = coreResult.UsageDetails.ToDictionary(usage => usage.ResourceId);

			var eligibleResources = resources.Select(resource =>
			{
				if (!usageByCoreResourceId.TryGetValue(resource.CoreResourceId, out var usageDetails))
				{
					throw new InvalidOperationException($"No usage details were returned for eligible resource {resource.Id}.");
				}

				return new EligibleResource(resource, MapUsage(resource, usageDetails));
			});

			return new EligibleResourcesResult(eligibleResources);
		}

		private static ResourceUsage MapUsage(Resource resource, ResourceUsageDetails usageDetails)
		{
			var capacitySettingsById = resource.Capacities.ToDictionary(capacity => capacity.Id);
			var capacityUsages = usageDetails.CapacityUsageDetails
				.Where(usage => capacitySettingsById.ContainsKey(usage.CapacityParameterId))
				.Select(usage => MapCapacityUsage(capacitySettingsById[usage.CapacityParameterId], usage))
				.ToList();

			var remainingConcurrency = Math.Max(0, usageDetails.ConcurrencyLeft);
			return new ResourceUsage(Math.Max(0, resource.Concurrency - remainingConcurrency), remainingConcurrency, capacityUsages);
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
	}
}
