namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;

	internal class StorageNumberCapacitySetting : NumberCapacitySetting
	{
		internal StorageNumberCapacitySetting(NumberCapacitySetting numberCapacitySetting) : base(numberCapacitySetting)
		{
		}

		internal StorageNumberCapacitySetting(ProfileParameterValue profileParameterValue)
		{
			ParseProfileParameterValue(profileParameterValue);
			InitTracking();
		}

		internal ProfileParameterValue GetProfileParameterValueWithChanges()
		{
			return new ProfileParameterValue
			{
				ProfileParameterId = Id,
				DoubleMaxValue = Value.HasValue ? (double)Value : null,
				Reference = Reference?.ToStorage(),
			};
		}

		private void ParseProfileParameterValue(ProfileParameterValue profileParameterValue)
		{
			if (profileParameterValue == null)
			{
				throw new ArgumentNullException(nameof(profileParameterValue));
			}

			Id = profileParameterValue.ProfileParameterId;
			Value = profileParameterValue.DoubleMaxValue.HasValue ? (decimal)profileParameterValue.DoubleMaxValue.Value : null;
			Reference = DataReference.FromStorage(profileParameterValue.Reference);
		}
	}
}
