namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	internal class PropertySettingValidator : ApiObjectValidator
	{
		private readonly Guid apiObjectId;

		private readonly Property property;

		private readonly PropertySetting propertySetting;

		private readonly bool valueExpected;

		private PropertySettingValidator(Guid apiObjectId, Property property, PropertySetting propertySetting, bool valueExpected)
		{
			if (apiObjectId == Guid.Empty)
			{
				throw new ArgumentException("API object ID cannot be an empty GUID.", nameof(apiObjectId));
			}

			this.apiObjectId = apiObjectId;
			this.property = property;
			this.propertySetting = propertySetting;
			this.valueExpected = valueExpected;

			Validate();
		}

		public static PropertySettingValidator Validate(Guid apiObjectId, Property property, PropertySetting propertySetting, bool valueExpected)
		{
			return new PropertySettingValidator(apiObjectId, property, propertySetting, valueExpected);
		}

		private void Validate()
		{
			if (propertySetting is StringPropertySetting stringPropertySetting)
			{
				ValidateStringPropertySetting(stringPropertySetting);
			}
			else if (propertySetting is BooleanPropertySetting)
			{
				ValidateBooleanPropertySetting();
			}
			else if (propertySetting is DiscretePropertySetting discretePropertySetting)
			{
				ValidateDiscretePropertySetting(discretePropertySetting);
			}
			else
			{
				throw new InvalidOperationException($"Unsupported property setting type: {propertySetting.GetType().FullName}");
			}
		}

		private void ValidateStringPropertySetting(StringPropertySetting setting)
		{
			if (property is not StringProperty stringProperty)
			{
				ReportError(apiObjectId, ComposePropertySettingError(propertySetting.Id, $"A string property setting cannot be used with a property of type '{property.GetType().Name}'."));
				return;
			}

			ValidateValue(setting.Value, stringProperty);
		}

		private void ValidateBooleanPropertySetting()
		{
			if (property is not BooleanProperty)
			{
				ReportError(apiObjectId, ComposePropertySettingError(propertySetting.Id, $"A boolean property setting cannot be used with a property of type '{property.GetType().Name}'."));
			}
		}

		private void ValidateDiscretePropertySetting(DiscretePropertySetting setting)
		{
			if (property is not DiscreteProperty discreteProperty)
			{
				ReportError(apiObjectId, ComposePropertySettingError(propertySetting.Id, $"A discrete property setting cannot be used with a property of type '{property.GetType().Name}'."));
				return;
			}

			ValidateValue(setting.Value, discreteProperty);
		}

		private MediaOpsErrorData ComposePropertySettingError(Guid propertyId, string errorMessage)
		{
			return new PropertySettingCollectionInvalidPropertySettingsError
			{
				ErrorMessage = errorMessage,
				PropertyId = propertyId,
				Id = apiObjectId,
			};
		}

		private void ValidateValue(string value, StringProperty stringProperty)
		{
			if (value == null)
			{
				if (valueExpected)
				{
					ReportError(apiObjectId, ComposePropertySettingError(propertySetting.Id, "Value cannot be null."));
				}

				return;
			}

			if (!InputValidator.HasValidTextLength(value, stringProperty.SizeLimit))
			{
				ReportError(apiObjectId, ComposePropertySettingError(propertySetting.Id, $"Value exceeds the maximum length of {stringProperty.SizeLimit} characters."));
			}
		}

		private void ValidateValue(string value, DiscreteProperty discreteProperty)
		{
			if (value == null)
			{
				if (valueExpected)
				{
					ReportError(apiObjectId, ComposePropertySettingError(propertySetting.Id, "Value cannot be null."));
				}

				return;
			}

			if (!discreteProperty.Discretes.Contains(value))
			{
				ReportError(apiObjectId, ComposePropertySettingError(propertySetting.Id, $"Value '{value}' is not a valid discrete value for this property."));
			}
		}
	}
}
