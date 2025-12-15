namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    /// <summary>
    /// Represents an error that occurs when a resource pool configuration specifies an invalid resource pool name.
    /// </summary>
    public class ResourcePoolConfigurationInvalidNameError : ResourcePoolConfigurationError
    {
        /// <summary>
        /// Gets the name of the resource pool.
        /// </summary>
        public string Name { get; set; }
    }
}
