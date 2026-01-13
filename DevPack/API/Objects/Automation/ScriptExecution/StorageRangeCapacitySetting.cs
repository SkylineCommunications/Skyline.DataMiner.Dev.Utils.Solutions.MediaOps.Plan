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
                DoubleMinValue = (double)MinValue,
                DoubleMaxValue = (double)MaxValue,
            };
        }

        private void ParseProfileParameterValue(ProfileParameterValue profileParameterValue)
        {
            if (profileParameterValue == null)
            {
                throw new ArgumentNullException(nameof(profileParameterValue));
            }

            Id = profileParameterValue.ProfileParameterId;
            MinValue = (decimal)profileParameterValue.DoubleMinValue;
            MaxValue = (decimal)profileParameterValue.DoubleMaxValue;
        }
    }
}
