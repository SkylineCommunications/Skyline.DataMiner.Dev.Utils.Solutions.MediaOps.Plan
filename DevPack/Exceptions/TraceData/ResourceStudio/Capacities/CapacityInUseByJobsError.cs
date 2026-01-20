namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents an error that occurs when a capacity is referenced by one or multiple jobs.
    /// </summary>
    public class CapacityInUseByJobsError : CapacityInUseError
    {
        /// <summary>
        /// Ids of the jobs referencing the capacity.
        /// </summary>
        public IReadOnlyCollection<Guid> JobIds { get; internal set; } = new List<Guid>();
    }
}
