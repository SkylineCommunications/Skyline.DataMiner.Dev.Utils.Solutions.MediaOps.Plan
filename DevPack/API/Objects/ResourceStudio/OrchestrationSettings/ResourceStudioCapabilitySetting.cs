namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Linq;

	using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

	internal class ResourceStudioCapabilitySetting : CapabilitySetting
	{
		private StorageResourceStudio.ProfileParameterValuesSection originalSection;
		private StorageResourceStudio.ProfileParameterValuesSection updatedSection;

		internal ResourceStudioCapabilitySetting(CapabilitySetting capabilitySetting) : base(capabilitySetting)
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
			updatedSection.StringValue = Value;
			updatedSection.Reference = Reference?.ToStorage();

			return updatedSection;
		}

		private void ParseSection(StorageResourceStudio.ProfileParameterValuesSection section)
		{
			originalSection = section ?? throw new ArgumentNullException(nameof(section));

			Id = section.ProfileParameterId;
			Value = section.StringValue;
			if (!String.IsNullOrEmpty(Value) && Value.Contains(';'))
			{
				Value = Value.Split([";"], StringSplitOptions.RemoveEmptyEntries).First();
			}

			Reference = DataReference.FromStorage(section.Reference);
		}
	}
}
