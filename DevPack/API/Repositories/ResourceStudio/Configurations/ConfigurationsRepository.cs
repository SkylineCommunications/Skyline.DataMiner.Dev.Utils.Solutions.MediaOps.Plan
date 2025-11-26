namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.Logging;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.ActivityHelper;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

    internal partial class ConfigurationsRepository : ProfileParameterRepository<Configuration>, IConfigurationsRepository
    {
        public Guid Create(Configuration apiObject)
        {
            PlanApi.Logger.LogInformation("Creating new Configuration...");

            if (apiObject == null)
            {
                throw new ArgumentNullException(nameof(apiObject));
            }

            return ActivityHelper.Track(nameof(ConfigurationsRepository), nameof(Create), act =>
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

                return configurationId;
            });
        }

        public IEnumerable<Guid> Create(IEnumerable<Configuration> apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            return ActivityHelper.Track(nameof(ConfigurationsRepository), nameof(Create), act =>
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

                return configurationIds;
            });
        }

        public IEnumerable<Guid> CreateOrUpdate(IEnumerable<Configuration> apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            return ActivityHelper.Track(nameof(ConfigurationsRepository), nameof(CreateOrUpdate), act =>
            {
                if (!CoreConfigurationHandler.TryCreateOrUpdate(PlanApi, apiObjects, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var configurationIds = apiObjects.Select(x => x.Id);
                act?.AddTag("Created or Updated Configurations", String.Join(", ", configurationIds));
                act?.AddTag("Created or Updated Configurations Count", configurationIds.Count());

                return configurationIds;
            });
        }

        public void Delete(params Configuration[] apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            Delete(apiObjects.Select(x => x.Id).ToArray());
        }

        public void Delete(params Guid[] apiObjectIds)
        {
            if (apiObjectIds == null)
            {
                throw new ArgumentNullException(nameof(apiObjectIds));
            }

            var configurationsToDelete = Read(apiObjectIds).Values;

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
    }
}
