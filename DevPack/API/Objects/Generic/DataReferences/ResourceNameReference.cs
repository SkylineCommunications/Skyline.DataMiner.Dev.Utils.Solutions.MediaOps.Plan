namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    /// <summary>
    /// Represents a reference to the name of a resource.
    /// </summary>
    public class ResourceNameReference : DataReference
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceNameReference"/> class.
        /// </summary>
        public ResourceNameReference()
            : base(DataReferenceType.ResourceName)
        {
        }
    }
}
