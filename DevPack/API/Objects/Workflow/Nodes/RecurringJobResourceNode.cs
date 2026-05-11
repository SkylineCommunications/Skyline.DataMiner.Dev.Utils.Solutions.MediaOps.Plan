namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    /// <summary>
    /// Represents a resource node in a recurring job context.
    /// This class is used when displaying or manipulating resource nodes for recurring job definitions.
    /// Properties related to recurring job patterns and scheduling are exposed.
    /// </summary>
    public class RecurringJobResourceNode : RecurringJobNode
	{
        /// <summary>
        /// Initializes a new instance of the <see cref="RecurringJobResourceNode"/> class.
        /// </summary>
        public RecurringJobResourceNode() : base()
        {
        }

        /// <summary>
        /// Gets or sets the relative offset from the recurring job start time when this node should begin.
        /// </summary>
        public TimeSpan? StartOffset { get; set; }

        /// <summary>
        /// Gets or sets the relative offset from the recurring job start time when this node should end.
        /// </summary>
        public TimeSpan? EndOffset { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this node is optional in the recurring job pattern.
        /// </summary>
        public bool IsOptional { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this node should be reserved.
        /// </summary>
        public bool ReserveNode { get; set; }

        /// <summary>
        /// Gets or sets the configuration ID for this node in the recurring job context.
        /// </summary>
        public Guid? ConfigurationId { get; set; }
    }
}
