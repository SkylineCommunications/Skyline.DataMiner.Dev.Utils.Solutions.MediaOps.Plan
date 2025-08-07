namespace Skyline.DataMiner.MediaOps.Plan.API
{
    /// <summary>
    /// Represents options for deleting a resource pool.
    /// </summary>
    public class ResourcePoolDeleteOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether deprecated resources, part of the resource pool, should be deleted.
        /// </summary>
        public bool DeleteDeprecatedResources { get; set; } = false;

        internal static ResourcePoolDeleteOptions GetDefaults()
        {
            return new ResourcePoolDeleteOptions
            {
                DeleteDeprecatedResources = false
            };
        }
    }
}
