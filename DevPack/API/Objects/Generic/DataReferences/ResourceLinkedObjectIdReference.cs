namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    /// <summary>
    /// Represents a reference to the linked object ID of a resource.
    /// </summary>
    public class ResourceLinkedObjectIdReference : DataReference
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceLinkedObjectIdReference"/> class.
        /// </summary>
        public ResourceLinkedObjectIdReference()
            : base(DataReferenceType.ResourceLinkedObjectID)
        {
        }
    }
}
