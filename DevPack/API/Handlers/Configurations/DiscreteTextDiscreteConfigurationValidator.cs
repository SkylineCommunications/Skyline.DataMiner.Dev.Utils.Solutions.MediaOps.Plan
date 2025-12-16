namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Linq;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

    internal class DiscreteTextDiscreteConfigurationValidator : ApiObjectValidator
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
                ReportError(discreteTextConfiguration.Id, new ConfigurationConfigurationError
                {
                    ErrorReason = ConfigurationConfigurationError.Reason.InvalidDiscretes,
                    ErrorMessage = "A discreet configuration should have at least one discreet option defined",
                });
                return;
            }

            // Validate default discrete option
            if (!String.IsNullOrEmpty(discreteTextConfiguration.DefaultValue) && !discreteTextConfiguration.Discretes.Any(x => x.Key.Equals(discreteTextConfiguration.DefaultValue)))
            {
                ReportError(discreteTextConfiguration.Id, new ConfigurationConfigurationError
                {
                    ErrorReason = ConfigurationConfigurationError.Reason.InvalidDefaultDiscreet,
                    ErrorMessage = "Default discreet should be the display value of any of the discreet options",
                });
            }

            foreach (var discreet in discreteTextConfiguration.Discretes)
            {
                // Validate Display Value
                if (!HasValidDisplayValue(discreet.Key, out string invalidDisplayNameReason))
                {
                    ReportError(discreteTextConfiguration.Id, new ConfigurationConfigurationError
                    {
                        ErrorReason = ConfigurationConfigurationError.Reason.InvalidDiscretes,
                        ErrorMessage = invalidDisplayNameReason,
                    });
                }

                // Validate String Value
                if (!IsValidDiscreetText(discreet.Value, out string invalidDiscreetTextReason))
                {
                    ReportError(discreteTextConfiguration.Id, new ConfigurationConfigurationError
                    {
                        ErrorReason = ConfigurationConfigurationError.Reason.InvalidDiscretes,
                        ErrorMessage = invalidDiscreetTextReason,
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
            else if (discreteTextConfiguration.Discretes.Count(x => x.Key.Equals(displayValue)) > 1)
            {
                reason = $"Multiple discretes have {displayValue} as their display value";
                return false;
            }
            else if (!InputValidator.ValidateTextLength(displayValue))
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
            else if (!InputValidator.ValidateTextLength(value))
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
