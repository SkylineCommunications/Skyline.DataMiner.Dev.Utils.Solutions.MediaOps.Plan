namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;

	internal class StorageDiscreteNumberConfigurationSetting : DiscreteNumberConfigurationSetting
	{
		internal StorageDiscreteNumberConfigurationSetting(DiscreteNumberConfigurationSetting discreteNumberConfigurationSetting) : base(discreteNumberConfigurationSetting)
		{
		}

		internal StorageDiscreteNumberConfigurationSetting(DiscreteNumberConfiguration configuration, ProfileParameterValue profileParameterValue)
		{
			ParseProfileParameterValue(configuration, profileParameterValue);
			InitTracking();
		}

		internal ProfileParameterValue GetProfileParameterValueWithChanges()
		{
			return new ProfileParameterValue
			{
				ProfileParameterId = Id,
				DoubleMaxValue = (Value != null) ? (double)Value.Value : null,
				Reference = Reference?.ToStorage(),
			};
		}

		private void ParseProfileParameterValue(DiscreteNumberConfiguration configuration, ProfileParameterValue profileParameterValue)
		{
			if (profileParameterValue == null)
			{
				throw new ArgumentNullException(nameof(profileParameterValue));
			}

			Id = profileParameterValue.ProfileParameterId;

			if (profileParameterValue.DoubleMaxValue.HasValue)
			{
				var discreteValue = configuration.Discretes.FirstOrDefault(dv => dv.Value == (decimal)profileParameterValue.DoubleMaxValue.Value);
				if (discreteValue != null)
				{
					Value = discreteValue;
				}
			}

			Reference = profileParameterValue.Reference.ToDataReference();
		}
	}
}
