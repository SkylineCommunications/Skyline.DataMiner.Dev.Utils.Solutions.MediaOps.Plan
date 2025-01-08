namespace Skyline.DataMiner.MediaOps.API.Common.Handlers.ResourceStudio
{
    using System;

    using DomHelpers.SlcResource_Studio;

    using Skyline.DataMiner.MediaOps.API.Common.Handlers.SRM;
    using Skyline.DataMiner.MediaOps.API.Common.Providers;
    using Skyline.DataMiner.MediaOps.API.Common.ResourceStudio;

    internal class CreateResourceHandler
    {
        private readonly DataProviders dataProviders;
        private readonly ClientMetadata clientMetadata;
        private readonly ResourceConfiguration configuration;
        private readonly ObjectMetadata objectMetadata;

        private Lazy<SlcResource_StudioIds.Enums.Type> lazyResourceType;

        public CreateResourceHandler(DataProviders dataProviders, ClientMetadata clientMetadata, ResourceConfiguration configuration, ObjectMetadata objectMetadata)
        {
            this.dataProviders = dataProviders ?? throw new ArgumentNullException(nameof(dataProviders));
            this.clientMetadata = clientMetadata ?? throw new ArgumentNullException(nameof(clientMetadata));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            this.objectMetadata = objectMetadata ?? throw new ArgumentNullException(nameof(objectMetadata));

            Init();
        }

        private SlcResource_StudioIds.Enums.Type ResourceType => lazyResourceType.Value;

        public Guid Execute()
        {
            var domResource = BuildBasicDomResource();

            if (configuration.DesiredStatus == DesiredStatus.Complete)
            {
                var handler = new CoreResourceHandler(dataProviders);
                handler.CreateOrUpdate(domResource);
            }

            domResource.Save(dataProviders.ResourceStudioProvider.DomHelper);
            return domResource.ID.Id;
        }

        private static string TranslateResourceStatus(DesiredStatus resourceStatus)
        {
            if (resourceStatus == DesiredStatus.Complete)
            {
                return SlcResource_StudioIds.Behaviors.Resource_Behavior.Statuses.Complete;
            }

            return SlcResource_StudioIds.Behaviors.Resource_Behavior.Statuses.Draft;
        }

        private void Init()
        {
            lazyResourceType = new Lazy<SlcResource_StudioIds.Enums.Type>(() => DetermineResourceType());
        }

        private SlcResource_StudioIds.Enums.Type DetermineResourceType()
        {
            if (configuration is ServiceResourceConfiguration)
            {
                return SlcResource_StudioIds.Enums.Type.Service;
            }

            if (configuration is ElementResourceConfiguration)
            {
                return SlcResource_StudioIds.Enums.Type.Element;
            }

            return SlcResource_StudioIds.Enums.Type.Unmanaged;
        }

        private ResourceInstance BuildBasicDomResource()
        {
            //Todo: add validation

            var domResource = new ResourceInstance(new Net.Apps.DataMinerObjectModel.DomInstance()
            {
                // work around since it's not possible to define the status upfront
                StatusId = TranslateResourceStatus(configuration.DesiredStatus),
                DomDefinitionId = new Net.Apps.DataMinerObjectModel.DomDefinitionId(SlcResource_StudioIds.Definitions.Resource.Id),
            })
            {
                ResourceInfo = new ResourceInfoSection
                {
                    Name = configuration.Name,
                    Concurrency = configuration.Concurrency,
                    Favorite = configuration.IsFavorite,
                    Type = ResourceType,
                },
                ResourceInternalProperties = new ResourceInternalPropertiesSection(),
            };

            return domResource;
        }
    }
}
