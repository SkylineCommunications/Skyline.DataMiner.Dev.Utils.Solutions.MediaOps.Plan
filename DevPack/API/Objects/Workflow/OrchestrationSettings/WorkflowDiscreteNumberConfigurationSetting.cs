namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Linq;

    using StorageWorkflow = Storage.DOM.SlcWorkflow;

    internal class WorkflowDiscreteNumberConfigurationSetting : DiscreteNumberConfigurationSetting
    {
        internal StorageWorkflow.ProfileParameterValuesSection originalSection;
        internal StorageWorkflow.ProfileParameterValuesSection updatedSection;

        internal WorkflowDiscreteNumberConfigurationSetting(DiscreteNumberConfigurationSetting discreteNumberConfigurationSetting) : base(discreteNumberConfigurationSetting)
        {
        }

        internal WorkflowDiscreteNumberConfigurationSetting(DiscreteNumberConfiguration configuration, StorageWorkflow.ProfileParameterValuesSection section)
        {
            ParseSection(configuration, section);
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
            updatedSection.DoubleMaxValue = (Value != null) ? (double)Value.Value : null;

            return updatedSection;
        }

        private void ParseSection(DiscreteNumberConfiguration configuration, StorageWorkflow.ProfileParameterValuesSection section)
        {
            originalSection = section ?? throw new ArgumentNullException(nameof(section));

            Id = section.ProfileParameterId;

            if (section.DoubleMaxValue != null)
            {
                var discreteValue = configuration.Discretes.FirstOrDefault(dv => dv.Value == (decimal)section.DoubleMaxValue);
                if (discreteValue != null)
                {
                    Value = discreteValue;
                }
            }
        }
    }
}
