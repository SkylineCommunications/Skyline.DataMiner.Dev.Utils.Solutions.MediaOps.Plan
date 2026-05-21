namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	public class JobResourcePoolNode : JobNode, IResourcePoolNode
    {
        public JobResourcePoolNode(ResourcePool resourcePool)
			: this (resourcePool?.Id ?? throw new ArgumentNullException(nameof(resourcePool)))
        {
		}

		public JobResourcePoolNode(Guid resourcePoolId) : base()
		{
			if (resourcePoolId == Guid.Empty)
			{
				throw new ArgumentException(nameof(resourcePoolId));
			}

			ResourcePoolId = resourcePoolId;
		}

		internal JobResourcePoolNode(MediaOpsPlanApi planApi, StorageWorkflow.NodesSection section) : base(planApi, section)
		{
			ParseSection(section);
			InitTracking();
		}

		/// <inheritdoc/>
		public Guid ResourcePoolId { get; private set; }

		/// <inheritdoc/>
		public override bool IsResourcePoolNode => true;

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
