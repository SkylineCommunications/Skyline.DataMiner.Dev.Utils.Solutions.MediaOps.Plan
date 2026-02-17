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

    internal class SlcWorkflowResourcePoolUsageValidator : ApiObjectValidator
    {
        private readonly HashSet<Guid> resourcePoolIdsToValidate;
        private readonly IReadOnlyCollection<ResourcePool> resourcePoolsToValidate;
        private readonly MediaOpsPlanApi planApi;

        private SlcWorkflowResourcePoolUsageValidator(MediaOpsPlanApi planApi, IReadOnlyCollection<ResourcePool> resourcePoolsToValidate)
        {
            this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
            this.resourcePoolsToValidate = resourcePoolsToValidate ?? throw new ArgumentNullException(nameof(resourcePoolsToValidate));
            resourcePoolIdsToValidate = resourcePoolsToValidate.Select(x => x.ID).ToHashSet();
        }

        public static ApiObjectValidator Validate(MediaOpsPlanApi planApi, ICollection<ResourcePool> resourcePoolsToValidate)
        {
            if (resourcePoolsToValidate == null)
            {
                throw new ArgumentNullException(nameof(resourcePoolsToValidate));
            }

            var validator = new SlcWorkflowResourcePoolUsageValidator(planApi, resourcePoolsToValidate.ToList());
            validator.Validate();

            return validator;
        }

        private void Validate()
        {
            if (!resourcePoolIdsToValidate.Any())
            {
                return;
            }

            ValidateJobUsage((resourcePool, ids) =>
            {
                return new ResourcePoolInUseByJobsError
                {
                    Id = resourcePool.ID,
                    ErrorMessage = $"Resource pool '{resourcePool.Name}' is in use by {ids.Count} job(s).",
                    JobIds = ids.ToArray(),
                };
            });


            ValidateRecurringJobUsage((resourcePool, ids) =>
            {
                return new ResourcePoolInUseByRecurringJobsError
                {
                    Id = resourcePool.ID,
                    ErrorMessage = $"Resource pool '{resourcePool.Name}' is in use by {ids.Count} recurrence(s).",
                    RecurringJobIds = ids.ToArray(),
                };
            });


            ValidateWorkflowUsage((resourcePool, ids) =>
            {
                return new ResourcePoolInUseByWorkflowsError
                {
                    Id = resourcePool.ID,
                    ErrorMessage = $"Resource pool '{resourcePool.Name}' is in use by {ids.Count} workflow(s).",
                    WorkflowIds = ids.ToArray(),
                };
            });
        }

        private void ValidateJobUsage(Func<ResourcePool, ICollection<Guid>, MediaOpsErrorData> createJobsError)
        {
            var jobsReferencingResourcePools = GetJobsReferencingResourcePools();
            foreach (var resourcePool in resourcePoolsToValidate)
            {
                if (!jobsReferencingResourcePools.TryGetValue(resourcePool.ID, out var jobIds))
                {
                    continue;
                }

                ReportError(resourcePool.ID, createJobsError(resourcePool, jobIds));
            }
        }

        private void ValidateRecurringJobUsage(Func<ResourcePool, ICollection<Guid>, MediaOpsErrorData> createRecurringJobsError)
        {
            var recurringJobsReferencingResourcePools = GetRecurringJobsReferencingResourcePools();
            foreach (var resourcePool in resourcePoolsToValidate)
            {
                if (!recurringJobsReferencingResourcePools.TryGetValue(resourcePool.ID, out var recurringJobIds))
                {
                    continue;
                }

                ReportError(resourcePool.ID, createRecurringJobsError(resourcePool, recurringJobIds));
            }
        }

        private void ValidateWorkflowUsage(Func<ResourcePool, ICollection<Guid>, MediaOpsErrorData> createWorkflowsError)
        {
            var workflowsReferencingResourcePools = GetWorkflowsReferencingResourcePools();
            foreach (var resourcePool in resourcePoolsToValidate)
            {
                if (!workflowsReferencingResourcePools.TryGetValue(resourcePool.ID, out var jobIds))
                {
                    continue;
                }

                ReportError(resourcePool.ID, createWorkflowsError(resourcePool, jobIds));
            }
        }

        private void ValidateNodeSection(Dictionary<Guid, List<Guid>> result, Guid domInstanceId, NodesSection nodesSection)
        {
            var resourcePoolId = Guid.Empty;
            if (nodesSection.NodeType == SlcWorkflowIds.Enums.Nodetype.ResourcePool && nodesSection.ReferenceId != Guid.Empty && resourcePoolIdsToValidate.Contains(nodesSection.ReferenceId))
            {
                resourcePoolId = nodesSection.ReferenceId;
            }
            else if (nodesSection.NodeType == SlcWorkflowIds.Enums.Nodetype.Resource && nodesSection.ParentReferenceId != Guid.Empty && resourcePoolIdsToValidate.Contains(nodesSection.ParentReferenceId))
            {
                resourcePoolId = nodesSection.ParentReferenceId;
            }

            if (resourcePoolId == Guid.Empty)
            {
                return;
            }

            if (!result.TryGetValue(resourcePoolId, out var jobIds))
            {
                jobIds = new List<Guid>();
                result[resourcePoolId] = jobIds;
            }

            if (!jobIds.Contains(domInstanceId))
            {
                jobIds.Add(domInstanceId);
            }
        }

        private IEnumerable<JobsInstance> GetJobInstancesReferencingResourcePools()
        {
            var jobFilter = new ANDFilterElement<DomInstance>(
                    DomInstanceExposers.DomDefinitionId.Equal(SlcWorkflowIds.Definitions.Jobs.Id),
                    DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.JobInfo.Postroll).GreaterThan(DateTimeOffset.UtcNow),
                    DomInstanceExposers.StatusId.NotEqual(SlcWorkflowIds.Behaviors.Job_Behavior.Statuses.ToValue(SlcWorkflowIds.Behaviors.Job_Behavior.StatusesEnum.Canceled))
                );

            var nodeFilters = resourcePoolIdsToValidate
                .Select(id =>
                {
                    var resourceNodeFilter = new ANDFilterElement<DomInstance>(
                        DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.Nodes.NodeType).Equal((int)SlcWorkflowIds.Enums.Nodetype.Resource),
                        DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.Nodes.NodeParentReferenceID).Equal(id.ToString())
                    );

                    var resourcePoolNodeFilter = new ANDFilterElement<DomInstance>(
                        DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.Nodes.NodeType).Equal((int)SlcWorkflowIds.Enums.Nodetype.ResourcePool),
                        DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.Nodes.NodeReferenceID).Equal(id.ToString())
                    );

                    return new ORFilterElement<DomInstance>(resourceNodeFilter, resourcePoolNodeFilter);
                })
                .ToArray();

            var nodeFilter = new ORFilterElement<DomInstance>(nodeFilters);
            var fullFilter = new ANDFilterElement<DomInstance>(jobFilter, nodeFilter);

            var jobs = planApi.DomHelpers.SlcWorkflowHelper
                .GetJobs(fullFilter)
                .DistinctBy(x => x.ID);

            return jobs;
        }

        private Dictionary<Guid, List<Guid>> GetJobsReferencingResourcePools()
        {
            var result = new Dictionary<Guid, List<Guid>>();
            var jobs = GetJobInstancesReferencingResourcePools();

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

        private IEnumerable<RecurringJobsInstance> GetRecurringJobInstancesReferencingResourcePools()
        {
            var recurringJobFilter = new ANDFilterElement<DomInstance>(
                    DomInstanceExposers.DomDefinitionId.Equal(SlcWorkflowIds.Definitions.RecurringJobs.Id),
                    DomInstanceExposers.StatusId.Equal(SlcWorkflowIds.Behaviors.Recurringjob_Behavior.Statuses.ToValue(SlcWorkflowIds.Behaviors.Recurringjob_Behavior.StatusesEnum.Active))
                );

            var nodeFilters = resourcePoolIdsToValidate
                .Select(id =>
                {
                    var resourceNodeFilter = new ANDFilterElement<DomInstance>(
                        DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.Nodes.NodeType).Equal((int)SlcWorkflowIds.Enums.Nodetype.Resource),
                        DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.Nodes.NodeParentReferenceID).Equal(id.ToString())
                    );

                    var resourcePoolNodeFilter = new ANDFilterElement<DomInstance>(
                        DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.Nodes.NodeType).Equal((int)SlcWorkflowIds.Enums.Nodetype.ResourcePool),
                        DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.Nodes.NodeReferenceID).Equal(id.ToString())
                    );

                    return new ORFilterElement<DomInstance>(resourceNodeFilter, resourcePoolNodeFilter);
                })
                .ToArray();

            var nodeFilter = new ORFilterElement<DomInstance>(nodeFilters);
            var fullFilter = new ANDFilterElement<DomInstance>(recurringJobFilter, nodeFilter);

            var recurringJobs = planApi.DomHelpers.SlcWorkflowHelper
                .GetRecurringJobs(fullFilter)
                .DistinctBy(x => x.ID);

            return recurringJobs;
        }

        private Dictionary<Guid, List<Guid>> GetRecurringJobsReferencingResourcePools()
        {
            var result = new Dictionary<Guid, List<Guid>>();
            var recurringJobs = GetRecurringJobInstancesReferencingResourcePools();

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

        private IEnumerable<WorkflowsInstance> GetWorkflowInstancesReferencingResourcePools()
        {
            var workflowFilter = DomInstanceExposers.DomDefinitionId.Equal(SlcWorkflowIds.Definitions.Workflows.Id);

            var nodeFilters = resourcePoolIdsToValidate
                .Select(id =>
                {
                    var resourceNodeFilter = new ANDFilterElement<DomInstance>(
                        DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.Nodes.NodeType).Equal((int)SlcWorkflowIds.Enums.Nodetype.Resource),
                        DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.Nodes.NodeParentReferenceID).Equal(id.ToString())
                    );

                    var resourcePoolNodeFilter = new ANDFilterElement<DomInstance>(
                        DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.Nodes.NodeType).Equal((int)SlcWorkflowIds.Enums.Nodetype.ResourcePool),
                        DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.Nodes.NodeReferenceID).Equal(id.ToString())
                    );

                    return new ORFilterElement<DomInstance>(resourceNodeFilter, resourcePoolNodeFilter);
                })
                .ToArray();

            var nodeFilter = new ORFilterElement<DomInstance>(nodeFilters);
            var fullFilter = new ANDFilterElement<DomInstance>(workflowFilter, nodeFilter);

            var workflows = planApi.DomHelpers.SlcWorkflowHelper
                .GetWorkflows(fullFilter)
                .DistinctBy(x => x.ID);

            return workflows;
        }

        private Dictionary<Guid, List<Guid>> GetWorkflowsReferencingResourcePools()
        {
            var result = new Dictionary<Guid, List<Guid>>();
            var workflows = GetWorkflowInstancesReferencingResourcePools();

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
