namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;

	internal class StorageCapabilitySetting : CapabilitySetting
	{
		internal StorageCapabilitySetting(CapabilitySetting capabilitySetting)
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
				StringValue = Value,
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
			Value = profileParameterValue.StringValue;
			if (!String.IsNullOrEmpty(Value) && Value.Contains(';'))
			{
				Value = Value.Split([";"], StringSplitOptions.RemoveEmptyEntries).First();
			}

			Reference = profileParameterValue.Reference?.ToDataReference();
		}
	}
}
