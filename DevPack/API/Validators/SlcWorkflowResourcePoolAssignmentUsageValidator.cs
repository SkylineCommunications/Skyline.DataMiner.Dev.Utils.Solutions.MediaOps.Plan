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

	/// <summary>
	/// Validates that resources are not unassigned from resource pools from which jobs or workflows still use them.
	/// </summary>
	internal class SlcWorkflowResourcePoolAssignmentUsageValidator : ApiObjectValidator
	{
		private readonly MediaOpsPlanApi planApi;
		private readonly IReadOnlyCollection<(Resource Resource, Guid ResourcePoolId)> assignmentsToValidate;
		private readonly HashSet<(Guid ResourceId, Guid ResourcePoolId)> assignmentIdsToValidate;

		private SlcWorkflowResourcePoolAssignmentUsageValidator(MediaOpsPlanApi planApi, IReadOnlyCollection<(Resource Resource, Guid ResourcePoolId)> assignmentsToValidate)
		{
			this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
			this.assignmentsToValidate = assignmentsToValidate ?? throw new ArgumentNullException(nameof(assignmentsToValidate));
			assignmentIdsToValidate = assignmentsToValidate.Select(x => (x.Resource.Id, x.ResourcePoolId)).ToHashSet();
		}

		/// <summary>
		/// Validates the removal of the given resource pool assignments.
		/// </summary>
		/// <param name="planApi">The API used to read the jobs and workflows.</param>
		/// <param name="removedAssignments">The resources together with the identifiers of the resource pools they are unassigned from.</param>
		/// <returns>A validator holding an error per resource that cannot be unassigned.</returns>
		public static ApiObjectValidator Validate(MediaOpsPlanApi planApi, ICollection<(Resource Resource, Guid ResourcePoolId)> removedAssignments)
		{
			if (removedAssignments == null)
			{
				throw new ArgumentNullException(nameof(removedAssignments));
			}

			var validator = new SlcWorkflowResourcePoolAssignmentUsageValidator(planApi, removedAssignments.Distinct().ToList());
			validator.Validate();

			return validator;
		}

		private void Validate()
		{
			if (assignmentIdsToValidate.Count == 0)
			{
				return;
			}

			var jobIdsByAssignment = GetReferencingInstanceIds(planApi.DomHelpers.SlcWorkflowHelper
				.GetJobs(CreateJobFilter())
				.DistinctBy(x => x.ID)
				.Select(x => (x.ID.Id, (IEnumerable<NodesSection>)x.Nodes)));
			var workflowIdsByAssignment = GetReferencingInstanceIds(planApi.DomHelpers.SlcWorkflowHelper
				.GetWorkflows(CreateWorkflowFilter())
				.DistinctBy(x => x.ID)
				.Select(x => (x.ID.Id, (IEnumerable<NodesSection>)x.Nodes)));

			if (jobIdsByAssignment.Count == 0 && workflowIdsByAssignment.Count == 0)
			{
				return;
			}

			var resourcePoolNamesById = GetResourcePoolNames(jobIdsByAssignment.Keys.Concat(workflowIdsByAssignment.Keys).Select(x => x.ResourcePoolId));

			foreach (var (resource, resourcePoolId) in assignmentsToValidate)
			{
				var assignment = (resource.Id, resourcePoolId);
				var resourcePoolText = resourcePoolNamesById.TryGetValue(resourcePoolId, out var resourcePoolName)
					? $"resource pool '{resourcePoolName}'"
					: $"resource pool with ID '{resourcePoolId}'";

				if (jobIdsByAssignment.TryGetValue(assignment, out var jobIds))
				{
					ReportError(resource.Id, new ResourcePoolAssignmentInUseByJobsError
					{
						Id = resource.Id,
						ResourcePoolId = resourcePoolId,
						ErrorMessage = $"Resource '{resource.Name}' cannot be unassigned from {resourcePoolText} because {jobIds.Count} job(s) use it from that pool.",
						JobIds = jobIds.ToArray(),
					});
				}

				if (workflowIdsByAssignment.TryGetValue(assignment, out var workflowIds))
				{
					ReportError(resource.Id, new ResourcePoolAssignmentInUseByWorkflowsError
					{
						Id = resource.Id,
						ResourcePoolId = resourcePoolId,
						ErrorMessage = $"Resource '{resource.Name}' cannot be unassigned from {resourcePoolText} because {workflowIds.Count} workflow(s) use it from that pool.",
						WorkflowIds = workflowIds.ToArray(),
					});
				}
			}
		}

		private Dictionary<Guid, string> GetResourcePoolNames(IEnumerable<Guid> resourcePoolIds)
		{
			return planApi.DomHelpers.SlcResourceStudioHelper
				.GetResourcePools(resourcePoolIds.Distinct().ToList())
				.GroupBy(x => x.ID.Id)
				.ToDictionary(x => x.Key, x => x.First().ResourcePoolInfo.Name);
		}

		private Dictionary<(Guid ResourceId, Guid ResourcePoolId), List<Guid>> GetReferencingInstanceIds(IEnumerable<(Guid InstanceId, IEnumerable<NodesSection> Nodes)> instances)
		{
			var result = new Dictionary<(Guid ResourceId, Guid ResourcePoolId), List<Guid>>();

			foreach (var (instanceId, nodes) in instances)
			{
				if (nodes == null)
				{
					continue;
				}

				foreach (var node in nodes)
				{
					if (node.NodeType != SlcWorkflowIds.Enums.Nodetype.Resource)
					{
						continue;
					}

					var assignment = (node.ReferenceId, node.ParentReferenceId);
					if (!assignmentIdsToValidate.Contains(assignment))
					{
						continue;
					}

					if (!result.TryGetValue(assignment, out var instanceIds))
					{
						instanceIds = new List<Guid>();
						result[assignment] = instanceIds;
					}

					if (!instanceIds.Contains(instanceId))
					{
						instanceIds.Add(instanceId);
					}
				}
			}

			return result;
		}

		private FilterElement<DomInstance> CreateJobFilter()
		{
			var jobFilter = new ANDFilterElement<DomInstance>(
				DomInstanceExposers.DomDefinitionId.Equal(SlcWorkflowIds.Definitions.Jobs.Id),
				DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.JobInfo.Postroll).GreaterThan(DateTimeOffset.UtcNow),
				DomInstanceExposers.StatusId.NotEqual(SlcWorkflowIds.Behaviors.Job_Behavior.Statuses.ToValue(SlcWorkflowIds.Behaviors.Job_Behavior.StatusesEnum.Canceled)));

			return new ANDFilterElement<DomInstance>(jobFilter, CreateResourceNodeFilter());
		}

		private FilterElement<DomInstance> CreateWorkflowFilter()
		{
			var workflowFilter = DomInstanceExposers.DomDefinitionId.Equal(SlcWorkflowIds.Definitions.Workflows.Id);

			return new ANDFilterElement<DomInstance>(workflowFilter, CreateResourceNodeFilter());
		}

		private FilterElement<DomInstance> CreateResourceNodeFilter()
		{
			var nodeFilters = assignmentIdsToValidate
				.Select(x => new ANDFilterElement<DomInstance>(
					DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.Nodes.NodeType).Equal((int)SlcWorkflowIds.Enums.Nodetype.Resource),
					DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.Nodes.NodeReferenceID).Equal(x.ResourceId.ToString()),
					DomInstanceExposers.FieldValues.DomInstanceField(SlcWorkflowIds.Sections.Nodes.NodeParentReferenceID).Equal(x.ResourcePoolId.ToString())))
				.ToArray();

			return new ORFilterElement<DomInstance>(nodeFilters);
		}
	}
}
