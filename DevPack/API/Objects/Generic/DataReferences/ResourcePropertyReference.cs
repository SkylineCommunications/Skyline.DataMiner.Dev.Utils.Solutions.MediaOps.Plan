namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    /// <summary>
    /// Represents a reference to a resource property.
    /// </summary>
    public class ResourcePropertyReference : DataReference
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourcePropertyReference"/> class with the specified property ID.
        /// </summary>
        /// <param name="propertyId">The unique identifier of the resource property.</param>
        public ResourcePropertyReference(Guid propertyId)
            : base(DataReferenceType.ResourceProperty)
        {
            PropertyId = propertyId;
        }

        /// <summary>
        /// Gets the unique identifier of the resource property.
        /// </summary>
        public Guid PropertyId { get; }
    }
}
