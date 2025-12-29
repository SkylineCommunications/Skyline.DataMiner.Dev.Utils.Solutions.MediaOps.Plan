namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

    internal class ResourceCapabilitySetting : CapabilitySetting
    {
        private StorageResourceStudio.ResourceCapabilitiesSection originalSection;

        private StorageResourceStudio.ResourceCapabilitiesSection updatedSection;

        internal ResourceCapabilitySetting(CapabilitySetting capabilitySetting) : base(capabilitySetting)
        {
        }

        internal ResourceCapabilitySetting(StorageResourceStudio.ResourceCapabilitiesSection section)
        {
            ParseSection(section);
            InitTracking();
        }

        internal override Storage.DOM.DomSectionBase OriginalSection => originalSection;

        internal StorageResourceStudio.ResourceCapabilitiesSection GetSectionWithChanges()
        {
            if (updatedSection == null)
            {
                updatedSection = IsNew ? new StorageResourceStudio.ResourceCapabilitiesSection() : originalSection.Clone();
            }

            updatedSection.ProfileParameterId = Id;
            updatedSection.DiscreteValues = discretes;

            return updatedSection;
        }

        private void ParseSection(StorageResourceStudio.ResourceCapabilitiesSection section)
        {
            originalSection = section ?? throw new ArgumentNullException(nameof(section));

            Id = section.ProfileParameterId;
            discretes = new HashSet<string>(section.DiscreteValues);
        }
    }
}
