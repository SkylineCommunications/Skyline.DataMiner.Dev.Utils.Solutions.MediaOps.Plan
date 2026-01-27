namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents an error that occurs when a capability discrete value is referenced by one or multiple Recurring Jobs.
    /// </summary>
    public class CapabilityDiscreteValueInUseByRecurringJobsError : CapabilityDiscreteValueInUseError
    {
        /// <summary>
        /// Ids of the recurring jobs referencing the capability discrete value.
        /// </summary>
        public IReadOnlyCollection<Guid> RecurringJobIds { get; internal set; } = new List<Guid>();
    }
}
