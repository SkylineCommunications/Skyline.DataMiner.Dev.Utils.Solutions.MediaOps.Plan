namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents an error that occurs when a configuration text discrete value is in use by one or multiple Resource Pools.
    /// </summary>
    public class ConfigurationTextDiscreteValueInUseByResourcePoolsError : ConfigurationTextDiscreteValueInUseError
    {
        /// <summary>
        /// Ids of the resource pools referencing the configuration text discrete value.
        /// </summary>
        public IReadOnlyCollection<Guid> ResourcePoolIds { get; internal set; } = new List<Guid>();
    }
}
