namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents an error that occurs when a capability is referenced by one or multiple jobs.
    /// </summary>
    public class CapabilityInUseByJobsError : CapabilityInUseError
    {
        /// <summary>
        /// Ids of the jobs referencing the capability.
        /// </summary>
        public IReadOnlyCollection<Guid> JobIds { get; internal set; } = new List<Guid>();
    }
}
