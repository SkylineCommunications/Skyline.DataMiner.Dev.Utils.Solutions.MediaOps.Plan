namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Represents a resource node in a job context.
	/// This class is used when displaying or manipulating resource nodes for a specific job instance.
	/// Properties specific to job execution and resource selection are exposed.
	/// </summary>
	public class JobResourceNode : JobNode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JobResourceNode"/> class.
        /// </summary>
        public JobResourceNode() : base()
        {
        }

		internal JobResourceNode(StorageWorkflow.NodesSection section) : base(section)
		{

		}
    }
}
