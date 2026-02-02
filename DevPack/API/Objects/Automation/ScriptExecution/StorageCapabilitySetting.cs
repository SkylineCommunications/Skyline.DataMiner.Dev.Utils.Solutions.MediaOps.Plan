namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;

    internal class StorageCapabilitySetting : CapabilitySettings
    {
        internal StorageCapabilitySetting(CapabilitySettings capabilitySetting)
            : base(capabilitySetting)
        {
        }

        internal StorageCapabilitySetting(ProfileParameterValue profileParameterValue)
        {
            ParseProfileParameterValue(profileParameterValue);
            InitTracking();
        }

        internal ProfileParameterValue GetProfileParameterValueWithChanges()
        {
            return new ProfileParameterValue
            {
                ProfileParameterId = Id,
                StringValue = (discretes == null || !discretes.Any()) ? string.Empty : string.Join(";", discretes),
            };
        }

        private static IEnumerable<string> GetDiscreteValues(ProfileParameterValue profileParameterValue)
        {
            if (string.IsNullOrWhiteSpace(profileParameterValue.StringValue))
            {
                return Array.Empty<string>();
            }

            return profileParameterValue.StringValue.Split([";"], StringSplitOptions.RemoveEmptyEntries);
        }

        private void ParseProfileParameterValue(ProfileParameterValue profileParameterValue)
        {
            if (profileParameterValue == null)
            {
                throw new ArgumentNullException(nameof(profileParameterValue));
            }

            Id = profileParameterValue.ProfileParameterId;
            discretes.Clear();
            foreach (var discreteValue in GetDiscreteValues(profileParameterValue))
            {
                discretes.Add(discreteValue);
            }
        }
    }
}
