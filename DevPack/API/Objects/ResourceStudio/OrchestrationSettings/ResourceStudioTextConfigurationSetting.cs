namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

	internal class ResourceStudioTextConfigurationSetting : TextConfigurationSetting
	{
		private StorageResourceStudio.ProfileParameterValuesSection originalSection;
		private StorageResourceStudio.ProfileParameterValuesSection updatedSection;

		internal ResourceStudioTextConfigurationSetting(TextConfigurationSetting textConfigurationSetting) : base(textConfigurationSetting)
		{
		}

		internal ResourceStudioTextConfigurationSetting(StorageResourceStudio.ProfileParameterValuesSection section)
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
			updatedSection.StringValue = Value;
			updatedSection.Reference = Reference?.ToStorage();

			return updatedSection;
		}

		private void ParseSection(StorageResourceStudio.ProfileParameterValuesSection section)
		{
			originalSection = section ?? throw new ArgumentNullException(nameof(section));

			Id = section.ProfileParameterId;
			Value = section.StringValue;
			Reference = section.Reference?.ToDataReference();
		}
	}
}
