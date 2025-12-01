namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

    /// <summary>
    /// Represents an unmanaged resource in the MediaOps Plan API.
    /// </summary>
    public class UnmanagedResource : Resource
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnmanagedResource"/> class.
        /// </summary>
        public UnmanagedResource() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnmanagedResource"/> class with a specific resource ID.
        /// </summary>
        /// <param name="resourceId">The unique identifier of the resource.</param>
        public UnmanagedResource(Guid resourceId) : base(resourceId)
        {
        }

        internal UnmanagedResource(StorageResourceStudio.ResourceInstance instance) : base(instance)
        {
        }

        internal override void ApplyChanges(StorageResourceStudio.ResourceInstance instance)
        {
            instance.ResourceInfo.Type = StorageResourceStudio.SlcResource_StudioIds.Enums.Type.Unmanaged;
        }
    }
}
