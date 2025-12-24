namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    /// <summary>
    /// Represents an error that occurs when a resource configuration contains an invalid element link.
    /// </summary>
    public class ResourceInvalidElementLinkError : ResourceError
    {
        /// <summary>
        /// Gets or sets the agent ID associated with the resource link.
        /// </summary>
        public int AgentId { get; set; }

        /// <summary>
        /// Gets or sets the element ID associated with the resource link.
        /// </summary>
        public int ElementId { get; set; }
    }
}
