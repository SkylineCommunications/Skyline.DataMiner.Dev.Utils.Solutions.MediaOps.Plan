namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	public class RecurringJobResourcePoolNode : RecurringJobNode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RecurringJobResourcePoolNode"/> class.
        /// </summary>
        public RecurringJobResourcePoolNode() : base()
        {
		}

		internal override void ApplyChanges(StorageWorkflow.NodesSection section)
		{

		}

		private void ParseSection(StorageWorkflow.NodesSection section)
		{

		}
	}
}
