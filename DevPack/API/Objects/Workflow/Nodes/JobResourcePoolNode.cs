namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Represents a resource pool node in a job context.
	/// This class is used when displaying or manipulating resource pool nodes for a specific job instance.
	/// Properties specific to job execution and resource selection are exposed.
	/// </summary>
	public class JobResourcePoolNode : JobNode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JobResourcePoolNode"/> class.
        /// </summary>
        public JobResourcePoolNode() : base()
        {
		}

		internal JobResourcePoolNode(StorageWorkflow.NodesSection section) : base(section)
		{

		}
    }
}
