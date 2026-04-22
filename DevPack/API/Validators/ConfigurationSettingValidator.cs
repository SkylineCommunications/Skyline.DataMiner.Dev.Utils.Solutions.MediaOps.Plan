namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	internal abstract class ConfigurationSettingValidator : ApiObjectValidator
	{
		private readonly Guid apiObjectId;

		private readonly Configuration configuration;

		private readonly ConfigurationSetting configurationSetting;

		private readonly bool valueExpected;

		private protected ConfigurationSettingValidator(Guid apiObjectId, Configuration configuration, ConfigurationSetting configurationSetting, bool valueExpected)
		{
			if (apiObjectId == Guid.Empty)
			{
				throw new ArgumentException("API object ID cannot be an empty GUID.", nameof(apiObjectId));
			}

			this.apiObjectId = apiObjectId;
			this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
			this.configurationSetting = configurationSetting ?? throw new ArgumentNullException(nameof(configurationSetting));
			this.valueExpected = valueExpected;

			Validate();
		}

		private protected Guid ApiObjectId => apiObjectId;

		internal abstract MediaOpsErrorData ComposeConfigurationSettingError(Guid configurationId, string errorMessage);

		private void Validate()
		{
			var actionMap = new Dictionary<Type, Action>
			{
				[typeof(TextConfigurationSetting)] = () => ValidateTextConfigurationSetting((TextConfigurationSetting)configurationSetting),
				[typeof(NumberConfigurationSetting)] = () => ValidateNumberConfigurationSetting((NumberConfigurationSetting)configurationSetting),
				[typeof(DiscreteTextConfigurationSetting)] = () => ValidateDiscreteTextConfigurationSetting((DiscreteTextConfigurationSetting)configurationSetting),
				[typeof(DiscreteNumberConfigurationSetting)] = () => ValidateDiscreteNumberConfigurationSetting((DiscreteNumberConfigurationSetting)configurationSetting),
			};

			if (!actionMap.TryGetValue(configurationSetting.GetType(), out var validateAction))
			{
				throw new InvalidOperationException($"Unsupported configuration setting type: {configurationSetting.GetType().FullName}");
			}

			validateAction();
		}

		private void ValidateTextConfigurationSetting(TextConfigurationSetting setting)
		{
			if (configuration is not TextConfiguration)
			{
				ReportError(apiObjectId, ComposeConfigurationSettingError(configurationSetting.Id, $"A text configuration setting cannot be used with a configuration of type '{configuration.GetType().Name}'."));
				return;
			}

			ValidateValue(setting.Value);
		}

		private void ValidateNumberConfigurationSetting(NumberConfigurationSetting setting)
		{
			if (configuration is not NumberConfiguration numberConfiguration)
			{
				ReportError(ApiObjectId, ComposeConfigurationSettingError(configurationSetting.Id, $"A number configuration setting cannot be used with a configuration of type '{configuration.GetType().Name}'."));
				return;
			}

			ValidateValue(setting.Value, numberConfiguration);
		}

		private void ValidateDiscreteTextConfigurationSetting(DiscreteTextConfigurationSetting setting)
		{
			if (configuration is not DiscreteTextConfiguration discreteTextConfiguration)
			{
				ReportError(ApiObjectId, ComposeConfigurationSettingError(configurationSetting.Id, $"A discrete text configuration setting cannot be used with a configuration of type '{configuration.GetType().Name}'."));
				return;
			}

			ValidateValue(setting.Value, discreteTextConfiguration);
		}

		private void ValidateDiscreteNumberConfigurationSetting(DiscreteNumberConfigurationSetting setting)
		{
			if (configuration is not DiscreteNumberConfiguration discreteNumberConfiguration)
			{
				ReportError(ApiObjectId, ComposeConfigurationSettingError(configurationSetting.Id, $"A discrete number configuration setting cannot be used with a configuration of type '{configuration.GetType().Name}'."));
				return;
			}

			ValidateValue(setting.Value, discreteNumberConfiguration);
		}

		private void ValidateValue(string value)
		{
			if (value == null)
			{
				if (valueExpected)
				{
					ReportError(apiObjectId, ComposeConfigurationSettingError(configurationSetting.Id, "Value cannot be null."));
				}

				return;
			}

			if (!InputValidator.HasValidTextLength(value))
			{
				ReportError(apiObjectId, ComposeConfigurationSettingError(configurationSetting.Id, $"Value exceeds the maximum length of {InputValidator.DefaultMaxTextLength} characters."));
			}
		}

		private void ValidateValue(decimal? value, NumberConfiguration numberConfiguration)
		{
			if (!value.HasValue)
			{
				if (valueExpected)
				{
					ReportError(apiObjectId, ComposeConfigurationSettingError(configurationSetting.Id, "Value cannot be null."));
				}

				return;
			}

			if (numberConfiguration.RangeMin.HasValue && value < numberConfiguration.RangeMin.Value)
			{
				ReportError(apiObjectId, ComposeConfigurationSettingError(configurationSetting.Id, $"Value '{value}' must be greater than or equal to '{numberConfiguration.RangeMin}'."));

				return;
			}

			if (numberConfiguration.RangeMax.HasValue && value > numberConfiguration.RangeMax.Value)
			{
				ReportError(apiObjectId, ComposeConfigurationSettingError(configurationSetting.Id, $"Value '{value}' must be lower than or equal to '{numberConfiguration.RangeMax}'."));

				return;
			}

			if (numberConfiguration.StepSize.HasValue && !ValidateValueStepSize(value.Value, numberConfiguration))
			{
				return;
			}

			if (numberConfiguration.Decimals.HasValue && (Math.Round(value.Value, numberConfiguration.Decimals.Value) - value) != 0)
			{
				ReportError(apiObjectId, ComposeConfigurationSettingError(configurationSetting.Id, $"Value '{value}' must contain less than '{numberConfiguration.Decimals}' decimals."));

				return;
			}
		}

		private bool ValidateValueStepSize(decimal value, NumberConfiguration numberConfiguration)
		{
			if (numberConfiguration.StepSize - 0.0m == 0)
			{
				return true;
			}

			var valueToCheck = value;
			if (numberConfiguration.RangeMin.HasValue)
			{
				valueToCheck = value - numberConfiguration.RangeMin.Value;
			}
			else if (numberConfiguration.RangeMax.HasValue)
			{
				valueToCheck = numberConfiguration.RangeMax.Value - value;
			}
			else
			{
				// no range defined
			}

			if ((valueToCheck % numberConfiguration.StepSize.Value) != 0)
			{
				ReportError(apiObjectId, ComposeConfigurationSettingError(configurationSetting.Id, $"Value '{value}' must align with the step size of '{numberConfiguration.StepSize}'."));

				return false;
			}

			return true;
		}

		private void ValidateValue(TextDiscreet value, DiscreteTextConfiguration discreteTextConfiguration)
		{
			if (value == null)
			{
				if (valueExpected)
				{
					ReportError(apiObjectId, ComposeConfigurationSettingError(configurationSetting.Id, "Value cannot be null."));
				}

				return;
			}

			if (!discreteTextConfiguration.Discretes.Contains(value))
			{
				ReportError(apiObjectId, ComposeConfigurationSettingError(configurationSetting.Id, $"Value '{value}' is not a valid discrete value for this configuration."));
			}
		}

		private void ValidateValue(NumberDiscreet value, DiscreteNumberConfiguration discreteNumberConfiguration)
		{
			if (value == null)
			{
				if (valueExpected)
				{
					ReportError(apiObjectId, ComposeConfigurationSettingError(configurationSetting.Id, "Value cannot be null."));
				}

				return;
			}

			if (!discreteNumberConfiguration.Discretes.Contains(value))
			{
				ReportError(apiObjectId, ComposeConfigurationSettingError(configurationSetting.Id, $"Value '{value}' is not a valid discrete value for this configuration."));
			}
		}
	}

	internal class OrchestrationSettingsConfigurationSettingValidator : ConfigurationSettingValidator
	{
		private OrchestrationSettingsConfigurationSettingValidator(Guid orchestrationSettingsId, Configuration configuration, ConfigurationSetting configurationSetting, bool valueExpected)
			: base(orchestrationSettingsId, configuration, configurationSetting, valueExpected)
		{
		}

		public static ConfigurationSettingValidator Validate(Guid orchestrationSettingsId, Configuration configuration, ConfigurationSetting configurationSetting, bool valueExpected)
		{
			return new OrchestrationSettingsConfigurationSettingValidator(orchestrationSettingsId, configuration, configurationSetting, valueExpected);
		}

		internal override MediaOpsErrorData ComposeConfigurationSettingError(Guid configurationId, string errorMessage)
		{
			return new OrchestrationSettingsInvalidConfigurationSettingsError
			{
				ErrorMessage = errorMessage,
				ConfigurationId = configurationId,
				Id = ApiObjectId,
			};
		}
	}
}
