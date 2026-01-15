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
            updatedSection.DoubleMinValue = MinValue.HasValue ? (double)MinValue : null;
            updatedSection.DoubleMaxValue = MaxValue.HasValue ? (double)MaxValue : null;

            return updatedSection;
        }

        private void ParseSection(StorageResourceStudio.ResourceCapacitiesSection section)
        {
            originalSection = section ?? throw new ArgumentNullException(nameof(section));

            Id = section.ProfileParameterId;
            MinValue = section.DoubleMinValue.HasValue ? (decimal)section.DoubleMinValue.Value : null;
            MaxValue = section.DoubleMaxValue.HasValue ? (decimal)section.DoubleMaxValue.Value : null;
        }
    }
}