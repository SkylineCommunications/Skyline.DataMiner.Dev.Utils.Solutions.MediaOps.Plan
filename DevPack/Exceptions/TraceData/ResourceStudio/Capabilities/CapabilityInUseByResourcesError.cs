namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents an error that occurs when a capability is referenced by one or multiple resources.
    /// </summary>
    public class CapabilityInUseByResourcesError : CapabilityInUseError
    {
        /// <summary>
        /// Ids of the resources referencing the capability.
        /// </summary>
        public IReadOnlyCollection<Guid> ResourceIds { get; internal set; } = new List<Guid>();
    }
}
