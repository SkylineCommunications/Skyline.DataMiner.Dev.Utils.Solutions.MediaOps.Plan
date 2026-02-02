namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using StorageWorkflow = Storage.DOM.SlcWorkflow;

    internal class WorkflowCapabilitySetting : CapabilitySettings
    {
        internal StorageWorkflow.ProfileParameterValuesSection originalSection;
        internal StorageWorkflow.ProfileParameterValuesSection updatedSection;

        internal WorkflowCapabilitySetting(CapabilitySettings capabilitySetting) : base(capabilitySetting)
        {
        }

        internal WorkflowCapabilitySetting(StorageWorkflow.ProfileParameterValuesSection section)
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
            SetDiscreteValues(updatedSection, discretes);

            return updatedSection;
        }

        private static IEnumerable<string> GetDiscreteValues(StorageWorkflow.ProfileParameterValuesSection section)
        {
            if (string.IsNullOrWhiteSpace(section.StringValue))
            {
                return Array.Empty<string>();
            }

            return section.StringValue.Split([";"], StringSplitOptions.RemoveEmptyEntries);
        }

        private static void SetDiscreteValues(StorageWorkflow.ProfileParameterValuesSection section, IEnumerable<string> discretes)
        {
            if (discretes == null || !discretes.Any())
            {
                section.StringValue = string.Empty;
                return;
            }

            section.StringValue = string.Join(";", discretes);
        }

        private void ParseSection(StorageWorkflow.ProfileParameterValuesSection section)
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
