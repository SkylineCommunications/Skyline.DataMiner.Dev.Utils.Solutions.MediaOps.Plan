namespace Skyline.DataMiner.MediaOps.API.Common.ResourceStudio
{
    /// <summary>
    /// Represents the configuration for an element resource.
    /// </summary>
    public class ElementResourceConfiguration : ResourceConfiguration
    {
        /// <summary>
        /// Gets or sets the DMS Agent ID.
        /// </summary>
        public int AgentId { get; set; }

        /// <summary>
        /// Gets or sets the DMS Element ID.
        /// </summary>
        public int ElementId { get; set; }
    }
}
