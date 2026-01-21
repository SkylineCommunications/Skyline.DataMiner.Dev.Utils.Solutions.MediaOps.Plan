namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents an error that occurs when a configuration is in use by one or multiple Resource Pools.
    /// </summary>
    public class ConfigurationInUseByResourcePoolsError : ConfigurationInUseError
    {
        /// <summary>
        /// Ids of the resource pools referencing the configuration.
        /// </summary>
        public IReadOnlyCollection<Guid> ResourcePoolIds { get; internal set; } = new List<Guid>();
    }
}
