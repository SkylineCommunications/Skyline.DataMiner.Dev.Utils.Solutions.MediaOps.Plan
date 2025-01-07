namespace Skyline.DataMiner.MediaOps.API.Common.Storage.DOM
{
    using System;
    using System.Collections.Generic;

    using DomHelpers.SlcResource_Studio;

    using Skyline.DataMiner.MediaOps.API.Common.ResourceStudio;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    internal class SlcResourceStudioDomStorageProvider : IStorageProvider<ResourceStudio.Resource>
    {
        private readonly ICommunication communication;
        private readonly DomHelper domHelper;

        public SlcResourceStudioDomStorageProvider(ICommunication communication)
        {
            this.communication = communication ?? throw new ArgumentNullException(nameof(communication));
            domHelper = new DomHelper(communication.Connection.HandleMessages, SlcResource_StudioIds.ModuleId);
        }

        public ResourceStudio.Resource Create(ResourceStudio.Resource oToCreate)
        {
            throw new NotImplementedException();
        }

        public ResourceStudio.Resource Delete(ResourceStudio.Resource oToDelete)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ResourceStudio.Resource> Read(FilterElement<ResourceStudio.Resource> filter)
        {
            throw new NotImplementedException();
        }

        public ResourceStudio.Resource Update(ResourceStudio.Resource oToUpdate)
        {
            throw new NotImplementedException();
        }

        private static ResourceInstance ToInstance(ResourceStudio.Resource resource)
        {

        }

        private static ResourceStudio.Resource FromInstance(ResourceInstance domResource)
        {

        }
    }
}
