namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.MediaOps.Plan.ActivityHelper;
    using Skyline.DataMiner.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    using SLDataGateway.API.Types.Querying;

    using DomResourceProperty = Storage.DOM.SlcResource_Studio.ResourcepropertyInstance;
    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

    internal class ResourcePropertiesRepository : Repository<ResourceProperty>, IResourcePropertiesRepository
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

        public Guid Create(ResourceProperty apiObject)
        {
            PlanApi.Logger.LogInformation("Creating new ResourceProperty...");

            if (apiObject == null)
            {
                throw new ArgumentNullException(nameof(apiObject));
            }

            return ActivityHelper.Track(nameof(ResourcePropertiesRepository), nameof(Create), act =>
            {
                if (!apiObject.IsNew)
                {
                    throw new MediaOpsException("Not possible to use method Create for existing resource property. Use CreateOrUpdate or Update instead.");
                }

                if (!DomResourcePropertyHandler.TryCreateOrUpdate(PlanApi, [apiObject], out var result))
                    {
                    throw new MediaOpsException(result.TraceDataPerItem[apiObject.Id]);
                }

                var resourcePropertyId = result.SuccessfulIds.First();
                act.AddTag("ResourcePropertyId", resourcePropertyId);

                return resourcePropertyId;
            });
        }

        public IEnumerable<Guid> Create(IEnumerable<ResourceProperty> apiObjects)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Guid> CreateOrUpdate(IEnumerable<ResourceProperty> apiObjects)
        {
            throw new NotImplementedException();
        }

        public void Delete(params ResourceProperty[] apiObjects)
        {
            throw new NotImplementedException();
        }

        public void Delete(params Guid[] apiObjectIds)
        {
            throw new NotImplementedException();
        }

        public IQueryable<ResourceProperty> Query()
        {
            throw new NotImplementedException();
        }

        public IQueryable<IEnumerable<ResourceProperty>> QueryPaged()
        {
            throw new NotImplementedException();
        }

        public ResourceProperty Read(Guid id)
        {
            throw new NotImplementedException();
        }

        public IDictionary<Guid, ResourceProperty> Read(IEnumerable<Guid> ids)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ResourceProperty> ReadAll()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IEnumerable<ResourceProperty>> ReadAllPaged()
        {
            throw new NotImplementedException();
        }

        public void Update(ResourceProperty apiObject)
        {
            throw new NotImplementedException();
        }

        public void Update(IEnumerable<ResourceProperty> apiObjects)
        {
            throw new NotImplementedException();
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
