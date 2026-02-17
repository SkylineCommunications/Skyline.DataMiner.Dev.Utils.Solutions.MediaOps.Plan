namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Helper;
    using Skyline.DataMiner.Net.Jobs;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;

    internal class SlcWorkflowParameterUsageValidator : ApiObjectValidator
    {
        private readonly HashSet<Guid> parameterIdsToValidate;
        private readonly IReadOnlyCollection<Parameter> parametersToValidate;
        private readonly MediaOpsPlanApi planApi;
        private HashSet<Guid> workflowConfigurationIds;
        private Dictionary<Guid, List<Guid>> workflowConfigurationsPerParameter;

        private SlcWorkflowParameterUsageValidator(MediaOpsPlanApi planApi, IReadOnlyCollection<Parameter> parametersToValidate)
        {
            this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
            this.parametersToValidate = parametersToValidate ?? throw new ArgumentNullException(nameof(parametersToValidate));
            parameterIdsToValidate = parametersToValidate.Select(x => x.ID).ToHashSet();
        }

        public static ApiObjectValidator Validate(MediaOpsPlanApi planApi, ICollection<Configuration> configurationsToValidate)
        {
            if (configurationsToValidate == null)
                throw new ArgumentNullException(nameof(configurationsToValidate));

            var validator = new SlcWorkflowParameterUsageValidator(planApi, configurationsToValidate.ToList<Parameter>());
            validator.ValidateConfigurations();
            return validator;
        }

        public static ApiObjectValidator Validate(MediaOpsPlanApi planApi, ICollection<Capability> capabilitiesToValidate)
        {
            if (capabilitiesToValidate == null)
                throw new ArgumentNullException(nameof(capabilitiesToValidate));

            var validator = new SlcWorkflowParameterUsageValidator(planApi, capabilitiesToValidate.ToList<Parameter>());
            validator.ValidateCapabilities();
            return validator;
        }

        public static ApiObjectValidator Validate(MediaOpsPlanApi planApi, ICollection<Capacity> capacitiesToValidate)
        {
            if (capacitiesToValidate == null)
                throw new ArgumentNullException(nameof(capacitiesToValidate));

            var validator = new SlcWorkflowParameterUsageValidator(planApi, capacitiesToValidate.ToList<Parameter>());
            validator.ValidateCapacities();
            return validator;
        }

        private IEnumerable<JobsInstance> GetJobInstancesReferencingConfigurations()
        {
            var jobFilter = new ANDFilterElement<DomInstance>(
                    DomInstanceExposers.DomDefinitionId.Equal(SlcWorkflowIds.Definitions.Jobs.Id),
                    DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.JobInfo.Postroll).GreaterThan(DateTimeOffset.UtcNow),
                    DomInstanceExposers.StatusId.NotEqual(SlcWorkflowIds.Behaviors.Job_Behavior.Statuses.ToValue(SlcWorkflowIds.Behaviors.Job_Behavior.StatusesEnum.Canceled))
                );

            var jobConfigurationFilter = new ORFilterElement<DomInstance>(workflowConfigurationIds
                .Select(id => DomInstanceExposers.FieldValues
                    .DomInstanceField(SlcWorkflowIds.Sections.JobExecution.JobConfiguration)
                    .Equal(id))
                .ToArray());

            var nodeConfigurationFilter = new ORFilterElement<DomInstance>(workflowConfigurationIds
                .Select(id => DomInstanceExposers.FieldValues
                    .DomInstanceField(SlcWorkflowIds.Sections.Nodes.NodeConfiguration)
                    .Equal(id))
                .ToArray());

            var jobs = planApi.DomHelpers.SlcWorkflowHelper
                .GetJobs(jobFilter.AND(jobConfigurationFilter.OR(nodeConfigurationFilter)))
                .DistinctBy(x => x.ID);

            return jobs;
        }

        private Dictionary<Guid, List<Guid>> GetJobsReferencingConfigurations()
        {
            var result = new Dictionary<Guid, List<Guid>>();
            var jobs = GetJobInstancesReferencingConfigurations();

            foreach (var job in jobs)
            {
                var jobId = job.ID.Id;

                if (job.JobExecution != null)
                {
                    ValidateJobExecutionSection(result, jobId, job.JobExecution);
                }

                if (job.Nodes != null)
                {
                    foreach (var nodeSection in job.Nodes)
                        ValidateNodeSection(result, jobId, nodeSection);
                }
            }

            return result;
        }

        private IEnumerable<RecurringJobsInstance> GetRecurringJobInstancesReferencingConfigurations()
        {
            var recurringJobFilter = new ANDFilterElement<DomInstance>(
                    DomInstanceExposers.DomDefinitionId.Equal(SlcWorkflowIds.Definitions.RecurringJobs.Id),
                    DomInstanceExposers.StatusId.Equal(SlcWorkflowIds.Behaviors.Recurringjob_Behavior.Statuses.ToValue(SlcWorkflowIds.Behaviors.Recurringjob_Behavior.StatusesEnum.Active))
                );

            var jobConfigurationFilter = new ORFilterElement<DomInstance>(workflowConfigurationIds
                .Select(id => DomInstanceExposers.FieldValues
                    .DomInstanceField(SlcWorkflowIds.Sections.JobExecution.JobConfiguration)
                    .Equal(id))
                .ToArray());

            var nodeConfigurationFilter = new ORFilterElement<DomInstance>(workflowConfigurationIds
                .Select(id => DomInstanceExposers.FieldValues
                    .DomInstanceField(SlcWorkflowIds.Sections.Nodes.NodeConfiguration)
                    .Equal(id))
                .ToArray());

            var recurringJobInstances = planApi.DomHelpers.SlcWorkflowHelper
                .GetRecurringJobs(recurringJobFilter.AND(jobConfigurationFilter.OR(nodeConfigurationFilter)))
                .DistinctBy(x => x.ID);

            return recurringJobInstances;
        }

        private Dictionary<Guid, List<Guid>> GetRecurringJobsReferencingConfigurations()
        {
            var result = new Dictionary<Guid, List<Guid>>();
            var recurringJobInstances = GetRecurringJobInstancesReferencingConfigurations();

            foreach (var recurringJob in recurringJobInstances)
            {
                var recurringJobId = recurringJob.ID.Id;

                if (recurringJob.JobExecution != null)
                {
                    ValidateJobExecutionSection(result, recurringJobId, recurringJob.JobExecution);
                }

                if (recurringJob.Nodes != null)
                {
                    foreach (var nodeSection in recurringJob.Nodes)
                        ValidateNodeSection(result, recurringJobId, nodeSection);
                }
            }

            return result;
        }

        private IEnumerable<ConfigurationInstance> GetWorkflowConfigurations(HashSet<Guid> apiParameterIds)
        {
            var configurationFilter = DomInstanceExposers.DomDefinitionId.Equal(SlcWorkflowIds.Definitions.Configuration.Id);
            configurationFilter.AND(new ORFilterElement<DomInstance>(apiParameterIds.Select(x =>
                DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.ProfileParameterValues.ProfileParameterID).Equal(x).OR(
                DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.OrchestrationEvents.ScriptInputValues).Contains(x.ToString())))
                .ToArray()));

            return planApi.DomHelpers.SlcWorkflowHelper.GetConfigurations(configurationFilter);
        }

        /// <summary>
        /// Returns a mapping of API Parameter IDs to the Configuration Instance IDs that reference them.
        /// </summary>
        /// <param name="parameterIds">Ids of the API parameters to check.</param>
        /// <returns>A dictionary where the key is the Configuration Instance ID and the value is the list of referenced Parameter IDs.</returns>
        private Dictionary<Guid, List<Guid>> GetWorkflowConfigurationsPerParameter(HashSet<Guid> parameterIds)
        {
            var result = new Dictionary<Guid, List<Guid>>();

            if (parameterIds == null || parameterIds.Count == 0)
            {
                return result;
            }

            var possibleWorkflowConfigurations = GetWorkflowConfigurations(parameterIds);

            foreach (var possibleWorkflowConfiguration in possibleWorkflowConfigurations)
            {
                var configurationId = possibleWorkflowConfiguration.ID.Id;

                // ProfileParameterValues section
                foreach (var profileParameterValue in possibleWorkflowConfiguration.ProfileParameterValues)
                {
                    ValidateProfileParameterValuesSection(result, configurationId, profileParameterValue);
                }

                // OrchestrationEvents section
                foreach (var section in possibleWorkflowConfiguration.OrchestrationEvents)
                {
                    ValidateOrchestrationEventsSection(result, configurationId, section);
                }
            }

            return result;
        }

        private IEnumerable<WorkflowsInstance> GetWorkflowInstancesReferencingConfigurations()
        {
            var workflowFilter = DomInstanceExposers.DomDefinitionId.Equal(SlcWorkflowIds.Definitions.Workflows.Id);
            var workflowConfigurationFilter = new ORFilterElement<DomInstance>(workflowConfigurationIds
                .Select(id => DomInstanceExposers.FieldValues
                    .DomInstanceField(SlcWorkflowIds.Sections.WorkflowExecution.WorkflowConfiguration)
                    .Equal(id))
                .ToArray());

            var nodeConfigurationFilter = new ORFilterElement<DomInstance>(workflowConfigurationIds
                .Select(id => DomInstanceExposers.FieldValues
                    .DomInstanceField(SlcWorkflowIds.Sections.Nodes.NodeConfiguration)
                    .Equal(id))
                .ToArray());

            var workflowInstances = planApi.DomHelpers.SlcWorkflowHelper
                .GetWorkflows(workflowFilter.AND(workflowConfigurationFilter.OR(nodeConfigurationFilter)))
                .DistinctBy(x => x.ID);

            return workflowInstances;
        }

        private Dictionary<Guid, List<Guid>> GetWorkflowsReferencingConfigurations()
        {
            var result = new Dictionary<Guid, List<Guid>>();
            var workflowInstances = GetWorkflowInstancesReferencingConfigurations();

            foreach (var workflow in workflowInstances)
            {
                var workflowId = workflow.ID.Id;

                if (workflow.WorkflowExecution != null)
                {
                    ValidateWorkflowExecutionSection(result, workflowId, workflow.WorkflowExecution);
                }

                if (workflow.Nodes != null)
                {
                    foreach (var nodeSection in workflow.Nodes)
                        ValidateNodeSection(result, workflowId, nodeSection);
                }
            }

            return result;
        }

        private void ValidateCapabilities()
        {
            if (!parameterIdsToValidate.Any())
            {
                return;
            }

            workflowConfigurationsPerParameter = GetWorkflowConfigurationsPerParameter(parametersToValidate.Select(x => x.ID).ToHashSet());
            workflowConfigurationIds = workflowConfigurationsPerParameter.SelectMany(x => x.Value).Distinct().ToHashSet();

            ValidateJobUsage((parameter, ids) =>
            {
                return new CapabilityInUseByJobsError
                {
                    Id = parameter.ID,
                    ErrorMessage = $"Capability '{parameter.Name}' is in use by {ids.Count} job(s).",
                    JobIds = ids.ToArray(),
                };
            });


            ValidateRecurringJobUsage((parameter, ids) =>
            {
                return new CapabilityInUseByRecurringJobsError
                {
                    Id = parameter.ID,
                    ErrorMessage = $"Capability '{parameter.Name}' is in use by {ids.Count} recurrence(s).",
                    RecurringJobIds = ids.ToArray(),
                };
            });


            ValidateWorkflowUsage((parameter, ids) =>
            {
                return new CapabilityInUseByWorkflowsError
                {
                    Id = parameter.ID,
                    ErrorMessage = $"Capability '{parameter.Name}' is in use by {ids.Count} workflow(s).",
                    WorkflowIds = ids.ToArray(),
                };
            });
        }

        private void ValidateCapacities()
        {
            if (!parameterIdsToValidate.Any())
            {
                return;
            }

            workflowConfigurationsPerParameter = GetWorkflowConfigurationsPerParameter(parametersToValidate.Select(x => x.ID).ToHashSet());
            workflowConfigurationIds = workflowConfigurationsPerParameter.SelectMany(x => x.Value).Distinct().ToHashSet();

            ValidateJobUsage((parameter, ids) =>
            {
                return new CapacityInUseByJobsError
                {
                    Id = parameter.ID,
                    ErrorMessage = $"Capacity '{parameter.Name}' is in use by {ids.Count} job(s).",
                    JobIds = ids.ToArray(),
                };
            });


            ValidateRecurringJobUsage((parameter, ids) =>
            {
                return new CapacityInUseByRecurringJobsError
                {
                    Id = parameter.ID,
                    ErrorMessage = $"Capacity '{parameter.Name}' is in use by {ids.Count} recurrence(s).",
                    RecurringJobIds = ids.ToArray(),
                };
            });


            ValidateWorkflowUsage((parameter, ids) =>
            {
                return new CapacityInUseByWorkflowsError
                {
                    Id = parameter.ID,
                    ErrorMessage = $"Capacity '{parameter.Name}' is in use by {ids.Count} workflow(s).",
                    WorkflowIds = ids.ToArray(),
                };
            });
        }

        private void ValidateConfigurations()
        {
            if (!parameterIdsToValidate.Any())
            {
                return;
            }

            workflowConfigurationsPerParameter = GetWorkflowConfigurationsPerParameter(parametersToValidate.Select(x => x.ID).ToHashSet());
            workflowConfigurationIds = workflowConfigurationsPerParameter.SelectMany(x => x.Value).Distinct().ToHashSet();

            ValidateJobUsage((parameter, ids) =>
            {
                return new ConfigurationInUseByJobsError
                {
                    Id = parameter.ID,
                    ErrorMessage = $"Configuration '{parameter.Name}' is in use by {ids.Count} job(s).",
                    JobIds = ids.ToArray(),
                };
            });


            ValidateRecurringJobUsage((parameter, ids) =>
            {
                return new ConfigurationInUseByRecurringJobsError
                {
                    Id = parameter.ID,
                    ErrorMessage = $"Configuration '{parameter.Name}' is in use by {ids.Count} recurrence(s).",
                    RecurringJobIds = ids.ToArray(),
                };
            });


            ValidateWorkflowUsage((parameter, ids) =>
            {
                return new ConfigurationInUseByWorkflowsError
                {
                    Id = parameter.ID,
                    ErrorMessage = $"Configuration '{parameter.Name}' is in use by {ids.Count} workflow(s).",
                    WorkflowIds = ids.ToArray(),
                };
            });
        }

        private void ValidateJobExecutionSection(Dictionary<Guid, List<Guid>> result, Guid jobId, JobExecutionSection jobExecutionSection)
        {
            if (!jobExecutionSection.JobConfiguration.HasValue)
                return;

            var jobConfigurationId = jobExecutionSection.JobConfiguration.Value;
            if (!workflowConfigurationIds.Contains(jobConfigurationId))
                return;

            List<Guid> jobIds;
            if (!result.TryGetValue(jobConfigurationId, out jobIds))
            {
                jobIds = new List<Guid>();
                result[jobConfigurationId] = jobIds;
            }

            if (!jobIds.Contains(jobId))
            {
                jobIds.Add(jobId);
            }
        }

        private void ValidateJobUsage(Func<Parameter, ICollection<Guid>, MediaOpsErrorData> createJobsError)
        {
            var jobsReferencingConfigurations = GetJobsReferencingConfigurations();
            foreach (var parameter in parametersToValidate)
            {
                HashSet<Guid> referencedJobIds = new HashSet<Guid>();
                if (!workflowConfigurationsPerParameter.TryGetValue(parameter.ID, out var referencedWorkflowConfigurationIds))
                {
                    continue;
                }

                // Parameter is referenced by workflow configuration instances.
                foreach (var referencedWorkflowConfigurationId in referencedWorkflowConfigurationIds)
                {
                    if (!jobsReferencingConfigurations.TryGetValue(referencedWorkflowConfigurationId, out var jobIds))
                    {
                        // Workflow configuration is not referenced by any job.
                        continue;
                    }

                    foreach (var jobId in jobIds)
                        referencedJobIds.Add(jobId);
                }

                if (referencedJobIds.Any())
                {
                    ReportError(parameter.ID, createJobsError(parameter, referencedJobIds));
                }
            }
        }

        private void ValidateNodeSection(Dictionary<Guid, List<Guid>> result, Guid domInstanceId, NodesSection nodesSection)
        {
            if (!nodesSection.NodeConfiguration.HasValue)
                return;

            var nodeConfigurationId = nodesSection.NodeConfiguration.Value;
            if (!workflowConfigurationIds.Contains(nodeConfigurationId))
            {
                return;
            }

            List<Guid> jobIds;
            if (!result.TryGetValue(nodeConfigurationId, out jobIds))
            {
                jobIds = new List<Guid>();
                result[nodeConfigurationId] = jobIds;
            }

            if (!jobIds.Contains(domInstanceId))
            {
                jobIds.Add(domInstanceId);
            }
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

        private void ValidateRecurringJobUsage(Func<Parameter, ICollection<Guid>, MediaOpsErrorData> createRecurringJobsError)
        {
            var recurringJobsReferencingConfigurations = GetRecurringJobsReferencingConfigurations();
            foreach (var parameter in parametersToValidate)
            {
                HashSet<Guid> referencedRecurringJobIds = new HashSet<Guid>();
                if (!workflowConfigurationsPerParameter.TryGetValue(parameter.ID, out var referencedWorkflowConfigurationIds))
                {
                    continue;
                }

                // Parameter is referenced by workflow configuration instances.
                foreach (var referencedWorkflowConfigurationId in referencedWorkflowConfigurationIds)
                {
                    if (!recurringJobsReferencingConfigurations.TryGetValue(referencedWorkflowConfigurationId, out var jobIds))
                    {
                        // Workflow configuration is not referenced by any job.
                        continue;
                    }

                    foreach (var jobId in jobIds)
                        referencedRecurringJobIds.Add(jobId);
                }

                if (referencedRecurringJobIds.Any())
                {
                    ReportError(parameter.ID, createRecurringJobsError(parameter, referencedRecurringJobIds));
                }
            }
        }

        private void ValidateWorkflowExecutionSection(Dictionary<Guid, List<Guid>> result, Guid workflowId, WorkflowExecutionSection workflowExecutionSection)
        {
            if (!workflowExecutionSection.WorkflowConfiguration.HasValue)
                return;

            var workflowConfigurationId = workflowExecutionSection.WorkflowConfiguration.Value;
            if (!workflowConfigurationIds.Contains(workflowConfigurationId))
                return;

            List<Guid> workflowIds;
            if (!result.TryGetValue(workflowConfigurationId, out workflowIds))
            {
                workflowIds = new List<Guid>();
                result[workflowConfigurationId] = workflowIds;
            }

            if (!workflowIds.Contains(workflowId))
            {
                workflowIds.Add(workflowId);
            }
        }

        private void ValidateWorkflowUsage(Func<Parameter, ICollection<Guid>, MediaOpsErrorData> createWorkflowsError)
        {
            var workflowsReferencingConfigurations = GetWorkflowsReferencingConfigurations();
            foreach (var configurationParameter in parametersToValidate)
            {
                HashSet<Guid> referencedWorkflowIds = new HashSet<Guid>();
                if (!workflowConfigurationsPerParameter.TryGetValue(configurationParameter.ID, out var referencedWorkflowConfigurationIds))
                {
                    continue;
                }

                // Parameter is referenced by workflow configuration instances.
                foreach (var referencedWorkflowConfigurationId in referencedWorkflowConfigurationIds)
                {
                    if (!workflowsReferencingConfigurations.TryGetValue(referencedWorkflowConfigurationId, out var jobIds))
                    {
                        // Workflow configuration is not referenced by any job.
                        continue;
                    }

                    foreach (var jobId in jobIds)
                        referencedWorkflowIds.Add(jobId);
                }

                if (referencedWorkflowIds.Any())
                {
                    ReportError(configurationParameter.ID, createWorkflowsError.Invoke(configurationParameter, referencedWorkflowIds));
                }
            }
        }
    }
}
