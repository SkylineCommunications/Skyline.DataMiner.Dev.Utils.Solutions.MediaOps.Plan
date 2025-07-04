namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.MediaOps.Plan.Extensions;
    using Skyline.DataMiner.MediaOps.Plan.Storage.Core;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    using CoreResourcePool = Skyline.DataMiner.Net.Messages.ResourcePool;
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

        public static BulkCreateOrUpdateResult<Guid> CreateOrUpdate(MediaOpsPlanApi planApi, IEnumerable<DomResourcePool> domResourcePools)
        {
            var handler = new CoreResourcePoolHandler(planApi);
            handler.CreateOrUpdate(domResourcePools);

            var result = new BulkCreateOrUpdateResult<Guid>(handler.successfulIds, handler.unsuccessfulIds, handler.traceDataPerItem);
            result.ThrowOnFailure();

            return result;
        }

        public static bool TryCreateOrUpdate(MediaOpsPlanApi planApi, IEnumerable<DomResourcePool> domResourcePools, out BulkCreateOrUpdateResult<Guid> result)
        {
            var handler = new CoreResourcePoolHandler(planApi);
            handler.CreateOrUpdate(domResourcePools);

            result = new BulkCreateOrUpdateResult<Guid>(handler.successfulIds, handler.unsuccessfulIds, handler.traceDataPerItem);

            return !result.HasFailures();
        }

        public static BulkDeleteResult<Guid> Delete(MediaOpsPlanApi planApi, IEnumerable<DomResourcePool> domResourcePools, ResourcePoolDeleteOptions options)
        {
            var handler = new CoreResourcePoolHandler(planApi);
            handler.Delete(domResourcePools, options ?? ResourcePoolDeleteOptions.GetDefaults());

            var result = new BulkDeleteResult<Guid>(handler.successfulIds, handler.unsuccessfulIds, handler.traceDataPerItem);
            result.ThrowOnFailure();

            return result;
        }

        public static bool TryDelete(MediaOpsPlanApi planApi, IEnumerable<DomResourcePool> domResourcePools, ResourcePoolDeleteOptions options, out BulkDeleteResult<Guid> result)
        {
            var handler = new CoreResourcePoolHandler(planApi);
            handler.Delete(domResourcePools, options ?? ResourcePoolDeleteOptions.GetDefaults());

            result = new BulkDeleteResult<Guid>(handler.successfulIds, handler.unsuccessfulIds, handler.traceDataPerItem);

            return !result.HasFailures();
        }

        private void CreateOrUpdate(IEnumerable<DomResourcePool> domResourcePools)
        {
            if (domResourcePools == null)
            {
                throw new ArgumentNullException(nameof(domResourcePools));
            }

            if (!domResourcePools.Any())
            {
                return;
            }

            var resourcePoolMappingByDomId = ResourcePoolMapping.GetMappings(planApi, domResourcePools).ToDictionary(x => x.DomResourcePool.ID.Id);

            ValidateNames(resourcePoolMappingByDomId.Values.Where(x => x.NeedsNameValidation).Select(x => x.DomResourcePool));
            CreateOrUpdate(resourcePoolMappingByDomId.Where(x => !traceDataPerItem.Keys.Contains(x.Key)).Select(x => x.Value));
        }

        private void CreateOrUpdate(IEnumerable<ResourcePoolMapping> resourcePoolMappings)
        {
            if (resourcePoolMappings == null)
            {
                throw new ArgumentNullException(nameof(resourcePoolMappings));
            }

            if (!resourcePoolMappings.Any())
            {
                return;
            }

            var domPoolsById = new Dictionary<Guid, DomResourcePool>();
            var DomIdByCoreId = new Dictionary<Guid, Guid>();

            var poolsToCreateOrUpdate = new List<CoreResourcePool>();
            foreach (var mapping in resourcePoolMappings)
            {
                var dom = mapping.DomResourcePool;
                var core = mapping.CoreResourcePool ?? new CoreResourcePool(Guid.NewGuid());

                core.Name = dom.ResourcePoolInfo.Name;

                poolsToCreateOrUpdate.Add(core);

                domPoolsById.Add(dom.ID.Id, dom);
                DomIdByCoreId.Add(core.ID, dom.ID.Id);
            }

            planApi.CoreHelpers.ResourceManagerHelper.TryCreateOrUpdateResourcePoolsInBatches(poolsToCreateOrUpdate, out var result);

            foreach (var id in result.UnsuccessfulIds)
            {
                if (!DomIdByCoreId.TryGetValue(id, out var domId))
                {
                    planApi.Logger.Error(this, $"Failed to find DOM ID for CORE resource pool ID {id}.");
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
                if (!DomIdByCoreId.TryGetValue(id, out var domId))
                {
                    planApi.Logger.Error(this, $"Failed to find DOM ID for CORE resource pool ID {id}.");
                    continue;
                }

                domPoolsById[domId].ResourcePoolInternalProperties.ResourcePoolId = id;

                successfulIds.Add(domId);
            }
        }

        private void Delete(IEnumerable<DomResourcePool> domResourcePools, ResourcePoolDeleteOptions options)
        {
            if (domResourcePools == null)
            {
                throw new ArgumentNullException(nameof(domResourcePools));
            }

            if (!domResourcePools.Any())
            {
                return;
            }

            var coreResourcePoolsById = planApi.CoreHelpers.ResourceManagerHelper.GetResourcePoolsInBatches(domResourcePools
                .Where(x => x.ResourcePoolInternalProperties.ResourcePoolId != Guid.Empty)
                .Select(x => x.ResourcePoolInternalProperties.ResourcePoolId)
                .Distinct());

            // DOM resource pools without a CORE can be removed.
            successfulIds.AddRange(domResourcePools.Where(x => !coreResourcePoolsById.ContainsKey(x.ResourcePoolInternalProperties.ResourcePoolId)).Select(x => x.ID.Id));

            /* Todo: Define how pool and resource deletion should work > see loop for more details

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

        private void ValidateNames(IEnumerable<DomResourcePool> domResourcePools)
        {
            if (domResourcePools == null)
            {
                throw new ArgumentNullException(nameof(domResourcePools));
            }

            if (!domResourcePools.Any())
            {
                return;
            }

            var poolsRequiringValidation = domResourcePools.ToList();
            var poolsWithDuplicateNames = poolsRequiringValidation
                .GroupBy(pool => pool.ResourcePoolInfo.Name)
                .Where(g => g.Count() > 1)
                .SelectMany(x => x)
                .ToList();
            foreach (var pool in poolsWithDuplicateNames)
            {
                var error = new ResourcePoolConfigurationError
                {
                    ErrorReason = ResourcePoolConfigurationError.Reason.DuplicateName,
                    ErrorMessage = $"Resource pool '{pool.ResourcePoolInfo.Name}' has a duplicate name.",
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

                planApi.Logger.Information(this, $"Name '{pool.ResourcePoolInfo.Name}' is already in use by CORE resource pool(s) with ID(s): {string.Join(", ", existingPools.Select(x => x.ID))}.");

                var error = new ResourcePoolConfigurationError
                {
                    ErrorReason = ResourcePoolConfigurationError.Reason.NameExists,
                    ErrorMessage = "Name is already in use.",
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

        private class ResourcePoolMapping
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

            public static IEnumerable<ResourcePoolMapping> GetMappings(MediaOpsPlanApi planApi, IEnumerable<DomResourcePool> domResourcePools)
            {
                if (planApi == null)
                {
                    throw new ArgumentNullException(nameof(planApi));
                }

                if (domResourcePools == null)
                {
                    throw new ArgumentNullException(nameof(domResourcePools));
                }

                if (!domResourcePools.Any())
                {
                    return [];
                }

                return GetMappingsIterator(planApi, domResourcePools);
            }

            private static IEnumerable<ResourcePoolMapping> GetMappingsIterator(MediaOpsPlanApi planApi, IEnumerable<DomResourcePool> domResourcePools)
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
