namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	public class RecurringJobResourceNode : RecurringJobNode
	{
        /// <summary>
        /// Initializes a new instance of the <see cref="RecurringJobResourceNode"/> class.
        /// </summary>
        public RecurringJobResourceNode() : base()
        {
		}

		internal override void ApplyChanges(StorageWorkflow.NodesSection section)
		{

		}
	}
}
