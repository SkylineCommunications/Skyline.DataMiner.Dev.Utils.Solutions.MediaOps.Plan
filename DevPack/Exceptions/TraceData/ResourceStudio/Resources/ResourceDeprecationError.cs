namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents an error that occurs when the an operation cannot be executed because the Resource is booked in an ongoing or future Job.
    /// </summary>
    public class ResourceDeprecationError : ResourceError
    {
        /// <summary>
        /// Gets the collection of job details that prevent the resource action from being executed.
        /// </summary>
        public IReadOnlyCollection<ResourceBookedErrorJobDetails> JobDetails { get; internal set; }

        /// <summary>
        /// Represents the details of a job that blocks the resource operation.
        /// </summary>
        public sealed class ResourceBookedErrorJobDetails
        {
            /// <summary>
            /// Gets or sets the unique identifier for the job.
            /// </summary>
            public Guid Id { get; set; }

            /// <summary>
            /// Gets or sets the name associated with the job.
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Gets or sets the job start time.
            /// </summary>
            public DateTimeOffset Start { get; set; }

            /// <summary>
            /// Gets or sets the job end time.
            /// </summary>
            public DateTimeOffset End { get; set; }
        }
    }
}
