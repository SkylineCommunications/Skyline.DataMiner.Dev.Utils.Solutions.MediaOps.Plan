namespace Skyline.DataMiner.MediaOps.API.Common.ResourceStudio
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using DomHelpers.SlcResource_Studio;
    using Skyline.DataMiner.Core.DataMinerSystem.Common;
    using Skyline.DataMiner.MediaOps.API.Common.API;
    using Skyline.DataMiner.MediaOps.API.Common.Handlers.ResourceStudio;
    using Skyline.DataMiner.MediaOps.API.Common.Storage.DOM.ResourceStudio;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;

    internal class DomResourceStudioHelper : IResourceStudioHelper
    {
        #region Fields
        private readonly MediaOpsPlanApi _helper;
        private readonly ResourceStudioProvider _resourceStudioDomHelper;

        private Cache cache;

        private ClientMetadata clientMetadata;
        #endregion

        internal DomResourceStudioHelper(MediaOpsPlanApi helpers)
        {
            _helper = helpers ?? throw new ArgumentNullException(nameof(helpers));
            var domHelper = new DomHelper(helpers.Communication.Connection.HandleMessages, SlcResource_StudioIds.ModuleId);
            _resourceStudioDomHelper = new ResourceStudioProvider(domHelper);

            Init();
        }

        #region Methods
        public void SetClientMetadata(ClientMetadata clientMetadata)
        {
            clientMetadata = clientMetadata ?? throw new ArgumentNullException(nameof(clientMetadata));
        }

        public IResource GetResource(Guid id)
        {
            if (id == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(id));
            }

            var resourcesById = cache.GetResourcesById(new List<Guid> { id });
            if (!resourcesById.TryGetValue(id, out var resource))
            {
                return null;
            }

            return resource;
        }

        public IResource GetResource(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            var resourcesByName = cache.GetResourcesByName(new List<string> { name });
            if (!resourcesByName.TryGetValue(name, out var resource))
            {
                return null;
            }

            return resource;
        }

        public IEnumerable<IResource> GetAllResources()
        {
            return _resourceStudioDomHelper.GetAllResources()
                .Select(x => new Resource(x));
        }

        public long GetResourcesCount()
        {
            return _resourceStudioDomHelper.CountAllResources();
        }

        public Guid CreateResource(ResourceConfiguration configuration, ObjectMetadata objectMetadata)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (objectMetadata == null)
            {
                throw new ArgumentNullException(nameof(objectMetadata));
            }

            var handler = new CreateResourceHandler(_helper.DataProviders, clientMetadata, configuration, objectMetadata);
            return handler.Execute();
        }

        public Guid CreateElementResource(ElementResourceConfiguration configuration, ObjectMetadata objectMetadata)
        {
            throw new NotImplementedException();
        }

        public Guid CreateServiceResource(ServiceResourceConfiguration configuration, ObjectMetadata objectMetadata)
        {
            throw new NotImplementedException();
        }

        private void Init()
        {
            clientMetadata = new ClientMetadata();
            cache = new Cache(_helper.DataProviders);
        }
        #endregion
    }
}
