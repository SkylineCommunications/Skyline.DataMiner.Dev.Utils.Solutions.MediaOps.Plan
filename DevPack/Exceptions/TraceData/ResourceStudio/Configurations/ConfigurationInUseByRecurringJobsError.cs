namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents an error that occurs when a configuration is referenced by one or multiple Recurring Jobs.
    /// </summary>
    public class ConfigurationInUseByRecurringJobsError : ConfigurationInUseError
    {
        /// <summary>
        /// Ids of the recurring jobs referencing the configuration.
        /// </summary>
        public IReadOnlyCollection<Guid> RecurringJobIds { get; internal set; } = new List<Guid>();
    }
}
