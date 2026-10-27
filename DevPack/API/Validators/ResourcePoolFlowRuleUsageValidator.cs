namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	// Only used when deleting: deprecating a resource pool keeps the overrides that reference it, so it can be restored.
	internal sealed class ResourcePoolFlowRuleUsageValidator : ApiObjectValidator
	{
		private readonly MediaOpsPlanApi planApi;
		private readonly IReadOnlyCollection<ResourcePool> resourcePools;

		private ResourcePoolFlowRuleUsageValidator(MediaOpsPlanApi planApi, IReadOnlyCollection<ResourcePool> resourcePools)
		{
			this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
			this.resourcePools = resourcePools ?? throw new ArgumentNullException(nameof(resourcePools));
		}

		public static ApiObjectValidator Validate(MediaOpsPlanApi planApi, ICollection<ResourcePool> resourcePools)
		{
			if (resourcePools == null)
			{
				throw new ArgumentNullException(nameof(resourcePools));
			}

			var validator = new ResourcePoolFlowRuleUsageValidator(planApi, resourcePools.ToList());
			validator.Validate();

			return validator;
		}

		private void Validate()
		{
			if (resourcePools.Count == 0)
			{
				return;
			}

			var poolIdsToDelete = resourcePools.Select(x => x.Id).ToHashSet();

			// References from rule sets of pools that are deleted in the same operation do not block the delete.
			var referencingRuleSets = planApi.DomHelpers.SlcResourceStudioHelper
				.GetFlowRulesByTarget(poolIdsToDelete)
				.Where(x => x.Flowrulesinfo.OwnerId != Guid.Empty && !poolIdsToDelete.Contains(x.Flowrulesinfo.OwnerId))
				.ToList();

			if (referencingRuleSets.Count == 0)
			{
				return;
			}

			// Rule sets left behind by a pool that no longer exists must not block the delete either.
			var existingOwnerIds = planApi.DomHelpers.SlcResourceStudioHelper
				.GetResourcePools(referencingRuleSets.Select(x => x.Flowrulesinfo.OwnerId).Distinct().ToList())
				.Select(x => x.ID.Id)
				.ToHashSet();

			foreach (var resourcePool in resourcePools)
			{
				var referencingPoolIds = referencingRuleSets
					.Where(x => existingOwnerIds.Contains(x.Flowrulesinfo.OwnerId) && x.Flowrule.Any(rule => rule.TargetId == resourcePool.Id))
					.Select(x => x.Flowrulesinfo.OwnerId)
					.Distinct()
					.ToArray();

				if (referencingPoolIds.Length == 0)
				{
					continue;
				}

				ReportError(resourcePool.Id, new ResourcePoolInUseByFlowRulesError
				{
					Id = resourcePool.Id,
					ErrorMessage = $"Resource pool '{resourcePool.Name}' is referenced by the flow rules of {referencingPoolIds.Length} resource pool(s).",
					ReferencingOwnerIds = referencingPoolIds,
				});
			}
		}
	}
}
