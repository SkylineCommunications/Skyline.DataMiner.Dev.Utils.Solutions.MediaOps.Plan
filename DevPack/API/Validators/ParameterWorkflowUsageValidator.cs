namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Helper;
    using Skyline.DataMiner.Net.Jobs;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;

    internal class ParameterWorkflowUsageValidator : ApiObjectValidator
    {
        private readonly MediaOpsPlanApi planApi;
        private readonly IReadOnlyCollection<Parameter> parametersToValidate;
        private readonly HashSet<Guid> parameterIdsToValidate;

        private HashSet<Guid> workflowConfigurationIds;
        private Dictionary<Guid, List<Guid>> WorkflowConfigurationsPerParameter;

        private ParameterWorkflowUsageValidator(MediaOpsPlanApi planApi, IReadOnlyCollection<Parameter> parametersToValidate)
        {
            this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
            this.parametersToValidate = parametersToValidate ?? throw new ArgumentNullException(nameof(parametersToValidate));
            parameterIdsToValidate = parametersToValidate.Select(x => x.Id).ToHashSet();
        }

        public static ApiObjectValidator Validate(MediaOpsPlanApi planApi, ICollection<Configuration> configurationsToValidate)
        {
            if (configurationsToValidate == null)
                throw new ArgumentNullException(nameof(configurationsToValidate));

            var validator = new ParameterWorkflowUsageValidator(planApi, configurationsToValidate.ToList<Parameter>());
            validator.ValidateConfigurations();
            return validator;
        }

        public static ApiObjectValidator Validate(MediaOpsPlanApi planApi, ICollection<Capacity> capacitiesToValidate)
        {
            if (capacitiesToValidate == null)
                throw new ArgumentNullException(nameof(capacitiesToValidate));

            var validator = new ParameterWorkflowUsageValidator(planApi, capacitiesToValidate.ToList<Parameter>());
            validator.ValidateCapacities();
            return validator;
        }

        public static ApiObjectValidator Validate(MediaOpsPlanApi planApi, ICollection<Capability> capabilitiesToValidate)
        {
            if (capabilitiesToValidate == null)
                throw new ArgumentNullException(nameof(capabilitiesToValidate));

            var validator = new ParameterWorkflowUsageValidator(planApi, capabilitiesToValidate.ToList<Parameter>());
            validator.ValidateCapabilities();
            return validator;
        }

        private void ValidateConfigurations()
        {
            WorkflowConfigurationsPerParameter = GetWorkflowConfigurationsPerParameter(parametersToValidate.Select(x => x.Id).ToHashSet());

            if (!WorkflowConfigurationsPerParameter.Any())
                return;

            workflowConfigurationIds = WorkflowConfigurationsPerParameter.SelectMany(x => x.Value).Distinct().ToHashSet();

            ValidateConfigurationJobUsage();
            ValidateConfigurationRecurringJobUsage();
            ValidateConfigurationWorkflowUsage();
        }

        private void ValidateConfigurationJobUsage()
        {
            var jobsReferencingConfigurations = GetJobsReferencingConfigurations();
            foreach (var configurationParameter in parametersToValidate)
            {
                HashSet<Guid> referencedJobIds = new HashSet<Guid>();
                if (!WorkflowConfigurationsPerParameter.TryGetValue(configurationParameter.Id, out var referencedWorkflowConfigurationIds))
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
                    ReportError(configurationParameter.Id, new ConfigurationInUseByJobsError
                    {
                        Id = configurationParameter.Id,
                        ErrorMessage = $"Configuration '{configurationParameter.Name}' is in use by Jobs.",
                        JobIds = referencedJobIds.ToArray(),
                    });

                    workflowConfigurationIds.Remove(configurationParameter.Id);
                }
            }
        }

        private void ValidateConfigurationRecurringJobUsage()
        {
            var recurringJobsReferencingConfigurations = GetRecurringJobsReferencingConfigurations();
            foreach (var configurationParameter in parametersToValidate)
            {
                HashSet<Guid> referencedRecurringJobIds = new HashSet<Guid>();
                if (!WorkflowConfigurationsPerParameter.TryGetValue(configurationParameter.Id, out var referencedWorkflowConfigurationIds))
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
                    ReportError(configurationParameter.Id, new ConfigurationInUseByRecurringJobsError
                    {
                        Id = configurationParameter.Id,
                        ErrorMessage = $"Configuration '{configurationParameter.Name}' is in use by Recurring Jobs.",
                        RecurringJobIds = referencedRecurringJobIds.ToArray(),
                    });

                    workflowConfigurationIds.Remove(configurationParameter.Id);
                }
            }
        }

        private void ValidateConfigurationWorkflowUsage()
        {
            var workflowsReferencingConfigurations = GetWorkflowsReferencingConfigurations();
            foreach (var apiConfiguration in parametersToValidate)
            {
                HashSet<Guid> referencedWorkflowIds = new HashSet<Guid>();
                if (!WorkflowConfigurationsPerParameter.TryGetValue(apiConfiguration.Id, out var referencedWorkflowConfigurationIds))
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
                    ReportError(apiConfiguration.Id, new ConfigurationInUseByWorkflowsError
                    {
                        Id = apiConfiguration.Id,
                        ErrorMessage = $"Configuration '{apiConfiguration.Name}' is in use by Workflows.",
                        WorkflowIds = referencedWorkflowIds.ToArray(),
                    });

                    workflowConfigurationIds.Remove(apiConfiguration.Id);
                }
            }
        }

        private void ValidateCapacities()
        {
            throw new NotImplementedException();
        }

        private void ValidateCapabilities()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns a mapping of API Parameter IDs to the Configuration Instance IDs that reference them.
        /// </summary>
        /// <param name="apiParameterIds">Ids of the API parameters to check.</param>
        /// <returns>A dictionary where the key is the Configuration Instance ID and the value is the list of referenced Parameter IDs.</returns>
        private Dictionary<Guid, List<Guid>> GetWorkflowConfigurationsPerParameter(HashSet<Guid> apiParameterIds)
        {
            var result = new Dictionary<Guid, List<Guid>>();

            if (apiParameterIds == null || apiParameterIds.Count == 0)
            {
                return result;
            }

            var possibleWorkflowConfigurations = GetWorkflowConfigurations(apiParameterIds);

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

        private IEnumerable<ConfigurationInstance> GetWorkflowConfigurations(HashSet<Guid> apiParameterIds)
        {
            var configurationFilter = DomInstanceExposers.DomDefinitionId.Equal(Storage.DOM.SlcWorkflow.SlcWorkflowIds.Definitions.Configuration.Id);
            configurationFilter.AND(new ORFilterElement<DomInstance>(apiParameterIds.Select(x =>
                DomInstanceExposers.FieldValues.DomInstanceField(Storage.DOM.SlcWorkflow.SlcWorkflowIds.Sections.ProfileParameterValues.ProfileParameterID).Equal(x).OR(
                DomInstanceExposers.FieldValues.DomInstanceField(Storage.DOM.SlcWorkflow.SlcWorkflowIds.Sections.OrchestrationEvents.ScriptInputValues).Contains(x.ToString())))
                .ToArray()));

            return planApi.DomHelpers.SlcWorkflowHelper.GetConfigurations(configurationFilter);
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

        private Dictionary<Guid, List<Guid>> GetJobsReferencingConfigurations()
        {
            var result = new Dictionary<Guid, List<Guid>>();

            if (workflowConfigurationIds == null || workflowConfigurationIds.Count == 0)
            {
                return result;
            }

            var jobs = GetJobInstancesReferencingConfigurations();

            foreach (var job in jobs)
            {
                var jobId = job.ID.Id;

                ValidateJobExecutionSection(result, jobId, job.JobExecution);
                foreach (var nodeSection in job.Nodes)
                    ValidateNodeSection(result, jobId, nodeSection);
            }

            return result;
        }

        private IEnumerable<JobsInstance> GetJobInstancesReferencingConfigurations()
        {
            var jobFilter = DomInstanceExposers.DomDefinitionId.Equal(Storage.DOM.SlcWorkflow.SlcWorkflowIds.Definitions.Jobs.Id);
            var jobConfigurationFilter = new ORFilterElement<DomInstance>(workflowConfigurationIds
                .Select(id => DomInstanceExposers.FieldValues
                    .DomInstanceField(Storage.DOM.SlcWorkflow.SlcWorkflowIds.Sections.JobExecution.JobConfiguration)
                    .Equal(id))
                .ToArray());

            var nodeConfigurationFilter = new ORFilterElement<DomInstance>(workflowConfigurationIds
                .Select(id => DomInstanceExposers.FieldValues
                    .DomInstanceField(Storage.DOM.SlcWorkflow.SlcWorkflowIds.Sections.Nodes.NodeConfiguration)
                    .Equal(id))
                .ToArray());

            var jobs = planApi.DomHelpers.SlcWorkflowHelper
                .GetJobs(jobFilter.AND(jobConfigurationFilter.OR(nodeConfigurationFilter)))
                .DistinctBy(x => x.ID);

            return jobs;
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

        private Dictionary<Guid, List<Guid>> GetRecurringJobsReferencingConfigurations()
        {
            var result = new Dictionary<Guid, List<Guid>>();

            if (workflowConfigurationIds.Count == 0)
            {
                return result;
            }

            var recurringJobInstances = GetRecurringJobInstancesReferencingConfigurations();

            foreach (var recurringJob in recurringJobInstances)
            {
                var recurringJobId = recurringJob.ID.Id;

                ValidateJobExecutionSection(result, recurringJobId, recurringJob.JobExecution);
                foreach (var nodeSection in recurringJob.Nodes)
                    ValidateNodeSection(result, recurringJobId, nodeSection);
            }

            return result;
        }

        private IEnumerable<RecurringJobsInstance> GetRecurringJobInstancesReferencingConfigurations()
        {
            var recurringJobFilter = DomInstanceExposers.DomDefinitionId.Equal(Storage.DOM.SlcWorkflow.SlcWorkflowIds.Definitions.RecurringJobs.Id);
            var jobConfigurationFilter = new ORFilterElement<DomInstance>(workflowConfigurationIds
                .Select(id => DomInstanceExposers.FieldValues
                    .DomInstanceField(Storage.DOM.SlcWorkflow.SlcWorkflowIds.Sections.JobExecution.JobConfiguration)
                    .Equal(id))
                .ToArray());

            var nodeConfigurationFilter = new ORFilterElement<DomInstance>(workflowConfigurationIds
                .Select(id => DomInstanceExposers.FieldValues
                    .DomInstanceField(Storage.DOM.SlcWorkflow.SlcWorkflowIds.Sections.Nodes.NodeConfiguration)
                    .Equal(id))
                .ToArray());

            var recurringJobInstances = planApi.DomHelpers.SlcWorkflowHelper
                .GetRecurringJobs(recurringJobFilter.AND(jobConfigurationFilter.OR(nodeConfigurationFilter)))
                .DistinctBy(x => x.ID);

            return recurringJobInstances;
        }

        private Dictionary<Guid, List<Guid>> GetWorkflowsReferencingConfigurations()
        {
            var result = new Dictionary<Guid, List<Guid>>();

            if (workflowConfigurationIds.Count == 0)
            {
                return result;
            }

            var workflowInstances = GetWorkflowInstancesReferencingConfigurations();

            foreach (var workflow in workflowInstances)
            {
                var workflowId = workflow.ID.Id;

                ValidateWorkflowExecutionSection(result, workflowId, workflow.WorkflowExecution);

                foreach (var nodeSection in workflow.Nodes)
                    ValidateNodeSection(result, workflowId, nodeSection);
            }

            return result;
        }

        private IEnumerable<WorkflowsInstance> GetWorkflowInstancesReferencingConfigurations()
        {
            var workflowFilter = DomInstanceExposers.DomDefinitionId.Equal(Storage.DOM.SlcWorkflow.SlcWorkflowIds.Definitions.Workflows.Id);
            var workflowConfigurationFilter = new ORFilterElement<DomInstance>(workflowConfigurationIds
                .Select(id => DomInstanceExposers.FieldValues
                    .DomInstanceField(Storage.DOM.SlcWorkflow.SlcWorkflowIds.Sections.WorkflowExecution.WorkflowConfiguration)
                    .Equal(id))
                .ToArray());

            var nodeConfigurationFilter = new ORFilterElement<DomInstance>(workflowConfigurationIds
                .Select(id => DomInstanceExposers.FieldValues
                    .DomInstanceField(Storage.DOM.SlcWorkflow.SlcWorkflowIds.Sections.Nodes.NodeConfiguration)
                    .Equal(id))
                .ToArray());

            var workflowInstances = planApi.DomHelpers.SlcWorkflowHelper
                .GetWorkflows(workflowFilter.AND(workflowConfigurationFilter.OR(nodeConfigurationFilter)))
                .DistinctBy(x => x.ID);

            return workflowInstances;
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
    }
}
