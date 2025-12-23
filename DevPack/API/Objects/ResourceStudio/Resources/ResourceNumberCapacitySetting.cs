namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

    internal class ResourceNumberCapacitySetting : NumberCapacitySetting
    {
        internal StorageResourceStudio.ResourceCapacitiesSection originalSection;

        internal StorageResourceStudio.ResourceCapacitiesSection updatedSection;

        internal ResourceNumberCapacitySetting(NumberCapacitySetting numberCapacitySetting) : base(numberCapacitySetting)
        {
        }

        internal ResourceNumberCapacitySetting(StorageResourceStudio.ResourceCapacitiesSection section)
        {
            ParseSection(section);
        }

        internal override Storage.DOM.DomSectionBase OriginalSection => originalSection;

        internal StorageResourceStudio.ResourceCapacitiesSection GetSectionWithChanges()
        {
            if (updatedSection == null)
            {
                updatedSection = IsNew ? new StorageResourceStudio.ResourceCapacitiesSection() : originalSection.Clone();
            }

            updatedSection.ProfileParameterId = Id;
            updatedSection.DoubleMaxValue = (double)value;
            updatedSection.DoubleMinValue = null;

            return updatedSection;
        }

        private void ParseSection(StorageResourceStudio.ResourceCapacitiesSection section)
        {
            originalSection = section ?? throw new ArgumentNullException(nameof(section));

            Id = section.ProfileParameterId;
            value = (decimal)section.DoubleMaxValue.Value;
        }
    }
}