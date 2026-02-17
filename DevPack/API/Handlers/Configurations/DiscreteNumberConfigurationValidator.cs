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
                ReportError(discreteNumberConfiguration.ID, new ConfigurationNoDiscretesError
                {
                    ErrorMessage = "A discreet configuration should have at least one discreet option defined",
                    Id = discreteNumberConfiguration.ID,
                });
                return;
            }

            // Validate default discrete option
            if (discreteNumberConfiguration.DefaultValue != null && !discreteNumberConfiguration.Discretes.Any(x => discreteNumberConfiguration.DefaultValue == x))
            {
                ReportError(discreteNumberConfiguration.ID, new ConfigurationInvalidDefaultDiscreetError
                {
                    ErrorMessage = "Default discreet should any of the discreet options",
                    Id = discreteNumberConfiguration.ID,
                });
            }

            foreach (var discreet in discreteNumberConfiguration.Discretes)
            {
                // Validate Display Value
                if (!HasValidDisplayValue(discreet.DisplayName, out string invalidDisplayNameReason))
                {
                    ReportError(discreteNumberConfiguration.ID, new ConfigurationInvalidDiscretesError
                    {
                        ErrorMessage = invalidDisplayNameReason,
                        Id = discreteNumberConfiguration.ID,
                    });
                }

                if (!IsValidDiscreetNumber(discreet.Value, out string invalidDiscreetNumberReason))
                {
                    ReportError(discreteNumberConfiguration.ID, new ConfigurationInvalidDiscretesError
                    {
                        ErrorMessage = invalidDiscreetNumberReason,
                        Id = discreteNumberConfiguration.ID,
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
            else if (discreteNumberConfiguration.Discretes.Count(x => x.DisplayName.Equals(displayValue)) > 1)
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

        private bool IsValidDiscreetNumber(decimal value, out string reason)
        {
            reason = String.Empty;
            if (discreteNumberConfiguration.Discretes.Count(x => x.Value.Equals(value)) > 1)
            {
                reason = $"Multiple discretes have {value} as their value";
                return false;
            }
            else
            {
                // valid number value
                return true;
            }
        }
    }
}
