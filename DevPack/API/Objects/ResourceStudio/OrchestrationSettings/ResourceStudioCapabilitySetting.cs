namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

    internal class ResourceStudioCapabilitySetting : CapabilitySettings
    {
        internal StorageResourceStudio.ProfileParameterValuesSection originalSection;
        internal StorageResourceStudio.ProfileParameterValuesSection updatedSection;

        internal ResourceStudioCapabilitySetting(CapabilitySettings capabilitySetting) : base(capabilitySetting)
        {
        }

        internal ResourceStudioCapabilitySetting(StorageResourceStudio.ProfileParameterValuesSection section)
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
            SetDiscreteValues(updatedSection, discretes);

            return updatedSection;
        }

        private static IEnumerable<string> GetDiscreteValues(StorageResourceStudio.ProfileParameterValuesSection section)
        {
            if (string.IsNullOrWhiteSpace(section.StringValue))
            {
                return Array.Empty<string>();
            }

            return section.StringValue.Split([";"], StringSplitOptions.RemoveEmptyEntries);
        }

        private static void SetDiscreteValues(StorageResourceStudio.ProfileParameterValuesSection section, IEnumerable<string> discretes)
        {
            if (discretes == null || !discretes.Any())
            {
                section.StringValue = string.Empty;
                return;
            }

            section.StringValue = string.Join(";", discretes);
        }

        private void ParseSection(StorageResourceStudio.ProfileParameterValuesSection section)
        {
            originalSection = section ?? throw new ArgumentNullException(nameof(section));

            Id = section.ProfileParameterId;
            discretes.Clear();
            foreach (var discreteValue in GetDiscreteValues(section))
            {
                discretes.Add(discreteValue);
            }
        }
    }
}
