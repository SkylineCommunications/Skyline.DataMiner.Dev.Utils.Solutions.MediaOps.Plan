namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.ActivityHelper;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    using SLDataGateway.API.Types.Querying;

    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

    internal partial class ResourcePropertiesRepository : DomRepository<ResourceProperty>, IResourcePropertiesRepository
    {
        public ResourcePropertiesRepository(MediaOpsPlanApi planApi)
            : base(planApi)
        {
        }

        public long CountAll()
        {
            return PlanApi.DomHelpers.SlcResourceStudioHelper.DomHelper.DomInstances
                .Count(DomInstanceExposers.DomDefinitionId.Equal(StorageResourceStudio.SlcResource_StudioIds.Definitions.Resourceproperty.Id));
        }

        public IQueryable<ResourceProperty> Query()
        {
            return new ApiRepositoryQuery<ResourceProperty, DomInstance>(QueryProvider);
        }

        public ResourceProperty Read(Guid id)
        {
            PlanApi.Logger.LogInformation($"Reading Resource Property with ID: {id}...");

            if (id == Guid.Empty)
            {
                throw new ArgumentException(nameof(id));
            }

            return ActivityHelper.Track(nameof(ResourcePropertiesRepository), nameof(Read), act =>
            {
                act?.AddTag("ResourcePropertyId", id);
                var filter = DomInstanceExposers.DomDefinitionId.Equal(StorageResourceStudio.SlcResource_StudioIds.Definitions.Resourceproperty.Id)
                        .AND(DomInstanceExposers.Id.Equal(id));
                var domResourceProperty = PlanApi.DomHelpers.SlcResourceStudioHelper.GetResourceProperties(filter)
                    .FirstOrDefault();

                if (domResourceProperty == null)
                {
                    act?.AddTag("Hit", false);
                    return null;
                }

                act?.AddTag("Hit", true);

                return new ResourceProperty(domResourceProperty);
            });
        }

        public IDictionary<Guid, ResourceProperty> Read(IEnumerable<Guid> ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            return ActivityHelper.Track(nameof(ResourcePropertiesRepository), nameof(Read), act =>
            {
                act?.AddTag("ResourcePropertyIds", String.Join(", ", ids));
                act?.AddTag("ResourcePropertyIds Count", ids.Count());

                var properties = PlanApi.DomHelpers.SlcResourceStudioHelper.GetResourceProperties(ids);
                return properties.Select(x => new ResourceProperty(x)).ToDictionary(x => x.Id);
            });
        }

        public IEnumerable<ResourceProperty> ReadAll()
        {
            return ActivityHelper.Track(nameof(ResourcePropertiesRepository), nameof(ReadAll), act =>
            {
                var filter = DomInstanceExposers.DomDefinitionId.Equal(StorageResourceStudio.SlcResource_StudioIds.Definitions.Resource.Id);
                return PlanApi.DomHelpers.SlcResourceStudioHelper.GetResourceProperties(filter).Select(x => new ResourceProperty(x));
            });
        }

        public IEnumerable<IEnumerable<ResourceProperty>> ReadAllPaged()
        {
            return PlanApi.DomHelpers.SlcResourceStudioHelper.GetAllResourcePropertiesPaged()
                .Select(Page => Page.Select(x => new ResourceProperty(x)));
        }

        internal override long Count(FilterElement<DomInstance> domFilter)
        {
            return PlanApi.DomHelpers.SlcResourceStudioHelper.CountResourceStudioInstances(AddDomDefinitionFilter(domFilter, StorageResourceStudio.SlcResource_StudioIds.Definitions.Resourceproperty));
        }

        internal override IEnumerable<ResourceProperty> Read(IQuery<DomInstance> query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            var domFilter = AddDomDefinitionFilter(query.Filter, StorageResourceStudio.SlcResource_StudioIds.Definitions.Resourceproperty);

            query = query.WithFilter(domFilter);

            var domInstances = PlanApi.DomHelpers.SlcResourceStudioHelper.GetResourceProperties(query);

            return domInstances.Select(x => new ResourceProperty(x));
        }
    }
}
