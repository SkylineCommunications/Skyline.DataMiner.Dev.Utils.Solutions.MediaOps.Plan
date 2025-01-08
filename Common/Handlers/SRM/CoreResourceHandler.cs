namespace Skyline.DataMiner.MediaOps.API.Common.Handlers.SRM
{
    using System;
    using System.Collections.Generic;

    using DomHelpers.SlcResource_Studio;

    using Skyline.DataMiner.MediaOps.API.Common.Core.Components;
    using Skyline.DataMiner.MediaOps.API.Common.Providers;
    using Skyline.DataMiner.MediaOps.Common.SRM;

    internal class CoreResourceHandler
    {
        private readonly DataProviders dataProviders;

        public CoreResourceHandler(DataProviders dataProviders)
        {
            this.dataProviders = dataProviders ?? throw new ArgumentNullException(nameof(dataProviders));
        }

        public void CreateOrUpdate(ResourceInstance domResource)
        {
            if (domResource.ResourceInternalProperties.Resource_Id.HasValue
                && domResource.ResourceInternalProperties.Resource_Id.Value != Guid.Empty)
            {
                Update(domResource);
            }
            else
            {
                Create(domResource);
            }
        }

        private void Create(ResourceInstance domResource)
        {
            var existingResource = dataProviders.ResourceManagerHelper.GetResourceByName(domResource.ResourceInfo.Name);
            if (existingResource != null)
            {
                //TODO: replace by MediaOpsException
                throw new InvalidOperationException($"Resource '{domResource.ResourceInfo.Name}' ({existingResource.ID}) already exists.");
            }

            var coreResource = new Skyline.DataMiner.Net.Messages.Resource
            {
                Name = domResource.ResourceInfo.Name,
                MaxConcurrency = (int)domResource.ResourceInfo.Concurrency,
            };

            coreResource.Capabilities.Add(new Skyline.DataMiner.Net.SRM.Capabilities.ResourceCapability(Capabilities.ResourceType.Id)
            {
                Value = new Skyline.DataMiner.Net.Profiles.CapabilityParameterValue(new List<string> { "Unlinked Resource" }),
            });

            coreResource = dataProviders.ResourceManagerHelper.AddOrUpdateResources(coreResource)[0];
            domResource.ResourceInternalProperties.Resource_Id = coreResource.ID;
        }

        private void Update(ResourceInstance domResource)
        {
            throw new NotImplementedException();
        }
    }
}
