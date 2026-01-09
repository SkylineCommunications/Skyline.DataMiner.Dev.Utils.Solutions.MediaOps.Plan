namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Linq;

    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

    internal class ResourceStudioDiscreteNumberConfigurationSetting : DiscreteNumberConfigurationSetting
    {
        internal StorageResourceStudio.ProfileParameterValuesSection originalSection;
        internal StorageResourceStudio.ProfileParameterValuesSection updatedSection;

        internal ResourceStudioDiscreteNumberConfigurationSetting(DiscreteNumberConfigurationSetting discreteNumberConfigurationSetting) : base(discreteNumberConfigurationSetting)
        {
        }

        internal ResourceStudioDiscreteNumberConfigurationSetting(DiscreteNumberConfiguration configuration, StorageResourceStudio.ProfileParameterValuesSection section)
        {
            ParseSection(configuration, section);
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
            updatedSection.DoubleMaxValue = (Value != null) ? (double)Value.Value : null;

            return updatedSection;
        }

        private void ParseSection(DiscreteNumberConfiguration configuration, StorageResourceStudio.ProfileParameterValuesSection section)
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
