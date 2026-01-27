namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;

    internal class SlcResourceStudioParameterDiscreteValueUsageValidator : ApiObjectValidator
    {
        private readonly MediaOpsPlanApi planApi;

        public SlcResourceStudioParameterDiscreteValueUsageValidator(MediaOpsPlanApi planApi)
        {
            this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
        }

        public static ApiObjectValidator Validate(MediaOpsPlanApi planApi, ICollection<ParameterDiscreteValue<string>> capabilityDiscreteValues)
        {
            if (capabilityDiscreteValues == null)
            {
                throw new ArgumentNullException(nameof(capabilityDiscreteValues));
            }

            var validator = new SlcResourceStudioParameterDiscreteValueUsageValidator(planApi);
            validator.ValidateCapabilityDiscreteValues(capabilityDiscreteValues);

            return validator;
        }

        public static ApiObjectValidator Validate(MediaOpsPlanApi planApi, ICollection<ParameterDiscreteValue<TextDiscreet>> configurationTextDiscreteValues)
        {
            var validator = new SlcResourceStudioParameterDiscreteValueUsageValidator(planApi);

            return validator;
        }

        public static ApiObjectValidator Validate(MediaOpsPlanApi planApi, ICollection<ParameterDiscreteValue<NumberDiscreet>> configurationNumberDiscreteValues)
        {
            var validator = new SlcResourceStudioParameterDiscreteValueUsageValidator(planApi);

            return validator;
        }

        private IEnumerable<OrchestrationSettings> GetOrchestrationSettings(HashSet<Guid> parameterIds)
        {
            if (parameterIds == null)
            {
                throw new ArgumentNullException(nameof(parameterIds));
            }

            if (!parameterIds.Any())
            {
                return Enumerable.Empty<OrchestrationSettings>();
            }

            var configurationFilter = DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Configuration.Id)
                .AND(new ORFilterElement<DomInstance>(parameterIds.Select(x =>
                DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ProfileParameterValues.ProfileParameterID).Equal(x).OR(
                DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.OrchestrationEvents.ScriptInput).Contains(x.ToString())))
                .ToArray()));

            return planApi.DomHelpers.SlcResourceStudioHelper.GetConfigurations(configurationFilter).Select(x => new ResourceStudioOrchestrationSettings(planApi, x));
        }

        private Dictionary<Guid, ICollection<Guid>> GetResourcePoolsPerOrchestrationSettings(HashSet<Guid> orchestrationSettingsIds)
        {
            if (orchestrationSettingsIds == null)
            {
                throw new ArgumentNullException(nameof(orchestrationSettingsIds));
            }

            var result = new Dictionary<Guid, ICollection<Guid>>();

            if (!orchestrationSettingsIds.Any())
            {
                return result;
            }

            var resourcePoolFilter = DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resourcepool.Id)
                .AND(new ORFilterElement<DomInstance>(orchestrationSettingsIds.Select(x => DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ConfigurationInfo.PoolConfiguration).Equal(x))
                .ToArray()));

            var resourcePoolInstances = planApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(resourcePoolFilter);
            if (resourcePoolInstances == null || !resourcePoolInstances.Any())
            {
                return result;
            }

            return resourcePoolInstances
                .Where(x => x.ConfigurationInfo.PoolConfiguration.HasValue
                    && x.ConfigurationInfo.PoolConfiguration.Value != Guid.Empty)
                .GroupBy(x => x.ConfigurationInfo.PoolConfiguration.Value)
                .ToDictionary(x => x.Key, x => (ICollection<Guid>)x.Select(y => y.ID.Id).ToList());
        }

        private Dictionary<ParameterDiscreteValue<string>, ICollection<Guid>> GetResourcesPerCapabilityDiscreteValue(ICollection<ParameterDiscreteValue<string>> capabilityDiscreteValues)
        {
            var result = new Dictionary<ParameterDiscreteValue<string>, ICollection<Guid>>();

            var resourceFilter = DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resource.Id)
                .AND(new ORFilterElement<DomInstance>(capabilityDiscreteValues.Select(x => DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceCapabilities.ProfileParameterID).Equal(x.ParameterId.ToString()))
                .ToArray()));

            var resourceInstances = planApi.DomHelpers.SlcResourceStudioHelper.GetResources(resourceFilter);

            foreach (var capabilityDiscreteValue in capabilityDiscreteValues)
            {
                HashSet<Guid> resourceIds = new HashSet<Guid>();
                foreach (var resourceInstance in resourceInstances)
                {
                    if (resourceInstance.ResourceCapabilities == null)
                    {
                        continue;
                    }

                    if (resourceInstance.ResourceCapabilities.Any(x => x.ProfileParameterId == capabilityDiscreteValue.ParameterId && x.DiscreteValues.Contains(capabilityDiscreteValue.DiscreteValue)))
                    {
                        resourceIds.Add(resourceInstance.ID.Id);
                    }
                }

                if (resourceIds.Any())
                {
                    result.Add(capabilityDiscreteValue, resourceIds);
                }
            }

            return result;
        }

        private Dictionary<ParameterDiscreteValue<string>, ICollection<Guid>> GetResourcePoolsPerCapabilityDiscreteValue(ICollection<ParameterDiscreteValue<string>> capabilityDiscreteValues)
        {
            var result = new Dictionary<ParameterDiscreteValue<string>, ICollection<Guid>>();

            var resourcePoolFilter = DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resourcepool.Id)
                .AND(new ORFilterElement<DomInstance>(capabilityDiscreteValues.Select(x => DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourcePoolCapabilities.ProfileParameterID).Equal(x.ParameterId.ToString()))
                .ToArray()));

            var resourcePoolInstances = planApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(resourcePoolFilter);

            foreach (var capabilityDiscreteValue in capabilityDiscreteValues)
            {
                HashSet<Guid> poolIds = new HashSet<Guid>();
                foreach (var resourcePoolInstance in resourcePoolInstances)
                {
                    if (resourcePoolInstance.ResourcePoolCapabilities == null)
                    {
                        continue;
                    }

                    if (resourcePoolInstance.ResourcePoolCapabilities.Any(x => x.ProfileParameterId == capabilityDiscreteValue.ParameterId && x.DiscreteValues.Contains(capabilityDiscreteValue.DiscreteValue)))
                    {
                        poolIds.Add(resourcePoolInstance.ID.Id);
                    }
                }

                if (poolIds.Any())
                {
                    result.Add(capabilityDiscreteValue, poolIds);
                }
            }

            return result;
        }

        private Dictionary<ParameterDiscreteValue<string>, ICollection<Guid>> GetOrchestrationSettingsPerCapabilityDiscreteValue(ICollection<ParameterDiscreteValue<string>> capabilityDiscreteValues)
        {
            var result = new Dictionary<ParameterDiscreteValue<string>, ICollection<Guid>>();

            var parameterIds = capabilityDiscreteValues.Select(x => x.ParameterId).ToHashSet();
            var orchestrationSettings = GetOrchestrationSettings(parameterIds);

            foreach (var capabilityDiscreteValue in capabilityDiscreteValues)
            {
                HashSet<Guid> orchestrationSettingsIds = new HashSet<Guid>();
                foreach (var orchestrationSetting in orchestrationSettings)
                {
                    if (orchestrationSetting.Capabilities.Any(x => x.Id == capabilityDiscreteValue.ParameterId && x.Discretes.Contains(capabilityDiscreteValue.DiscreteValue)))
                    {
                        orchestrationSettingsIds.Add(orchestrationSetting.Id);
                        continue;
                    }

                    if (orchestrationSetting.OrchestrationEvents.Any(x => x.ExecutionDetails.Capabilities.Any(y => y.Id == capabilityDiscreteValue.ParameterId && y.Discretes.Contains(capabilityDiscreteValue.DiscreteValue))))
                    {
                        orchestrationSettingsIds.Add(orchestrationSetting.Id);
                    }
                }

                if (orchestrationSettingsIds.Any())
                {
                    result.Add(capabilityDiscreteValue, orchestrationSettingsIds);
                }
            }

            return result;
        }

        private void ValidateCapabilityDiscreteValues(ICollection<ParameterDiscreteValue<string>> capabilityDiscreteValues)
        {
            if (capabilityDiscreteValues.Any())
            {
                return;
            }

            ValidateResourcePoolCapabilityDiscreteValueUsage(capabilityDiscreteValues);
            ValidateResourceCapabilityDiscreteValueUsage(capabilityDiscreteValues);
        }

        private void ValidateResourceCapabilityDiscreteValueUsage(ICollection<ParameterDiscreteValue<string>> capabilityDiscreteValues)
        {
            var resourcesPerCapabilityDiscreteValue = GetResourcesPerCapabilityDiscreteValue(capabilityDiscreteValues);

            foreach (var capabilityDisreteValue in capabilityDiscreteValues)
            {
                // Verify references by Resource via Resource Capacities section
                if (!resourcesPerCapabilityDiscreteValue.TryGetValue(capabilityDisreteValue, out var resourceIds))
                {
                    continue;
                }

                if (resourceIds.Any())
                {
                    ReportError(capabilityDisreteValue.ParameterId, new CapabilityDiscreteValueInUseByResourcesError
                    {
                        Id = capabilityDisreteValue.ParameterId,
                        DiscreteValue = capabilityDisreteValue.DiscreteValue,
                        ErrorMessage = $"Discrete value '{capabilityDisreteValue.DiscreteValue}' from Capability with ID '{capabilityDisreteValue.ParameterId}' is in use by {resourceIds.Count} resource(s).",
                        ResourceIds = resourceIds.ToArray(),
                    });
                }
            }
        }

        private void ValidateResourcePoolCapabilityDiscreteValueUsage(ICollection<ParameterDiscreteValue<string>> capabilityDiscreteValues)
        {
            var resourcePoolsPerCapabilityDiscreteValue = GetResourcePoolsPerCapabilityDiscreteValue(capabilityDiscreteValues);
            var orchestrationSettingsPerCapabilityDiscreteValue = GetOrchestrationSettingsPerCapabilityDiscreteValue(capabilityDiscreteValues);

            var orchestrationSettingIds = orchestrationSettingsPerCapabilityDiscreteValue.Values.SelectMany(x => x).ToHashSet();
            var resourcePoolsPerOrchestrationSettings = GetResourcePoolsPerOrchestrationSettings(orchestrationSettingIds);

            foreach (var capabilityDisreteValue in capabilityDiscreteValues)
            {
                var referencingResourcePoolIds = new HashSet<Guid>();
                if (resourcePoolsPerCapabilityDiscreteValue.TryGetValue(capabilityDisreteValue, out var poolIds))
                {
                    referencingResourcePoolIds.UnionWith(poolIds);
                }

                if (orchestrationSettingsPerCapabilityDiscreteValue.TryGetValue(capabilityDisreteValue, out var orchestrationSettingsIds))
                {
                    var orchestrationSettingsPoolIds = resourcePoolsPerOrchestrationSettings
                         .Where(x => orchestrationSettingsIds.Contains(x.Key))
                         .SelectMany(x => x.Value)
                         .ToHashSet();

                    referencingResourcePoolIds.UnionWith(orchestrationSettingsPoolIds);
                }

                if (referencingResourcePoolIds.Any())
                {
                    ReportError(capabilityDisreteValue.ParameterId, new CapabilityDiscreteValueInUseByResourcePoolsError
                    {
                        Id = capabilityDisreteValue.ParameterId,
                        DiscreteValue = capabilityDisreteValue.DiscreteValue,
                        ErrorMessage = $"Discrete value '{capabilityDisreteValue.DiscreteValue}' from Capability with ID '{capabilityDisreteValue.ParameterId}' is in use by {referencingResourcePoolIds.Count} resource pool(s).",
                        ResourcePoolIds = referencingResourcePoolIds.ToArray(),
                    });
                }
            }
        }
    }

    internal class ParameterDiscreteValue<T>
    {
        public Guid ParameterId { get; set; }

        public T DiscreteValue { get; set; }
    }
}
