namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	internal class DiscreteNumberConfigurationValidator : ParameterApiObjectValidator
	{
		private readonly DiscreteNumberConfiguration discreteNumberConfiguration;

		private DiscreteNumberConfigurationValidator(DiscreteNumberConfiguration apiConfiguration)
		{
			this.discreteNumberConfiguration = apiConfiguration ?? throw new ArgumentNullException(nameof(apiConfiguration));
		}

		public static DiscreteNumberConfigurationValidator Validate(DiscreteNumberConfiguration apiConfiguration)
		{
			var validator = new DiscreteNumberConfigurationValidator(apiConfiguration);
			validator.ValidateDiscreteNumber();
			return validator;
		}

		private void ValidateDiscreteNumber()
		{
			// Any discreet options available
			if (!discreteNumberConfiguration.Discretes.Any())
			{
				ReportError(discreteNumberConfiguration.Id, new ConfigurationNoDiscretesError
				{
					ErrorMessage = "A discreet configuration should have at least one discreet option defined",
					Id = discreteNumberConfiguration.Id,
				});
				return;
			}

			// Validate default discrete option
			if (discreteNumberConfiguration.DefaultValue != null && !discreteNumberConfiguration.Discretes.Any(x => discreteNumberConfiguration.DefaultValue == x))
			{
				ReportError(discreteNumberConfiguration.Id, new ConfigurationInvalidDefaultDiscreetError
				{
					ErrorMessage = "Default discreet should any of the discreet options",
					Id = discreteNumberConfiguration.Id,
				});
			}

			foreach (var discreet in discreteNumberConfiguration.Discretes)
			{
				// Validate Display Value
				if (!HasValidDisplayValue(discreet.DisplayName, out string invalidDisplayNameReason))
				{
					ReportError(discreteNumberConfiguration.Id, new ConfigurationInvalidDiscretesError
					{
						ErrorMessage = invalidDisplayNameReason,
						Id = discreteNumberConfiguration.Id,
					});
				}
			}

			// Validate duplicate raw values
			var duplicateDiscreteValues = discreteNumberConfiguration.Discretes
				.GroupBy(x => x.Value)
				.Where(g => g.Count() > 1)
				.SelectMany(g => g)
				.Select(x => x.Value.ToString())
				.ToList();

			if (duplicateDiscreteValues.Count != 0)
			{
				ReportError(discreteNumberConfiguration.Id, new ConfigurationDuplicateDiscretesError
				{
					ErrorMessage = $"The configuration defines the following duplicate discrete values: {String.Join(", ", duplicateDiscreteValues)}.",
					Id = discreteNumberConfiguration.Id,
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
	}
}
