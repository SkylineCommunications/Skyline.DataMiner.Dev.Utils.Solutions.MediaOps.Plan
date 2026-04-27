namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	internal class DiscreteTextDiscreteConfigurationValidator : ParameterApiObjectValidator
	{
		private readonly DiscreteTextConfiguration discreteTextConfiguration;

		private DiscreteTextDiscreteConfigurationValidator(DiscreteTextConfiguration apiConfiguration)
		{
			this.discreteTextConfiguration = apiConfiguration ?? throw new ArgumentNullException(nameof(apiConfiguration));
		}

		public static DiscreteTextDiscreteConfigurationValidator Validate(DiscreteTextConfiguration apiConfiguration)
		{
			var validator = new DiscreteTextDiscreteConfigurationValidator(apiConfiguration);
			validator.ValidateDiscreteText();
			return validator;
		}

		private void ValidateDiscreteText()
		{
			// Any discreet options available
			if (!discreteTextConfiguration.Discretes.Any())
			{
				ReportError(discreteTextConfiguration.Id, new ConfigurationNoDiscretesError
				{
					ErrorMessage = "A discreet configuration should have at least one discreet option defined",
					Id = discreteTextConfiguration.Id,
				});
				return;
			}

			// Validate default discrete option
			if (discreteTextConfiguration.DefaultValue != null && !discreteTextConfiguration.Discretes.Any(x => discreteTextConfiguration.DefaultValue == x))
			{
				ReportError(discreteTextConfiguration.Id, new ConfigurationInvalidDefaultDiscreetError
				{
					ErrorMessage = "Default discreet should any of the discreet options",
					Id = discreteTextConfiguration.Id,
				});
			}

			foreach (var discreet in discreteTextConfiguration.Discretes)
			{
				// Validate Display Value
				if (!HasValidDisplayValue(discreet.DisplayName, out string invalidDisplayNameReason))
				{
					ReportError(discreteTextConfiguration.Id, new ConfigurationInvalidDiscretesError
					{
						ErrorMessage = invalidDisplayNameReason,
						Id = discreteTextConfiguration.Id,
					});
				}

				// Validate String Value
				if (!IsValidDiscreetText(discreet.Value, out string invalidDiscreetTextReason))
				{
					ReportError(discreteTextConfiguration.Id, new ConfigurationInvalidDiscretesError
					{
						ErrorMessage = invalidDiscreetTextReason,
						Id = discreteTextConfiguration.Id,
					});
				}
			}
		}

		private bool HasValidDisplayValue(string displayValue, out string reason)
		{
			reason = String.Empty;
			if (String.IsNullOrEmpty(displayValue))
			{
				reason = "The display value of a discreet cannot be empty";
				return false;
			}
			else if (discreteTextConfiguration.Discretes.Count(x => x.DisplayName.Equals(displayValue)) > 1)
			{
				reason = $"Multiple discretes have {displayValue} as their display value";
				return false;
			}
			else if (!InputValidator.HasValidTextLength(displayValue))
			{
				reason = $"The display value of the discreet exceeds {InputValidator.DefaultMaxTextLength} characters";
				return false;
			}
			else
			{
				// valid display value
				return true;
			}
		}

		private bool IsValidDiscreetText(string value, out string reason)
		{
			reason = String.Empty;
			if (String.IsNullOrEmpty(value))
			{
				reason = "The value of a discreet in a text discreet configuration cannot be empty";
				return false;
			}
			else if (discreteTextConfiguration.Discretes.Count(x => x.Value.Equals(value)) > 1)
			{
				reason = $"Multiple discretes have {value} as their value";
				return false;
			}
			else if (!InputValidator.HasValidTextLength(value))
			{
				reason = $"The value of the discreet exceeds {InputValidator.DefaultMaxTextLength} characters";
				return false;
			}
			else
			{
				// valid string value
				return true;
			}
		}
	}
}
