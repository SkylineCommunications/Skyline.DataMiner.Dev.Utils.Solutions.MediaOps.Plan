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
    /// Provides repository operations for managing <see cref="Capability"/> objects.
    /// </summary>
    internal class CapabilitiesRepository : Repository, ICapabilitiesRepository
    {
        private readonly CapabilityFilterTranslator filterTranslator = new CapabilityFilterTranslator();

        /// <summary>
        /// Initializes a new instance of the <see cref="CapabilitiesRepository"/> class.
        /// </summary>
        /// <param name="planApi">The MediaOps Plan API instance.</param>
        public CapabilitiesRepository(MediaOpsPlanApi planApi) : base(planApi)
        {
        }

        /// <summary>
        /// Gets the total number of capabilities in the repository.
        /// </summary>
        /// <returns>The total count of capabilities.</returns>
        public long Count()
        {
            return Count(new TRUEFilterElement<Capability>());
        }

        /// <summary>
        /// Gets the number of capabilities that match the specified filter.
        /// </summary>
        /// <param name="filter">The filter criteria to apply when counting capabilities.</param>
        /// <returns>The count of capabilities matching the filter.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public long Count(FilterElement<Capability> filter)
        {
            return ActivityHelper.Track(nameof(CapabilitiesRepository), nameof(Count), act =>
            {
                var paramFilter = filterTranslator.Translate(filter);
                return PlanApi.CoreHelpers.ProfileProvider.CountCapabilities(paramFilter);
            });
        }

        /// <summary>
        /// Gets the number of capabilities that match the specified query.
        /// </summary>
        /// <param name="query">The query criteria to apply when counting capabilities.</param>
        /// <returns>The count of capabilities matching the query.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public long Count(IQuery<Capability> query)
        {
            return Count(query.Filter);
        }

        /// <summary>
        /// Creates a new capability in the repository.
        /// </summary>
        /// <param name="apiObject">The capability to create.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObject"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when attempting to create an existing capability.</exception>
        /// <exception cref="MediaOpsException">Thrown when the creation operation fails.</exception>
        public void Create(Capability apiObject)
        {
            PlanApi.Logger.LogInformation("Creating new Capability...");

            if (apiObject == null)
            {
                throw new ArgumentNullException(nameof(apiObject));
            }

            ActivityHelper.Track(nameof(CapabilitiesRepository), nameof(Create), act =>
            {
                if (!apiObject.IsNew)
                {
                    throw new InvalidOperationException("Not possible to use method Create for an existing capability. Use CreateOrUpdate or Update instead.");
                }

                if (!CoreCapabilityHandler.TryCreateOrUpdate(PlanApi, [apiObject], out var result))
                {
                    throw new MediaOpsException(result.TraceDataPerItem[apiObject.Id]);
                }

                var capabilityId = apiObject.Id;
                act?.AddTag("CapabilityId", capabilityId);
            });
        }

        /// <summary>
        /// Creates multiple new capabilities in the repository.
        /// </summary>
        /// <param name="apiObjects">The collection of capabilities to create.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when attempting to create existing capabilities.</exception>
        /// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk creation operation fails.</exception>
        public void Create(IEnumerable<Capability> apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            ActivityHelper.Track(nameof(CapabilitiesRepository), nameof(Create), act =>
            {
                var existingCapabilities = apiObjects.Where(x => !x.IsNew);
                if (existingCapabilities.Any())
                {
                    throw new InvalidOperationException("Not possible to use method Create for existing capabilities. Use CreateOrUpdate or Update instead.");
                }

                if (!CoreCapabilityHandler.TryCreateOrUpdate(PlanApi, apiObjects, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var capabilityIds = apiObjects.Select(x => x.Id);
                act?.AddTag("CapabilityIds", string.Join(", ", capabilityIds));
            });
        }

        /// <summary>
        /// Creates new capabilities or updates existing ones in the repository.
        /// </summary>
        /// <param name="apiObjects">The collection of capabilities to create or update.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
        /// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk operation fails.</exception>
        public void CreateOrUpdate(IEnumerable<Capability> apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            ActivityHelper.Track(nameof(CapabilitiesRepository), nameof(CreateOrUpdate), act =>
            {
                if (!CoreCapabilityHandler.TryCreateOrUpdate(PlanApi, apiObjects, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var capabilityIds = apiObjects.Select(x => x.Id);
                act?.AddTag("Created or Updated Capabilities", String.Join(", ", capabilityIds));
                act?.AddTag("Created or Updated Capabilities Count", capabilityIds.Count());
            });
        }

        /// <summary>
        /// Deletes the specified capabilities from the repository.
        /// </summary>
        /// <param name="apiObjects">The capabilities to delete.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
        public void Delete(params Capability[] apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            Delete(apiObjects.Select(x => x.Id).ToArray());
        }

        /// <summary>
        /// Deletes capabilities with the specified identifiers from the repository.
        /// </summary>
        /// <param name="apiObjectIds">The unique identifiers of the capabilities to delete.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjectIds"/> is <c>null</c>.</exception>
        /// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk deletion operation fails.</exception>
        public void Delete(params Guid[] apiObjectIds)
        {
            if (apiObjectIds == null)
            {
                throw new ArgumentNullException(nameof(apiObjectIds));
            }

            var capabilitiesToDelete = Read(apiObjectIds);

            ActivityHelper.Track(nameof(CapabilitiesRepository), nameof(Delete), act =>
            {
                if (!CoreCapabilityHandler.TryDelete(PlanApi, capabilitiesToDelete, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var capabilityIds = apiObjectIds;
                act?.AddTag("Removed Capabilities", String.Join(", ", capabilityIds));
                act?.AddTag("Removed Capabilities Count", capabilityIds.Count());
            });
        }

        /// <summary>
        /// Reads a single capability by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the capability.</param>
        /// <returns>The capability with the specified identifier, or <c>null</c> if not found.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is <see cref="Guid.Empty"/>.</exception>
        public Capability Read(Guid id)
        {
            PlanApi.Logger.LogInformation($"Reading Capability with ID: {id}...");

            if (id == Guid.Empty)
            {
                throw new ArgumentException(nameof(id));
            }

            return Read(CapabilityExposers.Id.Equal(id)).FirstOrDefault();
        }

        /// <summary>
        /// Reads multiple capabilities by their unique identifiers.
        /// </summary>
        /// <param name="ids">A collection of unique identifiers.</param>
        /// <returns>An enumerable collection of capabilities matching the specified identifiers.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="ids"/> is <c>null</c>.</exception>
        public IEnumerable<Capability> Read(IEnumerable<Guid> ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            return Read(new ORFilterElement<Capability>(ids.Select(x => CapabilityExposers.Id.Equal(x)).ToArray()));
        }

        /// <summary>
        /// Reads all capabilities from the repository.
        /// </summary>
        /// <returns>An enumerable collection of all capabilities.</returns>
        public IEnumerable<Capability> Read()
        {
            return Read(new TRUEFilterElement<Capability>());
        }

        /// <summary>
        /// Reads capabilities that match the specified filter.
        /// </summary>
        /// <param name="filter">The filter criteria to apply when reading capabilities.</param>
        /// <returns>An enumerable collection of capabilities matching the filter.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public IEnumerable<Capability> Read(FilterElement<Capability> filter)
        {
            return ActivityHelper.Track(nameof(CapabilitiesRepository), nameof(Read), act =>
            {
                var paramFilter = filterTranslator.Translate(filter);
                return PlanApi.CoreHelpers.ProfileProvider.GetCapabilities(paramFilter).Select(x => new Capability(x));
            });
        }

        /// <summary>
        /// Reads capabilities that match the specified query.
        /// </summary>
        /// <param name="query">The query criteria to apply when reading capabilities.</param>
        /// <returns>An enumerable collection of capabilities matching the query.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public IEnumerable<Capability> Read(IQuery<Capability> query)
        {
            return Read(query.Filter);
        }

        /// <summary>
        /// Reads all capabilities in pages.
        /// </summary>
        /// <returns>An enumerable collection of pages, where each page contains a collection of capabilities.</returns>
        public IEnumerable<IPagedResult<Capability>> ReadPaged()
        {
            return ReadPaged(new TRUEFilterElement<Capability>());
        }

        /// <summary>
        /// Reads capabilities that match the specified filter in pages.
        /// </summary>
        /// <param name="filter">The filter criteria to apply when reading capabilities.</param>
        /// <returns>An enumerable collection of pages, where each page contains capabilities matching the filter.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public IEnumerable<IPagedResult<Capability>> ReadPaged(FilterElement<Capability> filter)
        {
            return ReadPaged(filter, MediaOpsPlanApi.DefaultPageSize);
        }

        /// <summary>
        /// Reads capabilities that match the specified query in pages.
        /// </summary>
        /// <param name="query">The query criteria to apply when reading capabilities.</param>
        /// <returns>An enumerable collection of pages, where each page contains capabilities matching the query.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public IEnumerable<IPagedResult<Capability>> ReadPaged(IQuery<Capability> query)
        {
            return ReadPaged(query.Filter);
        }

        /// <summary>
        /// Reads capabilities that match the specified filter in pages with a custom page size.
        /// </summary>
        /// <param name="filter">The filter criteria to apply when reading capabilities.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>An enumerable collection of pages, where each page contains up to the specified number of capabilities matching the filter.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public IEnumerable<IPagedResult<Capability>> ReadPaged(FilterElement<Capability> filter, int pageSize)
        {
            var pageNumber = 0;
            var paramFilter = filterTranslator.Translate(filter);
            var items = PlanApi.CoreHelpers.ProfileProvider.GetCapabilitiesPaged(paramFilter, pageSize);
            var enumerator = items.GetEnumerator();
            var hasNext = enumerator.MoveNext();

            while (hasNext)
            {
                var page = enumerator.Current;
                hasNext = enumerator.MoveNext();
                yield return new PagedResult<Capability>(page.Select(x => new Capability(x)), pageNumber++, pageSize, hasNext);
            }
        }

        /// <summary>
        /// Reads capabilities that match the specified query in pages with a custom page size.
        /// </summary>
        /// <param name="query">The query criteria to apply when reading capabilities.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>An enumerable collection of pages, where each page contains up to the specified number of capabilities matching the query.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public IEnumerable<IPagedResult<Capability>> ReadPaged(IQuery<Capability> query, int pageSize)
        {
            return ReadPaged(query.Filter, pageSize);
        }

        /// <summary>
        /// Updates an existing capability in the repository.
        /// </summary>
        /// <param name="apiObject">The capability to update.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObject"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when attempting to update a new capability that doesn't exist yet.</exception>
        /// <exception cref="MediaOpsException">Thrown when the update operation fails.</exception>
        public void Update(Capability apiObject)
        {
            if (apiObject == null)
            {
                throw new ArgumentNullException(nameof(apiObject));
            }

            PlanApi.Logger.LogInformation($"Updating existing capability {apiObject.Name}...");

            ActivityHelper.Track(nameof(CapabilitiesRepository), nameof(Update), act =>
            {
                if (apiObject.IsNew)
                {
                    throw new InvalidOperationException("Not possible to use method Update for new capability. Use Create or CreateOrUpdate instead.");
                }

                if (!CoreCapabilityHandler.TryCreateOrUpdate(PlanApi, [apiObject], out var result))
                {
                    throw new MediaOpsException(result.TraceDataPerItem[apiObject.Id]);
                }

                var capabilityId = apiObject.Id;
                act?.AddTag("CapabilityId", capabilityId);
            });
        }

        /// <summary>
        /// Updates multiple existing capabilities in the repository.
        /// </summary>
        /// <param name="apiObjects">The collection of capabilities to update.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when attempting to update new capabilities that don't exist yet.</exception>
        /// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk update operation fails.</exception>
        public void Update(IEnumerable<Capability> apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            ActivityHelper.Track(nameof(CapabilitiesRepository), nameof(Update), act =>
            {
                var newCapabilities = apiObjects.Where(x => x.IsNew);
                if (newCapabilities.Any())
                {
                    throw new InvalidOperationException("Not possible to use method Update for new capabilities. Use Create or CreateOrUpdate instead.");
                }

                if (!CoreCapabilityHandler.TryCreateOrUpdate(PlanApi, apiObjects, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var capabilityIds = apiObjects.Select(x => x.Id);
                act?.AddTag("CapabilityIds", String.Join(", ", capabilityIds));
            });
        }
    }
}
