namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

	internal class ResourceStudioRangeCapacitySetting : RangeCapacitySetting
	{
		private StorageResourceStudio.ProfileParameterValuesSection originalSection;
		private StorageResourceStudio.ProfileParameterValuesSection updatedSection;

		internal ResourceStudioRangeCapacitySetting(RangeCapacitySetting rangeCapacitySetting) : base(rangeCapacitySetting)
		{
		}

		internal ResourceStudioRangeCapacitySetting(StorageResourceStudio.ProfileParameterValuesSection section)
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
			updatedSection.DoubleMinValue = MinValue.HasValue ? (double)MinValue : null;
			updatedSection.DoubleMaxValue = MaxValue.HasValue ? (double)MaxValue : null;
			updatedSection.Reference = Reference?.ToStorage();

			return updatedSection;
		}

		private void ParseSection(StorageResourceStudio.ProfileParameterValuesSection section)
		{
			originalSection = section ?? throw new ArgumentNullException(nameof(section));

			Id = section.ProfileParameterId;
			MinValue = section.DoubleMinValue.HasValue ? (decimal)section.DoubleMinValue.Value : null;
			MaxValue = section.DoubleMaxValue.HasValue ? (decimal)section.DoubleMaxValue.Value : null;
			Reference = DataReference.FromStorage(section.Reference);
		}
	}
}
