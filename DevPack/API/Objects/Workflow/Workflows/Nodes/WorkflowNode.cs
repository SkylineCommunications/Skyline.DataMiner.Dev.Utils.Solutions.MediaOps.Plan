namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Base class for nodes within workflows.
	/// </summary>
	public abstract class WorkflowNode : NodeBase
	{
		private protected WorkflowNode() : base()
		{
		}

		private protected WorkflowNode(MediaOpsPlanApi planApi, StorageWorkflow.NodesSection section) : base(planApi, section)
		{
		}

		/// <summary>
		/// Determines whether this node represents a resource and, if so, returns it as a <see cref="WorkflowResourceNode"/>.
		/// </summary>
		/// <param name="resourceNode">When this method returns, contains the current node as a <see cref="WorkflowResourceNode"/> when it represents a resource; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this node represents a resource; otherwise, <c>false</c>.</returns>
		public bool IsResourceNode(out WorkflowResourceNode resourceNode)
		{
			resourceNode = this as WorkflowResourceNode;
			return resourceNode != null;
		}

		/// <summary>
		/// Determines whether this node represents a resource pool and, if so, returns it as a <see cref="WorkflowResourcePoolNode"/>.
		/// </summary>
		/// <param name="resourcePoolNode">When this method returns, contains the current node as a <see cref="WorkflowResourcePoolNode"/> when it represents a resource pool; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this node represents a resource pool; otherwise, <c>false</c>.</returns>
		public bool IsResourcePoolNode(out WorkflowResourcePoolNode resourcePoolNode)
		{
			resourcePoolNode = this as WorkflowResourcePoolNode;
			return resourcePoolNode != null;
		}
	}
}
