namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    using System;

    /// <summary>
    /// Represents an error that occurs when resource pool capability settings are invalid.
    /// </summary>
    public class ResourcePoolInvalidCapabilitySettingsError : ResourcePoolError
    {
        /// <summary>
        /// Gets the unique identifier for the capability.
        /// </summary>
        public Guid CapabilityId { get; set; }
    }
}
