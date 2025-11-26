namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.ActivityHelper;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

    internal partial class ResourcePropertiesRepository : DomRepository<ResourceProperty>, IResourcePropertiesRepository
    {
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
    }
}
