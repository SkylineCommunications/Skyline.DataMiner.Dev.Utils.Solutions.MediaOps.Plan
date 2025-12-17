namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    using System;

    /// <summary>
    /// Represents an error that occurs when deleting a resource property.
    /// </summary>
    public class ResourcePropertyDeleteError : MediaOpsErrorData
    {
        /// <summary>
        /// Gets the unique identifier for the resource property.
        /// </summary>
        public Guid Id { get; set; }
    }
}
