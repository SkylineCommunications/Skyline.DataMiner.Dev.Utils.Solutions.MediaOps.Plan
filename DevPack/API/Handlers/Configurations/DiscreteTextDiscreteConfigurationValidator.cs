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

			// Validate duplicate display values
			var duplicateDisplayValues = discreteTextConfiguration.Discretes
				.GroupBy(x => x.DisplayName)
				.Where(g => g.Count() > 1)
				.SelectMany(g => g)
				.Select(x => x.DisplayName)
				.ToList();

			if (duplicateDisplayValues.Count != 0)
			{
				ReportError(discreteTextConfiguration.Id, new ConfigurationDuplicateDisplayDiscretesError
				{
					ErrorMessage = $"The configuration defines the following duplicate discrete display values: {String.Join(", ", duplicateDisplayValues)}.",
					Id = discreteTextConfiguration.Id,
				});
			}

			// Validate duplicate raw values
			var duplicateDiscreteValues = discreteTextConfiguration.Discretes
				.GroupBy(x => x.Value)
				.Where(g => g.Count() > 1)
				.SelectMany(g => g)
				.Select(x => x.Value)
				.ToList();

			if (duplicateDiscreteValues.Count != 0)
			{
				ReportError(discreteTextConfiguration.Id, new ConfigurationDuplicateTextDiscretesError
				{
					ErrorMessage = $"The configuration defines the following duplicate discrete values: {String.Join(", ", duplicateDiscreteValues)}.",
					Id = discreteTextConfiguration.Id,
					Discretes = duplicateDiscreteValues,
				});
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
