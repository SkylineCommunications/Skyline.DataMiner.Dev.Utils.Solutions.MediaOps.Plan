namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using StorageProperties = Storage.DOM.SlcProperties;

	internal class InnerBooleanPropertyValue : BooleanPropertyValue
	{
		private StorageProperties.PropertyValueSection originalSection;
		private StorageProperties.PropertyValueSection updatedSection;

		internal InnerBooleanPropertyValue(BooleanPropertyValue booleanPropertyValue)
			: base(booleanPropertyValue)
		{
		}

		internal InnerBooleanPropertyValue(StorageProperties.PropertyValueSection section)
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

			updatedSection.PropertyID = Id;
			updatedSection.Value = Convert.ToString(Value);

			return updatedSection;
		}

		private void ParseSection(StorageProperties.PropertyValueSection section)
		{
			originalSection = section ?? throw new ArgumentNullException(nameof(section));

			Id = section.PropertyID.Value;
			Value = Convert.ToBoolean(section.Value);
		}
	}
}
