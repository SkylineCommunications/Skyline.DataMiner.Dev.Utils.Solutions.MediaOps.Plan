namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.ActivityHelper;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    using SLDataGateway.API.Types.Querying;

    internal partial class ResourcesRepository : DomRepository<Resource>, IResourcesRepository
    {
        public ResourcesRepository(MediaOpsPlanApi planApi) : base(planApi)
        {
        }

        public Resource Read(Guid id)
        {
            PlanApi.Logger.LogInformation($"Reading Resource with ID: {id}...");

            if (id == Guid.Empty)
            {
                throw new ArgumentException(nameof(id));
            }

            return ActivityHelper.Track(nameof(ResourcesRepository), nameof(Read), act =>
            {
                act?.AddTag("ResourceId", id);
                var filter = DomInstanceExposers.DomDefinitionId.Equal(Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Definitions.Resource.Id)
                        .AND(DomInstanceExposers.Id.Equal(id));
                var domResource = PlanApi.DomHelpers.SlcResourceStudioHelper.GetResources(filter)
                    .FirstOrDefault();

                if (domResource == null)
                {
                    act?.AddTag("Hit", false);
                    return null;
                }

                act?.AddTag("Hit", true);

                return Resource.InstantiateResources([domResource]).FirstOrDefault();
            });
        }

        public IDictionary<Guid, Resource> Read(IEnumerable<Guid> ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            return ActivityHelper.Track(nameof(ResourcesRepository), nameof(Read), act =>
            {
                act?.AddTag("ResourceIds", String.Join(", ", ids));
                act?.AddTag("ResourceIds Count", ids.Count());

                var resources = PlanApi.DomHelpers.SlcResourceStudioHelper.GetResources(ids);
                return Resource.InstantiateResources(resources).ToDictionary(x => x.Id);
            });
        }

        public IEnumerable<Resource> ReadAll()
        {
            return ActivityHelper.Track(nameof(ResourcesRepository), nameof(ReadAll), act =>
            {
                var filter = DomInstanceExposers.DomDefinitionId.Equal(Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Definitions.Resource.Id);
                return Resource.InstantiateResources(PlanApi.DomHelpers.SlcResourceStudioHelper.GetResources(filter));
            });
        }

        public IEnumerable<IEnumerable<Resource>> ReadAllPaged()
        {
            return PlanApi.DomHelpers.SlcResourceStudioHelper.GetAllResourcesPaged()
                .Select(page => Resource.InstantiateResources(page));
        }

        public bool TryConvertToElementResource(Resource resource, ResourceElementLinkConfiguration configuration, out ElementResource elementResource)
        {
            elementResource = null;

            try
            {
                elementResource = ConvertToElementResource(resource, configuration);
                return true;
            }
            catch (Exception e)
            {
                PlanApi.Logger.LogWarning($"Unable to convert Resource {resource.Name} [{resource.Id}] to an Element Resource because of: {e}");
                return false;
            }
        }

        public bool TryConvertToElementResource(Guid resourceId, ResourceElementLinkConfiguration configuration, out ElementResource elementResource)
        {
            elementResource = null;

            try
            {
                elementResource = ConvertToElementResource(resourceId, configuration);
                return true;
            }
            catch (Exception e)
            {
                PlanApi.Logger.LogWarning($"Unable to convert Resource with ID {resourceId} to an Element Resource because of: {e}");
                return false;
            }
        }

        public bool TryConvertToServiceResource(Resource resource, ResourceServiceLinkConfiguration configuration, out ServiceResource serviceResource)
        {
            serviceResource = null;

            try
            {
                serviceResource = ConvertToServiceResource(resource, configuration);
                return true;
            }
            catch (Exception e)
            {
                PlanApi.Logger.LogWarning($"Unable to convert Resource {resource.Name} [{resource.Id}] to a Service Resource because of: {e}");
                return false;
            }
        }

        public bool TryConvertToServiceResource(Guid resourceId, ResourceServiceLinkConfiguration configuration, out ServiceResource serviceResource)
        {
            serviceResource = null;

            try
            {
                serviceResource = ConvertToServiceResource(resourceId, configuration);
                return true;
            }
            catch (Exception e)
            {
                PlanApi.Logger.LogWarning($"Unable to convert Resource with ID {resourceId} to a Service Resource because of: {e}");
                return false;
            }
        }

        public bool TryConvertToUnmanagedResource(Resource resource, out UnmanagedResource unmanagedResource)
        {
            unmanagedResource = null;

            try
            {
                unmanagedResource = ConvertToUnmanagedResource(resource);
                return true;
            }
            catch (Exception e)
            {
                PlanApi.Logger.LogWarning($"Unable to convert Resource {resource.Name} [{resource.Id}] to an Unmanaged Resource because of: {e}");
                return false;
            }
        }

        public bool TryConvertToUnmanagedResource(Guid resourceId, out UnmanagedResource unmanagedResource)
        {
            unmanagedResource = null;

            try
            {
                unmanagedResource = ConvertToUnmanagedResource(resourceId);
                return true;
            }
            catch (Exception e)
            {
                PlanApi.Logger.LogWarning($"Unable to convert Resource with ID {resourceId} to an Unmanaged Resource because of: {e}");
                return false;
            }
        }

        public bool TryConvertToVirtualFunctionResource(Resource resource, ResourceVirtualFunctionLinkConfiguration configuration, out VirtualFunctionResource virtualFunctionResource)
        {
            virtualFunctionResource = null;

            try
            {
                virtualFunctionResource = ConvertToVirtualFunctionResource(resource, configuration);
                return true;
            }
            catch (Exception e)
            {
                PlanApi.Logger.LogWarning($"Unable to convert Resource {resource.Name} [{resource.Id}] to a Virtual Function Resource because of: {e}");
                return false;
            }
        }

        public bool TryConvertToVirtualFunctionResource(Guid resourceId, ResourceVirtualFunctionLinkConfiguration configuration, out VirtualFunctionResource virtualFunctionResource)
        {
            virtualFunctionResource = null;

            try
            {
                virtualFunctionResource = ConvertToVirtualFunctionResource(resourceId, configuration);
                return true;
            }
            catch (Exception e)
            {
                PlanApi.Logger.LogWarning($"Unable to convert Resource with ID {resourceId} to a Virtual Function Resource because of: {e}");
                return false;
            }
        }

        public long CountAll()
        {
            return PlanApi.DomHelpers.SlcResourceStudioHelper.CountResourceStudioInstances(
                DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resource.Id));
        }

        public IEnumerable<Resource> GetResourcesInPool(ResourcePool resourcePool)
        {
            if (resourcePool == null)
            {
                throw new ArgumentNullException(nameof(resourcePool));
            }

            return Resource.InstantiateResources(PlanApi.DomHelpers.SlcResourceStudioHelper.GetResourcesByPool(resourcePool.Id));
        }

        public IEnumerable<Resource> GetResourcesInPool(ResourcePool resourcePool, ResourceState state)
        {
            if (resourcePool == null)
            {
                throw new ArgumentNullException(nameof(resourcePool));
            }

            var filter = DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInternalProperties.Pool_Ids)
                .Contains(Convert.ToString(resourcePool.Id))
                .AND(DomInstanceExposers.StatusId.Equal(SlcResource_StudioIds.Behaviors.Resource_Behavior.Statuses.ToValue(EnumExtensions.MapEnum<ResourceState, SlcResource_StudioIds.Behaviors.Resource_Behavior.StatusesEnum>(state))));

            return Resource.InstantiateResources(PlanApi.DomHelpers.SlcResourceStudioHelper.GetResources(filter));
        }

        public IReadOnlyDictionary<ResourcePool, IEnumerable<Resource>> GetResourcesPerPool(IEnumerable<ResourcePool> resourcePools)
        {
            if (resourcePools == null)
            {
                throw new ArgumentNullException(nameof(resourcePools));
            }

            var domResources = PlanApi.DomHelpers.SlcResourceStudioHelper.GetAllResourcesInPools(resourcePools.Select(x => x.Id));
            var apiResourcesById = Resource.InstantiateResources(domResources).ToDictionary(x => x.Id);

            var resourcesPerPool = resourcePools.ToDictionary(
                pool => pool,
                pool =>
                    domResources
                        .Where(x => x.ResourceInternalProperties.PoolIds.Contains(pool.Id))
                        .Select(x => apiResourcesById[x.ID.Id])
            );

            return resourcesPerPool;
        }

        public IReadOnlyDictionary<ResourcePool, IEnumerable<Resource>> GetResourcesPerPool(IEnumerable<ResourcePool> resourcePools, ResourceState state)
        {
            if (resourcePools == null)
            {
                throw new ArgumentNullException(nameof(resourcePools));
            }

            var resourceFilters = resourcePools
                .Select(x => DomInstanceExposers.FieldValues
                    .DomInstanceField(SlcResource_StudioIds.Sections.ResourceInternalProperties.Pool_Ids)
                    .Contains(Convert.ToString(x.Id)))
                .ToArray();

            var filter = new ORFilterElement<DomInstance>(resourceFilters)
                .AND(DomInstanceExposers.StatusId.Equal(SlcResource_StudioIds.Behaviors.Resource_Behavior.Statuses.ToValue(EnumExtensions.MapEnum<ResourceState, SlcResource_StudioIds.Behaviors.Resource_Behavior.StatusesEnum>(state))));

            var domResources = PlanApi.DomHelpers.SlcResourceStudioHelper.GetResources(filter);
            var apiResourcesById = Resource.InstantiateResources(domResources).ToDictionary(x => x.Id);

            var resourcesPerPool = resourcePools.ToDictionary(
                pool => pool,
                pool =>
                    domResources
                        .Where(x => x.ResourceInternalProperties.PoolIds.Contains(pool.Id))
                        .Select(x => apiResourcesById[x.ID.Id])
            );

            return resourcesPerPool;
        }

        public IQueryable<Resource> Query()
        {
            return new ApiRepositoryQuery<Resource, DomInstance>(QueryProvider);
        }

        public bool HasResources(ResourcePool resourcePool)
        {
            throw new NotImplementedException();
        }

        public long ResourceCount(ResourcePool resourcePool)
        {
            throw new NotImplementedException();
        }

        internal override IEnumerable<Resource> Read(IQuery<DomInstance> query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            var domFilter = AddDomDefinitionFilter(query.Filter, SlcResource_StudioIds.Definitions.Resource);

            query = query.WithFilter(domFilter);

            var domInstances = PlanApi.DomHelpers.SlcResourceStudioHelper.GetResources(query);

            return Resource.InstantiateResources(domInstances);
        }

        internal override long Count(FilterElement<DomInstance> domFilter)
        {
            return PlanApi.DomHelpers.SlcResourceStudioHelper.CountResourceStudioInstances(
                DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resource.Id).AND(domFilter));
        }

        protected internal override FilterElement<DomInstance> CreateFilter(string fieldName, Comparer comparer, object value)
        {
            switch (fieldName)
            {
                case nameof(Resource.Concurrency):
                    return FilterElementFactory<DomInstance>.Create<long>(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInfo.Concurrency), comparer, value);
                case nameof(Resource.State):
                    return FilterElementFactory<DomInstance>.Create(DomInstanceExposers.StatusId, comparer, TranslateResourceState((ResourceState)value));
                case nameof(Resource.IsFavorite):
                    return FilterElementFactory<DomInstance>.Create<bool>(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInfo.Favorite), comparer, value);
            }

            return base.CreateFilter(fieldName, comparer, value);
        }

        protected internal override FilterElement<DomInstance> CreateFilter(Type type, Comparer comparer)
        {
            if (type == typeof(UnmanagedResource))
            {
                return FilterElementFactory<DomInstance>.Create<int>(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInfo.Type), comparer, SlcResource_StudioIds.Enums.Type.Unmanaged);
            }
            else if (type == typeof(VirtualFunctionResource))
            {
                return FilterElementFactory<DomInstance>.Create<int>(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInfo.Type), comparer, SlcResource_StudioIds.Enums.Type.VirtualFunction);
            }
            else if (type == typeof(ServiceResource))
            {
                return FilterElementFactory<DomInstance>.Create<int>(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInfo.Type), comparer, SlcResource_StudioIds.Enums.Type.Service);
            }
            else if (type == typeof(ElementResource))
            {
                return FilterElementFactory<DomInstance>.Create<int>(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInfo.Type), comparer, SlcResource_StudioIds.Enums.Type.Element);
            }

            return base.CreateFilter(type, comparer);
        }

        protected internal override IOrderByElement CreateOrderBy(string fieldName, SortOrder sortOrder, bool naturalSort = false)
        {
            switch (fieldName)
            {
                case nameof(Resource.Concurrency):
                    return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInfo.Concurrency), sortOrder, naturalSort);
                case nameof(Resource.State):
                    return OrderByElementFactory.Create(DomInstanceExposers.StatusId, sortOrder, naturalSort);
                case nameof(Resource.IsFavorite):
                    return OrderByElementFactory.Create(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInfo.Favorite), sortOrder, naturalSort);
            }

            return base.CreateOrderBy(fieldName, sortOrder, naturalSort);
        }

        /// <summary>
        /// Translates the ResourceState enum to the state in DOM.
        /// </summary>
        /// <param name="state">State to be translated.</param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">If the provided state is not supported.</exception>
        private string TranslateResourceState(ResourceState state)
        {
            return state switch
            {
                ResourceState.Draft => SlcResource_StudioIds.Behaviors.Resource_Behavior.Statuses.Draft,
                ResourceState.Complete => SlcResource_StudioIds.Behaviors.Resource_Behavior.Statuses.Complete,
                ResourceState.Deprecated => SlcResource_StudioIds.Behaviors.Resource_Behavior.Statuses.Deprecated,
                _ => throw new NotSupportedException($"Resource state '{state}' is not supported.")
            };
        }
    }
}
