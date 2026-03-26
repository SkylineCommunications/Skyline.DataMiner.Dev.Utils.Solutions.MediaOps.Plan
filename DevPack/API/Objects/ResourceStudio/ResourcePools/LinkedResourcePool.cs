namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;
    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

    /// <summary>
    /// Represents a link to another resource pool.
    /// </summary>
    public class LinkedResourcePool : TrackableObject
    {
        private readonly StorageResourceStudio.ResourcePoolLinksSection originalSection;

        private StorageResourceStudio.ResourcePoolLinksSection updatedSection;

        /// <summary>
        /// Initializes a new instance of the <see cref="LinkedResourcePool"/> class with the linked resource pool.
        /// </summary>
        /// <param name="resourcePool">The linked resource pool.</param>
        public LinkedResourcePool(ResourcePool resourcePool) : this(resourcePool?.Id ?? throw new ArgumentNullException(nameof(resourcePool)))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LinkedResourcePool"/> class with the linked resource pool ID.
        /// </summary>
        /// <param name="resourcePoolId">The unique identifier of the linked resource pool.</param>
        public LinkedResourcePool(Guid resourcePoolId)
        {
            if (resourcePoolId == Guid.Empty)
            {
                throw new ArgumentException(nameof(resourcePoolId));
            }

            LinkedResourcePoolId = resourcePoolId;

            IsNew = true;
        }

        internal LinkedResourcePool(StorageResourceStudio.ResourcePoolLinksSection section)
        {
            originalSection = section ?? throw new ArgumentNullException(nameof(section));

            ParseSection();
            InitTracking();
        }

        /// <summary>
        /// Gets the unique identifier of the linked resource pool.
        /// </summary>
        public Guid LinkedResourcePoolId { get; private set; }

        /// <summary>
        /// Gets or sets the selection type for the linked resource pool.
        /// </summary>
        public ResourceSelectionType SelectionType { get; set; }

        internal StorageResourceStudio.ResourcePoolLinksSection OriginalSection => originalSection;

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 23) + LinkedResourcePoolId.GetHashCode();
                hash = (hash * 23) + SelectionType.GetHashCode();
                hash = (hash * 23) + (originalSection != null ? originalSection.ID.Id.GetHashCode() : 0);
                return hash;
            }
        }

        internal StorageResourceStudio.ResourcePoolLinksSection GetSectionWithChanges()
        {
            if (updatedSection == null)
            {
                updatedSection = IsNew ? new StorageResourceStudio.ResourcePoolLinksSection() : originalSection.Clone();
            }

            updatedSection.LinkedResourcePool = LinkedResourcePoolId;
            updatedSection.ResourceSelectionType = EnumExtensions.MapEnum<ResourceSelectionType, StorageResourceStudio.SlcResource_StudioIds.Enums.Resourceselectiontype>(SelectionType);

            return updatedSection;
        }

        private void ParseSection()
        {
            LinkedResourcePoolId = originalSection.LinkedResourcePool.HasValue ? originalSection.LinkedResourcePool.Value : Guid.Empty;
            SelectionType = originalSection.ResourceSelectionType.HasValue ? EnumExtensions.MapEnum<StorageResourceStudio.SlcResource_StudioIds.Enums.Resourceselectiontype, ResourceSelectionType>(originalSection.ResourceSelectionType.Value) : ResourceSelectionType.Manual;
        }
    }
}
