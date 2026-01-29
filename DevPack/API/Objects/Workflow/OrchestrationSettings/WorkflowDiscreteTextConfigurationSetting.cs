namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Linq;

    using StorageWorkflow = Storage.DOM.SlcWorkflow;

    internal class WorkflowDiscreteTextConfigurationSetting : DiscreteTextConfigurationSetting
    {
        internal StorageWorkflow.ProfileParameterValuesSection originalSection;
        internal StorageWorkflow.ProfileParameterValuesSection updatedSection;

        internal WorkflowDiscreteTextConfigurationSetting(DiscreteTextConfigurationSetting discreteTextConfigurationSetting) : base(discreteTextConfigurationSetting)
        {
        }

        internal WorkflowDiscreteTextConfigurationSetting(DiscreteTextConfiguration configuration, StorageWorkflow.ProfileParameterValuesSection section)
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
            updatedSection.StringValue = Value?.Value;

            return updatedSection;
        }

        private void ParseSection(DiscreteTextConfiguration configuration, StorageWorkflow.ProfileParameterValuesSection section)
        {
            originalSection = section ?? throw new ArgumentNullException(nameof(section));

            Id = section.ProfileParameterId;

            if (!string.IsNullOrEmpty(section.StringValue))
            {
                var discreteValue = configuration.Discretes.FirstOrDefault(dv => dv.Value == section.StringValue);
                if (discreteValue != null)
                {
                    Value = discreteValue;
                }
            }
        }
    }
}
