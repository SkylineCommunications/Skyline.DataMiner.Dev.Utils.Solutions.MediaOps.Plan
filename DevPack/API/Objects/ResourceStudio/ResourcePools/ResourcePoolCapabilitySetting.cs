namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

    internal class ResourcePoolCapabilitySetting : CapabilitySettings
    {
        private StorageResourceStudio.ResourcePoolCapabilitiesSection originalSection;

        private StorageResourceStudio.ResourcePoolCapabilitiesSection updatedSection;

        internal ResourcePoolCapabilitySetting(CapabilitySettings capabilitySetting) : base(capabilitySetting)
        {
        }

        internal ResourcePoolCapabilitySetting(StorageResourceStudio.ResourcePoolCapabilitiesSection section)
        {
            ParseSection(section);
            InitTracking();
        }

        internal override Storage.DOM.DomSectionBase OriginalSection => originalSection;

        internal StorageResourceStudio.ResourcePoolCapabilitiesSection GetSectionWithChanges()
        {
            if (updatedSection == null)
            {
                updatedSection = IsNew ? new StorageResourceStudio.ResourcePoolCapabilitiesSection() : originalSection.Clone();
            }

            updatedSection.ProfileParameterId = Id;
            updatedSection.DiscreteValues = discretes;

            return updatedSection;
        }

        private void ParseSection(StorageResourceStudio.ResourcePoolCapabilitiesSection section)
        {
            originalSection = section ?? throw new ArgumentNullException(nameof(section));

            Id = section.ProfileParameterId;

            discretes.Clear();
            foreach (var discreteValue in section.DiscreteValues)
            {
                discretes.Add(discreteValue);
            }
        }
    }
}
