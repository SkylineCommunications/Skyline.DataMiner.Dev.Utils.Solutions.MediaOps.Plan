namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;

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

        /// <summary>
        /// Gets or sets the scheduled start time for this node in the job.
        /// </summary>
        public DateTimeOffset? StartTime { get; set; }

        /// <summary>
        /// Gets or sets the scheduled end time for this node in the job.
        /// </summary>
        public DateTimeOffset? EndTime { get; set; }

        /// <summary>
        /// Gets or sets the resource selection mode (Manual, AutoSelectAtBooking, AutoSelectAtRuntime).
        /// </summary>
        public ResourceSelectMode? ResourceSelectMode { get; set; }

        /// <summary>
        /// Gets or sets the current resource selection state.
        /// </summary>
        public ResourceSelectState? ResourceSelectState { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this node is billable.
        /// </summary>
        public bool IsBillable { get; set; }

        /// <summary>
        /// Gets or sets the booking IDs linked to this node.
        /// </summary>
        public ICollection<Guid> LinkedBookingIds { get; set; } = new List<Guid>();

        /// <summary>
        /// Gets or sets a value indicating whether this node should be reserved.
        /// </summary>
        public bool ReserveNode { get; set; }

        /// <summary>
        /// Gets or sets the configuration ID for this node in the job context.
        /// </summary>
        public Guid? ConfigurationId { get; set; }

        /// <summary>
        /// Gets or sets the configuration status of this node.
        /// </summary>
        public NodeConfigurationStatus? ConfigurationStatus { get; set; }
    }
}
