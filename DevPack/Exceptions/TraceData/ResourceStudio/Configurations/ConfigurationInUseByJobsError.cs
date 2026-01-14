namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents an error that occurs when a configuration is referenced by one or multiple Jobs.
    /// </summary>
    public class ConfigurationInUseByJobsError : ConfigurationInUseError
    {
        /// <summary>
        /// Ids of the Jobs referencing the configuration.
        /// </summary>
        public IReadOnlyCollection<Guid> JobIds { get; internal set; }
    }
}
