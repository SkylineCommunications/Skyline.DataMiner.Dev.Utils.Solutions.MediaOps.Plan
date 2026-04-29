namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;

	internal class StorageRangeCapacitySetting : RangeCapacitySetting
	{
		internal StorageRangeCapacitySetting(RangeCapacitySetting rangeCapacitySetting) : base(rangeCapacitySetting)
		{
		}

		internal StorageRangeCapacitySetting(ProfileParameterValue profileParameterValue)
		{
			ParseProfileParameterValue(profileParameterValue);
			InitTracking();
		}

		internal ProfileParameterValue GetProfileParameterValueWithChanges()
		{
			return new ProfileParameterValue
			{
				ProfileParameterId = Id,
				DoubleMinValue = MinValue.HasValue ? (double)MinValue : null,
				DoubleMaxValue = MaxValue.HasValue ? (double)MaxValue : null,
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
			MinValue = profileParameterValue.DoubleMinValue.HasValue ? (decimal)profileParameterValue.DoubleMinValue : null;
			MaxValue = profileParameterValue.DoubleMaxValue.HasValue ? (decimal)profileParameterValue.DoubleMaxValue : null;
			Reference = profileParameterValue.Reference.ToDataReference();
		}
	}
}
