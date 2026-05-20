namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// A <see cref="ReferenceResolver"/> that resolves <see cref="DataReference"/> instances in the context
	/// of a specific <see cref="Workflow"/>. Workflow-level references are resolved using the workflow's
	/// own data and property values.
	/// </summary>
	public class WorkflowReferenceResolver : ReferenceResolver
	{
		private readonly Lazy<IDictionary<Guid, PropertyValueBase>> _lazyPropertyValues;

		/// <summary>
		/// Initializes a new instance of the <see cref="WorkflowReferenceResolver"/> class.
		/// </summary>
		/// <param name="planApi">The MediaOps Plan API used to retrieve definitions and property values.</param>
		/// <param name="workflow">The workflow whose context is used to resolve references.</param>
		public WorkflowReferenceResolver(IMediaOpsPlanApi planApi, Workflow workflow) : base(planApi)
		{
			Workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));

			_lazyPropertyValues = new Lazy<IDictionary<Guid, PropertyValueBase>>(() => ReadPropertyValues(workflow.Id));
		}

		/// <summary>
		/// Gets the workflow used as the resolution context.
		/// </summary>
		protected Workflow Workflow { get; }

		/// <summary>
		/// Gets the lazily-loaded dictionary of property values defined at the workflow level.
		/// </summary>
		protected IDictionary<Guid, PropertyValueBase> PropertyValues => _lazyPropertyValues.Value;

		/// <inheritdoc />
		protected override ResolvedValue ResolveWorkflowName(WorkflowNameReference reference)
		{
			return new StringResolvedValue(Workflow.Name);
		}

		/// <inheritdoc />
		protected override ResolvedValue ResolveWorkflowPropertyValue(WorkflowPropertyReference reference)
		{
			if (PropertyValues.TryGetValue(reference.WorkflowPropertyId, out var value))
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

			// TODO: return resource for the specified node ID
			return null;
		}

		/// <inheritdoc />
		protected override OrchestrationSettings GetOrchestrationSettings(string nodeId)
		{
			if (String.IsNullOrEmpty(nodeId))
			{
				return Workflow.OrchestrationSettings;
			}

			// TODO: return orchestration settings for the specified node ID
			return null;
		}
	}
}
