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
			// Any discrete options available
			if (!discreteTextConfiguration.Discretes.Any())
			{
				ReportError(discreteTextConfiguration.Id, new ConfigurationNoDiscretesError
				{
					ErrorMessage = "A discrete configuration should have at least one discrete option defined",
					Id = discreteTextConfiguration.Id,
				});
				return;
			}

			// Validate default discrete option
			if (discreteTextConfiguration.DefaultValue != null && !discreteTextConfiguration.Discretes.Any(x => discreteTextConfiguration.DefaultValue == x))
			{
				ReportError(discreteTextConfiguration.Id, new ConfigurationInvalidDefaultDiscreteError
				{
					ErrorMessage = "Default discrete should any of the discrete options",
					Id = discreteTextConfiguration.Id,
				});
			}

			foreach (var discrete in discreteTextConfiguration.Discretes)
			{
				// Validate Display Value
				if (!HasValidDisplayValue(discrete.DisplayName, out string invalidDisplayNameReason))
				{
					ReportError(discreteTextConfiguration.Id, new ConfigurationInvalidDiscretesError
					{
						ErrorMessage = invalidDisplayNameReason,
						Id = discreteTextConfiguration.Id,
					});
				}

				// Validate String Value
				if (!IsValidDiscreteText(discrete.Value, out string invalidDiscreteTextReason))
				{
					ReportError(discreteTextConfiguration.Id, new ConfigurationInvalidDiscretesError
					{
						ErrorMessage = invalidDiscreteTextReason,
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
					DisplayValues = duplicateDisplayValues,
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
				reason = "The display value of a discrete cannot be empty";
				return false;
			}
			else if (!InputValidator.HasValidTextLength(displayValue))
			{
				reason = $"The display value of the discrete exceeds {InputValidator.DefaultMaxTextLength} characters";
				return false;
			}
			else
			{
				// valid display value
				return true;
			}
		}

		private bool IsValidDiscreteText(string value, out string reason)
		{
			reason = String.Empty;
			if (String.IsNullOrEmpty(value))
			{
				reason = "The value of a discrete in a text discrete configuration cannot be empty";
				return false;
			}
			else if (!InputValidator.HasValidTextLength(value))
			{
				reason = $"The value of the discrete exceeds {InputValidator.DefaultMaxTextLength} characters";
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
