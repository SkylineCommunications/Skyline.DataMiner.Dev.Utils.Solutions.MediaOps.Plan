namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

    internal class ResourceStudioRangeCapacitySetting : RangeCapacitySetting
    {
        internal StorageResourceStudio.ProfileParameterValuesSection originalSection;
        internal StorageResourceStudio.ProfileParameterValuesSection updatedSection;

        internal ResourceStudioRangeCapacitySetting(RangeCapacitySetting rangeCapacitySetting) : base(rangeCapacitySetting)
        {
        }

        internal ResourceStudioRangeCapacitySetting(StorageResourceStudio.ProfileParameterValuesSection section)
        {
            ParseSection(section);
            InitTracking();
        }
        internal override Storage.DOM.DomSectionBase OriginalSection => originalSection;

        internal StorageResourceStudio.ProfileParameterValuesSection GetSectionWithChanges()
        {
            if (updatedSection == null)
            {
                updatedSection = IsNew ? new StorageResourceStudio.ProfileParameterValuesSection() : originalSection.Clone();
            }

            updatedSection.ProfileParameterId = Id;
            updatedSection.DoubleMinValue = (double)MinValue;
            updatedSection.DoubleMaxValue = (double)MaxValue;

            return updatedSection;
        }

        private void ParseSection(StorageResourceStudio.ProfileParameterValuesSection section)
        {
            originalSection = section ?? throw new ArgumentNullException(nameof(section));

            Id = section.ProfileParameterId;
            MinValue = (decimal)section.DoubleMinValue.Value;
            MaxValue = (decimal)section.DoubleMaxValue.Value;
        }
    }
}
