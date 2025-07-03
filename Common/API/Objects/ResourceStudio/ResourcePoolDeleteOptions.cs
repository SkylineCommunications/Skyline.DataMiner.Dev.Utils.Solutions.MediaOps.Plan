namespace Skyline.DataMiner.MediaOps.Plan.API
{
    /// <summary>
    /// Represents options for deleting a resource pool.
    /// </summary>
    public class ResourcePoolDeleteOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether draft or deprecated resources,
        /// which only belong to this resource pool, are allowed to be deleted.
        /// </summary>
        public bool DeleteChildren { get; set; } = false;

        internal static ResourcePoolDeleteOptions GetDefaults()
        {
            return new ResourcePoolDeleteOptions
            {
                DeleteChildren = false
            };
        }
    }
}
