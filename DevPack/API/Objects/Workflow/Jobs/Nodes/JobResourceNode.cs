namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	public class JobResourceNode : JobNode, IResourceNode
    {
		public JobResourceNode(ResourcePool resourcePool, Resource resource)
			: this(resourcePool?.Id ?? throw new ArgumentNullException(nameof(resourcePool)), resource?.Id ?? throw new ArgumentNullException(nameof(resource)))
		{
		}

		public JobResourceNode(Guid resourcePoolId, Resource resource)
			: this(resourcePoolId, resource?.Id ?? throw new ArgumentNullException(nameof(resource)))
		{
		}

		public JobResourceNode(ResourcePool resourcePool, Guid resourceId)
			: this(resourcePool?.Id ?? throw new ArgumentNullException(nameof(resourcePool)), resourceId)
		{
		}

		public JobResourceNode(Guid resourcePoolId, Guid resourceId) : base()
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

		internal JobResourceNode(MediaOpsPlanApi planApi, StorageWorkflow.NodesSection section) : base(planApi, section)
		{
			ParseSection(section);
			InitTracking();
		}

		/// <inheritdoc/>
		public Guid ResourcePoolId { get; private set; }

		/// <inheritdoc/>
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
