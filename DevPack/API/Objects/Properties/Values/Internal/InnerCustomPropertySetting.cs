namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using StorageProperties = Storage.DOM.SlcProperties;

	internal class InnerCustomPropertySetting : CustomPropertySetting
	{
		private StorageProperties.PropertyValueSection originalSection;
		private StorageProperties.PropertyValueSection updatedSection;

		internal InnerCustomPropertySetting(CustomPropertySetting customPropertySetting)
			: base(customPropertySetting)
		{
		}

		internal InnerCustomPropertySetting(StorageProperties.PropertyValueSection section)
		{
			ParseSection(section);
			InitTracking();
		}

		internal override Storage.DOM.DomSectionBase OriginalSection => originalSection;

		internal StorageProperties.PropertyValueSection GetSectionWithChanges()
		{
			if (updatedSection == null)
			{
				updatedSection = IsNew ? new StorageProperties.PropertyValueSection() : originalSection.Clone();
			}

			updatedSection.PropertyName = Name;
			updatedSection.Value = Value;

			return updatedSection;
		}

		private void ParseSection(StorageProperties.PropertyValueSection section)
		{
			originalSection = section ?? throw new ArgumentNullException(nameof(section));

			Name = section.PropertyName;
			Value = section.Value;
		}
	}
}
