namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

	internal class ResourceStudioNumberConfigurationSetting : NumberConfigurationSetting
	{
		private StorageResourceStudio.ProfileParameterValuesSection originalSection;
		private StorageResourceStudio.ProfileParameterValuesSection updatedSection;

		internal ResourceStudioNumberConfigurationSetting(NumberConfigurationSetting numberConfigurationSetting) : base(numberConfigurationSetting)
		{
		}

		internal ResourceStudioNumberConfigurationSetting(StorageResourceStudio.ProfileParameterValuesSection section)
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
			updatedSection.DoubleMaxValue = Value.HasValue ? (double)Value : null;
			updatedSection.Reference = Reference?.ToStorage();

			return updatedSection;
		}

		private void ParseSection(StorageResourceStudio.ProfileParameterValuesSection section)
		{
			originalSection = section ?? throw new ArgumentNullException(nameof(section));

			Id = section.ProfileParameterId;
			Value = section.DoubleMaxValue.HasValue ? (decimal)section.DoubleMaxValue.Value : null;
			Reference = DataReference.FromStorage(section.Reference);
		}
	}
}
