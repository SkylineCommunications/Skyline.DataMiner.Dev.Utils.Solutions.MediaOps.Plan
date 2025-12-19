namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.ActivityHelper;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

    using SLDataGateway.API.Types.Querying;

    /// <summary>
    /// Provides repository operations for managing <see cref="ResourceProperty"/> objects.
    /// </summary>
    internal class ResourcePropertiesRepository : Repository, IResourcePropertiesRepository
    {
        private readonly ResourcePropertyFilterTranslator filterTranslator = new ResourcePropertyFilterTranslator();

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourcePropertiesRepository"/> class.
        /// </summary>
        /// <param name="planApi">The MediaOps Plan API instance.</param>
        public ResourcePropertiesRepository(MediaOpsPlanApi planApi)
            : base(planApi)
        {
        }

        /// <summary>
        /// Gets the total number of resource properties in the repository.
        /// </summary>
        /// <returns>The total count of resource properties.</returns>
        public long Count()
        {
            return Count(new TRUEFilterElement<ResourceProperty>());
        }

        /// <summary>
        /// Gets the number of resource properties that match the specified filter.
        /// </summary>
        /// <param name="filter">The filter criteria to apply when counting resource properties.</param>
        /// <returns>The count of resource properties matching the filter.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public long Count(FilterElement<ResourceProperty> filter)
        {
            return PlanApi.DomHelpers.SlcResourceStudioHelper.CountResourceStudioInstances(filterTranslator.Translate(filter));
        }

        /// <summary>
        /// Gets the number of resource properties that match the specified query.
        /// </summary>
        /// <param name="query">The query criteria to apply when counting resource properties.</param>
        /// <returns>The count of resource properties matching the query.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public long Count(IQuery<ResourceProperty> query)
        {
            return Count(query.Filter);
        }

        /// <summary>
        /// Creates a new resource property in the repository.
        /// </summary>
        /// <param name="apiObject">The resource property to create.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObject"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when attempting to create an existing resource property.</exception>
        /// <exception cref="MediaOpsException">Thrown when the creation operation fails.</exception>
        public void Create(ResourceProperty apiObject)
        {
            PlanApi.Logger.LogInformation("Creating new ResourceProperty...");

            if (apiObject == null)
            {
                throw new ArgumentNullException(nameof(apiObject));
            }

            ActivityHelper.Track(nameof(ResourcePropertiesRepository), nameof(Create), act =>
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
            });
        }

        /// <summary>
        /// Creates multiple new resource properties in the repository.
        /// </summary>
        /// <param name="apiObjects">The collection of resource properties to create.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when attempting to create existing resource properties.</exception>
        /// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk creation operation fails.</exception>
        public void Create(IEnumerable<ResourceProperty> apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            ActivityHelper.Track(nameof(ResourcePropertiesRepository), nameof(Create), act =>
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
            });
        }

        /// <summary>
        /// Creates new resource properties or updates existing ones in the repository.
        /// </summary>
        /// <param name="apiObjects">The collection of resource properties to create or update.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
        /// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk operation fails.</exception>
        public void CreateOrUpdate(IEnumerable<ResourceProperty> apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            ActivityHelper.Track(nameof(ResourcePropertiesRepository), nameof(CreateOrUpdate), act =>
            {
                if (!DomResourcePropertyHandler.TryCreateOrUpdate(PlanApi, apiObjects, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var propertyIds = result.SuccessfulIds;
                act?.AddTag("Created or Updated Resource Properties", String.Join(", ", propertyIds));
                act?.AddTag("Created or Updated Resource Properties Count", propertyIds.Count);
            });
        }

        /// <summary>
        /// Deletes the specified resource properties from the repository.
        /// </summary>
        /// <param name="apiObjects">The resource properties to delete.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
        public void Delete(params ResourceProperty[] apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            Delete(apiObjects.Select(x => x.Id).ToArray());
        }

        /// <summary>
        /// Deletes resource properties with the specified identifiers from the repository.
        /// </summary>
        /// <param name="apiObjectIds">The unique identifiers of the resource properties to delete.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjectIds"/> is <c>null</c>.</exception>
        /// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk deletion operation fails.</exception>
        public void Delete(params Guid[] apiObjectIds)
        {
            if (apiObjectIds == null)
            {
                throw new ArgumentNullException(nameof(apiObjectIds));
            }

            var propertiesToDelete = Read(apiObjectIds);

            ActivityHelper.Track(nameof(ResourcePropertiesRepository), nameof(Delete), act =>
            {
                if (!DomResourcePropertyHandler.TryDelete(PlanApi, propertiesToDelete, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var propertyIds = result.SuccessfulIds;
                act?.AddTag("Removed Resource Properties", String.Join(", ", propertyIds));
                act?.AddTag("Removed Resource Properties Count", propertyIds.Count);
            });
        }

        /// <summary>
        /// Reads a single resource property by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the resource property.</param>
        /// <returns>The resource property with the specified identifier, or <c>null</c> if not found.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is <see cref="Guid.Empty"/>.</exception>
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
                var property = Read(ResourcePropertyExposers.Id.Equal(id)).FirstOrDefault();

                if (property == null)
                {
                    act?.AddTag("Hit", false);
                    return null;
                }

                act?.AddTag("Hit", true);

                return property;
            });
        }

        /// <summary>
        /// Reads multiple resource properties by their unique identifiers.
        /// </summary>
        /// <param name="ids">A collection of unique identifiers.</param>
        /// <returns>An enumerable collection of resource properties matching the specified identifiers.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="ids"/> is <c>null</c>.</exception>
        public IEnumerable<ResourceProperty> Read(IEnumerable<Guid> ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            return Read(new ORFilterElement<ResourceProperty>(ids.Select(x => ResourcePropertyExposers.Id.Equal(x)).ToArray()));
        }

        /// <summary>
        /// Reads all resource properties from the repository.
        /// </summary>
        /// <returns>An enumerable collection of all resource properties.</returns>
        public IEnumerable<ResourceProperty> Read()
        {
            return ActivityHelper.Track(nameof(ResourcePropertiesRepository), nameof(Read), act =>
            {
                return Read(new TRUEFilterElement<ResourceProperty>());
            });
        }

        /// <summary>
        /// Reads resource properties that match the specified filter.
        /// </summary>
        /// <param name="filter">The filter criteria to apply when reading resource properties.</param>
        /// <returns>An enumerable collection of resource properties matching the filter.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public IEnumerable<ResourceProperty> Read(FilterElement<ResourceProperty> filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            return ActivityHelper.Track(nameof(ResourcePropertiesRepository), nameof(Read), act =>
            {
                var properties = PlanApi.DomHelpers.SlcResourceStudioHelper.GetResourceProperties(filterTranslator.Translate(filter));
                return properties.Select(x => new ResourceProperty(x));
            });
        }

        /// <summary>
        /// Reads resource properties that match the specified query.
        /// </summary>
        /// <param name="query">The query criteria to apply when reading resource properties.</param>
        /// <returns>An enumerable collection of resource properties matching the query.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public IEnumerable<ResourceProperty> Read(IQuery<ResourceProperty> query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            return ActivityHelper.Track(nameof(ResourcePropertiesRepository), nameof(Read), act =>
            {
                return Read(query.Filter);
            });
        }

        /// <summary>
        /// Reads all resource properties in pages.
        /// </summary>
        /// <returns>An enumerable collection of pages, where each page contains a collection of resource properties.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        IEnumerable<IPagedResult<ResourceProperty>> IPageableRepository<ResourceProperty>.ReadPaged()
        {
            return ReadPaged(new TRUEFilterElement<ResourceProperty>());
        }

        /// <summary>
        /// Reads resource properties that match the specified filter in pages.
        /// </summary>
        /// <param name="filter">The filter criteria to apply when reading resource properties.</param>
        /// <returns>An enumerable collection of pages, where each page contains resource properties matching the filter.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public IEnumerable<IPagedResult<ResourceProperty>> ReadPaged(FilterElement<ResourceProperty> filter)
        {
            return ReadPaged(filter, MediaOpsPlanApi.DefaultPageSize);
        }

        /// <summary>
        /// Reads resource properties that match the specified query in pages.
        /// </summary>
        /// <param name="query">The query criteria to apply when reading resource properties.</param>
        /// <returns>An enumerable collection of pages, where each page contains resource properties matching the query.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public IEnumerable<IPagedResult<ResourceProperty>> ReadPaged(IQuery<ResourceProperty> query)
        {
            return ReadPaged(query.Filter);
        }

        /// <summary>
        /// Reads resource properties that match the specified filter in pages with a custom page size.
        /// </summary>
        /// <param name="filter">The filter criteria to apply when reading resource properties.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>An enumerable collection of pages, where each page contains up to the specified number of resource properties matching the filter.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public IEnumerable<IPagedResult<ResourceProperty>> ReadPaged(FilterElement<ResourceProperty> filter, int pageSize)
        {
            var pageNumber = 0;
            var paramFilter = filterTranslator.Translate(filter);
            var items = PlanApi.DomHelpers.SlcResourceStudioHelper.GetResourcePropertiesPaged(paramFilter, pageSize);
            var enumerator = items.GetEnumerator();
            var hasNext = enumerator.MoveNext();

            while (hasNext)
            {
                var page = enumerator.Current;
                hasNext = enumerator.MoveNext();
                yield return new PagedResult<ResourceProperty>(page.Select(x => new ResourceProperty(x)), pageNumber++, pageSize, hasNext);
            }
        }

        /// <summary>
        /// Reads resource properties that match the specified query in pages with a custom page size.
        /// </summary>
        /// <param name="query">The query criteria to apply when reading resource properties.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>An enumerable collection of pages, where each page contains up to the specified number of resource properties matching the query.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public IEnumerable<IPagedResult<ResourceProperty>> ReadPaged(IQuery<ResourceProperty> query, int pageSize)
        {
            return ReadPaged(query.Filter, pageSize);
        }

        /// <summary>
        /// Updates an existing resource property in the repository.
        /// </summary>
        /// <param name="apiObject">The resource property to update.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObject"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when attempting to update a new resource property that doesn't exist yet.</exception>
        /// <exception cref="MediaOpsException">Thrown when the update operation fails.</exception>
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

        /// <summary>
        /// Updates multiple existing resource properties in the repository.
        /// </summary>
        /// <param name="apiObjects">The collection of resource properties to update.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when attempting to update new resource properties that don't exist yet.</exception>
        /// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk update operation fails.</exception>
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
