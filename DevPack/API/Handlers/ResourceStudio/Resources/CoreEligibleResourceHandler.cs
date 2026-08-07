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

		public static ICollection<Resource> GetEligibleResources(
			MediaOpsPlanApi planApi,
			DateTimeOffset start,
			DateTimeOffset end,
			ICollection<CapabilitySetting> capabilitySettings,
			ICollection<CapacitySetting> capacitySettings,
			FilterElement<Resource> filter)
		{
			var handler = new CoreEligibleResourceHandler(planApi);

			return ActivityHelper.Track(
				nameof(CoreEligibleResourceHandler),
				nameof(GetEligibleResources),
				act => handler.GetEligibleResources(start, end, capabilitySettings, capacitySettings, filter));
		}

		private static IEnumerable<ResourceCapabilityUsage> BuildCapabilities(ICollection<CapabilitySetting> capabilitySettings)
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

		private static IEnumerable<MultiResourceCapacityUsage> BuildCapacities(ICollection<CapacitySetting> capacitySettings)
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

		private ICollection<Resource> GetEligibleResources(
			DateTimeOffset start,
			DateTimeOffset end,
			ICollection<CapabilitySetting> capabilitySettings,
			ICollection<CapacitySetting> capacitySettings,
			FilterElement<Resource> filter)
		{
			var context = new EligibleResourceContext(new Net.Time.TimeRangeUtc(start.UtcDateTime, end.UtcDateTime))
			{
				RequiredCapabilities = BuildCapabilities(capabilitySettings).ToList(),
				RequiredCapacities = BuildCapacities(capacitySettings).ToList(),
			};

			if (filter != null && !filter.isEmpty())
			{
				if (!TryBuildCoreResourceFilter(filter, out var coreFilter))
				{
					// No Resource Studio resource matches the filter, so no resource can be eligible.
					return Array.Empty<Resource>();
				}

				context.ResourceFilter = coreFilter;
			}

			var eligibleCoreResources = planApi.CoreHelpers.ResourceManagerHelper.GetEligibleResourcesForContext(context);

			return MapToResourceStudioResources(eligibleCoreResources);
		}

		private bool TryBuildCoreResourceFilter(FilterElement<Resource> filter, out FilterElement<CoreResource> coreFilter)
		{
			// The DevPack filter is defined on the Resource Studio resources, so it is resolved first and the matching
			// core resources are then used to restrict the eligibility request.
			var coreResourceIds = planApi.Resources.Read(filter)
				.Select(x => x.CoreResourceId)
				.Where(x => x != Guid.Empty)
				.Distinct()
				.ToList();

			if (coreResourceIds.Count == 0)
			{
				coreFilter = null;
				return false;
			}

			coreFilter = new ORFilterElement<CoreResource>(coreResourceIds.Select(x => Net.Messages.ResourceExposers.ID.Equal(x)).ToArray());
			return true;
		}

		private ICollection<Resource> MapToResourceStudioResources(IReadOnlyCollection<CoreResource> coreResources)
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

			FilterElement<DomInstance> Filter(Guid coreResourceId) =>
				DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resource.Id)
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInternalProperties.Resource_Id).Equal(coreResourceId));

			// Core resources without a Resource Studio counterpart cannot be represented as a DevPack resource and are skipped.
			var domResources = planApi.DomHelpers.SlcResourceStudioHelper.GetResources(coreResourceIds, Filter);

			return Resource.InstantiateResources(planApi, domResources).ToList();
		}
	}
}
