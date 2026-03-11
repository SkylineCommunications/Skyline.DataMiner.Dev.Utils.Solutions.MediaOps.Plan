namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

	internal class ResourceCapabilitySetting : CapabilitySettings
	{
		private StorageResourceStudio.ResourceCapabilitiesSection originalSection;

		private StorageResourceStudio.ResourceCapabilitiesSection updatedSection;

		internal ResourceCapabilitySetting(CapabilitySettings capabilitySetting) : base(capabilitySetting)
		{
		}

		internal ResourceCapabilitySetting(StorageResourceStudio.ResourceCapabilitiesSection section)
		{
			ParseSection(section);
			InitTracking();
		}

		internal override Storage.DOM.DomSectionBase OriginalSection => originalSection;

		internal StorageResourceStudio.ResourceCapabilitiesSection GetSectionWithChanges()
		{
			if (updatedSection == null)
			{
				updatedSection = IsNew ? new StorageResourceStudio.ResourceCapabilitiesSection() : originalSection.Clone();
			}

			updatedSection.ProfileParameterId = Id;
			updatedSection.DiscreteValues = discretes;

			return updatedSection;
		}

		private void ParseSection(StorageResourceStudio.ResourceCapabilitiesSection section)
		{
			originalSection = section ?? throw new ArgumentNullException(nameof(section));

			Id = section.ProfileParameterId;

			discretes.Clear();
			foreach (var discreteValue in section.DiscreteValues)
			{
				discretes.Add(discreteValue);
			}
		}
	}
}
