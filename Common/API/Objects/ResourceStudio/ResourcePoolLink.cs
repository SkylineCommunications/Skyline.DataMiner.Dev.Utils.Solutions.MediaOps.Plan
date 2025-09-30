namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;

    using Skyline.DataMiner.MediaOps.Plan.Extensions;

    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

    /// <summary>
    /// Represents a link to another resource pool.
    /// </summary>
    public class ResourcePoolLink : TrackableObject
    {
        private StorageResourceStudio.ResourcePoolLinksSection originalSection;

        private StorageResourceStudio.ResourcePoolLinksSection updatedSection;

        private ResourceSelectionType resourceSelectionType;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourcePoolLink"/> class with a specific resource pool to link to.
        /// </summary>
        /// <param name="resourcePool">The resource pool to link to.</param>
        public ResourcePoolLink(ResourcePool resourcePool) : this(resourcePool?.Id ?? throw new ArgumentNullException(nameof(resourcePool)))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourcePoolLink"/> class with a specific resource pool ID to link to.
        /// </summary>
        /// <param name="resourcePoolId">The unique identifier of the resource pool to link to.</param>
        public ResourcePoolLink(Guid resourcePoolId)
        {
            if (resourcePoolId == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(resourcePoolId));
            }

            LinkedResourcePoolId = resourcePoolId;

            IsNew = true;
        }

        internal EventHandler<EventArgs> ValueChanged;

        internal ResourcePoolLink(StorageResourceStudio.ResourcePoolLinksSection section)
        {
            ParseSection(section);
        }

        /// <summary>
        /// Gets the unique identifier of the linked resource pool.
        /// </summary>
        public Guid LinkedResourcePoolId { get; private set; }

        /// <summary>
        /// Gets or sets the selection type for the linked resource pool.
        /// </summary>
        public ResourceSelectionType SelectionType
        {
            get => resourceSelectionType;
            set
            {
                HasChanges = true;
                ValueChanged?.Invoke(this, EventArgs.Empty);
                resourceSelectionType = value;
            }
        }

        internal StorageResourceStudio.ResourcePoolLinksSection OriginalSection => originalSection;

        internal StorageResourceStudio.ResourcePoolLinksSection GetSectionWithChanges()
        {
            if (updatedSection == null)
            {
                updatedSection = IsNew ? new StorageResourceStudio.ResourcePoolLinksSection() : originalSection.Clone();
            }

            updatedSection.LinkedResourcePool = LinkedResourcePoolId;
            updatedSection.ResourceSelectionType = EnumExtensions.MapEnum<ResourceSelectionType, StorageResourceStudio.SlcResource_StudioIds.Enums.Resourceselectiontype>(resourceSelectionType);

            return updatedSection;
        }

        private void ParseSection(StorageResourceStudio.ResourcePoolLinksSection section)
        {
            originalSection = section ?? throw new ArgumentNullException(nameof(section));

            LinkedResourcePoolId = section.LinkedResourcePool.Value;
            resourceSelectionType = EnumExtensions.MapEnum<StorageResourceStudio.SlcResource_StudioIds.Enums.Resourceselectiontype, ResourceSelectionType>(section.ResourceSelectionType.Value);
        }
    }
}
