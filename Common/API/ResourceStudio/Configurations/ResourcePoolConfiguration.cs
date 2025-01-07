namespace Skyline.DataMiner.MediaOps.API.Common.ResourceStudio
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents the configuration for a resource pool, including its name.
    /// </summary>
    public class ResourcePoolConfiguration
    {
        /// <summary>
        /// Gets or sets the name of the resource pool.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the desired status of the resource pool.
        /// </summary>
        public DesiredStatus DesiredStatus { get; set; }

        /// <summary>
        /// Gets the list of Resource IDs associated with the resource pool
        /// </summary>
        public ICollection<Guid> ResourceIds { get; } = new List<Guid>();
    }
}
