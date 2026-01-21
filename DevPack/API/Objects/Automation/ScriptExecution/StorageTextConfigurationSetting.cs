namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;

    internal class StorageTextConfigurationSetting : TextConfigurationSetting
    {
        internal StorageTextConfigurationSetting(TextConfigurationSetting textConfigurationSetting) : base(textConfigurationSetting)
        {
        }

        internal StorageTextConfigurationSetting(ProfileParameterValue profileParameterValue)
        {
            ParseProfileParameterValue(profileParameterValue);
            InitTracking();
        }

        internal ProfileParameterValue GetProfileParameterValueWithChanges()
        {
            return new ProfileParameterValue
            {
                ProfileParameterId = Id,
                StringValue = Value,
            };
        }

        private void ParseProfileParameterValue(ProfileParameterValue profileParameterValue)
        {
            if (profileParameterValue == null)
            {
                throw new ArgumentNullException(nameof(profileParameterValue));
            }

            Id = profileParameterValue.ProfileParameterId;
            Value = profileParameterValue.StringValue;
        }
    }
}
