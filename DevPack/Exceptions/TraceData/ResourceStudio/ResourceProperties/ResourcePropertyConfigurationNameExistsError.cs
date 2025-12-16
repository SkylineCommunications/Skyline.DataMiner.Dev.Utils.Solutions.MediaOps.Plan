namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    /// <summary>
    /// Represents an error that occurs when a resource property configuration name already exists.
    /// </summary>
    public class ResourcePropertyConfigurationNameExistsError : ResourcePropertyConfigurationError
    {
        /// <summary>
        /// Gets the name of the resource property.
        /// </summary>
        public string Name { get; set; }
    }
}
