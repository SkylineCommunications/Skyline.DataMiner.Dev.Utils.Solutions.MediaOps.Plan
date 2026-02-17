namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Helper;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;

    internal class SlcWorkflowResourceUsageValidator : ApiObjectValidator
    {
        private readonly HashSet<Guid> resourceIdsToValidate;
        private readonly IReadOnlyCollection<Resource> resourcesToValidate;
        private readonly MediaOpsPlanApi planApi;

        private SlcWorkflowResourceUsageValidator(MediaOpsPlanApi planApi, IReadOnlyCollection<Resource> resourcesToValidate)
        {
            this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
            this.resourcesToValidate = resourcesToValidate ?? throw new ArgumentNullException(nameof(resourcesToValidate));
            resourceIdsToValidate = resourcesToValidate.Select(x => x.ID).ToHashSet();
        }

        public static ApiObjectValidator Validate(MediaOpsPlanApi planApi, ICollection<Resource> resourcesToValidate)
        {
            if (resourcesToValidate == null)
            {
                throw new ArgumentNullException(nameof(resourcesToValidate));
            }

            var validator = new SlcWorkflowResourceUsageValidator(planApi, resourcesToValidate.ToList());
            validator.Validate();

            return validator;
        }

        private void Validate()
        {
            if (!resourceIdsToValidate.Any())
            {
                return;
            }

            ValidateJobUsage((resource, ids) =>
            {
                return new ResourceInUseByJobsError
                {
                    Id = resource.ID,
                    ErrorMessage = $"Resource '{resource.Name}' is in use by {ids.Count} job(s).",
                    JobIds = ids.ToArray(),
                };
            });


            ValidateRecurringJobUsage((resource, ids) =>
            {
                return new ResourceInUseByRecurringJobsError
                {
                    Id = resource.ID,
                    ErrorMessage = $"Resource '{resource.Name}' is in use by {ids.Count} recurrence(s).",
                    RecurringJobIds = ids.ToArray(),
                };
            });


            ValidateWorkflowUsage((resource, ids) =>
            {
                return new ResourceInUseByWorkflowsError
                {
                    Id = resource.ID,
                    ErrorMessage = $"Resource '{resource.Name}' is in use by {ids.Count} workflow(s).",
                    WorkflowIds = ids.ToArray(),
                };
            });
        }

        private void ValidateJobUsage(Func<Resource, ICollection<Guid>, MediaOpsErrorData> createJobsError)
        {
            var jobsReferencingResources = GetJobsReferencingResources();
            foreach (var resource in resourcesToValidate)
            {
                if (!jobsReferencingResources.TryGetValue(resource.ID, out var jobIds))
                {
                    continue;
                }

                ReportError(resource.ID, createJobsError(resource, jobIds));
            }
        }

        private void ValidateRecurringJobUsage(Func<Resource, ICollection<Guid>, MediaOpsErrorData> createRecurringJobsError)
        {
            var recurringJobsReferencingResources = GetRecurringJobsReferencingResources();
            foreach (var resource in resourcesToValidate)
            {
                if (!recurringJobsReferencingResources.TryGetValue(resource.ID, out var recurringJobIds))
                {
                    continue;
                }

                ReportError(resource.ID, createRecurringJobsError(resource, recurringJobIds));
            }
        }

        private void ValidateWorkflowUsage(Func<Resource, ICollection<Guid>, MediaOpsErrorData> createWorkflowsError)
        {
            var workflowsReferencingResources = GetWorkflowsReferencingResources();
            foreach (var resource in resourcesToValidate)
            {
                if (!workflowsReferencingResources.TryGetValue(resource.ID, out var jobIds))
                {
                    continue;
                }

                ReportError(resource.ID, createWorkflowsError(resource, jobIds));
            }
        }

        private void ValidateNodeSection(Dictionary<Guid, List<Guid>> result, Guid domInstanceId, NodesSection nodesSection)
        {
            if (nodesSection.ReferenceId == Guid.Empty
                || !resourceIdsToValidate.Contains(nodesSection.ReferenceId))
            {
                return;
            }

            if (!result.TryGetValue(nodesSection.ReferenceId, out var jobIds))
            {
                jobIds = new List<Guid>();
                result[nodesSection.ReferenceId] = jobIds;
            }

            if (!jobIds.Contains(domInstanceId))
            {
                jobIds.Add(domInstanceId);
            }
        }

        private IEnumerable<JobsInstance> GetJobInstancesReferencingResources()
        {
            var jobFilter = new ANDFilterElement<DomInstance>(
                    DomInstanceExposers.DomDefinitionId.Equal(SlcWorkflowIds.Definitions.Jobs.Id),
                    DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.JobInfo.Postroll).GreaterThan(DateTimeOffset.UtcNow),
                    DomInstanceExposers.StatusId.NotEqual(SlcWorkflowIds.Behaviors.Job_Behavior.Statuses.ToValue(SlcWorkflowIds.Behaviors.Job_Behavior.StatusesEnum.Canceled))
                );

            var nodeFilters = resourceIdsToValidate
                .Select(id =>
                {
                    var resourceNodeFilter = new ANDFilterElement<DomInstance>(
                        DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.Nodes.NodeType).Equal((int)SlcWorkflowIds.Enums.Nodetype.Resource),
                        DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.Nodes.NodeReferenceID).Equal(id.ToString())
                    );

                    return resourceNodeFilter;
                })
                .ToArray();

            var nodeFilter = new ORFilterElement<DomInstance>(nodeFilters);
            var fullFilter = new ANDFilterElement<DomInstance>(jobFilter, nodeFilter);

            var jobs = planApi.DomHelpers.SlcWorkflowHelper
                .GetJobs(fullFilter)
                .DistinctBy(x => x.ID);

            return jobs;
        }

        private Dictionary<Guid, List<Guid>> GetJobsReferencingResources()
        {
            var result = new Dictionary<Guid, List<Guid>>();
            var jobs = GetJobInstancesReferencingResources();

            foreach (var job in jobs)
            {
                var jobId = job.ID.Id;

                if (job.Nodes != null)
                {
                    foreach (var nodeSection in job.Nodes)
                    {
                        ValidateNodeSection(result, jobId, nodeSection);
                    }
                }
            }

            return result;
        }

        private IEnumerable<RecurringJobsInstance> GetRecurringJobInstancesReferencingResources()
        {
            var recurringJobFilter = new ANDFilterElement<DomInstance>(
                    DomInstanceExposers.DomDefinitionId.Equal(SlcWorkflowIds.Definitions.RecurringJobs.Id),
                    DomInstanceExposers.StatusId.Equal(SlcWorkflowIds.Behaviors.Recurringjob_Behavior.Statuses.ToValue(SlcWorkflowIds.Behaviors.Recurringjob_Behavior.StatusesEnum.Active))
                );

            var nodeFilters = resourceIdsToValidate
                .Select(id =>
                {
                    var resourceNodeFilter = new ANDFilterElement<DomInstance>(
                        DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.Nodes.NodeType).Equal((int)SlcWorkflowIds.Enums.Nodetype.Resource),
                        DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.Nodes.NodeParentReferenceID).Equal(id.ToString())
                    );

                    return resourceNodeFilter;
                })
                .ToArray();

            var nodeFilter = new ORFilterElement<DomInstance>(nodeFilters);
            var fullFilter = new ANDFilterElement<DomInstance>(recurringJobFilter, nodeFilter);

            var recurringJobs = planApi.DomHelpers.SlcWorkflowHelper
                .GetRecurringJobs(fullFilter)
                .DistinctBy(x => x.ID);

            return recurringJobs;
        }

        private Dictionary<Guid, List<Guid>> GetRecurringJobsReferencingResources()
        {
            var result = new Dictionary<Guid, List<Guid>>();
            var recurringJobs = GetRecurringJobInstancesReferencingResources();

            foreach (var recurringJob in recurringJobs)
            {
                var recurringJobId = recurringJob.ID.Id;

                if (recurringJob.Nodes != null)
                {
                    foreach (var nodeSection in recurringJob.Nodes)
                    {
                        ValidateNodeSection(result, recurringJobId, nodeSection);
                    }
                }
            }

            return result;
        }

        private IEnumerable<WorkflowsInstance> GetWorkflowInstancesReferencingResources()
        {
            var workflowFilter = DomInstanceExposers.DomDefinitionId.Equal(SlcWorkflowIds.Definitions.Workflows.Id);

            var nodeFilters = resourceIdsToValidate
                .Select(id =>
                {
                    var resourceNodeFilter = new ANDFilterElement<DomInstance>(
                        DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.Nodes.NodeType).Equal((int)SlcWorkflowIds.Enums.Nodetype.Resource),
                        DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.Nodes.NodeParentReferenceID).Equal(id.ToString())
                    );

                    return resourceNodeFilter;
                })
                .ToArray();

            var nodeFilter = new ORFilterElement<DomInstance>(nodeFilters);
            var fullFilter = new ANDFilterElement<DomInstance>(workflowFilter, nodeFilter);

            var workflows = planApi.DomHelpers.SlcWorkflowHelper
                .GetWorkflows(fullFilter)
                .DistinctBy(x => x.ID);

            return workflows;
        }

        private Dictionary<Guid, List<Guid>> GetWorkflowsReferencingResources()
        {
            var result = new Dictionary<Guid, List<Guid>>();
            var workflows = GetWorkflowInstancesReferencingResources();

            foreach (var workflow in workflows)
            {
                var workflowId = workflow.ID.Id;

                if (workflow.Nodes != null)
                {
                    foreach (var nodeSection in workflow.Nodes)
                    {
                        ValidateNodeSection(result, workflowId, nodeSection);
                    }
                }
            }

            return result;
        }
    }
}
