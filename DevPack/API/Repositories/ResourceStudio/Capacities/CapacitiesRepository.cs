namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.SDM;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.ActivityHelper;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

    using SLDataGateway.API.Types.Querying;

    /// <summary>
    /// Provides repository operations for managing <see cref="Capacity"/> objects.
    /// </summary>
    internal class CapacitiesRepository : Repository, ICapacitiesRepository
    {
        private readonly CapacityFilterTranslator filterTranslator = new CapacityFilterTranslator();

        /// <summary>
        /// Initializes a new instance of the <see cref="CapacitiesRepository"/> class.
        /// </summary>
        /// <param name="planApi">The MediaOps Plan API instance.</param>
        public CapacitiesRepository(MediaOpsPlanApi planApi) : base(planApi)
        {
        }

        /// <summary>
        /// Gets the total number of capacities in the repository.
        /// </summary>
        /// <returns>The total count of capacities.</returns>
        public long Count()
        {
            return Count(new TRUEFilterElement<Capacity>());
        }

        /// <summary>
        /// Gets the number of capacities that match the specified filter.
        /// </summary>
        /// <param name="filter">The filter criteria to apply when counting capacities.</param>
        /// <returns>The count of capacities matching the filter.</returns>
        public long Count(FilterElement<Capacity> filter)
        {
            return ActivityHelper.Track(nameof(CapacitiesRepository), nameof(Count), act =>
            {
                var paramFilter = filterTranslator.Translate(filter);
                return PlanApi.CoreHelpers.ProfileProvider.CountCapacities(paramFilter);
            });
        }

        /// <summary>
        /// Gets the number of capacities that match the specified query.
        /// </summary>
        /// <param name="query">The query criteria to apply when counting capacities.</param>
        /// <returns>The count of capacities matching the query.</returns>
        public long Count(IQuery<Capacity> query)
        {
            return Count(query.Filter);
        }

        /// <summary>
        /// Creates a new capacity in the repository.
        /// </summary>
        /// <param name="apiObject">The capacity to create.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObject"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when attempting to create an existing capacity.</exception>
        /// <exception cref="MediaOpsException">Thrown when the creation operation fails for the specified capacity.</exception>
        public void Create(Capacity apiObject)
        {
            PlanApi.Logger.LogInformation("Creating new Capacity...");

            if (apiObject == null)
            {
                throw new ArgumentNullException(nameof(apiObject));
            }

            ActivityHelper.Track(nameof(CapacitiesRepository), nameof(Create), act =>
            {
                if (!apiObject.IsNew)
                {
                    throw new InvalidOperationException("Not possible to use method Create for an existing capacity. Use CreateOrUpdate or Update instead.");
                }

                if (!CoreCapacityHandler.TryCreateOrUpdate(PlanApi, [apiObject], out var result))
                {
                    result.ThrowSingleException(apiObject.Id);
                }

                var capacityId = apiObject.Id;
                act?.AddTag("CapacityId", capacityId);
            });
        }

        /// <summary>
        /// Creates multiple new capacities in the repository.
        /// </summary>
        /// <param name="apiObjects">The collection of capacities to create.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when attempting to create existing capacities.</exception>
        /// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk creation operation fails for one or more capacities.</exception>
        public void Create(IEnumerable<Capacity> apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            ActivityHelper.Track(nameof(CapacitiesRepository), nameof(Create), act =>
            {
                if (apiObjects.Any(x => !x.IsNew))
                {
                    throw new InvalidOperationException("Not possible to use method Create for existing capacities. Use CreateOrUpdate or Update instead.");
                }

                if (!CoreCapacityHandler.TryCreateOrUpdate(PlanApi, apiObjects.ToList(), out var result))
                {
                    result.ThrowBulkException();
                }

                var capacityIds = apiObjects.Select(x => x.Id);
                act?.AddTag("CapacityIds", string.Join(", ", capacityIds));
            });
        }

        /// <summary>
        /// Creates new capacities or updates existing ones in the repository.
        /// </summary>
        /// <param name="apiObjects">The collection of capacities to create or update.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
        /// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk create or update operation fails for one or more capacities.</exception>
        public void CreateOrUpdate(IEnumerable<Capacity> apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            ActivityHelper.Track(nameof(CapacitiesRepository), nameof(CreateOrUpdate), act =>
            {
                if (!CoreCapacityHandler.TryCreateOrUpdate(PlanApi, apiObjects?.ToList(), out var result))
                {
                    result.ThrowBulkException();
                }

                var capacityIds = result.SuccessfulIds;
                act?.AddTag("Created or Updated Capacities", String.Join(", ", capacityIds));
                act?.AddTag("Created or Updated Capacities Count", capacityIds.Count);
            });
        }

        /// <summary>
        /// Deletes the specified capacities from the repository.
        /// </summary>
        /// <param name="apiObjects">The capacities to delete.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
        public void Delete(IEnumerable<Capacity> apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            Delete(apiObjects.Select(x => x.Id).ToArray());
        }

        /// <summary>
        /// Deletes capacities with the specified identifiers from the repository.
        /// </summary>
        /// <param name="apiObjectIds">The unique identifiers of the capacities to delete.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjectIds"/> is <c>null</c>.</exception>
        /// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk deletion operation fails for one or more capacities.</exception>
        public void Delete(IEnumerable<Guid> apiObjectIds)
        {
            if (apiObjectIds == null)
            {
                throw new ArgumentNullException(nameof(apiObjectIds));
            }

            var capacitiesToDelete = Read(apiObjectIds.ToArray());

            ActivityHelper.Track(nameof(CapacitiesRepository), nameof(Delete), act =>
            {
                if (!CoreCapacityHandler.TryDelete(PlanApi, capacitiesToDelete?.ToList(), out var result))
                {
                    result.ThrowBulkException();
                }

                var capacityIds = capacitiesToDelete.Select(x => x.Id);
                act?.AddTag("Removed Capacities", String.Join(", ", capacityIds));
                act?.AddTag("Removed Capacities Count", capacityIds.Count());
            });
        }

        /// <summary>
        /// Deletes the specified capacity from the repository.
        /// </summary>
        /// <param name="oToDelete">The capacity to delete.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="oToDelete"/> is <c>null</c>.</exception>
        /// <exception cref="MediaOpsException">Thrown when the deletion operation fails for the specified capacity.</exception>
        public void Delete(Capacity oToDelete)
        {
            if (oToDelete == null)
            {
                throw new ArgumentNullException(nameof(oToDelete));
            }

            Delete(oToDelete.Id);
        }

        /// <summary>
        /// Deletes the specified capacity from the repository.
        /// </summary>
        /// <param name="apiObjectId">The unique identifier of the capacity to delete.</param>
        /// <exception cref="MediaOpsException">Thrown when the deletion operation fails for the specified capacity.</exception>
        public void Delete(Guid apiObjectId)
        {
            var capacityToDelete = Read(apiObjectId);
            if (capacityToDelete == null)
            {
                return;
            }

            ActivityHelper.Track(nameof(CapacitiesRepository), nameof(Delete), act =>
            {
                if (!CoreCapacityHandler.TryDelete(PlanApi, [capacityToDelete], out var result))
                {
                    result.ThrowSingleException(apiObjectId);
                }

                var capacityId = result.SuccessfulIds.First();
                act?.AddTag("CapacityId", capacityId);
            });
        }

        /// <summary>
        /// Reads a single capacity by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the capacity.</param>
        /// <returns>The capacity with the specified identifier, or <c>null</c> if not found.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is <see cref="Guid.Empty"/>.</exception>
        public Capacity Read(Guid id)
        {
            PlanApi.Logger.LogInformation($"Reading Capacity with ID: {id}...");

            if (id == Guid.Empty)
            {
                throw new ArgumentException(nameof(id));
            }

            return Read(CapacityExposers.Id.Equal(id)).FirstOrDefault();
        }

        /// <summary>
        /// Reads multiple capacities by their unique identifiers.
        /// </summary>
        /// <param name="ids">A collection of unique identifiers.</param>
        /// <returns>An enumerable collection of capacities matching the specified identifiers.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="ids"/> is <c>null</c>.</exception>
        public IEnumerable<Capacity> Read(IEnumerable<Guid> ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            if (!ids.Any())
            {
                return Array.Empty<Capacity>();
            }

            return Read(new ORFilterElement<Capacity>(ids.Select(x => CapacityExposers.Id.Equal(x)).ToArray()));
        }

        /// <summary>
        /// Reads all capacities from the repository.
        /// </summary>
        /// <returns>An enumerable collection of all capacities.</returns>
        public IEnumerable<Capacity> Read()
        {
            return Read(new TRUEFilterElement<Capacity>());
        }

        /// <summary>
        /// Reads capacities that match the specified filter.
        /// </summary>
        /// <param name="filter">The filter criteria to apply when reading capacities.</param>
        /// <returns>An enumerable collection of capacities matching the filter.</returns>
        public IEnumerable<Capacity> Read(FilterElement<Capacity> filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            return ActivityHelper.Track(nameof(CapacitiesRepository), nameof(Update), act =>
            {
                var coreCapacities = PlanApi.CoreHelpers.ProfileProvider.GetCapacities(filterTranslator.Translate(filter));
                return Capacity.InstantiateCapacities(coreCapacities);
            });
        }

        /// <summary>
        /// Reads capacities that match the specified query.
        /// </summary>
        /// <param name="query">The query criteria to apply when reading capacities.</param>
        /// <returns>An enumerable collection of capacities matching the query.</returns>
        public IEnumerable<Capacity> Read(IQuery<Capacity> query)
        {
            return Read(query.Filter);
        }

        /// <summary>
        /// Reads all capacities in pages.
        /// </summary>
        /// <returns>An enumerable collection of pages, where each page contains a collection of capacities.</returns>
        public IEnumerable<IPagedResult<Capacity>> ReadPaged()
        {
            return ReadPaged(new TRUEFilterElement<Capacity>());
        }

        /// <summary>
        /// Reads capacities that match the specified filter in pages.
        /// </summary>
        /// <param name="filter">The filter criteria to apply when reading capacities.</param>
        /// <returns>An enumerable collection of pages, where each page contains capacities matching the filter.</returns>
        public IEnumerable<IPagedResult<Capacity>> ReadPaged(FilterElement<Capacity> filter)
        {
            return ReadPaged(filter, MediaOpsPlanApi.DefaultPageSize);
        }

        /// <summary>
        /// Reads capacities that match the specified query in pages.
        /// </summary>
        /// <param name="query">The query criteria to apply when reading capacities.</param>
        /// <returns>An enumerable collection of pages, where each page contains capacities matching the query.</returns>
        public IEnumerable<IPagedResult<Capacity>> ReadPaged(IQuery<Capacity> query)
        {
            return ReadPaged(query.Filter);
        }

        /// <summary>
        /// Reads capacities that match the specified filter in pages with a custom page size.
        /// </summary>
        /// <param name="filter">The filter criteria to apply when reading capacities.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>An enumerable collection of pages, where each page contains up to the specified number of capacities matching the filter.</returns>
        public IEnumerable<IPagedResult<Capacity>> ReadPaged(FilterElement<Capacity> filter, int pageSize)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            if (pageSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");
            }

            return ReadPagedIterator(filter, pageSize);
        }

        /// <summary>
        /// Reads capacities that match the specified query in pages with a custom page size.
        /// </summary>
        /// <param name="query">The query criteria to apply when reading capacities.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>An enumerable collection of pages, where each page contains up to the specified number of capacities matching the query.</returns>
        public IEnumerable<IPagedResult<Capacity>> ReadPaged(IQuery<Capacity> query, int pageSize)
        {
            return ReadPaged(query.Filter, pageSize);
        }

        /// <summary>
        /// Reads all capacities in pages.
        /// </summary>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>An enumerable collection of pages, where each page contains a collection of capacities.</returns>
        public IEnumerable<IPagedResult<Capacity>> ReadPaged(int pageSize)
        {
            return ReadPaged(new TRUEFilterElement<Capacity>(), MediaOpsPlanApi.DefaultPageSize);
        }

        public IEnumerable<IPagedResult<Capacity>> ReadPagedIterator(FilterElement<Capacity> filter, int pageSize)
        {
            var pageNumber = 0;
            var paramFilter = filterTranslator.Translate(filter);
            var items = PlanApi.CoreHelpers.ProfileProvider.GetCapacitiesPaged(paramFilter, pageSize);
            var enumerator = items.GetEnumerator();
            var hasNext = enumerator.MoveNext();

            while (hasNext)
            {
                var page = enumerator.Current;
                hasNext = enumerator.MoveNext();
                yield return new PagedResult<Capacity>(Capacity.InstantiateCapacities(page), pageNumber++, pageSize, hasNext);
            }
        }
        /// <summary>
        /// Updates an existing capacity in the repository.
        /// </summary>
        /// <param name="apiObject">The capacity to update.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObject"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when attempting to update a new capacity that doesn't exist yet.</exception>
        /// <exception cref="MediaOpsException">Thrown when the update operation fails for the specified capacity.</exception>
        public void Update(Capacity apiObject)
        {
            if (apiObject == null)
            {
                throw new ArgumentNullException(nameof(apiObject));
            }

            PlanApi.Logger.LogInformation($"Updating existing capacity {apiObject.Name}...");

            ActivityHelper.Track(nameof(CapacitiesRepository), nameof(Update), act =>
            {
                if (apiObject.IsNew)
                {
                    throw new InvalidOperationException("Not possible to use method Update for new capacity. Use Create or CreateOrUpdate instead.");
                }

                if (!CoreCapacityHandler.TryCreateOrUpdate(PlanApi, [apiObject], out var result))
                {
                    result.ThrowSingleException(apiObject.Id);
                }

                var capacityId = apiObject.Id;
                act?.AddTag("CapacityId", capacityId);
            });
        }

        /// <summary>
        /// Updates multiple existing capacities in the repository.
        /// </summary>
        /// <param name="apiObjects">The collection of capacities to update.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when attempting to update new capacities that don't exist yet.</exception>
        /// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk update operation fails for one or more capacities.</exception>
        public void Update(IEnumerable<Capacity> apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            ActivityHelper.Track(nameof(CapacitiesRepository), nameof(Update), act =>
            {
                if (apiObjects.Any(x => x.IsNew))
                {
                    throw new InvalidOperationException("Not possible to use method Update for new capacities. Use Create or CreateOrUpdate instead.");
                }

                if (!CoreCapacityHandler.TryCreateOrUpdate(PlanApi, apiObjects.ToList(), out var result))
                {
                    result.ThrowBulkException();
                }

                var capacityIds = apiObjects.Select(x => x.Id);
                act?.AddTag("CapacityIds", String.Join(", ", capacityIds));
            });
        }
    }
}
