namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;

	internal class StorageDiscreteTextConfigurationSetting : DiscreteTextConfigurationSetting
	{
		internal StorageDiscreteTextConfigurationSetting(DiscreteTextConfigurationSetting discreteTextConfigurationSetting) : base(discreteTextConfigurationSetting)
		{
		}

		internal StorageDiscreteTextConfigurationSetting(DiscreteTextConfiguration configuration, ProfileParameterValue profileParameterValue)
		{
			ParseProfileParameterValue(configuration, profileParameterValue);
			InitTracking();
		}

		internal ProfileParameterValue GetProfileParameterValueWithChanges()
		{
			return new ProfileParameterValue
			{
				ProfileParameterId = Id,
				StringValue = Value?.Value,
			};
		}

		private void ParseProfileParameterValue(DiscreteTextConfiguration configuration, ProfileParameterValue profileParameterValue)
		{
			if (profileParameterValue == null)
			{
				throw new ArgumentNullException(nameof(profileParameterValue));
			}

			Id = profileParameterValue.ProfileParameterId;

			var discreteValue = configuration.Discretes.FirstOrDefault(dv => dv.Value == profileParameterValue.StringValue);
			if (discreteValue != null)
			{
				Value = discreteValue;
			}
		}
	}
}
