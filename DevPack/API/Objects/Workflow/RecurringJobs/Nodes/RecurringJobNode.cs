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
	}
}
