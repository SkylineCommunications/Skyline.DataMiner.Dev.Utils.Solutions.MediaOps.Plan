namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	public class WorkflowResourceNode : WorkflowNode
	{
		public WorkflowResourceNode(ResourcePool resourcePool, Resource resource)
			: this(resourcePool?.Id ?? throw new ArgumentNullException(nameof(resourcePool)), resource?.Id ?? throw new ArgumentNullException(nameof(resource)))
		{
		}

		public WorkflowResourceNode(Guid resourcePoolId, Resource resource)
			: this(resourcePoolId, resource?.Id ?? throw new ArgumentNullException(nameof(resource)))
		{
		}

		public WorkflowResourceNode(ResourcePool resourcePool, Guid resourceId)
			: this(resourcePool?.Id ?? throw new ArgumentNullException(nameof(resourcePool)), resourceId)
		{
		}

		public WorkflowResourceNode(Guid resourcePoolId, Guid resourceId) : base()
		{
			if (resourcePoolId == Guid.Empty)
			{
				throw new ArgumentException(nameof(resourcePoolId));
			}

			if (resourceId == Guid.Empty)
			{
				throw new ArgumentException(nameof(resourceId));
			}

			ResourcePoolId = resourcePoolId;
			ResourceId = resourceId;
		}

		internal WorkflowResourceNode(StorageWorkflow.NodesSection section) : base(section)
		{
			ParseSection(section);
			InitTracking();
		}

		/// <summary>
		/// Gets the unique identifier of the resource pool associated with this node.
		/// </summary>
		public Guid ResourcePoolId { get; private set; }

		/// <summary>
		/// Gets the unique identifier of the resource associated with this node.
		/// </summary>
		public Guid ResourceId { get; private set; }

		internal override void ApplyChanges(StorageWorkflow.NodesSection section)
		{
			section.NodeType = StorageWorkflow.SlcWorkflowIds.Enums.Nodetype.Resource;
			section.ReferenceId = ResourceId;
			section.ParentReferenceId = ResourcePoolId;
		}

		private void ParseSection(StorageWorkflow.NodesSection section)
		{
			ResourcePoolId = section.ParentReferenceId;
			ResourceId = section.ReferenceId;
		}
	}
}
