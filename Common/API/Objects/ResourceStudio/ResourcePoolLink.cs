namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;

    using Skyline.DataMiner.MediaOps.Plan.Extensions;

    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

    public class ResourcePoolLink : TrackableObject
    {
        private StorageResourceStudio.ResourcePoolLinksSection originalSection;

        private StorageResourceStudio.ResourcePoolLinksSection updatedSection;

        private ResourceSelectionType resourceSelectionType;

        public ResourcePoolLink(ResourcePool resourcePool) : this(resourcePool?.Id ?? throw new ArgumentNullException(nameof(resourcePool)))
        {
        }

        public ResourcePoolLink(Guid resourcePoolId)
        {
            if (resourcePoolId == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(resourcePoolId));
            }

            LinkedResourcePoolId = resourcePoolId;

            IsNew = true;
        }

        internal ResourcePoolLink(StorageResourceStudio.ResourcePoolLinksSection section)
        {
            ParseSection(section);
        }

        public Guid LinkedResourcePoolId { get; private set; }

        public ResourceSelectionType SelectionType
        {
            get => resourceSelectionType;
            set
            {
                HasChanges = true;
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

            updatedSection.ResourceSelectionType = EnumExtensions.MapEnum<ResourceSelectionType, StorageResourceStudio.SlcResource_StudioIds.Enums.Resourceselectiontype>(resourceSelectionType);

            return updatedSection;
        }

        private void ParseSection(StorageResourceStudio.ResourcePoolLinksSection section)
        {
            if (section == null)
            {
                throw new ArgumentNullException(nameof(section));
            }

            LinkedResourcePoolId = section.LinkedResourcePool.Value;
            resourceSelectionType = EnumExtensions.MapEnum<StorageResourceStudio.SlcResource_StudioIds.Enums.Resourceselectiontype, ResourceSelectionType>(section.ResourceSelectionType.Value);
        }
    }
}
