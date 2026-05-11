namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    /// <summary>
    /// Represents a resource node in a workflow context.
    /// This class is used when displaying or manipulating resource nodes in workflow definitions.
    /// </summary>
    public class WorkflowResourceNode : WorkflowNode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WorkflowResourceNode"/> class.
        /// </summary>
        public WorkflowResourceNode() : base()
        {
        }

        /// <summary>
        /// Gets or sets the name or description for the workflow context.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this node is hidden from the workflow view.
        /// </summary>
        public bool IsHidden { get; set; }
    }
}
