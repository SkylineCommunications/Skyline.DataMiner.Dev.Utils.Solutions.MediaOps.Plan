namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Base class for nodes within workflows.
	/// </summary>
	public abstract class WorkflowNode : NodeBase
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="WorkflowNode"/> class with a new unique identifier.
		/// </summary>
		private protected WorkflowNode() : base()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="WorkflowNode"/> class from a storage section.
		/// </summary>
		/// <param name="section">The storage workflow nodes section to parse.</param>
		private protected WorkflowNode(StorageWorkflow.NodesSection section) : base(section)
		{
		}
	}
}
