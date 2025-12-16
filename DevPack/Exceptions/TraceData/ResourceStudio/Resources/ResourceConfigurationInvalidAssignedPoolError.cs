namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    using System;

    /// <summary>
    /// Represents an error that occurs when a resource configuration specifies an invalid or unsupported assigned pool.
    /// </summary>
    public class ResourceConfigurationInvalidAssignedPoolError : ResourceConfigurationError
    {
        /// <summary>
        /// Gets or sets the unique identifier of the associated resource pool.
        /// </summary>
        public Guid ResourcePoolId { get; set; }
    }
}
