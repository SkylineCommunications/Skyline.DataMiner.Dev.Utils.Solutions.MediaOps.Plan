namespace Skyline.DataMiner.MediaOps.API.Common.ResourceStudio
{
    /// <summary>
    /// Represents the configuration for a service resource.
    /// </summary>
    public class ServiceResourceConfiguration : ResourceConfiguration
    {
        /// <summary>
        /// Gets or sets the DMS Agent ID.
        /// </summary>
        public int AgentId { get; set; }

        /// <summary>
        /// Gets or sets the DMS Service ID.
        /// </summary>
        public int ServiceId { get; set; }
    }
}
