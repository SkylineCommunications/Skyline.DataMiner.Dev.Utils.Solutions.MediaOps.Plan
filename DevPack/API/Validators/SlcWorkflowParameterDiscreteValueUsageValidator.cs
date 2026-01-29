namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;

    internal class SlcWorkflowParameterDiscreteValueUsageValidator : ApiObjectValidator
    {
        private readonly MediaOpsPlanApi planApi;

        public SlcWorkflowParameterDiscreteValueUsageValidator(MediaOpsPlanApi planApi)
        {
            this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
        }

        public static ApiObjectValidator Validate(MediaOpsPlanApi planApi, ICollection<ParameterDiscreteValue<string>> capabilityDiscreteValues)
        {
            if (capabilityDiscreteValues == null)
            {
                throw new ArgumentNullException(nameof(capabilityDiscreteValues));
            }

            var validator = new SlcWorkflowParameterDiscreteValueUsageValidator(planApi);
            validator.ValidateCapabilityDiscreteValues(capabilityDiscreteValues);

            return validator;
        }

        public static ApiObjectValidator Validate(MediaOpsPlanApi planApi, ICollection<ParameterDiscreteValue<TextDiscreet>> configurationTextDiscreteValues)
        {
            if (configurationTextDiscreteValues == null)
            {
                throw new ArgumentNullException(nameof(configurationTextDiscreteValues));
            }

            var validator = new SlcWorkflowParameterDiscreteValueUsageValidator(planApi);
            validator.ValidateConfigurationTextDiscreteValues(configurationTextDiscreteValues);

            return validator;
        }

        public static ApiObjectValidator Validate(MediaOpsPlanApi planApi, ICollection<ParameterDiscreteValue<NumberDiscreet>> configurationNumberDiscreteValues)
        {
            if (configurationNumberDiscreteValues == null)
            {
                throw new ArgumentNullException(nameof(configurationNumberDiscreteValues));
            }

            var validator = new SlcWorkflowParameterDiscreteValueUsageValidator(planApi);
            validator.ValidateConfigurationNumberDiscreteValues(configurationNumberDiscreteValues);

            return validator;
        }

        private static void AddObjectOrchestration(Dictionary<Guid, ICollection<Guid>> result, Guid objectId, Guid? configurationId)
        {
            if (!configurationId.HasValue
                || configurationId.Value == Guid.Empty)
            {
                return;
            }

            if (!result.TryGetValue(configurationId.Value, out var jobIds))
            {
                jobIds = new HashSet<Guid>();
                result.Add(configurationId.Value, jobIds);
            }

            if (!jobIds.Contains(objectId))
            {
                jobIds.Add(objectId);
            }
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

            var configurationFilter = DomInstanceExposers.DomDefinitionId.Equal(SlcWorkflowIds.Definitions.Configuration.Id)
                .AND(new ORFilterElement<DomInstance>(parameterIds.Select(x =>
                DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.ProfileParameterValues.ProfileParameterID).Equal(x).OR(
                DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.OrchestrationEvents.ScriptInputValues).Contains(x.ToString())))
                .ToArray()));

            return planApi.DomHelpers.SlcWorkflowHelper.GetConfigurations(configurationFilter).Select(x => new WorkflowOrchestrationSettings(planApi, x));
        }

        private Dictionary<Guid, ICollection<Guid>> GetJobsPerOrchestrationSettings(HashSet<Guid> orchestrationSettingsIds)
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

            var jobFilter = DomInstanceExposers.DomDefinitionId.Equal(SlcWorkflowIds.Definitions.Jobs.Id);

            var configurationFilters = orchestrationSettingsIds
                .SelectMany(id => new[]
                {
                    DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.JobExecution.JobConfiguration).Equal(id),
                    DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.Nodes.NodeConfiguration).Equal(id)
                })
                .ToArray();

            var configurationFilter = new ORFilterElement<DomInstance>(configurationFilters);
            var fullFilter = new ANDFilterElement<DomInstance>(jobFilter, configurationFilter);

            foreach (var job in planApi.DomHelpers.SlcWorkflowHelper.GetJobs(fullFilter))
            {
                AddObjectOrchestration(result ,job.ID.Id, job.JobExecution.JobConfiguration);

                foreach (var node in job.Nodes)
                {
                    AddObjectOrchestration(result, job.ID.Id, node.NodeConfiguration);
                }
            }

            return result;
        }

        private Dictionary<Guid, ICollection<Guid>> GetRecurringJobsPerOrchestrationSettings(HashSet<Guid> orchestrationSettingsIds)
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

            var recurringJobFilter = DomInstanceExposers.DomDefinitionId.Equal(SlcWorkflowIds.Definitions.RecurringJobs.Id);

            var configurationFilters = orchestrationSettingsIds
                .SelectMany(id => new[]
                {
                    DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.JobExecution.JobConfiguration).Equal(id),
                    DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.Nodes.NodeConfiguration).Equal(id),
                })
                .ToArray();

            var configurationFilter = new ORFilterElement<DomInstance>(configurationFilters);
            var fullFilter = new ANDFilterElement<DomInstance>(recurringJobFilter, configurationFilter);

            foreach (var job in planApi.DomHelpers.SlcWorkflowHelper.GetJobs(fullFilter))
            {
                AddObjectOrchestration(result, job.ID.Id, job.JobExecution.JobConfiguration);

                foreach (var node in job.Nodes)
                {
                    AddObjectOrchestration(result, job.ID.Id, node.NodeConfiguration);
                }
            }

            return result;
        }

        private Dictionary<Guid, ICollection<Guid>> GetWorkflowsPerOrchestrationSettings(HashSet<Guid> orchestrationSettingsIds)
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

            var workflowFilter = DomInstanceExposers.DomDefinitionId.Equal(SlcWorkflowIds.Definitions.Workflows.Id);

            var configurationFilters = orchestrationSettingsIds
                .SelectMany(id => new[]
                {
                    DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.WorkflowExecution.WorkflowConfiguration).Equal(id),
                    DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.Nodes.NodeConfiguration).Equal(id),
                })
                .ToArray();

            var configurationFilter = new ORFilterElement<DomInstance>(configurationFilters);
            var fullFilter = new ANDFilterElement<DomInstance>(workflowFilter, configurationFilter);

            foreach (var job in planApi.DomHelpers.SlcWorkflowHelper.GetJobs(fullFilter))
            {
                AddObjectOrchestration(result, job.ID.Id, job.JobExecution.JobConfiguration);

                foreach (var node in job.Nodes)
                {
                    AddObjectOrchestration(result, job.ID.Id, node.NodeConfiguration);
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

                        // Not needed to check further if found in capabilities.
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

        private Dictionary<ParameterDiscreteValue<TextDiscreet>, ICollection<Guid>> GetOrchestrationSettingsPerConfigurationTextDiscreteValue(ICollection<ParameterDiscreteValue<TextDiscreet>> configurationTextDiscreteValues)
        {
            var result = new Dictionary<ParameterDiscreteValue<TextDiscreet>, ICollection<Guid>>();

            var parameterIds = configurationTextDiscreteValues.Select(x => x.ParameterId).ToHashSet();
            var orchestrationSettings = GetOrchestrationSettings(parameterIds);

            foreach (var configurationTextDiscreteValue in configurationTextDiscreteValues)
            {
                HashSet<Guid> orchestrationSettingsIds = new HashSet<Guid>();
                foreach (var orchestrationSetting in orchestrationSettings)
                {
                    if (orchestrationSetting.Configurations.OfType<DiscreteTextConfigurationSetting>().Any(x => x.Id == configurationTextDiscreteValue.ParameterId && x.Value.Value.Equals(configurationTextDiscreteValue.DiscreteValue.Value)))
                    {
                        orchestrationSettingsIds.Add(orchestrationSetting.Id);

                        // Not needed to check further if found in configurations.
                        continue;
                    }

                    if (orchestrationSetting.OrchestrationEvents.Any(x => x.ExecutionDetails.Configurations.OfType<DiscreteTextConfigurationSetting>().Any(y => y.Id == configurationTextDiscreteValue.ParameterId && y.Value.Value.Equals(configurationTextDiscreteValue.DiscreteValue.Value))))
                    {
                        orchestrationSettingsIds.Add(orchestrationSetting.Id);
                    }
                }

                if (orchestrationSettingsIds.Any())
                {
                    result.Add(configurationTextDiscreteValue, orchestrationSettingsIds);
                }
            }

            return result;
        }

        private Dictionary<ParameterDiscreteValue<NumberDiscreet>, ICollection<Guid>> GetOrchestrationSettingsPerConfigurationNumberDiscreteValue(ICollection<ParameterDiscreteValue<NumberDiscreet>> configurationNumberDiscreteValues)
        {
            var result = new Dictionary<ParameterDiscreteValue<NumberDiscreet>, ICollection<Guid>>();

            var parameterIds = configurationNumberDiscreteValues.Select(x => x.ParameterId).ToHashSet();
            var orchestrationSettings = GetOrchestrationSettings(parameterIds);

            foreach (var configurationNumberDiscreteValue in configurationNumberDiscreteValues)
            {
                HashSet<Guid> orchestrationSettingsIds = new HashSet<Guid>();
                foreach (var orchestrationSetting in orchestrationSettings)
                {
                    if (orchestrationSetting.Configurations.OfType<DiscreteNumberConfigurationSetting>().Any(x => x.Id == configurationNumberDiscreteValue.ParameterId && x.Value.Value.Equals(configurationNumberDiscreteValue.DiscreteValue.Value)))
                    {
                        orchestrationSettingsIds.Add(orchestrationSetting.Id);

                        // Not needed to check further if found in configurations.
                        continue;
                    }

                    if (orchestrationSetting.OrchestrationEvents.Any(x => x.ExecutionDetails.Configurations.OfType<DiscreteNumberConfigurationSetting>().Any(y => y.Id == configurationNumberDiscreteValue.ParameterId && y.Value.Value.Equals(configurationNumberDiscreteValue.DiscreteValue.Value))))
                    {
                        orchestrationSettingsIds.Add(orchestrationSetting.Id);
                    }
                }

                if (orchestrationSettingsIds.Any())
                {
                    result.Add(configurationNumberDiscreteValue, orchestrationSettingsIds);
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

            var orchestrationSettingsPerCapabilityDiscreteValue = GetOrchestrationSettingsPerCapabilityDiscreteValue(capabilityDiscreteValues);

            ValidateJobCapabilityDiscreteValueUsage(capabilityDiscreteValues, orchestrationSettingsPerCapabilityDiscreteValue);
            ValidateRecurringJobCapabilityDiscreteValueUsage(capabilityDiscreteValues, orchestrationSettingsPerCapabilityDiscreteValue);
            ValidateWorkflowCapabilityDiscreteValueUsage(capabilityDiscreteValues, orchestrationSettingsPerCapabilityDiscreteValue);
        }

        private void ValidateConfigurationTextDiscreteValues(ICollection<ParameterDiscreteValue<TextDiscreet>> configurationTextDiscreteValues)
        {
            if (configurationTextDiscreteValues.Any())
            {
                return;
            }

            var orchestrationSettingsPerConfigurationTextDiscreteValue = GetOrchestrationSettingsPerConfigurationTextDiscreteValue(configurationTextDiscreteValues);

            ValidateJobConfigurationTextDiscreteValueUsage(configurationTextDiscreteValues, orchestrationSettingsPerConfigurationTextDiscreteValue);
            ValidateRecurringJobConfigurationTextDiscreteValueUsage(configurationTextDiscreteValues, orchestrationSettingsPerConfigurationTextDiscreteValue);
            ValidateWorkflowConfigurationTextDiscreteValueUsage(configurationTextDiscreteValues, orchestrationSettingsPerConfigurationTextDiscreteValue);
        }

        private void ValidateConfigurationNumberDiscreteValues(ICollection<ParameterDiscreteValue<NumberDiscreet>> configurationNumberDiscreteValues)
        {
            if (configurationNumberDiscreteValues.Any())
            {
                return;
            }

            var orchestrationSettingsPerConfigurationNumberDiscreteValue = GetOrchestrationSettingsPerConfigurationNumberDiscreteValue(configurationNumberDiscreteValues);

            ValidateJobConfigurationNumberDiscreteValueUsage(configurationNumberDiscreteValues, orchestrationSettingsPerConfigurationNumberDiscreteValue);
            ValidateRecurringJobConfigurationNumberDiscreteValueUsage(configurationNumberDiscreteValues, orchestrationSettingsPerConfigurationNumberDiscreteValue);
            ValidateWorkflowConfigurationNumberDiscreteValueUsage(configurationNumberDiscreteValues, orchestrationSettingsPerConfigurationNumberDiscreteValue);
        }

        private void ValidateJobCapabilityDiscreteValueUsage(ICollection<ParameterDiscreteValue<string>> capabilityDiscreteValues, Dictionary<ParameterDiscreteValue<string>, ICollection<Guid>> orchestrationSettingsPerCapabilityDiscreteValue)
        {
            var orchestrationSettingIds = orchestrationSettingsPerCapabilityDiscreteValue.Values.SelectMany(x => x).ToHashSet();
            var jobsPerOrchestrationSettings = GetJobsPerOrchestrationSettings(orchestrationSettingIds);

            foreach (var capabilityDisreteValue in capabilityDiscreteValues)
            {
                if (!orchestrationSettingsPerCapabilityDiscreteValue.TryGetValue(capabilityDisreteValue, out var orchestrationSettingsIds))
                {
                    continue;
                }

                var orchestrationSettingsJobIds = jobsPerOrchestrationSettings
                    .Where(x => orchestrationSettingsIds.Contains(x.Key))
                    .SelectMany(x => x.Value)
                    .ToHashSet();

                if (orchestrationSettingsJobIds.Any())
                {
                    ReportError(capabilityDisreteValue.ParameterId, new CapabilityDiscreteValueInUseByJobsError
                    {
                        Id = capabilityDisreteValue.ParameterId,
                        DiscreteValue = capabilityDisreteValue.DiscreteValue,
                        ErrorMessage = $"Discrete value '{capabilityDisreteValue.DiscreteValue}' from Capability with ID '{capabilityDisreteValue.ParameterId}' is in use by {orchestrationSettingsJobIds.Count} job(s).",
                        JobIds = orchestrationSettingsJobIds.ToArray(),
                    });
                }
            }
        }

        private void ValidateRecurringJobCapabilityDiscreteValueUsage(ICollection<ParameterDiscreteValue<string>> capabilityDiscreteValues, Dictionary<ParameterDiscreteValue<string>, ICollection<Guid>> orchestrationSettingsPerCapabilityDiscreteValue)
        {
            var orchestrationSettingIds = orchestrationSettingsPerCapabilityDiscreteValue.Values.SelectMany(x => x).ToHashSet();
            var recurringJobsPerOrchestrationSettings = GetRecurringJobsPerOrchestrationSettings(orchestrationSettingIds);

            foreach (var capabilityDisreteValue in capabilityDiscreteValues)
            {
                if (!orchestrationSettingsPerCapabilityDiscreteValue.TryGetValue(capabilityDisreteValue, out var orchestrationSettingsIds))
                {
                    continue;
                }

                var orchestrationSettingsRecurringJobIds = recurringJobsPerOrchestrationSettings
                    .Where(x => orchestrationSettingsIds.Contains(x.Key))
                    .SelectMany(x => x.Value)
                    .ToHashSet();

                if (orchestrationSettingsRecurringJobIds.Any())
                {
                    ReportError(capabilityDisreteValue.ParameterId, new CapabilityDiscreteValueInUseByRecurringJobsError
                    {
                        Id = capabilityDisreteValue.ParameterId,
                        DiscreteValue = capabilityDisreteValue.DiscreteValue,
                        ErrorMessage = $"Discrete value '{capabilityDisreteValue.DiscreteValue}' from Capability with ID '{capabilityDisreteValue.ParameterId}' is in use by {orchestrationSettingsRecurringJobIds.Count} recurring job(s).",
                        RecurringJobIds = orchestrationSettingsRecurringJobIds.ToArray(),
                    });
                }
            }
        }

        private void ValidateWorkflowCapabilityDiscreteValueUsage(ICollection<ParameterDiscreteValue<string>> capabilityDiscreteValues, Dictionary<ParameterDiscreteValue<string>, ICollection<Guid>> orchestrationSettingsPerCapabilityDiscreteValue)
        {
            var orchestrationSettingIds = orchestrationSettingsPerCapabilityDiscreteValue.Values.SelectMany(x => x).ToHashSet();
            var workflowsPerOrchestrationSettings = GetWorkflowsPerOrchestrationSettings(orchestrationSettingIds);

            foreach (var capabilityDisreteValue in capabilityDiscreteValues)
            {
                if (!orchestrationSettingsPerCapabilityDiscreteValue.TryGetValue(capabilityDisreteValue, out var orchestrationSettingsIds))
                {
                    continue;
                }

                var orchestrationSettingsWorkflowIds = workflowsPerOrchestrationSettings
                    .Where(x => orchestrationSettingsIds.Contains(x.Key))
                    .SelectMany(x => x.Value)
                    .ToHashSet();

                if (orchestrationSettingsWorkflowIds.Any())
                {
                    ReportError(capabilityDisreteValue.ParameterId, new CapabilityDiscreteValueInUseByWorkflowsError
                    {
                        Id = capabilityDisreteValue.ParameterId,
                        DiscreteValue = capabilityDisreteValue.DiscreteValue,
                        ErrorMessage = $"Discrete value '{capabilityDisreteValue.DiscreteValue}' from Capability with ID '{capabilityDisreteValue.ParameterId}' is in use by {orchestrationSettingsWorkflowIds.Count} workflow(s).",
                        WorkflowIds = orchestrationSettingsWorkflowIds.ToArray(),
                    });
                }
            }
        }

        private void ValidateJobConfigurationTextDiscreteValueUsage(ICollection<ParameterDiscreteValue<TextDiscreet>> configurationTextDiscreteValues, Dictionary<ParameterDiscreteValue<TextDiscreet>, ICollection<Guid>> orchestrationSettingsPerConfigurationTextDiscreteValue)
        {
            var orchestrationSettingIds = orchestrationSettingsPerConfigurationTextDiscreteValue.Values.SelectMany(x => x).ToHashSet();
            var jobsPerOrchestrationSettings = GetJobsPerOrchestrationSettings(orchestrationSettingIds);

            foreach (var configurationTextDiscreteValue in configurationTextDiscreteValues)
            {
                if (!orchestrationSettingsPerConfigurationTextDiscreteValue.TryGetValue(configurationTextDiscreteValue, out var orchestrationSettingsIds))
                {
                    continue;
                }

                var orchestrationSettingsJobIds = jobsPerOrchestrationSettings
                    .Where(x => orchestrationSettingsIds.Contains(x.Key))
                    .SelectMany(x => x.Value)
                    .ToHashSet();

                if (orchestrationSettingsJobIds.Any())
                {
                    ReportError(configurationTextDiscreteValue.ParameterId, new ConfigurationTextDiscreteValueInUseByJobsError
                    {
                        Id = configurationTextDiscreteValue.ParameterId,
                        DiscreteValue = configurationTextDiscreteValue.DiscreteValue,
                        ErrorMessage = $"Discrete value '{configurationTextDiscreteValue.DiscreteValue.Value}' ({configurationTextDiscreteValue.DiscreteValue.DisplayName}) from Configuration with ID '{configurationTextDiscreteValue.ParameterId}' is in use by {orchestrationSettingsJobIds.Count} job(s).",
                        JobIds = orchestrationSettingsJobIds.ToArray(),
                    });
                }
            }
        }

        private void ValidateRecurringJobConfigurationTextDiscreteValueUsage(ICollection<ParameterDiscreteValue<TextDiscreet>> configurationTextDiscreteValues, Dictionary<ParameterDiscreteValue<TextDiscreet>, ICollection<Guid>> orchestrationSettingsPerConfigurationTextDiscreteValue)
        {
            var orchestrationSettingIds = orchestrationSettingsPerConfigurationTextDiscreteValue.Values.SelectMany(x => x).ToHashSet();
            var recurringJobsPerOrchestrationSettings = GetRecurringJobsPerOrchestrationSettings(orchestrationSettingIds);

            foreach (var configurationTextDiscreteValue in configurationTextDiscreteValues)
            {
                if (!orchestrationSettingsPerConfigurationTextDiscreteValue.TryGetValue(configurationTextDiscreteValue, out var orchestrationSettingsIds))
                {
                    continue;
                }

                var orchestrationSettingsRecurringJobIds = recurringJobsPerOrchestrationSettings
                    .Where(x => orchestrationSettingsIds.Contains(x.Key))
                    .SelectMany(x => x.Value)
                    .ToHashSet();

                if (orchestrationSettingsRecurringJobIds.Any())
                {
                    ReportError(configurationTextDiscreteValue.ParameterId, new ConfigurationTextDiscreteValueInUseByRecurringJobsError
                    {
                        Id = configurationTextDiscreteValue.ParameterId,
                        DiscreteValue = configurationTextDiscreteValue.DiscreteValue,
                        ErrorMessage = $"Discrete value '{configurationTextDiscreteValue.DiscreteValue.Value}' ({configurationTextDiscreteValue.DiscreteValue.DisplayName}) from Configuration with ID '{configurationTextDiscreteValue.ParameterId}' is in use by {orchestrationSettingsRecurringJobIds.Count} recurring job(s).",
                        RecurringJobIds = orchestrationSettingsRecurringJobIds.ToArray(),
                    });
                }
            }
        }

        private void ValidateWorkflowConfigurationTextDiscreteValueUsage(ICollection<ParameterDiscreteValue<TextDiscreet>> configurationTextDiscreteValues, Dictionary<ParameterDiscreteValue<TextDiscreet>, ICollection<Guid>> orchestrationSettingsPerConfigurationTextDiscreteValue)
        {
            var orchestrationSettingIds = orchestrationSettingsPerConfigurationTextDiscreteValue.Values.SelectMany(x => x).ToHashSet();
            var workflowsPerOrchestrationSettings = GetWorkflowsPerOrchestrationSettings(orchestrationSettingIds);

            foreach (var configurationTextDiscreteValue in configurationTextDiscreteValues)
            {
                if (!orchestrationSettingsPerConfigurationTextDiscreteValue.TryGetValue(configurationTextDiscreteValue, out var orchestrationSettingsIds))
                {
                    continue;
                }

                var orchestrationSettingsRecurringJobIds = workflowsPerOrchestrationSettings
                    .Where(x => orchestrationSettingsIds.Contains(x.Key))
                    .SelectMany(x => x.Value)
                    .ToHashSet();

                if (orchestrationSettingsRecurringJobIds.Any())
                {
                    ReportError(configurationTextDiscreteValue.ParameterId, new ConfigurationTextDiscreteValueInUseByWorkflowsError
                    {
                        Id = configurationTextDiscreteValue.ParameterId,
                        DiscreteValue = configurationTextDiscreteValue.DiscreteValue,
                        ErrorMessage = $"Discrete value '{configurationTextDiscreteValue.DiscreteValue.Value}' ({configurationTextDiscreteValue.DiscreteValue.DisplayName}) from Configuration with ID '{configurationTextDiscreteValue.ParameterId}' is in use by {orchestrationSettingsRecurringJobIds.Count} workflow(s).",
                        WorkflowIds = orchestrationSettingsRecurringJobIds.ToArray(),
                    });
                }
            }
        }

        private void ValidateJobConfigurationNumberDiscreteValueUsage(ICollection<ParameterDiscreteValue<NumberDiscreet>> configurationNumberDiscreteValues, Dictionary<ParameterDiscreteValue<NumberDiscreet>, ICollection<Guid>> orchestrationSettingsPerConfigurationNumberDiscreteValue)
        {
            var orchestrationSettingIds = orchestrationSettingsPerConfigurationNumberDiscreteValue.Values.SelectMany(x => x).ToHashSet();
            var jobsPerOrchestrationSettings = GetJobsPerOrchestrationSettings(orchestrationSettingIds);

            foreach (var configurationNumberDiscreteValue in configurationNumberDiscreteValues)
            {
                if (!orchestrationSettingsPerConfigurationNumberDiscreteValue.TryGetValue(configurationNumberDiscreteValue, out var orchestrationSettingsIds))
                {
                    continue;
                }

                var orchestrationSettingsJobIds = jobsPerOrchestrationSettings
                    .Where(x => orchestrationSettingsIds.Contains(x.Key))
                    .SelectMany(x => x.Value)
                    .ToHashSet();

                if (orchestrationSettingsJobIds.Any())
                {
                    ReportError(configurationNumberDiscreteValue.ParameterId, new ConfigurationNumberDiscreteValueInUseByJobsError
                    {
                        Id = configurationNumberDiscreteValue.ParameterId,
                        DiscreteValue = configurationNumberDiscreteValue.DiscreteValue,
                        ErrorMessage = $"Discrete value '{configurationNumberDiscreteValue.DiscreteValue.Value}' ({configurationNumberDiscreteValue.DiscreteValue.DisplayName}) from Configuration with ID '{configurationNumberDiscreteValue.ParameterId}' is in use by {orchestrationSettingsJobIds.Count} job(s).",
                        JobIds = orchestrationSettingsJobIds.ToArray(),
                    });
                }
            }
        }

        private void ValidateRecurringJobConfigurationNumberDiscreteValueUsage(ICollection<ParameterDiscreteValue<NumberDiscreet>> configurationNumberDiscreteValues, Dictionary<ParameterDiscreteValue<NumberDiscreet>, ICollection<Guid>> orchestrationSettingsPerConfigurationNumberDiscreteValue)
        {
            var orchestrationSettingIds = orchestrationSettingsPerConfigurationNumberDiscreteValue.Values.SelectMany(x => x).ToHashSet();
            var recurringJobsPerOrchestrationSettings = GetRecurringJobsPerOrchestrationSettings(orchestrationSettingIds);

            foreach (var configurationNumberDiscreteValue in configurationNumberDiscreteValues)
            {
                if (!orchestrationSettingsPerConfigurationNumberDiscreteValue.TryGetValue(configurationNumberDiscreteValue, out var orchestrationSettingsIds))
                {
                    continue;
                }

                var orchestrationSettingsRecurringJobIds = recurringJobsPerOrchestrationSettings
                    .Where(x => orchestrationSettingsIds.Contains(x.Key))
                    .SelectMany(x => x.Value)
                    .ToHashSet();

                if (orchestrationSettingsRecurringJobIds.Any())
                {
                    ReportError(configurationNumberDiscreteValue.ParameterId, new ConfigurationNumberDiscreteValueInUseByRecurringJobsError
                    {
                        Id = configurationNumberDiscreteValue.ParameterId,
                        DiscreteValue = configurationNumberDiscreteValue.DiscreteValue,
                        ErrorMessage = $"Discrete value '{configurationNumberDiscreteValue.DiscreteValue.Value}' ({configurationNumberDiscreteValue.DiscreteValue.DisplayName}) from Configuration with ID '{configurationNumberDiscreteValue.ParameterId}' is in use by {orchestrationSettingsRecurringJobIds.Count} recurring job(s).",
                        RecurringJobIds = orchestrationSettingsRecurringJobIds.ToArray(),
                    });
                }
            }
        }

        private void ValidateWorkflowConfigurationNumberDiscreteValueUsage(ICollection<ParameterDiscreteValue<NumberDiscreet>> configurationNumberDiscreteValues, Dictionary<ParameterDiscreteValue<NumberDiscreet>, ICollection<Guid>> orchestrationSettingsPerConfigurationNumberDiscreteValue)
        {
            var orchestrationSettingIds = orchestrationSettingsPerConfigurationNumberDiscreteValue.Values.SelectMany(x => x).ToHashSet();
            var workflowsPerOrchestrationSettings = GetWorkflowsPerOrchestrationSettings(orchestrationSettingIds);

            foreach (var configurationNumberDiscreteValue in configurationNumberDiscreteValues)
            {
                if (!orchestrationSettingsPerConfigurationNumberDiscreteValue.TryGetValue(configurationNumberDiscreteValue, out var orchestrationSettingsIds))
                {
                    continue;
                }

                var orchestrationSettingsRecurringJobIds = workflowsPerOrchestrationSettings
                    .Where(x => orchestrationSettingsIds.Contains(x.Key))
                    .SelectMany(x => x.Value)
                    .ToHashSet();

                if (orchestrationSettingsRecurringJobIds.Any())
                {
                    ReportError(configurationNumberDiscreteValue.ParameterId, new ConfigurationNumberDiscreteValueInUseByWorkflowsError
                    {
                        Id = configurationNumberDiscreteValue.ParameterId,
                        DiscreteValue = configurationNumberDiscreteValue.DiscreteValue,
                        ErrorMessage = $"Discrete value '{configurationNumberDiscreteValue.DiscreteValue.Value}' ({configurationNumberDiscreteValue.DiscreteValue.DisplayName}) from Configuration with ID '{configurationNumberDiscreteValue.ParameterId}' is in use by {orchestrationSettingsRecurringJobIds.Count} workflow(s).",
                        WorkflowIds = orchestrationSettingsRecurringJobIds.ToArray(),
                    });
                }
            }
        }
    }
}
