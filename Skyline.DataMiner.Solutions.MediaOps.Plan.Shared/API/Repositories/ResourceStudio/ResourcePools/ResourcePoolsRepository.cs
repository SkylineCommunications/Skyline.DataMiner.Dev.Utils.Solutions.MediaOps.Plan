namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.ActivityHelper;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Net.Sections;

    using SLDataGateway.API.Types.Querying;
    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

    internal partial class ResourcePoolsRepository : DomRepository<ResourcePool>, IResourcePoolsRepository
    {
        public ResourcePoolsRepository(MediaOpsPlanApi planApi) : base(planApi)
        {
        }

        public long Count(FilterElement<ResourcePool> filter)
        {
            throw new NotImplementedException();
        }

        public long CountAll()
        {
            return DomResourcePoolHandler.CountAll(PlanApi);
        }

        public bool HasResources(ResourcePool resourcePool)
        {
            throw new NotImplementedException();
        }

        public ResourcePool Read(Guid id)
        {
            PlanApi.Logger.LogInformation("Reading ResourcePool with ID: {id}...", id);

            if (id == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(id));
            }

            return ActivityHelper.Track(nameof(ResourcePoolsRepository), nameof(Read), act =>
            {
                act?.AddTag("ResourcePoolId", id);
                var filter = DomInstanceExposers.DomDefinitionId.Equal(StorageResourceStudio.SlcResource_StudioIds.Definitions.Resourcepool.Id)
                        .AND(DomInstanceExposers.Id.Equal(id));
                var domResourcePool = PlanApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(filter)
                    .FirstOrDefault();

                if (domResourcePool == null)
                {
                    act?.AddTag("Hit", false);
                    return null;
                }

                act?.AddTag("Hit", true);
                return new ResourcePool(domResourcePool);
            });
        }

        public IDictionary<Guid, ResourcePool> Read(IEnumerable<Guid> ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            return ActivityHelper.Track(nameof(ResourcePoolsRepository), nameof(Read), act =>
            {
                act?.AddTag("RequestedResourcePoolCount", ids.Count());

                if (!ids.Any())
                {
                    return new Dictionary<Guid, ResourcePool>();
                }

                var retrievedPools = PlanApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(ids).SafeToDictionary(x => x.ID.Id, x => new ResourcePool(x));

                act?.AddTag("RetrievedResourcePoolCount", retrievedPools.Count);
                return retrievedPools;
            });
        }

        public IEnumerable<ResourcePool> Read(FilterElement<ResourcePool> filter)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ResourcePool> ReadAll()
        {
            return ActivityHelper.Track(nameof(ResourcePoolsRepository), nameof(ReadAll), act =>
            {
                var filter = DomInstanceExposers.DomDefinitionId.Equal(StorageResourceStudio.SlcResource_StudioIds.Definitions.Resourcepool.Id);
                IEnumerable<ResourcePool> Iterator()
                {
                    foreach (var domResourcePool in PlanApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(filter))
                    {
                        yield return new ResourcePool(domResourcePool);
                    }
                }

                return Iterator();
            });
        }

        public IEnumerable<IEnumerable<ResourcePool>> ReadAllPaged()
        {
            throw new NotImplementedException();
        }

        public long ResourceCount(ResourcePool resourcePool)
        {
            throw new NotImplementedException();
        }

        public IQueryable<ResourcePool> Query()
        {
            return new ApiRepositoryQuery<ResourcePool, DomInstance>(QueryProvider);
        }

        public IEnumerable<ResourcePool> GetResourcePools(Resource resource)
        {
            if (resource == null)
            {
                throw new ArgumentNullException(nameof(resource));
            }

            return PlanApi.DomHelpers.SlcResourceStudioHelper.GetPoolsByResource(resource.Id)
                .Select(x => new ResourcePool(x));
        }

        public IReadOnlyDictionary<Resource, IEnumerable<ResourcePool>> GetPoolsPerResource(IEnumerable<Resource> resources)
        {
            if (resources == null)
            {
                throw new ArgumentNullException(nameof(resources));
            }

            var domResourcesById = resources.Select(x => x.OriginalInstance).ToDictionary(x => x.ID.Id);
            var domResourcePools = PlanApi.DomHelpers.SlcResourceStudioHelper.GetAllPoolsForResources(domResourcesById.Values);
            var apiPoolsById = domResourcePools.Select(x => new ResourcePool(x)).ToDictionary(x => x.Id);

            var poolsPerResource = new Dictionary<Resource, List<ResourcePool>>();
            foreach (var resource in resources)
            {
                if (!domResourcesById.TryGetValue(resource.Id, out var domResource))
                {
                    continue;
                }

                var pools = domResource.ResourceInternalProperties.PoolIds
                    .Select(poolId => apiPoolsById.TryGetValue(poolId, out var pool) ? pool : null)
                    .Where(pool => pool != null)
                    .ToList();

                if (pools.Count == 0)
                {
                    continue;
                }

                poolsPerResource.Add(resource, pools);
            }

            return (IReadOnlyDictionary<Resource, IEnumerable<ResourcePool>>)poolsPerResource;
        }

        public IReadOnlyDictionary<ResourcePool, IEnumerable<ResourcePool>> GetParentPoolLinks(IEnumerable<ResourcePool> resourcePools)
        {
            if (resourcePools == null)
            {
                throw new ArgumentNullException(nameof(resourcePools));
            }

            var apiPoolsById = resourcePools.ToDictionary(x => x.Id);

            var poolFilters = resourcePools
                .Select(x => DomInstanceExposers.FieldValues
                    .DomInstanceField(StorageResourceStudio.SlcResource_StudioIds.Sections.ResourcePoolLinks.LinkedResourcePool)
                    .Equal(x.Id))
                .ToArray();

            var filter = new ORFilterElement<DomInstance>(poolFilters)
                .AND(DomInstanceExposers.DomDefinitionId.Equal(StorageResourceStudio.SlcResource_StudioIds.Definitions.Resourcepool.Id));

            var domPools = PlanApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(filter);
            var parentApiPoolsById = domPools.Select(x => new ResourcePool(x)).ToDictionary(x => x.Id);

            var parentPoolsPerPool = resourcePools.ToDictionary(
                pool => pool,
                pool =>
                    domPools
                        .Where(domPool => domPool.ResourcePoolLinks
                        .Any(link => link.LinkedResourcePool.Value == pool.Id))
                        .Select(domPool =>
                        {
                            if (apiPoolsById.ContainsKey(domPool.ID.Id))
                            {
                                return apiPoolsById[domPool.ID.Id];
                            }
                            else
                            {
                                return parentApiPoolsById[domPool.ID.Id];
                            }
                        })
            );

            return parentPoolsPerPool;
        }

        protected internal override FilterElement<DomInstance> CreateFilter(string fieldName, Comparer comparer, object value)
        {
            switch (fieldName)
            {
                case nameof(ResourcePool.State):
                    return FilterElementFactory<DomInstance>.Create(DomInstanceExposers.StatusId, comparer, TranslateResourcePoolState((ResourcePoolState)value));
            }

            return base.CreateFilter(fieldName, comparer, value);
        }

        protected internal override IOrderByElement CreateOrderBy(string fieldName, SortOrder sortOrder, bool naturalSort = false)
        {
            switch (fieldName)
            {
                case nameof(ResourcePool.State):
                    return OrderByElementFactory.Create(DomInstanceExposers.StatusId, sortOrder, naturalSort);
            }

            return base.CreateOrderBy(fieldName, sortOrder, naturalSort);
        }

        internal override IEnumerable<ResourcePool> Read(IQuery<DomInstance> query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            var domFilter = AddDomDefinitionFilter(query.Filter, StorageResourceStudio.SlcResource_StudioIds.Definitions.Resourcepool);

            query = query.WithFilter(domFilter);

            var domInstances = PlanApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(query);

            return domInstances.Select(x => new ResourcePool(x));
        }

        internal override long Count(FilterElement<DomInstance> domFilter)
        {
            return PlanApi.DomHelpers.SlcResourceStudioHelper.CountResourceStudioInstances(AddDomDefinitionFilter(domFilter, StorageResourceStudio.SlcResource_StudioIds.Definitions.Resourcepool));
        }

        /// <summary>
        /// Translates the ResourceState enum to the state in DOM.
        /// </summary>
        /// <param name="state">State to be translated.</param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">If the provided state is not supported.</exception>
        private string TranslateResourcePoolState(ResourcePoolState state)
        {
            return state switch
            {
                ResourcePoolState.Draft => StorageResourceStudio.SlcResource_StudioIds.Behaviors.Resourcepool_Behavior.Statuses.Draft,
                ResourcePoolState.Complete => StorageResourceStudio.SlcResource_StudioIds.Behaviors.Resourcepool_Behavior.Statuses.Complete,
                ResourcePoolState.Deprecated => StorageResourceStudio.SlcResource_StudioIds.Behaviors.Resourcepool_Behavior.Statuses.Deprecated,
                _ => throw new NotSupportedException($"Resource Pool state '{state}' is not supported.")
            };
        }
    }
}
