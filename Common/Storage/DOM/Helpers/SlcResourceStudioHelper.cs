namespace Skyline.DataMiner.MediaOps.Plan.Storage.DOM
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.MediaOps.Plan.Storage.DOM.SlcResource_Studio;
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    internal class SlcResourceStudioHelper : DomModuleHelperBase
    {
        public SlcResourceStudioHelper(IConnection connection) : base(SlcResource_Studio.SlcResource_StudioIds.ModuleId, connection)
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

        public IEnumerable<ResourceInstance> GetResources(FilterElement<DomInstance> filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            return GetResourceIterator(filter);
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

            return GetResourcesByPoolId(resourcePool.ID.Id);
        }

        public IEnumerable<ResourceInstance> GetResourcesByPoolId(Guid id)
        {
            var filter = DomInstanceExposers.FieldValues
                .DomInstanceField(SlcResource_StudioIds.Sections.ResourceInternalProperties.Pool_Ids)
                .Contains(Convert.ToString(id));

            return GetResourceIterator(filter);
        }

        public void TransitionToComplete(Guid resourceId)
        {
            var transitionId = SlcResource_StudioIds.Behaviors.Resource_Behavior.Transitions.Draft_To_Complete;
            DomHelper.DomInstances.DoStatusTransition(new DomInstanceId(resourceId), transitionId);
        }

        public void TransitionToDeprecated(Guid resourceId)
        {
            var transitionId = SlcResource_StudioIds.Behaviors.Resource_Behavior.Transitions.Complete_To_Deprecated;
            DomHelper.DomInstances.DoStatusTransition(new DomInstanceId(resourceId), transitionId);
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

        private IEnumerable<ResourcepoolInstance> GetResourcePoolIterator(FilterElement<DomInstance> filter)
        {
            return InstanceFactory.ReadAndCreateInstances(DomHelper, filter, instance => new ResourcepoolInstance(instance));
        }

        private IEnumerable<ResourceInstance> GetResourceIterator(FilterElement<DomInstance> filter)
        {
            return InstanceFactory.ReadAndCreateInstances(DomHelper, filter, instance => new ResourceInstance(instance));
        }
    }
}
