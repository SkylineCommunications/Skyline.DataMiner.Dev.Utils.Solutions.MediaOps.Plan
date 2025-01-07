namespace Skyline.DataMiner.MediaOps.API.Common.ResourceStudio
{
    using System;

    /// <summary>
    /// Represents the configuration for a resource, including its name and concurrency.
    /// </summary>
    public class ResourceConfiguration
    {
        /// <summary>
        /// Gets or sets the name of the resource.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the desired status of the resource.
        /// </summary>
        public DesiredStatus DesiredStatus { get; set; }

        /// <summary>
        /// Gets or sets the concurrency for the resource.
        /// </summary>
        public long Concurrency { get; set; }
    }
}
