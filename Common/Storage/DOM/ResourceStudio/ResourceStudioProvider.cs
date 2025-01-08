namespace Skyline.DataMiner.MediaOps.API.Common.Storage.DOM.ResourceStudio
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using DomHelpers.SlcResource_Studio;

    using Skyline.DataMiner.MediaOps.API.Common.Storage.DOM;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    internal class ResourceStudioProvider : ModuleHandlerBase
    {
        public ResourceStudioProvider(DomHelper domHelper) : base(domHelper, SlcResource_StudioIds.ModuleId)
        {
        }

        public IEnumerable<ResourceInstance> GetAllResources()
        {
            var filter = DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resource.Id);

            return GetResourcesIterator(filter);
        }

        public IEnumerable<ResourceInstance> GetResources(FilterElement<DomInstance> filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            return GetResourcesIterator(filter);
        }

        public ResourceInstance GetResource(Guid id)
        {
            var filter = DomInstanceExposers.Id.Equal(id);

            return GetResourcesIterator(filter).SingleOrDefault();
        }

        public ResourceInstance GetResource(string name)
        {
            var filter = DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInfo.Name).Equal(name);

            return GetResourcesIterator(filter).SingleOrDefault();
        }

        public long CountAllResources()
        {
            var filter = DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resource.Id);

            return DomHelper.DomInstances.Count(filter);
        }

        private IEnumerable<ResourceInstance> GetResourcesIterator(FilterElement<DomInstance> filter)
        {
            return InstanceFactory.ReadAndCreateInstances(DomHelper, filter, x => new ResourceInstance(x));
        }
    }
}
