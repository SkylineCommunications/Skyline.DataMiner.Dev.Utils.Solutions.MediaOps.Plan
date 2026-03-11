namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

	internal class ResourceNumberCapacitySetting : NumberCapacitySetting
	{
		private StorageResourceStudio.ResourceCapacitiesSection originalSection;
		private StorageResourceStudio.ResourceCapacitiesSection updatedSection;

		internal ResourceNumberCapacitySetting(NumberCapacitySetting numberCapacitySetting) : base(numberCapacitySetting)
		{
		}

		internal ResourceNumberCapacitySetting(StorageResourceStudio.ResourceCapacitiesSection section)
		{
			ParseSection(section);
			InitTracking();
		}

		internal override Storage.DOM.DomSectionBase OriginalSection => originalSection;

		internal StorageResourceStudio.ResourceCapacitiesSection GetSectionWithChanges()
		{
			if (updatedSection == null)
			{
				updatedSection = IsNew ? new StorageResourceStudio.ResourceCapacitiesSection() : originalSection.Clone();
			}

			updatedSection.ProfileParameterId = Id;
			updatedSection.DoubleMaxValue = Value.HasValue ? (double)Value : null;

			return updatedSection;
		}

		private void ParseSection(StorageResourceStudio.ResourceCapacitiesSection section)
		{
			originalSection = section ?? throw new ArgumentNullException(nameof(section));

			Id = section.ProfileParameterId;
			Value = section.DoubleMaxValue.HasValue ? (decimal)section.DoubleMaxValue.Value : null;
		}
	}
}