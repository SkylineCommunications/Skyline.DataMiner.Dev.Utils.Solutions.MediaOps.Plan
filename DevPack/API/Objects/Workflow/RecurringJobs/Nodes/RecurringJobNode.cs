namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Base class for nodes within recurring jobs.
	/// </summary>
	public abstract class RecurringJobNode : NodeBase
	{
		private protected RecurringJobNode() : base()
		{
		}

		private protected RecurringJobNode(MediaOpsPlanApi planApi, StorageWorkflow.NodesSection section) : base(planApi, section)
		{
		}

		/// <summary>
		/// Returns this node as a <see cref="RecurringJobResourceNode"/> when it represents a resource; otherwise, <c>null</c>.
		/// </summary>
		/// <returns>The current node as a <see cref="RecurringJobResourceNode"/>, or <c>null</c> when this node does not represent a resource.</returns>
		public new RecurringJobResourceNode AsResourceNode() => this as RecurringJobResourceNode;

		/// <summary>
		/// Returns this node as a <see cref="RecurringJobResourcePoolNode"/> when it represents a resource pool; otherwise, <c>null</c>.
		/// </summary>
		/// <returns>The current node as a <see cref="RecurringJobResourcePoolNode"/>, or <c>null</c> when this node does not represent a resource pool.</returns>
		public new RecurringJobResourcePoolNode AsResourcePoolNode() => this as RecurringJobResourcePoolNode;
	}
}
