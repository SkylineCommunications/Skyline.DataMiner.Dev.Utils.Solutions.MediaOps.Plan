namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	public class WorkflowResourcePoolNode : WorkflowNode
	{
		public WorkflowResourcePoolNode(ResourcePool resourcePool)
			: this(resourcePool?.Id ?? throw new ArgumentNullException(nameof(resourcePool)))
		{
		}

		public WorkflowResourcePoolNode(Guid resourcePoolId) : base()
		{
			if (resourcePoolId == Guid.Empty)
			{
				throw new ArgumentException(nameof(resourcePoolId));
			}

			ResourcePoolId = resourcePoolId;
		}

		internal WorkflowResourcePoolNode(StorageWorkflow.NodesSection section) : base(section)
		{
			ParseSection(section);
			InitTracking();
		}

		/// <summary>
		/// Gets the unique identifier of the resource pool associated with this node.
		/// </summary>
		public Guid ResourcePoolId { get; private set; }

		internal override void ApplyChanges(StorageWorkflow.NodesSection section)
		{
			section.NodeType = StorageWorkflow.SlcWorkflowIds.Enums.Nodetype.ResourcePool;
			section.ReferenceId = ResourcePoolId;
		}

		private void ParseSection(StorageWorkflow.NodesSection section)
		{
			ResourcePoolId = section.ReferenceId;
		}
	}
}
