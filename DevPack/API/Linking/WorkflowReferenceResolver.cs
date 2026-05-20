namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	public class WorkflowReferenceResolver : ReferenceResolver
	{
		private readonly Lazy<IDictionary<Guid, PropertyValueBase>> _lazyPropertyValues;

		public WorkflowReferenceResolver(IMediaOpsPlanApi planApi, Workflow workflow) : base(planApi)
		{
			Workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));

			_lazyPropertyValues = new Lazy<IDictionary<Guid, PropertyValueBase>>(() => ReadPropertyValues(workflow.Id));
		}

		protected Workflow Workflow { get; }

		protected IDictionary<Guid, PropertyValueBase> PropertyValues => _lazyPropertyValues.Value;

		protected override ResolvedValue ResolveWorkflowName(WorkflowNameReference reference)
		{
			return new StringResolvedValue(Workflow.Name);
		}

		protected override ResolvedValue ResolveWorkflowPropertyValue(WorkflowPropertyReference reference)
		{
			if (PropertyValues.TryGetValue(reference.WorkflowPropertyId, out var value))
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
				return Workflow.OrchestrationSettings;
			}
		}
	}
}
