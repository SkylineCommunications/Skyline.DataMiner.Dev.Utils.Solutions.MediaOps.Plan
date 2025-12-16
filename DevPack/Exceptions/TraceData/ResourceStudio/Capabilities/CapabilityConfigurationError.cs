namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    using System;

    /// <summary>
    /// Represents an error that occurs when creating or updating a capability with invalid configuration.
    /// </summary>
    public class CapabilityConfigurationError : MediaOpsErrorData
    {
        /// <summary>
        /// Gets the unique identifier for the capability.
        /// </summary>
        public Guid Id { get; set; }
    }
}
