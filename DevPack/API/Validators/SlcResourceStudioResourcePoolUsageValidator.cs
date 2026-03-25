namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;

	internal class SlcResourceStudioResourcePoolUsageValidator : ApiObjectValidator
	{
		private readonly HashSet<Guid> resourcePoolIdsToValidate;
		private readonly IReadOnlyCollection<ResourcePool> resourcePoolsToValidate;
		private readonly MediaOpsPlanApi planApi;

		private SlcResourceStudioResourcePoolUsageValidator(MediaOpsPlanApi planApi, IReadOnlyCollection<ResourcePool> resourcePoolsToValidate)
		{
			this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
			this.resourcePoolsToValidate = resourcePoolsToValidate ?? throw new ArgumentNullException(nameof(resourcePoolsToValidate));
			resourcePoolIdsToValidate = resourcePoolsToValidate.Select(x => x.Id).ToHashSet();
		}

		public static ApiObjectValidator Validate(MediaOpsPlanApi planApi, ICollection<ResourcePool> resourcePoolsToValidate)
		{
			if (resourcePoolsToValidate == null)
			{
				throw new ArgumentNullException(nameof(resourcePoolsToValidate));
			}

			var validator = new SlcResourceStudioResourcePoolUsageValidator(planApi, resourcePoolsToValidate.ToList());
			validator.Validate();

			return validator;
		}

		private void Validate()
		{
			if (!resourcePoolIdsToValidate.Any())
			{
				return;
			}

			ValdiateLinkedPoolUsage((resourcePool, ids) =>
			{
				return new ResourcePoolInuseByLinkedPoolsError
				{
					Id = resourcePool.Id,
					ErrorMessage = $"Resource pool '{resourcePool.Name}' is in use by {ids.Count} linked resource pool(s).",
					LinkedResourcePoolIds = ids.ToArray(),
				};
			});
		}

		private void ValdiateLinkedPoolUsage(Func<ResourcePool, ICollection<Guid>, MediaOpsErrorData> createLinkedPoolError)
		{
			var linkedPoolsReferencingResourcePools = GetLinkedPoolsReferencingResourcePools();
			foreach (var resourcePool in resourcePoolsToValidate)
			{
				if (!linkedPoolsReferencingResourcePools.TryGetValue(resourcePool.Id, out var linkedPoolIds))
				{
					continue;
				}

				ReportError(resourcePool.Id, createLinkedPoolError(resourcePool, linkedPoolIds));
			}
		}

		private Dictionary<Guid, ICollection<Guid>> GetLinkedPoolsReferencingResourcePools()
		{
			var result = new Dictionary<Guid, ICollection<Guid>>();

			var poolFilter = DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resourcepool.Id)
				.AND(new ORFilterElement<DomInstance>(resourcePoolIdsToValidate.Select(x => DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourcePoolLinks.LinkedResourcePool).Equal(x))
				.ToArray()));

			var poolInstances = planApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(poolFilter);

			foreach (var pool in resourcePoolsToValidate)
			{
				HashSet<Guid> poolIds = new HashSet<Guid>();
				foreach (var poolInstance in poolInstances)
				{
					if (poolInstance.ResourcePoolLinks == null)
					{
						continue;
					}

					if (poolInstance.ResourcePoolLinks.Any(x => x.LinkedResourcePool == pool.Id))
					{
						poolIds.Add(poolInstance.ID.Id);
					}
				}

				if (poolIds.Any())
				{
					result.Add(pool.Id, poolIds);
				}
			}

			return result;
		}
	}
}
