namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.ActivityHelper;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

    using SLDataGateway.API.Types.Querying;

    /// <summary>
    /// Provides repository operations for managing <see cref="Capacity"/> objects.
    /// </summary>
    internal class CapacitiesRepository : Repository, ICapacitiesRepository
    {
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
            return PlanApi.CoreHelpers.ProfileProvider.CountAllCapacities();
        }

        /// <summary>
        /// Gets the number of capacities that match the specified filter.
        /// </summary>
        /// <param name="filter">The filter criteria to apply when counting capacities.</param>
        /// <returns>The count of capacities matching the filter.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public long Count(FilterElement<Capacity> filter)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the number of capacities that match the specified query.
        /// </summary>
        /// <param name="query">The query criteria to apply when counting capacities.</param>
        /// <returns>The count of capacities matching the query.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public long Count(IQuery<Capacity> query)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a new capacity in the repository.
        /// </summary>
        /// <param name="apiObject">The capacity to create.</param>
        /// <returns>The unique identifier of the created capacity.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObject"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when attempting to create an existing capacity.</exception>
        /// <exception cref="MediaOpsException">Thrown when the creation operation fails.</exception>
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
                    throw new MediaOpsException(result.TraceDataPerItem[apiObject.Id]);
                }

                var capacityId = apiObject.Id;
                act?.AddTag("CapacityId", capacityId);
            });
        }

        /// <summary>
        /// Creates multiple new capacities in the repository.
        /// </summary>
        /// <param name="apiObjects">The collection of capacities to create.</param>
        /// <returns>A collection of unique identifiers for the created capacities.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when attempting to create existing capacities.</exception>
        /// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk creation operation fails.</exception>
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

                if (!CoreCapacityHandler.TryCreateOrUpdate(PlanApi, apiObjects, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var capacityIds = apiObjects.Select(x => x.Id);
                act?.AddTag("CapacityIds", string.Join(", ", capacityIds));
            });
        }

        /// <summary>
        /// Creates new capacities or updates existing ones in the repository.
        /// </summary>
        /// <param name="apiObjects">The collection of capacities to create or update.</param>
        /// <returns>A collection of unique identifiers for the created or updated capacities.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
        /// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk operation fails.</exception>
        public void CreateOrUpdate(IEnumerable<Capacity> apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            ActivityHelper.Track(nameof(CapacitiesRepository), nameof(CreateOrUpdate), act =>
            {
                if (!CoreCapacityHandler.TryCreateOrUpdate(PlanApi, apiObjects, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var capacityIds = apiObjects.Select(x => x.Id);
                act?.AddTag("Created or Updated Capacities", String.Join(", ", capacityIds));
                act?.AddTag("Created or Updated Capacities Count", capacityIds.Count());
            });
        }

        /// <summary>
        /// Deletes the specified capacities from the repository.
        /// </summary>
        /// <param name="apiObjects">The capacities to delete.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
        public void Delete(params Capacity[] apiObjects)
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
        /// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk deletion operation fails.</exception>
        public void Delete(params Guid[] apiObjectIds)
        {
            if (apiObjectIds == null)
            {
                throw new ArgumentNullException(nameof(apiObjectIds));
            }

            var capacitiesToDelete = Read(apiObjectIds);

            ActivityHelper.Track(nameof(CapacitiesRepository), nameof(Delete), act =>
            {
                if (!CoreCapacityHandler.TryDelete(PlanApi, capacitiesToDelete, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var capacityIds = apiObjectIds;
                act?.AddTag("Removed Capacities", String.Join(", ", capacityIds));
                act?.AddTag("Removed Capacities Count", capacityIds.Count());
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

            return ActivityHelper.Track(nameof(CapacitiesRepository), nameof(Read), act =>
            {
                act?.AddTag("CapacityId", id);
                var coreCapacity = PlanApi.CoreHelpers.ProfileProvider.GetCapacityById(id);

                if (coreCapacity == null)
                {
                    act?.AddTag("Hit", false);
                    return null;
                }

                act?.AddTag("Hit", true);
                return Capacity.InstantiateCapacities([coreCapacity]).FirstOrDefault();
            });
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

            return ActivityHelper.Track(nameof(CapacitiesRepository), nameof(Read), act =>
            {
                act?.AddTag("CapacityIds", String.Join(", ", ids));
                act?.AddTag("CapacityIds Count", ids.Count());

                var capacities = PlanApi.CoreHelpers.ProfileProvider.GetCapacitiesById(ids);
                return Capacity.InstantiateCapacities(capacities);
            });
        }

        /// <summary>
        /// Reads all capacities from the repository.
        /// </summary>
        /// <returns>An enumerable collection of all capacities.</returns>
        public IEnumerable<Capacity> Read()
        {
            return ActivityHelper.Track(nameof(CapacitiesRepository), nameof(Read), act =>
            {
                return Capacity.InstantiateCapacities(PlanApi.CoreHelpers.ProfileProvider.GetAllCapacities());
            });
        }

        /// <summary>
        /// Reads capacities that match the specified filter.
        /// </summary>
        /// <param name="filter">The filter criteria to apply when reading capacities.</param>
        /// <returns>An enumerable collection of capacities matching the filter.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public IEnumerable<Capacity> Read(FilterElement<Capacity> filter)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reads capacities that match the specified query.
        /// </summary>
        /// <param name="query">The query criteria to apply when reading capacities.</param>
        /// <returns>An enumerable collection of capacities matching the query.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public IEnumerable<Capacity> Read(IQuery<Capacity> query)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reads all capacities in pages.
        /// </summary>
        /// <returns>An enumerable collection of pages, where each page contains a collection of capacities.</returns>
        public IEnumerable<IEnumerable<Capacity>> ReadPaged()
        {
            return ActivityHelper.Track(nameof(CapacitiesRepository), nameof(ReadPaged), act =>
            {
                return PlanApi.CoreHelpers.ProfileProvider.GetAllCapacitiesPaged().Select(page => Capacity.InstantiateCapacities(page));
            });
        }

        /// <summary>
        /// Reads all capacities in pages.
        /// </summary>
        /// <returns>An enumerable collection of pages, where each page contains a collection of capacities.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        IEnumerable<IPagedResult<Capacity>> IPageableRepository<Capacity>.ReadPaged()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reads capacities that match the specified filter in pages.
        /// </summary>
        /// <param name="filter">The filter criteria to apply when reading capacities.</param>
        /// <returns>An enumerable collection of pages, where each page contains capacities matching the filter.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public IEnumerable<IPagedResult<Capacity>> ReadPaged(FilterElement<Capacity> filter)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reads capacities that match the specified query in pages.
        /// </summary>
        /// <param name="query">The query criteria to apply when reading capacities.</param>
        /// <returns>An enumerable collection of pages, where each page contains capacities matching the query.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public IEnumerable<IPagedResult<Capacity>> ReadPaged(IQuery<Capacity> query)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reads capacities that match the specified filter in pages with a custom page size.
        /// </summary>
        /// <param name="filter">The filter criteria to apply when reading capacities.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>An enumerable collection of pages, where each page contains up to the specified number of capacities matching the filter.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public IEnumerable<IPagedResult<Capacity>> ReadPaged(FilterElement<Capacity> filter, int pageSize)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reads capacities that match the specified query in pages with a custom page size.
        /// </summary>
        /// <param name="query">The query criteria to apply when reading capacities.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>An enumerable collection of pages, where each page contains up to the specified number of capacities matching the query.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public IEnumerable<IPagedResult<Capacity>> ReadPaged(IQuery<Capacity> query, int pageSize)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Updates an existing capacity in the repository.
        /// </summary>
        /// <param name="apiObject">The capacity to update.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObject"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when attempting to update a new capacity that doesn't exist yet.</exception>
        /// <exception cref="MediaOpsException">Thrown when the update operation fails.</exception>
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
                    throw new MediaOpsException(result.TraceDataPerItem[apiObject.Id]);
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
        /// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk update operation fails.</exception>
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

                if (!CoreCapacityHandler.TryCreateOrUpdate(PlanApi, apiObjects, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var capacityIds = apiObjects.Select(x => x.Id);
                act?.AddTag("CapacityIds", String.Join(", ", capacityIds));
            });
        }
    }
}
