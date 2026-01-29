namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    using StorageWorkflow = Storage.DOM.SlcWorkflow;

    internal class WorkflowRangeCapacitySetting : RangeCapacitySetting
    {
        internal StorageWorkflow.ProfileParameterValuesSection originalSection;
        internal StorageWorkflow.ProfileParameterValuesSection updatedSection;

        internal WorkflowRangeCapacitySetting(RangeCapacitySetting rangeCapacitySetting) : base(rangeCapacitySetting)
        {
        }

        internal WorkflowRangeCapacitySetting(StorageWorkflow.ProfileParameterValuesSection section)
        {
            ParseSection(section);
            InitTracking();
        }
        internal override Storage.DOM.DomSectionBase OriginalSection => originalSection;

        internal StorageWorkflow.ProfileParameterValuesSection GetSectionWithChanges()
        {
            if (updatedSection == null)
            {
                updatedSection = IsNew ? new StorageWorkflow.ProfileParameterValuesSection() : originalSection.Clone();
            }

            updatedSection.ProfileParameterId = Id;
            updatedSection.DoubleMinValue = MinValue.HasValue ? (double)MinValue : null;
            updatedSection.DoubleMaxValue = MaxValue.HasValue ? (double)MaxValue : null;

            return updatedSection;
        }

        private void ParseSection(StorageWorkflow.ProfileParameterValuesSection section)
        {
            originalSection = section ?? throw new ArgumentNullException(nameof(section));

            Id = section.ProfileParameterId;
            MinValue = section.DoubleMinValue.HasValue ? (decimal)section.DoubleMinValue.Value : null;
            MaxValue = section.DoubleMaxValue.HasValue ? (decimal)section.DoubleMaxValue.Value : null;
        }
    }
}
