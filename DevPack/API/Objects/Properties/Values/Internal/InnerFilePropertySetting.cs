namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Linq;

	using StorageProperties = Storage.DOM.SlcProperties;

	internal class InnerFilePropertySetting : FilePropertySetting
	{
		private StorageProperties.PropertyValueSection originalSection;
		private StorageProperties.PropertyValueSection updatedSection;

		internal InnerFilePropertySetting(FilePropertySetting filePropertySetting, Guid destinationCollectionId)
			: base(filePropertySetting, destinationCollectionId)
		{
		}

		internal InnerFilePropertySetting(MediaOpsPlanApi planApi, Guid settingCollectionId, StorageProperties.PropertyValueSection section)
		{
			ParseSection(section);
			SetStorageContext(planApi, settingCollectionId);
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
			updatedSection.Value = string.Join(FileSeparator.ToString(), Files);

			return updatedSection;
		}

		private void ParseSection(StorageProperties.PropertyValueSection section)
		{
			originalSection = section ?? throw new ArgumentNullException(nameof(section));

			Id = section.PropertyID.Value;

			if (string.IsNullOrEmpty(section.Value))
			{
				return;
			}

			foreach (var entry in section.Value.Split(new[] { FileSeparator }, StringSplitOptions.RemoveEmptyEntries))
			{
				AddParsedFile(StripLegacyPropertyIdPrefix(entry));
			}
		}

		// Before the DevPack, file names were stored prefixed with the property id. Reading both keeps existing values
		// usable; they are rewritten without the prefix the next time the collection is saved.
		private string StripLegacyPropertyIdPrefix(string entry)
		{
			var prefix = $"{Id}_";

			return entry.Length > prefix.Length && entry.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
				? entry.Substring(prefix.Length)
				: entry;
		}
	}
}
