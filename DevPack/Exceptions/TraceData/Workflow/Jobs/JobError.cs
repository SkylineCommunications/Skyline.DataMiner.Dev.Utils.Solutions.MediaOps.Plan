namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    using System;

    /// <summary>
    /// Represents an error that occurs when creating or updating a job with invalid configuration.
    /// </summary>
    public class JobError : MediaOpsErrorData
    {
        /// <summary>
        /// Gets the unique identifier for the job.
        /// </summary>
        public Guid Id { get; set; }
    }
}
