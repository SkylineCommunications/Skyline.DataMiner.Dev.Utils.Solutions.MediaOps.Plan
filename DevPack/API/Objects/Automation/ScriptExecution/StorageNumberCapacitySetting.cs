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
                DoubleMaxValue = (double)Value,
            };
        }

        private void ParseProfileParameterValue(ProfileParameterValue profileParameterValue)
        {
            if (profileParameterValue == null)
            {
                throw new ArgumentNullException(nameof(profileParameterValue));
            }

            Id = profileParameterValue.ProfileParameterId;
            Value = (decimal)profileParameterValue.DoubleMaxValue;
        }
    }
}
