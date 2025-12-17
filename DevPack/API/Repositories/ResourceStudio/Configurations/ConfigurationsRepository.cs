namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.ActivityHelper;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core;

    using SLDataGateway.API.Types.Querying;

    /// <summary>
    /// Provides repository operations for managing <see cref="Configuration"/> objects.
    /// </summary>
    internal class ConfigurationsRepository : Repository, IConfigurationsRepository
    {
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
            return PlanApi.CoreHelpers.ProfileProvider.CountAllConfigurations();
        }

        /// <summary>
        /// Gets the number of configurations that match the specified filter.
        /// </summary>
        /// <param name="filter">The filter criteria to apply when counting configurations.</param>
        /// <returns>The count of configurations matching the filter.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public long Count(FilterElement<Configuration> filter)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the number of configurations that match the specified query.
        /// </summary>
        /// <param name="query">The query criteria to apply when counting configurations.</param>
        /// <returns>The count of configurations matching the query.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public long Count(IQuery<Configuration> query)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a new configuration in the repository.
        /// </summary>
        /// <param name="apiObject">The configuration to create.</param>
        /// <returns>The unique identifier of the created configuration.</returns>
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
        /// <returns>A collection of unique identifiers for the created configurations.</returns>
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

                if (!CoreConfigurationHandler.TryCreateOrUpdate(PlanApi, apiObjects, out var result))
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
        /// <returns>A collection of unique identifiers for the created or updated configurations.</returns>
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
                if (!CoreConfigurationHandler.TryCreateOrUpdate(PlanApi, apiObjects, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var configurationIds = apiObjects.Select(x => x.Id);
                act?.AddTag("Created or Updated Configurations", String.Join(", ", configurationIds));
                act?.AddTag("Created or Updated Configurations Count", configurationIds.Count());
            });
        }

        /// <summary>
        /// Deletes the specified configurations from the repository.
        /// </summary>
        /// <param name="apiObjects">The configurations to delete.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
        public void Delete(params Configuration[] apiObjects)
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
        public void Delete(params Guid[] apiObjectIds)
        {
            if (apiObjectIds == null)
            {
                throw new ArgumentNullException(nameof(apiObjectIds));
            }

            var configurationsToDelete = Read(apiObjectIds);

            ActivityHelper.Track(nameof(ConfigurationsRepository), nameof(Delete), act =>
            {
                if (!CoreConfigurationHandler.TryDelete(PlanApi, configurationsToDelete, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var configurationIds = apiObjectIds;
                act?.AddTag("Removed Configurations", String.Join(", ", configurationIds));
                act?.AddTag("Removed Configurations Count", configurationIds.Count());
            });
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

            return ActivityHelper.Track(nameof(ConfigurationsRepository), nameof(Read), act =>
            {
                act?.AddTag("CapacityId", id);
                var coreConfiguration = PlanApi.CoreHelpers.ProfileProvider.GetConfigurationById(id);

                if (coreConfiguration == null)
                {
                    act?.AddTag("Hit", false);
                    return null;
                }

                act?.AddTag("Hit", true);

                return InstantiateConfigurations([coreConfiguration]).FirstOrDefault();
            });
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

            return ActivityHelper.Track(nameof(ConfigurationsRepository), nameof(Read), act =>
            {
                act?.AddTag("ConfigurationIds", String.Join(", ", ids));
                act?.AddTag("ConfigurationIds Count", ids.Count());

                var configurations = PlanApi.CoreHelpers.ProfileProvider.GetConfigurationsById(ids);

                return InstantiateConfigurations(configurations);
            });
        }

        /// <summary>
        /// Reads all configurations from the repository.
        /// </summary>
        /// <returns>An enumerable collection of all configurations.</returns>
        public IEnumerable<Configuration> Read()
        {
            return ActivityHelper.Track(nameof(ConfigurationsRepository), nameof(Read), act =>
            {
                return InstantiateConfigurations(PlanApi.CoreHelpers.ProfileProvider.GetAllConfigurations());
            });
        }

        /// <summary>
        /// Reads configurations that match the specified filter.
        /// </summary>
        /// <param name="filter">The filter criteria to apply when reading configurations.</param>
        /// <returns>An enumerable collection of configurations matching the filter.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public IEnumerable<Configuration> Read(FilterElement<Configuration> filter)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reads configurations that match the specified query.
        /// </summary>
        /// <param name="query">The query criteria to apply when reading configurations.</param>
        /// <returns>An enumerable collection of configurations matching the query.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public IEnumerable<Configuration> Read(IQuery<Configuration> query)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reads all configurations in pages.
        /// </summary>
        /// <returns>An enumerable collection of pages, where each page contains a collection of configurations.</returns>
        public IEnumerable<IEnumerable<Configuration>> ReadPaged()
        {
            return ActivityHelper.Track(nameof(ConfigurationsRepository), nameof(ReadPaged), act =>
            {
                return PlanApi.CoreHelpers.ProfileProvider.GetAllConfigurationsPaged().Select(page => InstantiateConfigurations(page));
            });
        }

        /// <summary>
        /// Reads all configurations in pages.
        /// </summary>
        /// <returns>An enumerable collection of pages, where each page contains a collection of configurations.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        IEnumerable<IPagedResult<Configuration>> IPageableRepository<Configuration>.ReadPaged()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reads configurations that match the specified filter in pages.
        /// </summary>
        /// <param name="filter">The filter criteria to apply when reading configurations.</param>
        /// <returns>An enumerable collection of pages, where each page contains configurations matching the filter.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public IEnumerable<IPagedResult<Configuration>> ReadPaged(FilterElement<Configuration> filter)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reads configurations that match the specified query in pages.
        /// </summary>
        /// <param name="query">The query criteria to apply when reading configurations.</param>
        /// <returns>An enumerable collection of pages, where each page contains configurations matching the query.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public IEnumerable<IPagedResult<Configuration>> ReadPaged(IQuery<Configuration> query)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reads configurations that match the specified filter in pages with a custom page size.
        /// </summary>
        /// <param name="filter">The filter criteria to apply when reading configurations.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>An enumerable collection of pages, where each page contains up to the specified number of configurations matching the filter.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public IEnumerable<IPagedResult<Configuration>> ReadPaged(FilterElement<Configuration> filter, int pageSize)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reads configurations that match the specified query in pages with a custom page size.
        /// </summary>
        /// <param name="query">The query criteria to apply when reading configurations.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>An enumerable collection of pages, where each page contains up to the specified number of configurations matching the query.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public IEnumerable<IPagedResult<Configuration>> ReadPaged(IQuery<Configuration> query, int pageSize)
        {
            throw new NotImplementedException();
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

                if (!CoreConfigurationHandler.TryCreateOrUpdate(PlanApi, apiObjects, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var configurationIds = apiObjects.Select(x => x.Id);
                act?.AddTag("ConfigurationIds", String.Join(", ", configurationIds));
            });
        }

        /// <summary>
        /// Creates configuration instances from the provided collection of core parameter instances.
        /// </summary>
        /// <param name="instances">The collection of core parameter instances to instantiate as configurations.</param>
        /// <returns>An enumerable collection of configuration objects created from the instances.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="instances"/> is <c>null</c>.</exception>
        internal static IEnumerable<Configuration> InstantiateConfigurations(IEnumerable<Net.Profiles.Parameter> instances)
        {
            if (instances == null)
            {
                throw new ArgumentNullException(nameof(instances));
            }

            if (!instances.Any())
            {
                return [];
            }

            return InstantiateConfigurationsIterator(instances);
        }

        /// <summary>
        /// Iterator method that creates configuration instances from the provided collection of core parameter instances.
        /// </summary>
        /// <param name="instances">The collection of core parameter instances to process.</param>
        /// <returns>An enumerable collection of configuration objects.</returns>
        private static IEnumerable<Configuration> InstantiateConfigurationsIterator(IEnumerable<Net.Profiles.Parameter> instances)
        {
            foreach (var instance in instances)
            {
                if (!instance.IsConfiguration())
                {
                    continue;
                }

                if (instance.IsText())
                {
                    yield return new TextConfiguration(instance);
                }
                else if (instance.IsNumber())
                {
                    yield return new NumberConfiguration(instance);
                }
                else if (instance.IsTextDiscreet())
                {
                    yield return new DiscreteTextConfiguration(instance);
                }
                else if (instance.IsNumberDiscreet())
                {
                    yield return new DiscreteNumberConfiguration(instance);
                }
                else
                {
                    // continue
                }
            }
        }
    }
}
