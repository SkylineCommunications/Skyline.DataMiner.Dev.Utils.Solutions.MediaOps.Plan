namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.MediaOps.Plan.ActivityHelper;
    using Skyline.DataMiner.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    using SLDataGateway.API.Types.Querying;

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
                    throw new InvalidOperationException("Not possible to use method Create for existing resource property. Use CreateOrUpdate or Update instead.");
                }

                if (!DomResourcePropertyHandler.TryCreateOrUpdate(PlanApi, [apiObject], out var result))
                {
                    throw new MediaOpsException(result.TraceDataPerItem[apiObject.Id]);
                }

                var resourcePropertyId = result.SuccessfulIds.First();
                act?.AddTag("ResourcePropertyId", resourcePropertyId);

                return resourcePropertyId;
            });
        }

        public IEnumerable<Guid> Create(IEnumerable<ResourceProperty> apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            return ActivityHelper.Track(nameof(ResourcePropertiesRepository), nameof(Create), act =>
            {
                var existingProperties = apiObjects.Where(x => !x.IsNew);
                if (existingProperties.Any())
                {
                    throw new InvalidOperationException("Not possible to use method Create for existing resource properties. Use CreateOrUpdate or Update instead.");
                }

                if (!DomResourcePropertyHandler.TryCreateOrUpdate(PlanApi, apiObjects, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var propertyIds = result.SuccessfulIds;
                act?.AddTag("ResourcePropertyIds", string.Join(", ", propertyIds));

                return propertyIds;
            });
        }

        public IEnumerable<Guid> CreateOrUpdate(IEnumerable<ResourceProperty> apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            return ActivityHelper.Track(nameof(ResourcePropertiesRepository), nameof(CreateOrUpdate), act =>
            {
                if (!DomResourcePropertyHandler.TryCreateOrUpdate(PlanApi, apiObjects, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var propertyIds = result.SuccessfulIds;
                act?.AddTag("Created or Updated Resource Properties", String.Join(", ", propertyIds));
                act?.AddTag("Created or Updated Resource Properties Count", propertyIds.Count);

                return propertyIds;
            });
        }

        public void Delete(params ResourceProperty[] apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            Delete(apiObjects.Select(x => x.Id).ToArray());

            ActivityHelper.Track(nameof(ResourcePropertiesRepository), nameof(Delete), act =>
            {
                if (!DomResourcePropertyHandler.TryDelete(PlanApi, apiObjects, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var propertyIds = result.SuccessfulIds;
                act?.AddTag("Removed Resource Properties", String.Join(", ", propertyIds));
                act?.AddTag("Removed Resource Properties Count", propertyIds.Count);
            });
        }

        public void Delete(params Guid[] apiObjectIds)
        {
            if (apiObjectIds == null)
            {
                throw new ArgumentNullException(nameof(apiObjectIds));
            }

            var propertiesToDelete = Read(apiObjectIds).Values;

            Delete(propertiesToDelete.ToArray());
        }

        public IQueryable<ResourceProperty> Query()
        {
            return new ApiRepositoryQuery<ResourceProperty>(QueryProvider);
        }

        public IQueryable<IEnumerable<ResourceProperty>> QueryPaged()
        {
            throw new NotImplementedException();
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

        public void Update(ResourceProperty apiObject)
        {
            if (apiObject == null)
            {
                throw new ArgumentNullException(nameof(apiObject));
            }

            PlanApi.Logger.LogInformation($"Updating existing Resource Property {apiObject.Name}...");

            ActivityHelper.Track(nameof(ResourcePropertiesRepository), nameof(Update), act =>
            {
                if (apiObject.IsNew)
                {
                    throw new InvalidOperationException("Not possible to use method Update for new resource property. Use Create or CreateOrUpdate instead.");
                }

                if (!DomResourcePropertyHandler.TryCreateOrUpdate(PlanApi, [apiObject], out var result))
                {
                    throw new MediaOpsException(result.TraceDataPerItem[apiObject.Id]);
                }

                var resourceId = result.SuccessfulIds.First();
                act?.AddTag("ResourcePropertyId", resourceId);
            });
        }

        public void Update(IEnumerable<ResourceProperty> apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            ActivityHelper.Track(nameof(ResourcePropertiesRepository), nameof(Update), act =>
            {
                var newProperties = apiObjects.Where(x => x.IsNew);
                if (newProperties.Any())
                {
                    throw new InvalidOperationException("Not possible to use method Update for new resource properties. Use Create or CreateOrUpdate instead.");
                }

                if (!DomResourcePropertyHandler.TryCreateOrUpdate(PlanApi, apiObjects, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var resourceIds = result.SuccessfulIds;
                act?.AddTag("ResourcePropertyIds", String.Join(", ", resourceIds));
            });
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
