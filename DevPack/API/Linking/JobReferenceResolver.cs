namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public class JobReferenceResolver : ReferenceResolver
	{
		private readonly Lazy<Workflow> _lazyWorkflow;
		private readonly Lazy<IDictionary<Guid, PropertyValueBase>> _lazyJobPropertyValues;
		private readonly Lazy<IDictionary<Guid, PropertyValueBase>> _lazyWorkflowPropertyValues;

		public JobReferenceResolver(IMediaOpsPlanApi planApi, Job job) : base(planApi)
		{
			Job = job ?? throw new ArgumentNullException(nameof(job));

			_lazyWorkflow = new Lazy<Workflow>(LoadWorkflow);
			_lazyJobPropertyValues = new Lazy<IDictionary<Guid, PropertyValueBase>>(() => ReadPropertyValues(Job.Id));
			_lazyWorkflowPropertyValues = new Lazy<IDictionary<Guid, PropertyValueBase>>(() => ReadPropertyValues(Workflow?.Id ?? Guid.Empty));
		}

		protected Job Job { get; }

		protected Workflow Workflow => _lazyWorkflow?.Value;

		protected IDictionary<Guid, PropertyValueBase> JobPropertyValues => _lazyJobPropertyValues.Value;

		protected IDictionary<Guid, PropertyValueBase> WorkflowPropertyValues => _lazyWorkflowPropertyValues.Value;

		protected override ResolvedValue ResolveJobName(JobNameReference reference)
		{
			return new StringResolvedValue(Job.Name);
		}

		protected override ResolvedValue ResolveWorkflowName(WorkflowNameReference reference)
		{
			return new StringResolvedValue(Workflow?.Name);
		}

		protected override ResolvedValue ResolveJobPropertyValue(JobPropertyReference reference)
		{
			if (JobPropertyValues.TryGetValue(reference.JobPropertyId, out var value))
			{
				return ConvertPropertyValue(value);
			}

			return ResolvedValue.FromUnresolvedReference(reference);
		}

		protected override ResolvedValue ResolveWorkflowPropertyValue(WorkflowPropertyReference reference)
		{
			if (WorkflowPropertyValues.TryGetValue(reference.WorkflowPropertyId, out var value))
			{
				return ConvertPropertyValue(value);
			}

			return ResolvedValue.FromUnresolvedReference(reference);
		}

		protected override Resource GetResource(DataReference reference)
		{
			if (String.IsNullOrEmpty(reference.NodeId))
			{
				return null;
			}

			todo;
		}

		protected override OrchestrationSettings GetOrchestrationSettings(string nodeId)
		{
			if (!String.IsNullOrEmpty(nodeId))
			{
				todo;
			}
			else
			{
				return Job.OrchestrationSettings;
			}
		}

		private Workflow LoadWorkflow()
		{
			if (Job.WorkflowId == Guid.Empty)
			{
				return null;
			}

			var workflow = PlanApi.Workflows.Read().FirstOrDefault(w => w.Id == Job.WorkflowId);
			if (workflow == null)
			{
				throw new InvalidOperationException($"Workflow with ID '{Job.WorkflowId}' not found for Job '{Job.Name}' ({Job.Id}).");
			}

			return workflow;
		}
	}
}
