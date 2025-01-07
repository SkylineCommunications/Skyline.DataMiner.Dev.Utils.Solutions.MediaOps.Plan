namespace Skyline.DataMiner.MediaOps.API.Common.ResourceStudio
{
    /// <summary>
    /// Represents metadata from the client application.
    /// </summary>
    public class ClientMetadata
    {
        /// <summary>
        /// The Module ID of the client application.
        /// </summary>
        public string ModuleId { get; set; }

        /// <summary>
        /// The prefix that will be used for all objects created by the Resource Studio.
        /// </summary>
        public string Prefix { get; set; }
    }
}
