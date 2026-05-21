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
		/// Determines whether this node represents a resource and, if so, returns it as a <see cref="RecurringJobResourceNode"/>.
		/// </summary>
		/// <param name="resourceNode">When this method returns, contains the current node as a <see cref="RecurringJobResourceNode"/> when it represents a resource; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this node represents a resource; otherwise, <c>false</c>.</returns>
		public bool IsResourceNode(out RecurringJobResourceNode resourceNode)
		{
			resourceNode = this as RecurringJobResourceNode;
			return resourceNode != null;
		}

		/// <summary>
		/// Determines whether this node represents a resource pool and, if so, returns it as a <see cref="RecurringJobResourcePoolNode"/>.
		/// </summary>
		/// <param name="resourcePoolNode">When this method returns, contains the current node as a <see cref="RecurringJobResourcePoolNode"/> when it represents a resource pool; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this node represents a resource pool; otherwise, <c>false</c>.</returns>
		public bool IsResourcePoolNode(out RecurringJobResourcePoolNode resourcePoolNode)
		{
			resourcePoolNode = this as RecurringJobResourcePoolNode;
			return resourcePoolNode != null;
		}
	}
}
