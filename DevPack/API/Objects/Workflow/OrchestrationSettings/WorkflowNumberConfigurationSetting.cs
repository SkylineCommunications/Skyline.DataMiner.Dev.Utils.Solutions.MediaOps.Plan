namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	internal class WorkflowNumberConfigurationSetting : NumberConfigurationSetting
	{
		private StorageWorkflow.ProfileParameterValuesSection originalSection;
		private StorageWorkflow.ProfileParameterValuesSection updatedSection;

		internal WorkflowNumberConfigurationSetting(NumberConfigurationSetting numberConfigurationSetting) : base(numberConfigurationSetting)
		{
		}

		internal WorkflowNumberConfigurationSetting(StorageWorkflow.ProfileParameterValuesSection section)
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
			updatedSection.DoubleMaxValue = Value.HasValue ? (double)Value : null;
			updatedSection.Reference = Reference?.ToStorage();

			return updatedSection;
		}

		private void ParseSection(StorageWorkflow.ProfileParameterValuesSection section)
		{
			originalSection = section ?? throw new ArgumentNullException(nameof(section));

			Id = section.ProfileParameterId;
			Value = section.DoubleMaxValue.HasValue ? (decimal)section.DoubleMaxValue.Value : null;
			Reference = section.Reference?.ToDataReference();
		}
	}
}
