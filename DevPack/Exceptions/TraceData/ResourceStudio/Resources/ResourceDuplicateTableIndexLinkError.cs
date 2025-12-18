namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    using System;

    /// <summary>
    /// Represents an error that occurs when a table index link is duplicated in a resource configuration.
    /// </summary>
    /// <remarks>This can only occur when resources with the same table index link are provided to a bulk operation.</remarks>
    public class ResourceDuplicateTableIndexLinkError : ResourceError
    {
        /// <summary>
        /// Gets or sets the agent ID associated with the resource link.
        /// </summary>
        public int AgentId { get; set; }

        /// <summary>
        /// Gets or sets the element ID associated with the resource link.
        /// </summary>
        public int ElementId { get; set; }

        /// <summary>
        /// Gets or sets the function ID associated with the resource link.
        /// </summary>
        public Guid FunctionId { get; set; }

        /// <summary>
        /// Gets or sets the function table index associated with the resource link.
        /// </summary>
        public string FunctionTableIndex { get; set; }
    }
}
