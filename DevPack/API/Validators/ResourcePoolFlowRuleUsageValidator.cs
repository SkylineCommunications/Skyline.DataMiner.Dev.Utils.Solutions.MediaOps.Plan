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

			var requestedPoolIds = resourcePools.Select(x => x.Id).ToHashSet();

			var referencingRuleSets = planApi.DomHelpers.SlcResourceStudioHelper
				.GetFlowRulesByTarget(requestedPoolIds)
				.Where(x => x.Flowrulesinfo.OwnerId != Guid.Empty)
				.ToList();

			if (referencingRuleSets.Count == 0)
			{
				return;
			}

			// Rule sets left behind by a pool that no longer exists must not block the delete. Requested pools exist.
			var existingOwnerIds = planApi.DomHelpers.SlcResourceStudioHelper
				.GetResourcePools(referencingRuleSets.Select(x => x.Flowrulesinfo.OwnerId).Where(x => !requestedPoolIds.Contains(x)).Distinct().ToList())
				.Select(x => x.ID.Id)
				.Concat(requestedPoolIds)
				.ToHashSet();

			var referencingOwnerIdsByPoolId = requestedPoolIds.ToDictionary(
				x => x,
				x => referencingRuleSets
					.Where(set => existingOwnerIds.Contains(set.Flowrulesinfo.OwnerId) && set.Flowrule.Any(rule => rule.TargetId == x))
					.Select(set => set.Flowrulesinfo.OwnerId)
					.Where(ownerId => ownerId != x)
					.ToHashSet());

			// References from pools that are deleted in the same operation do not block the delete, but only as long as
			// those pools can be deleted themselves: a rejected pool keeps its rules, so its references block again.
			var deletablePoolIds = new HashSet<Guid>(requestedPoolIds);
			bool changed;
			do
			{
				var blockedPoolIds = deletablePoolIds
					.Where(id => referencingOwnerIdsByPoolId[id].Any(ownerId => !deletablePoolIds.Contains(ownerId)))
					.ToList();

				deletablePoolIds.ExceptWith(blockedPoolIds);
				changed = blockedPoolIds.Count > 0;
			}
			while (changed);

			foreach (var resourcePool in resourcePools.Where(x => !deletablePoolIds.Contains(x.Id)))
			{
				var referencingPoolIds = referencingOwnerIdsByPoolId[resourcePool.Id]
					.Where(x => !deletablePoolIds.Contains(x))
					.ToArray();

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
