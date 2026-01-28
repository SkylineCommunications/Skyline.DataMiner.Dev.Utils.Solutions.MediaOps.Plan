namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents an error that occurs when a configuration number discrete value is in use by one or multiple Resource Pools.
    /// </summary>
    public class ConfigurationNumberDiscreteValueInUseByResourcePoolsError : ConfigurationNumberDiscreteValueInUseError
    {
        /// <summary>
        /// Ids of the resource pools referencing the configuration number discrete value.
        /// </summary>
        public IReadOnlyCollection<Guid> ResourcePoolIds { get; internal set; } = new List<Guid>();
    }
}
