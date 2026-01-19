namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Jobs;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;

    internal class SlcResourceStudioParameterUsageValidator : ApiObjectValidator
    {
        private readonly HashSet<Guid> parameterIdsToValidate;
        private readonly IReadOnlyCollection<Parameter> parametersToValidate;
        private readonly MediaOpsPlanApi planApi;
        private Dictionary<Guid, List<Guid>> resourcePoolsReferencingConfigurations;
        private HashSet<Guid> resourceStudioConfigurationIds;
        private Dictionary<Guid, List<Guid>> resourceStudioConfigurationsPerParameter;

        public SlcResourceStudioParameterUsageValidator(MediaOpsPlanApi planApi, IReadOnlyCollection<Parameter> parametersToValidate)
        {
            this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
            this.parametersToValidate = parametersToValidate ?? throw new ArgumentNullException(nameof(parametersToValidate));
            parameterIdsToValidate = parametersToValidate.Select(x => x.Id).ToHashSet();
        }

        public static ApiObjectValidator Validate(MediaOpsPlanApi planApi, ICollection<Configuration> configurations)
        {
            if (configurations == null)
                throw new ArgumentNullException(nameof(configurations));

            var validator = new SlcResourceStudioParameterUsageValidator(planApi, configurations.ToList<Parameter>());
            validator.ValidateConfigurations(configurations);
            return validator;
        }

        public static ApiObjectValidator Validate(MediaOpsPlanApi planApi, ICollection<Capability> capabilities)
        {
            if (capabilities == null)
                throw new ArgumentNullException(nameof(capabilities));

            var validator = new SlcResourceStudioParameterUsageValidator(planApi, capabilities.ToList<Parameter>());
            validator.ValidateCapabilities(capabilities);
            return validator;
        }

        public static ApiObjectValidator Validate(MediaOpsPlanApi planApi, ICollection<Capacity> capacities)
        {
            if (capacities == null)
                throw new ArgumentNullException(nameof(capacities));

            var validator = new SlcResourceStudioParameterUsageValidator(planApi, capacities.ToList<Parameter>());
            validator.ValidateCapacities(capacities);
            return validator;
        }

        private Dictionary<Guid, List<Guid>> GetResourceConfigurationsPerParameter(HashSet<Guid> parameterIds)
        {
            var result = new Dictionary<Guid, List<Guid>>();

            if (parameterIds == null || parameterIds.Count == 0)
            {
                return result;
            }

            var possibleResourcePoolConfigurations = GetResourceStudioConfigurations(parameterIds);

            foreach (var possibleResourcePoolConfiguration in possibleResourcePoolConfigurations)
            {
                var configurationId = possibleResourcePoolConfiguration.ID.Id;

                if (possibleResourcePoolConfiguration.ProfileParameterValues != null)
                {
                    // ProfileParameterValues section
                    foreach (var profileParameterValue in possibleResourcePoolConfiguration.ProfileParameterValues)
                    {
                        ValidateProfileParameterValuesSection(result, configurationId, profileParameterValue);
                    }
                }

                if (possibleResourcePoolConfiguration.OrchestrationEvents != null)
                {
                    // OrchestrationEvents section
                    foreach (var section in possibleResourcePoolConfiguration.OrchestrationEvents)
                    {
                        ValidateOrchestrationEventsSection(result, configurationId, section);
                    }
                }
            }

            return result;
        }

        private ICollection<ResourcepoolInstance> GetResourcePoolInstancesReferencingConfigurations()
        {
            if (!resourceStudioConfigurationIds.Any())
            {
                return Enumerable.Empty<ResourcepoolInstance>().ToArray();
            }

            var resourcePoolFilter = DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resourcepool.Id)
                .AND(new ORFilterElement<DomInstance>(resourceStudioConfigurationIds.Select(x => DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ConfigurationInfo.PoolConfiguration).Equal(x))
                .ToArray()));

            return planApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(resourcePoolFilter).ToArray();
        }

        /// <summary>
        /// Returns the parameters mapped to the resource pools referencing them via the DOM Resource Pool Capabilities section.
        /// </summary>
        /// <returns>Ids of the capabilities mapped to the ids of all resource pools referencing them through the DOM Resource Pool Capabilities section.</returns>
        private Dictionary<Guid, ICollection<Guid>> GetResourcePoolsPerCapability()
        {
            var result = new Dictionary<Guid, ICollection<Guid>>();

            var resourcePoolFilter = DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resourcepool.Id)
                .AND(new ORFilterElement<DomInstance>(parametersToValidate.Select(x => DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourcePoolCapabilities.ProfileParameterID).Equal(x.Id.ToString()))
                .ToArray()));

            var resourcePoolInstances = planApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(resourcePoolFilter);

            foreach (var parameter in parametersToValidate)
            {
                HashSet<Guid> poolIds = new HashSet<Guid>();
                foreach (var resourcePoolInstance in resourcePoolInstances)
                {
                    if (resourcePoolInstance.ResourcePoolCapabilities == null)
                        continue;

                    if (resourcePoolInstance.ResourcePoolCapabilities.Any(x => x.ProfileParameterID == parameter.Id.ToString()))
                        poolIds.Add(resourcePoolInstance.ID.Id);
                }

                if (poolIds.Any())
                    result.Add(parameter.Id, poolIds);
            }

            return result;
        }

        private Dictionary<Guid, List<Guid>> GetResourcePoolsReferencingWorkflowsConfigurationInstances()
        {
            var result = new Dictionary<Guid, List<Guid>>();
            var resourcePools = GetResourcePoolInstancesReferencingConfigurations();

            foreach (var resourcePool in resourcePools)
            {
                var resourcePoolId = resourcePool.ID.Id;

                ValidatePoolConfigurationInfoSection(result, resourcePoolId, resourcePool.ConfigurationInfo);
            }

            return result;
        }

        /// <summary>
        /// Returns the parameters mapped to the resources referencing them via the DOM Resource Capabilities section.
        /// </summary>
        /// <returns>Ids of the capabilities mapped to the ids of all resource pools referencing them through the DOM Resource Capabilities section.</returns>
        private Dictionary<Guid, ICollection<Guid>> GetResourcesPerCapability()
        {
            var result = new Dictionary<Guid, ICollection<Guid>>();

            var resourceFilter = DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resource.Id)
                .AND(new ORFilterElement<DomInstance>(parametersToValidate.Select(x => DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceCapabilities.ProfileParameterID).Equal(x.Id.ToString()))
                .ToArray()));

            var resourceInstances = planApi.DomHelpers.SlcResourceStudioHelper.GetResources(resourceFilter);

            foreach (var parameter in parametersToValidate)
            {
                HashSet<Guid> resourceIds = new HashSet<Guid>();
                foreach (var resourceInstance in resourceInstances)
                {
                    if (resourceInstance.ResourceCapabilities == null)
                        continue;

                    if (resourceInstance.ResourceCapabilities.Any(x => x.ProfileParameterID == parameter.Id.ToString()))
                        resourceIds.Add(resourceInstance.ID.Id);
                }

                if (resourceIds.Any())
                    result.Add(parameter.Id, resourceIds);
            }

            return result;
        }

        /// <summary>
        /// Returns the parameters mapped to the resources referencing them via the DOM Resource Capacities section.
        /// </summary>
        /// <returns>Ids of the capabilities mapped to the ids of all resource pools referencing them through the DOM Resource Capacities section.</returns>
        private Dictionary<Guid, ICollection<Guid>> GetResourcesPerCapacity()
        {
            var result = new Dictionary<Guid, ICollection<Guid>>();

            var resourceFilter = DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resource.Id)
                .AND(new ORFilterElement<DomInstance>(parametersToValidate.Select(x => DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceCapacities.ProfileParameterID).Equal(x.Id.ToString()))
                .ToArray()));

            var resourceInstances = planApi.DomHelpers.SlcResourceStudioHelper.GetResources(resourceFilter);

            foreach (var parameter in parametersToValidate)
            {
                HashSet<Guid> resourceIds = new HashSet<Guid>();
                foreach (var resourceInstance in resourceInstances)
                {
                    if (resourceInstance.ResourceCapacities == null)
                        continue;

                    if (resourceInstance.ResourceCapacities.Any(x => x.ProfileParameterID == parameter.Id.ToString()))
                        resourceIds.Add(resourceInstance.ID.Id);
                }

                if (resourceIds.Any())
                    result.Add(parameter.Id, resourceIds);
            }

            return result;
        }

        private IEnumerable<ConfigurationInstance> GetResourceStudioConfigurations(HashSet<Guid> parameterIds)
        {
            if (!parameterIds.Any())
            {
                return Enumerable.Empty<ConfigurationInstance>();
            }

            var configurationFilter = DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Configuration.Id)
                .AND(new ORFilterElement<DomInstance>(parameterIds.Select(x =>
                DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ProfileParameterValues.ProfileParameterID).Equal(x).OR(
                DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.OrchestrationEvents.ScriptInput).Contains(x.ToString())))
                .ToArray()));

            return planApi.DomHelpers.SlcResourceStudioHelper.GetConfigurations(configurationFilter);
        }

        private void ValidateCapabilities(ICollection<Capability> apiCapabilities)
        {
            if (!apiCapabilities.Any())
                return;

            resourceStudioConfigurationsPerParameter = GetResourceConfigurationsPerParameter(apiCapabilities.Select(x => x.Id).ToHashSet());
            resourceStudioConfigurationIds = resourceStudioConfigurationsPerParameter.SelectMany(x => x.Value).Distinct().ToHashSet();
            resourcePoolsReferencingConfigurations = GetResourcePoolsReferencingWorkflowsConfigurationInstances();

            ValidateResourcePoolCapabilityUsage();
            ValidateResourceCapabilityUsage();
        }

        private void ValidateCapacities(ICollection<Capacity> apiCapacities)
        {
            if (!apiCapacities.Any())
                return;

            resourceStudioConfigurationsPerParameter = GetResourceConfigurationsPerParameter(apiCapacities.Select(x => x.Id).ToHashSet());
            resourceStudioConfigurationIds = resourceStudioConfigurationsPerParameter.SelectMany(x => x.Value).Distinct().ToHashSet();
            resourcePoolsReferencingConfigurations = GetResourcePoolsReferencingWorkflowsConfigurationInstances();

            ValidateResourcePoolCapacityUsage();
            ValidateResourceCapacityUsage();
        }

        private void ValidateConfigurations(ICollection<Configuration> apiConfigurations)
        {
            if (!apiConfigurations.Any())
                return;

            resourceStudioConfigurationsPerParameter = GetResourceConfigurationsPerParameter(apiConfigurations.Select(x => x.Id).ToHashSet());
            resourceStudioConfigurationIds = resourceStudioConfigurationsPerParameter.SelectMany(x => x.Value).Distinct().ToHashSet();
            resourcePoolsReferencingConfigurations = GetResourcePoolsReferencingWorkflowsConfigurationInstances();

            ValidateResourcePoolConfigurationUsage();
        }
        private void ValidateOrchestrationEventsSection(Dictionary<Guid, List<Guid>> result, Guid configurationId, OrchestrationEventsSection orchestrationEventsSection)
        {
            if (orchestrationEventsSection.ScriptExecutionDetails?.ProfileParameterValues == null)
            {
                return;
            }

            foreach (var profileParameterValue in orchestrationEventsSection.ScriptExecutionDetails.ProfileParameterValues)
            {
                var profileParameterGuid = profileParameterValue.ProfileParameterId;
                if (!parameterIdsToValidate.Contains(profileParameterGuid))
                {
                    continue;
                }

                List<Guid> configurationIds;
                if (!result.TryGetValue(profileParameterGuid, out configurationIds))
                {
                    configurationIds = new List<Guid>();
                    result[profileParameterGuid] = configurationIds;
                }

                if (!configurationIds.Contains(configurationId))
                {
                    configurationIds.Add(configurationId);
                }
            }
        }

        private void ValidatePoolConfigurationInfoSection(Dictionary<Guid, List<Guid>> result, Guid jobId, ConfigurationInfoSection poolConfigurationSection)
        {
            if (!poolConfigurationSection.PoolConfiguration.HasValue)
                return;

            var poolConfigurationId = poolConfigurationSection.PoolConfiguration.Value;
            if (!resourceStudioConfigurationIds.Contains(poolConfigurationId))
                return;

            List<Guid> resourcePoolIds;
            if (!result.TryGetValue(poolConfigurationId, out resourcePoolIds))
            {
                resourcePoolIds = new List<Guid>();
                result[poolConfigurationId] = resourcePoolIds;
            }

            if (!resourcePoolIds.Contains(jobId))
            {
                resourcePoolIds.Add(jobId);
            }
        }

        private void ValidateProfileParameterValuesSection(Dictionary<Guid, List<Guid>> result, Guid configurationId, ProfileParameterValuesSection profileParameterValue)
        {
            Guid profileParameterGuid;
            if (!String.IsNullOrEmpty(profileParameterValue.ProfileParameterID) &&
                Guid.TryParse(profileParameterValue.ProfileParameterID, out profileParameterGuid) &&
                parameterIdsToValidate.Contains(profileParameterGuid))
            {
                List<Guid> configurationIds;
                if (!result.TryGetValue(profileParameterGuid, out configurationIds))
                {
                    configurationIds = new List<Guid>();
                    result[profileParameterGuid] = configurationIds;
                }

                if (!configurationIds.Contains(configurationId))
                {
                    configurationIds.Add(configurationId);
                }
            }
        }

        private void ValidateResourceCapabilityUsage()
        {
            var resourcesPerCapability = GetResourcesPerCapability();

            foreach (var parameter in parametersToValidate)
            {
                // Verify references by Resource via Resource Capabilities section
                if (!resourcesPerCapability.TryGetValue(parameter.Id, out var resourceIdsReferencingCapability))
                    continue;

                if (resourceIdsReferencingCapability.Any())
                {
                    ReportError(parameter.Id, new CapabilityInUseByResourcesError
                    {
                        Id = parameter.Id,
                        ErrorMessage = $"Capability '{parameter.Name}' is in use by {resourceIdsReferencingCapability.Count} resource(s).",
                        ResourceIds = resourceIdsReferencingCapability.ToArray(),
                    });
                }
            }
        }

        private void ValidateResourceCapacityUsage()
        {
            var resourcesPerCapacity = GetResourcesPerCapacity();

            foreach (var parameter in parametersToValidate)
            {
                // Verify references by Resource via Resource Capacities section
                if (!resourcesPerCapacity.TryGetValue(parameter.Id, out var resourceIdsReferencingCapacity))
                    continue;

                if (resourceIdsReferencingCapacity.Any())
                {
                    ReportError(parameter.Id, new CapacityInUseByResourcesError
                    {
                        Id = parameter.Id,
                        ErrorMessage = $"Capacity '{parameter.Name}' is in use by {resourceIdsReferencingCapacity.Count} resource(s).",
                        ResourceIds = resourceIdsReferencingCapacity.ToArray(),
                    });
                }
            }
        }

        private void ValidateResourcePoolCapabilityUsage()
        {
            var resourcePoolsPerCapability = GetResourcePoolsPerCapability();

            foreach (var parameter in parametersToValidate)
            {
                HashSet<Guid> referencingResourcePoolIds = new HashSet<Guid>();

                // Verify references by Resource Studio configuration instances
                if (resourceStudioConfigurationsPerParameter.TryGetValue(parameter.Id, out var referencedWorkflowConfigurationIds))
                {
                    // Parameter is referenced by resource studio configuration instances.
                    foreach (var referencedWorkflowConfigurationId in referencedWorkflowConfigurationIds)
                    {
                        if (!resourcePoolsReferencingConfigurations.TryGetValue(referencedWorkflowConfigurationId, out var resourcePoolIds))
                        {
                            // Resource Studio configuration is not referenced by any Resource Pool.
                            continue;
                        }

                        foreach (var resourcePoolId in resourcePoolIds)
                            referencingResourcePoolIds.Add(resourcePoolId);
                    }
                }

                // Verify references by Resource Pools via Resource Pool Capabilities section
                if (resourcePoolsPerCapability.TryGetValue(parameter.Id, out var resourcePoolIdsReferencingCapability))
                {
                    foreach (var resourcePoolId in resourcePoolIdsReferencingCapability)
                        referencingResourcePoolIds.Add(resourcePoolId);
                }

                if (referencingResourcePoolIds.Any())
                {
                    ReportError(parameter.Id, new CapabilityInUseByResourcePoolsError
                    {
                        Id = parameter.Id,
                        ErrorMessage = $"Capability '{parameter.Name}' is in use by {referencingResourcePoolIds.Count} resource pool(s).",
                        ResourcePoolIds = referencingResourcePoolIds.ToArray(),
                    });
                }
            }
        }

        private void ValidateResourcePoolCapacityUsage()
        {
            foreach (var parameter in parametersToValidate)
            {
                HashSet<Guid> referencingResourcePoolIds = new HashSet<Guid>();

                // Verify references by Resource Studio configuration instances
                if (resourceStudioConfigurationsPerParameter.TryGetValue(parameter.Id, out var referencedWorkflowConfigurationIds))
                {
                    // Parameter is referenced by resource studio configuration instances.
                    foreach (var referencedWorkflowConfigurationId in referencedWorkflowConfigurationIds)
                    {
                        if (!resourcePoolsReferencingConfigurations.TryGetValue(referencedWorkflowConfigurationId, out var resourcePoolIds))
                        {
                            // Resource Studio configuration is not referenced by any Resource Pool.
                            continue;
                        }

                        foreach (var resourcePoolId in resourcePoolIds)
                            referencingResourcePoolIds.Add(resourcePoolId);
                    }
                }

                if (referencingResourcePoolIds.Any())
                {
                    ReportError(parameter.Id, new CapacityInUseByResourcePoolsError
                    {
                        Id = parameter.Id,
                        ErrorMessage = $"Capacity '{parameter.Name}' is in use by {referencingResourcePoolIds.Count} resource pool(s).",
                        ResourcePoolIds = referencingResourcePoolIds.ToArray(),
                    });
                }
            }
        }
        private void ValidateResourcePoolConfigurationUsage()
        {
            foreach (var parameter in parametersToValidate)
            {
                HashSet<Guid> referencedResourcePoolIds = new HashSet<Guid>();
                if (!resourceStudioConfigurationsPerParameter.TryGetValue(parameter.Id, out var referencedWorkflowConfigurationIds))
                {
                    continue;
                }

                // Parameter is referenced by resource studio configuration instances.
                foreach (var referencedWorkflowConfigurationId in referencedWorkflowConfigurationIds)
                {
                    if (!resourcePoolsReferencingConfigurations.TryGetValue(referencedWorkflowConfigurationId, out var resourcePoolIds))
                    {
                        // Resource Studio configuration is not referenced by any Resource Pool.
                        continue;
                    }

                    foreach (var resourcePoolId in resourcePoolIds)
                        referencedResourcePoolIds.Add(resourcePoolId);
                }

                if (referencedResourcePoolIds.Any())
                {
                    ReportError(parameter.Id, new ConfigurationInUseByResourcePoolsError
                    {
                        Id = parameter.Id,
                        ErrorMessage = $"Configuration '{parameter.Name}' is in use by {referencedResourcePoolIds.Count} resource pool(s).",
                        ResourcePoolIds = referencedResourcePoolIds.ToArray(),
                    });
                }
            }
        }
    }
}
