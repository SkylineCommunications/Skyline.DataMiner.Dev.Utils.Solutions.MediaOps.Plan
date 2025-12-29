namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

    internal class ResourceRangeCapacitySetting : RangeCapacitySetting
    {
        internal StorageResourceStudio.ResourceCapacitiesSection originalSection;

        internal StorageResourceStudio.ResourceCapacitiesSection updatedSection;

        internal ResourceRangeCapacitySetting(RangeCapacitySetting rangeCapacitySetting) : base(rangeCapacitySetting)
        {
        }

        internal ResourceRangeCapacitySetting(StorageResourceStudio.ResourceCapacitiesSection section)
        {
            ParseSection(section);
            InitTracking();
        }

        internal override Storage.DOM.DomSectionBase OriginalSection => originalSection;

        internal StorageResourceStudio.ResourceCapacitiesSection GetSectionWithChanges()
        {
            if (updatedSection == null)
            {
                updatedSection = IsNew ? new StorageResourceStudio.ResourceCapacitiesSection() : originalSection.Clone();
            }

            updatedSection.ProfileParameterId = Id;
            updatedSection.DoubleMinValue = (double)minValue;
            updatedSection.DoubleMaxValue = (double)maxValue;

            return updatedSection;
        }

        private void ParseSection(StorageResourceStudio.ResourceCapacitiesSection section)
        {
            originalSection = section ?? throw new ArgumentNullException(nameof(section));

            Id = section.ProfileParameterId;
            minValue = (decimal)section.DoubleMinValue.Value;
            maxValue = (decimal)section.DoubleMaxValue.Value;
        }
    }
}