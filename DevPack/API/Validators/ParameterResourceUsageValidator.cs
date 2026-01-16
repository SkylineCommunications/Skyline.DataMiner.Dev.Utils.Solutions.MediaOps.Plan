namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;

    internal class ParameterResourceUsageValidator : ApiObjectValidator
    {
        private readonly MediaOpsPlanApi planApi;

        public ParameterResourceUsageValidator(MediaOpsPlanApi planApi)
        {
            this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
        }

        public static ApiObjectValidator Validate(MediaOpsPlanApi planApi, ICollection<Configuration> configurations)
        {
            if (configurations == null)
                throw new ArgumentNullException(nameof(configurations));

            var validator = new ParameterResourceUsageValidator(planApi);
            validator.ValidateResourcePoolParametersUsage(configurations);
            return validator;
        }

        private void ValidateResourcePoolParametersUsage(ICollection<Configuration> apiConfigurations)
        {
            var resourcePoolConfigurations = GetResourceConfigurationsReferencingParameters(apiConfigurations.Select(x => x.Id).ToHashSet());

            if (!resourcePoolConfigurations.Any())
                return;

            var resourcePools = GetResourcePoolsReferencingConfigurations(resourcePoolConfigurations);

            foreach (var apiConfiguration in apiConfigurations)
            {
                var poolConfigurationsUsingThisConfiguration = resourcePoolConfigurations.Where(x =>
                x.ProfileParameterValues.Any(y => y.ProfileParameterID == apiConfiguration.Id.ToString()) ||
                x.OrchestrationEvents.Any(y => y.ScriptExecutionDetails.ProfileParameterValues.Select(x => x.ProfileParameterId).Contains(apiConfiguration.Id))).ToArray();

                if (!poolConfigurationsUsingThisConfiguration.Any())
                    continue;

                var poolConfigurationIdsUsingThisConfiguration = poolConfigurationsUsingThisConfiguration.Select(x => x.ID.Id).Distinct();
                var poolsUsingThisConfiguration = resourcePools.Where(x => x.ConfigurationInfo.PoolConfiguration != null && poolConfigurationIdsUsingThisConfiguration.Contains(x.ConfigurationInfo.PoolConfiguration.Value)).ToArray();

                if (!poolsUsingThisConfiguration.Any())
                    continue;

                ReportError(apiConfiguration.Id, new ConfigurationInUseByResourcePoolsError
                {
                    ErrorMessage = $"Configuration '{apiConfiguration.Name}' is in use by Resource Pools.",
                    Id = apiConfiguration.Id,
                    ResourcePoolIds = poolsUsingThisConfiguration.Select(x => x.ID.Id).ToArray(),
                });
            }
        }

        private ICollection<ConfigurationInstance> GetResourceConfigurationsReferencingParameters(HashSet<Guid> apiParameterIds)
        {
            var configurationFilter = DomInstanceExposers.DomDefinitionId.Equal(Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Definitions.Configuration.Id);
            configurationFilter.AND(new ORFilterElement<DomInstance>(apiParameterIds.Select(x =>
                DomInstanceExposers.FieldValues.DomInstanceField(Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ProfileParameterValues.ProfileParameterID).Equal(x).OR(
                DomInstanceExposers.FieldValues.DomInstanceField(Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.OrchestrationEvents.ScriptInput).Contains(x.ToString())))
                .ToArray()));

            var possibleResourcePoolConfigurations = planApi.DomHelpers.SlcResourceStudioHelper.GetConfigurations(configurationFilter);

            List<ConfigurationInstance> resourcePoolConfigurations = new List<ConfigurationInstance>();
            if (!possibleResourcePoolConfigurations.Any())
                return resourcePoolConfigurations;

            foreach (var possibleResourcePoolConfiguration in possibleResourcePoolConfigurations)
            {
                if (possibleResourcePoolConfiguration.ProfileParameterValues.Any(x => Guid.TryParse(x.ProfileParameterID, out Guid profileParameterGuid) && apiParameterIds.Contains(profileParameterGuid)))
                {
                    resourcePoolConfigurations.Add(possibleResourcePoolConfiguration);
                    continue;
                }

                foreach (var section in possibleResourcePoolConfiguration.OrchestrationEvents)
                {
                    if (section.ScriptExecutionDetails?.ProfileParameterValues == null)
                    {
                        continue;
                    }

                    if (!section.ScriptExecutionDetails.ProfileParameterValues.Any(x => apiParameterIds.Contains(x.ProfileParameterId)))
                    {
                        continue;
                    }

                    resourcePoolConfigurations.Add(possibleResourcePoolConfiguration);
                    break;
                }
            }

            return resourcePoolConfigurations;
        }

        private ICollection<ResourcepoolInstance> GetResourcePoolsReferencingConfigurations(ICollection<ConfigurationInstance> resourcePoolConfigurations)
        {
            var resourcePoolFilter = DomInstanceExposers.DomDefinitionId.Equal(Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Definitions.Resourcepool.Id);
            var distinctResourcePoolConfigurationIds = resourcePoolConfigurations.Select(x => x.ID.Id).Distinct().ToList();
            resourcePoolFilter.AND(new ORFilterElement<DomInstance>(distinctResourcePoolConfigurationIds.Select(x => DomInstanceExposers.FieldValues.DomInstanceField(Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ConfigurationInfo.PoolConfiguration).Equal(x)).ToArray()));

            return planApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(resourcePoolFilter).ToArray();
        }
    }
}
