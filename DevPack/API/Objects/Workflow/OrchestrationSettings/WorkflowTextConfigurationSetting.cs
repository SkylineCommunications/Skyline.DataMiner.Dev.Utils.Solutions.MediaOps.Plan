namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	internal class WorkflowTextConfigurationSetting : TextConfigurationSetting
	{
		private StorageWorkflow.ProfileParameterValuesSection originalSection;
		private StorageWorkflow.ProfileParameterValuesSection updatedSection;

		internal WorkflowTextConfigurationSetting(TextConfigurationSetting textConfigurationSetting) : base(textConfigurationSetting)
		{
		}

		internal WorkflowTextConfigurationSetting(StorageWorkflow.ProfileParameterValuesSection section)
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
			updatedSection.StringValue = Value;

			return updatedSection;
		}

		private void ParseSection(StorageWorkflow.ProfileParameterValuesSection section)
		{
			originalSection = section ?? throw new ArgumentNullException(nameof(section));

			Id = section.ProfileParameterId;
			Value = section.StringValue;
		}
	}
}
