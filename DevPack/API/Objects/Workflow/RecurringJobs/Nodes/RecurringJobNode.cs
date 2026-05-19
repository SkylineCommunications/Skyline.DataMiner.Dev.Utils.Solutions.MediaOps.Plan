namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Base class for nodes within recurring jobs.
	/// </summary>
	public abstract class RecurringJobNode : NodeBase
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RecurringJobNode"/> class with a new unique identifier.
		/// </summary>
		private protected RecurringJobNode() : base()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RecurringJobNode"/> class from a storage section.
		/// </summary>
		/// <param name="section">The storage workflow nodes section to parse.</param>
		private protected RecurringJobNode(StorageWorkflow.NodesSection section) : base(section)
		{
		}
	}
}
