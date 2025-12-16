namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    /// <summary>
    /// Represents an error that occurs when a resource configuration contains an invalid service link.
    /// </summary>
    public class ResourceConfigurationInvalidServiceLinkError : ResourceConfigurationError
    {
        /// <summary>
        /// Gets or sets the agent ID associated with the resource link.
        /// </summary>
        public int AgentId { get; set; }

        /// <summary>
        /// Gets or sets the service ID associated with the resource link.
        /// </summary>
        public int ServiceId { get; set; }
    }
}
