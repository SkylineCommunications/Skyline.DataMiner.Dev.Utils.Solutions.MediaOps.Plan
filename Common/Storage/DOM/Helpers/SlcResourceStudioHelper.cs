namespace Skyline.DataMiner.MediaOps.Plan.Storage.DOM
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.MediaOps.Plan.Storage.DOM.SlcResource_Studio;
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Utils.DOM.Extensions;

    using SLDataGateway.API.Types.Querying;

    internal class SlcResourceStudioHelper : DomModuleHelperBase
    {
        public SlcResourceStudioHelper(IConnection connection) : base(SlcResource_StudioIds.ModuleId, connection)
        {
        }

        public long CountResourceStudioInstances(FilterElement<DomInstance> filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            return DomHelper.DomInstances.Count(filter);
        }

        public IEnumerable<DomInstance> GetResourceStudioInstances(IEnumerable<Guid> ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            if (!ids.Any())
            {
                return Enumerable.Empty<DomInstance>();
            }

            return FilterQueryExecutor.RetrieveFilteredItems(
                ids.Distinct(),
                x => DomInstanceExposers.Id.Equal(x),
                x => DomHelper.DomInstances.Read(x));
        }

        public IEnumerable<ResourcepoolInstance> GetResourcePools(FilterElement<DomInstance> filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            return GetResourcePoolIterator(filter);
        }

        internal IEnumerable<ResourcepoolInstance> GetResourcePools(IQuery<DomInstance> query)
        {
            return InstanceFactory.ReadAndCreateInstances(DomHelper, query, instance => new ResourcepoolInstance(instance));
        }

        public IEnumerable<ResourcepoolInstance> GetResourcePools(IEnumerable<Guid> ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            if (!ids.Any())
            {
                return Enumerable.Empty<ResourcepoolInstance>();
            }

            FilterElement<DomInstance> filter(Guid id) =>
                DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resourcepool.Id)
                .AND(DomInstanceExposers.Id.Equal(id));

            return FilterQueryExecutor.RetrieveFilteredItems(
                ids.Distinct(),
                x => filter(x),
                x => GetResourcePoolIterator(x));
        }

        public IEnumerable<ResourcepoolInstance> GetResourcePools<T>(IEnumerable<T> values, Func<T, FilterElement<DomInstance>> filter)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            return FilterQueryExecutor.RetrieveFilteredItems(
                values.Distinct(),
                x => filter(x),
                x => GetResourcePoolIterator(x));
        }

        public IEnumerable<ResourcepoolInstance> GetPoolsByResource(ResourceInstance resource)
        {
            if (resource == null)
            {
                throw new ArgumentNullException(nameof(resource));
            }

            return GetResourcePools(resource.ResourceInternalProperties.PoolIds);
        }

        public IEnumerable<ResourcepoolInstance> GetPoolsByResource(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(id));
            }

            return GetPoolsByResource(GetResources([id]).FirstOrDefault());
        }

        public IEnumerable<ResourcepoolInstance> GetAllPoolsForResources(IEnumerable<ResourceInstance> resources)
        {
            if (resources == null)
            {
                throw new ArgumentNullException(nameof(resources));
            }

            var poolIds = resources
                .Where(x => x != null)
                .SelectMany(x => x.ResourceInternalProperties.PoolIds)
                .Distinct()
                .ToArray();

            return GetResourcePools(poolIds);
        }

        public IEnumerable<ResourcepoolInstance> GetAllPoolsForResources(IEnumerable<Guid> resourceIds)
        {
            if (resourceIds == null)
            {
                throw new ArgumentNullException(nameof(resourceIds));
            }

            return GetAllPoolsForResources(GetResources(resourceIds));
        }

        public IEnumerable<ResourceInstance> GetAllResources()
        {
            var filter = DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resource.Id);
            return GetResourceIterator(filter);
        }

        public IEnumerable<IEnumerable<ResourceInstance>> GetAllResourcesPaged(long? pageSize = null)
        {
            var filter = DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resource.Id);

            var pages = pageSize.HasValue
                ? DomHelper.DomInstances.ReadPaged(filter, pageSize.Value)
                : DomHelper.DomInstances.ReadPaged(filter);

            return pages.Select(p => GetResourceIterator(p));
        }

        public IEnumerable<ResourceInstance> GetResources(FilterElement<DomInstance> filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            return GetResourceIterator(filter);
        }

        public IEnumerable<ResourceInstance> GetResources<T>(IEnumerable<T> values, Func<T, FilterElement<DomInstance>> filter)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            return FilterQueryExecutor.RetrieveFilteredItems(
                values.Distinct(),
                x => filter(x),
                x => GetResourceIterator(x));
        }

        internal IEnumerable<ResourceInstance> GetResources(IQuery<DomInstance> query)
        {
            return InstanceFactory.ReadAndCreateInstances(DomHelper, query, instance => new ResourceInstance(instance));
        }

        public IEnumerable<ResourceInstance> GetResources(IEnumerable<Guid> ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            if (!ids.Any())
            {
                return Enumerable.Empty<ResourceInstance>();
            }

            FilterElement<DomInstance> filter(Guid id) =>
                DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resource.Id)
                .AND(DomInstanceExposers.Id.Equal(id));

            return FilterQueryExecutor.RetrieveFilteredItems(
                ids.Distinct(),
                x => filter(x),
                x => GetResourceIterator(x));
        }

        public IEnumerable<ResourceInstance> GetResourcesByPool(ResourcepoolInstance resourcePool)
        {
            if (resourcePool == null)
            {
                throw new ArgumentNullException(nameof(resourcePool));
            }

            return GetResourcesByPool(resourcePool.ID.Id);
        }

        public IEnumerable<ResourceInstance> GetResourcesByPool(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(id));
            }

            var filter = DomInstanceExposers.FieldValues
                .DomInstanceField(SlcResource_StudioIds.Sections.ResourceInternalProperties.Pool_Ids)
                .Contains(Convert.ToString(id));

            return GetResourceIterator(filter);
        }

        /// <summary>
        /// Returns resources that are included in any of the provided pools.
        /// </summary>
        public IEnumerable<ResourceInstance> GetAllResourcesInPools(IEnumerable<ResourcepoolInstance> pools)
        {
            if (pools == null)
            {
                throw new ArgumentNullException(nameof(pools));
            }

            return GetAllResourcesInPools(pools.Select(x => x.ID.Id));
        }

        /// <summary>
        /// Returns resources that are included in any of the provided pools.
        /// </summary>
        public IEnumerable<ResourceInstance> GetAllResourcesInPools(IEnumerable<Guid> poolIds)
        {
            if (poolIds == null)
            {
                throw new ArgumentNullException(nameof(poolIds));
            }

            var filters = poolIds
                .Select(x => DomInstanceExposers.FieldValues
                    .DomInstanceField(SlcResource_StudioIds.Sections.ResourceInternalProperties.Pool_Ids)
                    .Contains(Convert.ToString(x)))
                .ToArray();

            if (filters.Length == 0)
            {
                return Enumerable.Empty<ResourceInstance>();
            }

            var filter = new ORFilterElement<DomInstance>(filters);

            return GetResourceIterator(filter);
        }

        public void TransitionResourceToComplete(Guid resourceId)
        {
            var transitionId = SlcResource_StudioIds.Behaviors.Resource_Behavior.Transitions.Draft_To_Complete;
            DomHelper.DomInstances.DoStatusTransition(new DomInstanceId(resourceId), transitionId);
        }

        public void TransitionResourceToDeprecated(Guid resourceId)
        {
            var transitionId = SlcResource_StudioIds.Behaviors.Resource_Behavior.Transitions.Complete_To_Deprecated;
            DomHelper.DomInstances.DoStatusTransition(new DomInstanceId(resourceId), transitionId);
        }

        public IEnumerable<ResourcepropertyInstance> GetAllResourceProperties()
        {
            var filter = DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resourceproperty.Id);
            return GetResourcePropertyIterator(filter);
        }

        public IEnumerable<IEnumerable<ResourcepropertyInstance>> GetAllResourcePropertiesPaged(long? pageSize = null)
        {
            var filter = DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resourceproperty.Id);

            var pages = pageSize.HasValue
                ? DomHelper.DomInstances.ReadPaged(filter, pageSize.Value)
                : DomHelper.DomInstances.ReadPaged(filter);

            return pages.Select(p => GetResourcePropertyIterator(p));
        }

        public IEnumerable<ResourcepropertyInstance> GetResourceProperties(FilterElement<DomInstance> filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            return GetResourcePropertyIterator(filter);
        }

        public IEnumerable<ResourcepropertyInstance> GetResourceProperties(IEnumerable<Guid> ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            if (!ids.Any())
            {
                return Enumerable.Empty<ResourcepropertyInstance>();
            }

            FilterElement<DomInstance> filter(Guid id) =>
                DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resourceproperty.Id)
                .AND(DomInstanceExposers.Id.Equal(id));

            return FilterQueryExecutor.RetrieveFilteredItems(
                ids.Distinct(),
                x => filter(x),
                x => GetResourcePropertyIterator(x));
        }

        public IEnumerable<ResourcepropertyInstance> GetResourceProperties<T>(IEnumerable<T> values, Func<T, FilterElement<DomInstance>> filter)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            return FilterQueryExecutor.RetrieveFilteredItems(
                values.Distinct(),
                x => filter(x),
                x => GetResourcePropertyIterator(x));
        }

        internal IEnumerable<ResourcepropertyInstance> GetResourceProperties(IQuery<DomInstance> query)
        {
            return InstanceFactory.ReadAndCreateInstances(DomHelper, query, instance => new ResourcepropertyInstance(instance));
        }

        private IEnumerable<ResourcepoolInstance> GetResourcePoolIterator(FilterElement<DomInstance> filter)
        {
            return InstanceFactory.ReadAndCreateInstances(DomHelper, filter, instance => new ResourcepoolInstance(instance));
        }

        private IEnumerable<ResourceInstance> GetResourceIterator(IEnumerable<DomInstance> instances)
        {
            foreach (var instance in instances)
            {
                if (instance == null || instance.DomDefinitionId.Id != SlcResource_StudioIds.Definitions.Resource.Id)
                {
                    continue;
                }

                yield return InstanceFactory.CreateInstance(instance, instance => new ResourceInstance(instance));
            }
        }

        private IEnumerable<ResourceInstance> GetResourceIterator(FilterElement<DomInstance> filter)
        {
            return InstanceFactory.ReadAndCreateInstances(DomHelper, filter, instance => new ResourceInstance(instance));
        }

        private IEnumerable<ResourcepropertyInstance> GetResourcePropertyIterator(FilterElement<DomInstance> filter)
        {
            return InstanceFactory.ReadAndCreateInstances(DomHelper, filter, instance => new ResourcepropertyInstance(instance));
        }

        private IEnumerable<ResourcepropertyInstance> GetResourcePropertyIterator(IEnumerable<DomInstance> instances)
        {
            foreach (var instance in instances)
            {
                if (instance == null || instance.DomDefinitionId.Id != SlcResource_StudioIds.Definitions.Resourceproperty.Id)
                {
                    continue;
                }

                yield return InstanceFactory.CreateInstance(instance, instance => new ResourcepropertyInstance(instance));
            }
        }
    }
}
