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
		/// Returns this node as a <see cref="WorkflowResourceNode"/> when it represents a resource; otherwise, <c>null</c>.
		/// </summary>
		/// <returns>The current node as a <see cref="WorkflowResourceNode"/>, or <c>null</c> when this node does not represent a resource.</returns>
		public new WorkflowResourceNode AsResourceNode() => this as WorkflowResourceNode;

		/// <summary>
		/// Returns this node as a <see cref="WorkflowResourcePoolNode"/> when it represents a resource pool; otherwise, <c>null</c>.
		/// </summary>
		/// <returns>The current node as a <see cref="WorkflowResourcePoolNode"/>, or <c>null</c> when this node does not represent a resource pool.</returns>
		public new WorkflowResourcePoolNode AsResourcePoolNode() => this as WorkflowResourcePoolNode;
	}
}
