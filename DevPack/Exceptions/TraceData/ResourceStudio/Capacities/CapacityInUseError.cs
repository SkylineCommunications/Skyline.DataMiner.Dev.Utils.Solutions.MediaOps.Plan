namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents an error that occurs when attempting to delete a capacity that is currently in use.
    /// </summary>
    public class CapacityInUseError : CapacityError
    {
        /// <summary>
        /// Gets or sets the collection of unique identifiers of the resources having the capacity implemented.
        /// </summary>
        public List<Guid> ResourceIds { get; set; } = [];
    }
}
