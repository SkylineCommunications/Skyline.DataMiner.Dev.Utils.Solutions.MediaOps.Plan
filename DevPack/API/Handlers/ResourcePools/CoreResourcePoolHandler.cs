namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core;

    using CoreResourcePool = Net.Messages.ResourcePool;
    using DomResourcePool = Storage.DOM.SlcResource_Studio.ResourcepoolInstance;

    internal class CoreResourcePoolHandler
    {
        private readonly MediaOpsPlanApi planApi;

        private readonly List<Guid> successfulIds = new List<Guid>();
        private readonly List<Guid> unsuccessfulIds = new List<Guid>();
        private readonly Dictionary<Guid, MediaOpsTraceData> traceDataPerItem = new Dictionary<Guid, MediaOpsTraceData>();

        private CoreResourcePoolHandler(MediaOpsPlanApi planApi)
        {
            this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
        }

        public static BulkOperationResult<Guid> CreateOrUpdate(MediaOpsPlanApi planApi, ICollection<DomResourcePool> domResourcePools)
        {
            var handler = new CoreResourcePoolHandler(planApi);
            handler.CreateOrUpdate(domResourcePools);

            var result = new BulkOperationResult<Guid>(handler.successfulIds, handler.unsuccessfulIds, handler.traceDataPerItem);
            result.ThrowOnFailure();

            return result;
        }

        public static bool TryCreateOrUpdate(MediaOpsPlanApi planApi, ICollection<DomResourcePool> domResourcePools, out BulkOperationResult<Guid> result)
        {
            var handler = new CoreResourcePoolHandler(planApi);
            handler.CreateOrUpdate(domResourcePools);

            result = new BulkOperationResult<Guid>(handler.successfulIds, handler.unsuccessfulIds, handler.traceDataPerItem);

            return !result.HasFailures;
        }

        public static BulkOperationResult<Guid> Delete(MediaOpsPlanApi planApi, ICollection<DomResourcePool> domResourcePools)
        {
            var handler = new CoreResourcePoolHandler(planApi);
            handler.Delete(domResourcePools);

            var result = new BulkOperationResult<Guid>(handler.successfulIds, handler.unsuccessfulIds, handler.traceDataPerItem);
            result.ThrowOnFailure();

            return result;
        }

        public static bool TryDelete(MediaOpsPlanApi planApi, ICollection<DomResourcePool> domResourcePools, out BulkOperationResult<Guid> result)
        {
            var handler = new CoreResourcePoolHandler(planApi);
            handler.Delete(domResourcePools);

            result = new BulkOperationResult<Guid>(handler.successfulIds, handler.unsuccessfulIds, handler.traceDataPerItem);

            return !result.HasFailures;
        }

        private void CreateOrUpdate(ICollection<DomResourcePool> domResourcePools)
        {
            if (domResourcePools == null)
            {
                throw new ArgumentNullException(nameof(domResourcePools));
            }

            if (domResourcePools.Count == 0)
            {
                return;
            }

            var resourcePoolMappingByDomId = ResourcePoolMapping.GetMappings(planApi, domResourcePools).ToDictionary(x => x.DomResourcePool.ID.Id);

            ValidateNames(resourcePoolMappingByDomId.Values.Where(x => x.NeedsNameValidation).Select(x => x.DomResourcePool).ToList());
            CreateOrUpdate(resourcePoolMappingByDomId.Where(x => !traceDataPerItem.Keys.Contains(x.Key)).Select(x => x.Value).ToList());
        }

        private void CreateOrUpdate(ICollection<ResourcePoolMapping> resourcePoolMappings)
        {
            if (resourcePoolMappings == null)
            {
                throw new ArgumentNullException(nameof(resourcePoolMappings));
            }

            if (resourcePoolMappings.Count == 0)
            {
                return;
            }

            var domPoolsById = new Dictionary<Guid, DomResourcePool>();
            var domIdByCoreId = new Dictionary<Guid, Guid>();

            var poolsToCreateOrUpdate = new List<CoreResourcePool>();
            foreach (var mapping in resourcePoolMappings)
            {
                var dom = mapping.DomResourcePool;
                var core = mapping.CoreResourcePool ?? new CoreResourcePool(Guid.NewGuid());

                core.Name = dom.ResourcePoolInfo.Name;

                poolsToCreateOrUpdate.Add(core);

                domPoolsById.Add(dom.ID.Id, dom);
                domIdByCoreId.Add(core.ID, dom.ID.Id);
            }

            planApi.CoreHelpers.ResourceManagerHelper.TryCreateOrUpdateResourcePoolsInBatches(poolsToCreateOrUpdate, out var result);

            foreach (var id in result.UnsuccessfulIds)
            {
                if (!domIdByCoreId.TryGetValue(id, out var domId))
                {
                    planApi.Logger.LogError($"Failed to find DOM ID for CORE resource pool ID", id);
                    continue;
                }

                unsuccessfulIds.Add(domId);

                if (result.TraceDataPerItem.TryGetValue(id, out var traceData))
                {
                    traceDataPerItem.Add(domId, traceData);
                }
            }

            foreach (var id in result.SuccessfulIds)
            {
                if (!domIdByCoreId.TryGetValue(id, out var domId))
                {
                    planApi.Logger.LogError($"Failed to find DOM ID for CORE resource pool ID", id);
                    continue;
                }

                domPoolsById[domId].ResourcePoolInternalProperties.ResourcePoolId = id;

                successfulIds.Add(domId);
            }
        }

        private void Delete(ICollection<DomResourcePool> domResourcePools)
        {
            if (domResourcePools == null)
            {
                throw new ArgumentNullException(nameof(domResourcePools));
            }

            if (domResourcePools.Count == 0)
            {
                return;
            }

            Delete(ResourcePoolMapping.GetMappings(planApi, domResourcePools).ToList());

            /* Todo: Define how pool and resource deletion should work > see loop for more details
             * 
            TOL: Not sure if we need to check if there are resources in this pool instead of just removing the pool?
            > Checks can be done in the DOM handler since we only care about the DOM resources.
            > Resource pool DOM Handler can then first delete the resources by using the resource repository before deleting the pool.

            FilterElement<CoreResource> filter(Guid resourcePoolId) => Skyline.DataMiner.Net.Messages.ResourceExposers.PoolGUIDs.Contains(resourcePoolId);

			var coreResourcesByCorePoolId = planApi.CoreHelpers.ResourceManagerHelper.GetResources(coreResourcePoolsById.Keys, filter)
				.SelectMany(resource => resource.PoolGUIDs.Select(poolGuid => new { PoolGuid = poolGuid, Resource = resource }))
				.GroupBy(x => x.PoolGuid)
				.ToDictionary(
					g => g.Key,
					g => (IReadOnlyCollection<CoreResource>)g.Select(x => x.Resource).ToList()
				);

            foreach (var coreResourcePool in coreResourcePoolsById.Values)
            {

            }*/
        }

        private void Delete(ICollection<ResourcePoolMapping> resourcePoolMappings)
        {
            if (resourcePoolMappings == null)
            {
                throw new ArgumentNullException(nameof(resourcePoolMappings));
            }

            if (resourcePoolMappings.Count == 0)
            {
                return;
            }

            var domIdByCoreId = new Dictionary<Guid, Guid>();
            var poolsToDelete = new List<CoreResourcePool>();

            foreach (var mapping in resourcePoolMappings)
            {
                if (mapping.CoreResourcePool == null)
                {
                    // DOM resource pools without a CORE can be removed.
                    successfulIds.Add(mapping.DomResourcePool.ID.Id);

                    continue;
                }

                poolsToDelete.Add(mapping.CoreResourcePool);
                domIdByCoreId.Add(mapping.CoreResourcePool.ID, mapping.DomResourcePool.ID.Id);
            }

            planApi.CoreHelpers.ResourceManagerHelper.TryDeleteResourcePoolsInBatches(poolsToDelete, out var result);

            foreach (var id in result.UnsuccessfulIds)
            {
                if (!domIdByCoreId.TryGetValue(id, out var domId))
                {
                    planApi.Logger.LogError($"Failed to find DOM ID for CORE resource pool ID", id);
                    continue;
                }

                unsuccessfulIds.Add(domId);

                if (result.TraceDataPerItem.TryGetValue(id, out var traceData))
                {
                    traceDataPerItem.Add(domId, traceData);
                }
            }

            successfulIds.AddRange(result.SuccessfulIds);
        }

        private void ValidateNames(ICollection<DomResourcePool> domResourcePools)
        {
            if (domResourcePools == null)
            {
                throw new ArgumentNullException(nameof(domResourcePools));
            }

            if (domResourcePools.Count == 0)
            {
                return;
            }

            var poolsRequiringValidation = domResourcePools.ToList();

            foreach (var pool in poolsRequiringValidation.Where(x => !InputValidator.IsNonEmptyText(x.ResourcePoolInfo.Name)).ToList())
            {
                var error = new ResourcePoolInvalidNameError
                {
                    ErrorMessage = "Name cannot be empty.",
                    Id = pool.ID.Id,
                };
                AddError(pool.ID.Id, error);

                poolsRequiringValidation.Remove(pool);
            }

            foreach (var pool in poolsRequiringValidation.Where(x => !InputValidator.HasValidTextLength(x.ResourcePoolInfo.Name)).ToList())
            {
                var error = new ResourcePoolInvalidNameError
                {
                    ErrorMessage = "Name exceeds maximum length of 150 characters.",
                    Id = pool.ID.Id,
                    Name = pool.ResourcePoolInfo.Name,
                };
                AddError(pool.ID.Id, error);

                poolsRequiringValidation.Remove(pool);
            }

            var poolsWithDuplicateNames = poolsRequiringValidation
                .GroupBy(pool => pool.ResourcePoolInfo.Name)
                .Where(g => g.Count() > 1)
                .SelectMany(x => x)
                .ToList();
            foreach (var pool in poolsWithDuplicateNames)
            {
                var error = new ResourcePoolDuplicateNameError
                {
                    ErrorMessage = $"Resource pool '{pool.ResourcePoolInfo.Name}' has a duplicate name.",
                    Id = pool.ID.Id,
                    Name = pool.ResourcePoolInfo.Name,
                };
                AddError(pool.ID.Id, error);

                poolsRequiringValidation.Remove(pool);
            }

            var corePoolsByName = planApi.CoreHelpers.ResourceManagerHelper.GetResourcePoolsInBatches(poolsRequiringValidation.Select(x => x.ResourcePoolInfo.Name).ToArray());

            foreach (var pool in poolsRequiringValidation)
            {
                if (!corePoolsByName.TryGetValue(pool.ResourcePoolInfo.Name, out var corePools))
                {
                    continue;
                }

                var existingPools = corePools.Where(x => x.ID != pool.ResourcePoolInternalProperties.ResourcePoolId).ToList();
                if (existingPools.Count == 0)
                {
                    continue;
                }

                planApi.Logger.LogInformation($"Name '{pool.ResourcePoolInfo.Name}' is already in use by CORE resource pool(s) with ID(s)", existingPools.Select(x => x.ID).ToArray());

                var error = new ResourcePoolNameExistsError
                {
                    ErrorMessage = "Name is already in use.",
                    Id = pool.ID.Id,
                    Name = pool.ResourcePoolInfo.Name,
                };
                AddError(pool.ID.Id, error);
            }
        }

        private void AddError(Guid id, MediaOpsErrorData error)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentException("Id cannot be empty.", nameof(id));
            }

            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            if (!traceDataPerItem.TryGetValue(id, out var mediaOpsTraceData))
            {
                mediaOpsTraceData = new MediaOpsTraceData();
                traceDataPerItem.Add(id, mediaOpsTraceData);

                unsuccessfulIds.Add(id);
            }

            mediaOpsTraceData.Add(error);
        }

        private sealed class ResourcePoolMapping
        {
            private ResourcePoolMapping(DomResourcePool domResourcePool)
            {
                DomResourcePool = domResourcePool ?? throw new ArgumentNullException(nameof(domResourcePool));
            }

            private ResourcePoolMapping(DomResourcePool domResourcePool, CoreResourcePool coreResourcePool)
            {
                DomResourcePool = domResourcePool ?? throw new ArgumentNullException(nameof(domResourcePool));
                CoreResourcePool = coreResourcePool ?? throw new ArgumentNullException(nameof(coreResourcePool));
            }

            public DomResourcePool DomResourcePool { get; }

            public CoreResourcePool CoreResourcePool { get; }

            public bool NeedsNameValidation =>
                CoreResourcePool == null
                || DomResourcePool.ResourcePoolInfo.Name != CoreResourcePool.Name;

            public static IEnumerable<ResourcePoolMapping> GetMappings(MediaOpsPlanApi planApi, ICollection<DomResourcePool> domResourcePools)
            {
                if (planApi == null)
                {
                    throw new ArgumentNullException(nameof(planApi));
                }

                if (domResourcePools == null)
                {
                    throw new ArgumentNullException(nameof(domResourcePools));
                }

                if (domResourcePools.Count == 0)
                {
                    return [];
                }

                return GetMappingsIterator(planApi, domResourcePools);
            }

            private static IEnumerable<ResourcePoolMapping> GetMappingsIterator(MediaOpsPlanApi planApi, ICollection<DomResourcePool> domResourcePools)
            {
                var coreResourcePoolsById = planApi.CoreHelpers.ResourceManagerHelper.GetResourcePoolsInBatches(domResourcePools
               .Where(x => x.ResourcePoolInternalProperties.ResourcePoolId != Guid.Empty)
               .Select(x => x.ResourcePoolInternalProperties.ResourcePoolId)
               .Distinct());

                foreach (var domResourcePool in domResourcePools)
                {
                    if (coreResourcePoolsById.TryGetValue(domResourcePool.ResourcePoolInternalProperties.ResourcePoolId, out var coreResourcePool))
                    {
                        yield return new ResourcePoolMapping(domResourcePool, coreResourcePool);
                        continue;
                    }

                    yield return new ResourcePoolMapping(domResourcePool);
                }
            }
        }
    }
}
