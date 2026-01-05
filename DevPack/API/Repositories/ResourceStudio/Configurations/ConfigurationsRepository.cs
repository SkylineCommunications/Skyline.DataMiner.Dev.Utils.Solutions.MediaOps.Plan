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
    /// Provides repository operations for managing <see cref="Configuration"/> objects.
    /// </summary>
    internal class ConfigurationsRepository : Repository, IConfigurationsRepository
    {
        private readonly ConfigurationFilterTranslator filterTranslator = new ConfigurationFilterTranslator();

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationsRepository"/> class.
        /// </summary>
        /// <param name="planApi">The MediaOps Plan API instance.</param>
        public ConfigurationsRepository(MediaOpsPlanApi planApi) : base(planApi)
        {
        }

        /// <summary>
        /// Gets the total number of configurations in the repository.
        /// </summary>
        /// <returns>The total count of configurations.</returns>
        public long Count()
        {
            return Count(new TRUEFilterElement<Configuration>());
        }

        /// <summary>
        /// Gets the number of configurations that match the specified filter.
        /// </summary>
        /// <param name="filter">The filter criteria to apply when counting configurations.</param>
        /// <returns>The count of configurations matching the filter.</returns>
        public long Count(FilterElement<Configuration> filter)
        {
            return ActivityHelper.Track(nameof(ConfigurationsRepository), nameof(Count), act =>
            {
                var paramFilter = filterTranslator.Translate(filter);
                return PlanApi.CoreHelpers.ProfileProvider.CountConfigurations(paramFilter);
            });
        }

        /// <summary>
        /// Gets the number of configurations that match the specified query.
        /// </summary>
        /// <param name="query">The query criteria to apply when counting configurations.</param>
        /// <returns>The count of configurations matching the query.</returns>
        public long Count(IQuery<Configuration> query)
        {
            return Count(query.Filter);
        }

        /// <summary>
        /// Creates a new configuration in the repository.
        /// </summary>
        /// <param name="apiObject">The configuration to create.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObject"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when attempting to create an existing configuration.</exception>
        /// <exception cref="MediaOpsException">Thrown when the creation operation fails.</exception>
        public void Create(Configuration apiObject)
        {
            PlanApi.Logger.LogInformation("Creating new Configuration...");

            if (apiObject == null)
            {
                throw new ArgumentNullException(nameof(apiObject));
            }

            ActivityHelper.Track(nameof(ConfigurationsRepository), nameof(Create), act =>
            {
                if (!apiObject.IsNew)
                {
                    throw new InvalidOperationException("Not possible to use method Create for an existing configuration. Use CreateOrUpdate or Update instead.");
                }

                if (!CoreConfigurationHandler.TryCreateOrUpdate(PlanApi, [apiObject], out var result))
                {
                    throw new MediaOpsException(result.TraceDataPerItem[apiObject.Id]);
                }

                var configurationId = apiObject.Id;
                act?.AddTag("CapacityId", configurationId);
            });
        }

        /// <summary>
        /// Creates multiple new configurations in the repository.
        /// </summary>
        /// <param name="apiObjects">The collection of configurations to create.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when attempting to create existing configurations.</exception>
        /// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk creation operation fails.</exception>
        public void Create(IEnumerable<Configuration> apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            ActivityHelper.Track(nameof(ConfigurationsRepository), nameof(Create), act =>
            {
                if (apiObjects.Any(x => !x.IsNew))
                {
                    throw new InvalidOperationException("Not possible to use method Create for existing configurations. Use CreateOrUpdate or Update instead.");
                }

                if (!CoreConfigurationHandler.TryCreateOrUpdate(PlanApi, apiObjects.ToList(), out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var configurationIds = apiObjects.Select(x => x.Id);
                act?.AddTag("ConfigurationIds", string.Join(", ", configurationIds));
            });
        }

        /// <summary>
        /// Creates new configurations or updates existing ones in the repository.
        /// </summary>
        /// <param name="apiObjects">The collection of configurations to create or update.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
        /// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk operation fails.</exception>
        public void CreateOrUpdate(IEnumerable<Configuration> apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            ActivityHelper.Track(nameof(ConfigurationsRepository), nameof(CreateOrUpdate), act =>
            {
                if (!CoreConfigurationHandler.TryCreateOrUpdate(PlanApi, apiObjects?.ToList(), out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var configurationIds = result.SuccessfulIds;
                act?.AddTag("Created or Updated Configurations", String.Join(", ", configurationIds));
                act?.AddTag("Created or Updated Configurations Count", configurationIds.Count);
            });
        }

        /// <summary>
        /// Deletes the specified configurations from the repository.
        /// </summary>
        /// <param name="apiObjects">The configurations to delete.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
        public void Delete(IEnumerable<Configuration> apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            Delete(apiObjects.Select(x => x.Id).ToArray());
        }

        /// <summary>
        /// Deletes configurations with the specified identifiers from the repository.
        /// </summary>
        /// <param name="apiObjectIds">The unique identifiers of the configurations to delete.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjectIds"/> is <c>null</c>.</exception>
        /// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk deletion operation fails.</exception>
        public void Delete(IEnumerable<Guid> apiObjectIds)
        {
            if (apiObjectIds == null)
            {
                throw new ArgumentNullException(nameof(apiObjectIds));
            }

            var configurationsToDelete = Read(apiObjectIds.ToArray());

            ActivityHelper.Track(nameof(ConfigurationsRepository), nameof(Delete), act =>
            {
                if (!CoreConfigurationHandler.TryDelete(PlanApi, configurationsToDelete?.ToList(), out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var configurationIds = result.SuccessfulIds;
                act?.AddTag("Removed Configurations", String.Join(", ", configurationIds));
                act?.AddTag("Removed Configurations Count", configurationIds.Count);
            });
        }

        /// <summary>
        /// Deletes the specified configuration from the repository.
        /// </summary>
        /// <param name="oToDelete">The configuration to delete.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="oToDelete"/> is <c>null</c>.</exception>
        public void Delete(Configuration oToDelete)
        {
            if (oToDelete == null)
            {
                throw new ArgumentNullException(nameof(oToDelete));
            }

            Delete([oToDelete]);
        }

        /// <summary>
        /// Deletes the specified configuration from the repository.
        /// </summary>
        /// <param name="apiObjectId">The unique identifier of the configuration to delete.</param>
        public void Delete(Guid apiObjectId)
        {
            Delete([apiObjectId]);
        }

        /// <summary>
        /// Reads a single configuration by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the configuration.</param>
        /// <returns>The configuration with the specified identifier, or <c>null</c> if not found.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is <see cref="Guid.Empty"/>.</exception>
        public Configuration Read(Guid id)
        {
            PlanApi.Logger.LogInformation($"Reading Configuration with ID: {id}...");

            if (id == Guid.Empty)
            {
                throw new ArgumentException(nameof(id));
            }

            return Read(ConfigurationExposers.Id.Equal(id)).FirstOrDefault();
        }

        /// <summary>
        /// Reads multiple configurations by their unique identifiers.
        /// </summary>
        /// <param name="ids">A collection of unique identifiers.</param>
        /// <returns>An enumerable collection of configurations matching the specified identifiers.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="ids"/> is <c>null</c>.</exception>
        public IEnumerable<Configuration> Read(IEnumerable<Guid> ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            if (!ids.Any())
            {
                return Array.Empty<Configuration>();
            }

            return Read(new ORFilterElement<Configuration>(ids.Select(x => ConfigurationExposers.Id.Equal(x)).ToArray()));
        }

        /// <summary>
        /// Reads all configurations from the repository.
        /// </summary>
        /// <returns>An enumerable collection of all configurations.</returns>
        public IEnumerable<Configuration> Read()
        {
            return Read(new TRUEFilterElement<Configuration>());
        }

        /// <summary>
        /// Reads configurations that match the specified filter.
        /// </summary>
        /// <param name="filter">The filter criteria to apply when reading configurations.</param>
        /// <returns>An enumerable collection of configurations matching the filter.</returns>
        public IEnumerable<Configuration> Read(FilterElement<Configuration> filter)
        {
            return ActivityHelper.Track(nameof(ConfigurationsRepository), nameof(Read), act =>
            {
                var configurations = PlanApi.CoreHelpers.ProfileProvider.GetConfigurations(filterTranslator.Translate(filter));
                return Configuration.InstantiateConfigurations(configurations);
            });
        }

        /// <summary>
        /// Reads configurations that match the specified query.
        /// </summary>
        /// <param name="query">The query criteria to apply when reading configurations.</param>
        /// <returns>An enumerable collection of configurations matching the query.</returns>
        public IEnumerable<Configuration> Read(IQuery<Configuration> query)
        {
            return Read(query.Filter);
        }

        /// <summary>
        /// Reads all configurations in pages.
        /// </summary>
        /// <returns>An enumerable collection of pages, where each page contains a collection of configurations.</returns>
        public IEnumerable<IPagedResult<Configuration>> ReadPaged()
        {
            return ReadPaged(new TRUEFilterElement<Configuration>());
        }

        /// <summary>
        /// Reads configurations that match the specified filter in pages.
        /// </summary>
        /// <param name="filter">The filter criteria to apply when reading configurations.</param>
        /// <returns>An enumerable collection of pages, where each page contains configurations matching the filter.</returns>
        public IEnumerable<IPagedResult<Configuration>> ReadPaged(FilterElement<Configuration> filter)
        {
            return ReadPaged(filter, MediaOpsPlanApi.DefaultPageSize);
        }

        /// <summary>
        /// Reads configurations that match the specified query in pages.
        /// </summary>
        /// <param name="query">The query criteria to apply when reading configurations.</param>
        /// <returns>An enumerable collection of pages, where each page contains configurations matching the query.</returns>
        public IEnumerable<IPagedResult<Configuration>> ReadPaged(IQuery<Configuration> query)
        {
            return ReadPaged(query.Filter);
        }

        /// <summary>
        /// Reads configurations that match the specified filter in pages with a custom page size.
        /// </summary>
        /// <param name="filter">The filter criteria to apply when reading configurations.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>An enumerable collection of pages, where each page contains up to the specified number of configurations matching the filter.</returns>
        public IEnumerable<IPagedResult<Configuration>> ReadPaged(FilterElement<Configuration> filter, int pageSize)
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
        /// Reads configurations that match the specified query in pages with a custom page size.
        /// </summary>
        /// <param name="query">The query criteria to apply when reading configurations.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>An enumerable collection of pages, where each page contains up to the specified number of configurations matching the query.</returns>
        public IEnumerable<IPagedResult<Configuration>> ReadPaged(IQuery<Configuration> query, int pageSize)
        {
            return ReadPaged(query.Filter, pageSize);
        }

        /// <summary>
        /// Reads all configurations in pages.
        /// </summary>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>An enumerable collection of pages, where each page contains a collection of configurations.</returns>
        public IEnumerable<IPagedResult<Configuration>> ReadPaged(int pageSize)
        {
            return ReadPaged(new TRUEFilterElement<Configuration>(), MediaOpsPlanApi.DefaultPageSize);
        }

        /// <summary>
        /// Updates an existing configuration in the repository.
        /// </summary>
        /// <param name="apiObject">The configuration to update.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObject"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when attempting to update a new configuration that doesn't exist yet.</exception>
        /// <exception cref="MediaOpsException">Thrown when the update operation fails.</exception>
        public void Update(Configuration apiObject)
        {
            if (apiObject == null)
            {
                throw new ArgumentNullException(nameof(apiObject));
            }

            PlanApi.Logger.LogInformation($"Updating existing configuration {apiObject.Name}...");

            ActivityHelper.Track(nameof(ConfigurationsRepository), nameof(Update), act =>
            {
                if (apiObject.IsNew)
                {
                    throw new InvalidOperationException("Not possible to use method Update for new configuration. Use Create or CreateOrUpdate instead.");
                }

                if (!CoreConfigurationHandler.TryCreateOrUpdate(PlanApi, [apiObject], out var result))
                {
                    throw new MediaOpsException(result.TraceDataPerItem[apiObject.Id]);
                }

                var configurationId = apiObject.Id;
                act?.AddTag("ConfigurationId", configurationId);
            });
        }

        /// <summary>
        /// Updates multiple existing configurations in the repository.
        /// </summary>
        /// <param name="apiObjects">The collection of configurations to update.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when attempting to update new configurations that don't exist yet.</exception>
        /// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk update operation fails.</exception>
        public void Update(IEnumerable<Configuration> apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            ActivityHelper.Track(nameof(ConfigurationsRepository), nameof(Update), act =>
            {
                if (apiObjects.Any(x => x.IsNew))
                {
                    throw new InvalidOperationException("Not possible to use method Update for new configurations. Use Create or CreateOrUpdate instead.");
                }

                if (!CoreConfigurationHandler.TryCreateOrUpdate(PlanApi, apiObjects.ToList(), out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var configurationIds = apiObjects.Select(x => x.Id);
                act?.AddTag("ConfigurationIds", String.Join(", ", configurationIds));
            });
        }

        private IEnumerable<IPagedResult<Configuration>> ReadPagedIterator(FilterElement<Configuration> filter, int pageSize)
        {
            var pageNumber = 0;
            var paramFilter = filterTranslator.Translate(filter);
            var items = PlanApi.CoreHelpers.ProfileProvider.GetConfigurationsPaged(paramFilter, pageSize);
            var enumerator = items.GetEnumerator();
            var hasNext = enumerator.MoveNext();

            while (hasNext)
            {
                var page = enumerator.Current;
                hasNext = enumerator.MoveNext();
                yield return new PagedResult<Configuration>(Configuration.InstantiateConfigurations(page), pageNumber++, pageSize, hasNext);
            }
        }
    }
}
