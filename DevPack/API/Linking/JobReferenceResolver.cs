namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// A <see cref="ReferenceResolver"/> that resolves <see cref="DataReference"/> instances in the context
	/// of a specific <see cref="Job"/>. Job-level and workflow-level references are resolved using the
	/// job's own data and its associated workflow.
	/// </summary>
	public class JobReferenceResolver : ReferenceResolver
	{
		private readonly Lazy<Workflow> _lazyWorkflow;
		private readonly Lazy<IDictionary<Guid, PropertyValueBase>> _lazyJobPropertyValues;
		private readonly Lazy<IDictionary<Guid, PropertyValueBase>> _lazyWorkflowPropertyValues;
		private readonly IDictionary<Guid, Resource> _resourceCache;

		/// <summary>
		/// Initializes a new instance of the <see cref="JobReferenceResolver"/> class.
		/// </summary>
		/// <param name="planApi">The MediaOps Plan API used to retrieve definitions and property values.</param>
		/// <param name="job">The job whose context is used to resolve references.</param>
		public JobReferenceResolver(IMediaOpsPlanApi planApi, Job job) : base(planApi)
		{
			Job = job ?? throw new ArgumentNullException(nameof(job));

			_lazyWorkflow = new Lazy<Workflow>(LoadWorkflow);
			_lazyJobPropertyValues = new Lazy<IDictionary<Guid, PropertyValueBase>>(() => ReadPropertyValues(Job.Id));
			_lazyWorkflowPropertyValues = new Lazy<IDictionary<Guid, PropertyValueBase>>(() => ReadPropertyValues(Workflow?.Id ?? Guid.Empty));
			_resourceCache = new Dictionary<Guid, Resource>();
		}

		/// <summary>
		/// Gets the job used as the resolution context.
		/// </summary>
		protected Job Job { get; }

		/// <summary>
		/// Gets the workflow associated with the job, lazily loaded on first access.
		/// </summary>
		protected Workflow Workflow => _lazyWorkflow?.Value;

		/// <summary>
		/// Gets the lazily-loaded dictionary of property values defined at the job level.
		/// </summary>
		protected IDictionary<Guid, PropertyValueBase> JobPropertyValues => _lazyJobPropertyValues.Value;

		/// <summary>
		/// Gets the lazily-loaded dictionary of property values defined at the workflow level.
		/// </summary>
		protected IDictionary<Guid, PropertyValueBase> WorkflowPropertyValues => _lazyWorkflowPropertyValues.Value;

		/// <inheritdoc />
		protected override ResolvedValue ResolveJobName(JobNameReference reference)
		{
			return new StringResolvedValue(Job.Name);
		}

		/// <inheritdoc />
		protected override ResolvedValue ResolveWorkflowName(WorkflowNameReference reference)
		{
			return new StringResolvedValue(Workflow?.Name);
		}

		/// <inheritdoc />
		protected override ResolvedValue ResolveJobPropertyValue(JobPropertyReference reference)
		{
			if (JobPropertyValues.TryGetValue(reference.JobPropertyId, out var value))
			{
				return ConvertPropertyValue(value);
			}

			return ResolvedValue.FromUnresolvedReference(reference);
		}

		/// <inheritdoc />
		protected override ResolvedValue ResolveWorkflowPropertyValue(WorkflowPropertyReference reference)
		{
			if (WorkflowPropertyValues.TryGetValue(reference.WorkflowPropertyId, out var value))
			{
				return ConvertPropertyValue(value);
			}

			return ResolvedValue.FromUnresolvedReference(reference);
		}

		/// <inheritdoc />
		protected override Resource GetResource(DataReference reference)
		{
			if (String.IsNullOrEmpty(reference.NodeId))
			{
				return null;
			}

			var node = Job.NodeGraph.Nodes.FirstOrDefault(n => String.Equals(n.Id, reference.NodeId, StringComparison.OrdinalIgnoreCase));

			if (!node.IsResourceNode(out var resourceNode))
			{
				return null;
			}

			var resourceId = resourceNode.ResourceId;

			if (resourceId == Guid.Empty)
			{
				return null;
			}

			if (_resourceCache.TryGetValue(resourceId, out var cachedResource))
			{
				return cachedResource;
			}

			var resource = PlanApi.Resources.Read(resourceId);
			_resourceCache[resourceId] = resource;

			return resource;
		}

		/// <inheritdoc />
		protected override OrchestrationSettings GetOrchestrationSettings(DataReference reference)
		{
			if (String.IsNullOrEmpty(reference.NodeId))
			{
				return Job.OrchestrationSettings;
			}

			var node = Job.NodeGraph.Nodes.FirstOrDefault(n => String.Equals(n.Id, reference.NodeId, StringComparison.OrdinalIgnoreCase));

			if (node != null)
			{
				return node.OrchestrationSettings;
			}

			return null;
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
