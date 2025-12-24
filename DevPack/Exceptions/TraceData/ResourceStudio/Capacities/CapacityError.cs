namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    using System;

    /// <summary>
    /// Represents an error that occurs when creating or updating a capacity with invalid configuration.
    /// </summary>
    public class CapacityError : MediaOpsErrorData
    {
        /// <summary>
        /// Gets the unique identifier for the capacity.
        /// </summary>
        public Guid Id { get; set; }
    }
}
