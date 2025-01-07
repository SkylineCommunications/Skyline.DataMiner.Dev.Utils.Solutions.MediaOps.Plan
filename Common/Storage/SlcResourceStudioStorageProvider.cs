namespace Skyline.DataMiner.MediaOps.API.Common.Storage
{
    using System;
    using System.Collections.Generic;

    using Skyline.DataMiner.MediaOps.API.Common.ResourceStudio;
    using Skyline.DataMiner.MediaOps.API.Common.Storage.DOM;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    internal class SlcResourceStudioStorageProvider : IStorageProvider<Resource>
    {
        private readonly ICommunication communication;
        private readonly IStorageProvider<Resource> provider;

        public SlcResourceStudioStorageProvider(ICommunication communication)
        {
            this.communication = communication ?? throw new ArgumentNullException(nameof(communication));
            provider = new SlcResourceStudioDomStorageProvider(communication);
        }

        public Resource Create(Resource oToCreate)
        {
            return provider.Create(oToCreate);
        }

        public Resource Delete(Resource oToDelete)
        {
            return provider.Delete(oToDelete);
        }

        public IEnumerable<Resource> Read(FilterElement<Resource> filter)
        {
            return provider.Read(filter);
        }

        public Resource Update(Resource oToUpdate)
        {
            return provider.Update(oToUpdate);
        }
    }
}
