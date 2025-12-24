namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents an error that occurs when attempting to delete a capability that is currently in use.
    /// </summary>
    public class CapabilityInUseError : CapabilityError
    {
        /// <summary>
        /// Gets or sets the collection of unique identifiers of the resources having the capability implemented.
        /// </summary>
        public List<Guid> ResourceIds { get; set; } = [];

        /// <summary>
        /// Gets or sets the collection of unique identifiers of the resource pools having the capability implemented.
        /// </summary>
        public List<Guid> ResourcePoolIds { get; set; } = [];
    }
}
