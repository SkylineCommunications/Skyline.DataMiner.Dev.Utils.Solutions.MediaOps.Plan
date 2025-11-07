namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using Skyline.DataMiner.MediaOps.Plan.Exceptions;

    internal class NumberConfigurationValidator : ApiObjectValidator<Guid>
    {
        private readonly NumberConfiguration numberConfiguration;

        private NumberConfigurationValidator(NumberConfiguration apiConfiguration)
        {
            this.numberConfiguration = apiConfiguration ?? throw new ArgumentNullException(nameof(apiConfiguration));
        }

        public static NumberConfigurationValidator Validate(NumberConfiguration apiConfiguration)
        {
            var validator = new NumberConfigurationValidator(apiConfiguration);

            validator.ValidateRanges();
            validator.ValidateStepSize();
            validator.ValidateDefaultNumberValue();
            validator.ValidateDecimalsCombinations();

            return validator;
        }

        private void ValidateRanges()
        {
            if (numberConfiguration.RangeMin.HasValue && numberConfiguration.RangeMax.HasValue && numberConfiguration.RangeMax <= numberConfiguration.RangeMin)
            {
                ReportError(numberConfiguration.Id, new ConfigurationConfigurationError
                {
                    ErrorReason = ConfigurationConfigurationError.Reason.InvalidRangeMax,
                    ErrorMessage = "RangeMax must be greater than RangeMin.",
                });
            }
        }

        private void ValidateStepSize()
        {
            if (numberConfiguration.StepSize.HasValue && numberConfiguration.StepSize <= 0)
            {
                ReportError(numberConfiguration.Id, new ConfigurationConfigurationError
                {
                    ErrorReason = ConfigurationConfigurationError.Reason.InvalidStepSize,
                    ErrorMessage = "StepSize must be greater than 0.",
                });
            }
        }

        private void ValidateDefaultNumberValue()
        {
            if (numberConfiguration.RangeMin.HasValue && numberConfiguration.DefaultValue < numberConfiguration.RangeMin)
            {
                ReportError(numberConfiguration.Id, new ConfigurationConfigurationError
                {
                    ErrorReason = ConfigurationConfigurationError.Reason.InvalidDefaultValue,
                    ErrorMessage = "The default value cannot be lower than the minimum allowed value",
                });
            }

            if (numberConfiguration.RangeMax.HasValue && numberConfiguration.DefaultValue > numberConfiguration.RangeMax)
            {
                ReportError(numberConfiguration.Id, new ConfigurationConfigurationError
                {
                    ErrorReason = ConfigurationConfigurationError.Reason.InvalidDefaultValue,
                    ErrorMessage = "The default value cannot be higher than the maximum allowed value",
                });
            }
        }

        private void ValidateDecimalsCombinations()
        {
            if (!numberConfiguration.Decimals.HasValue)
            {
                return;
            }

            if (numberConfiguration.Decimals < 0 || numberConfiguration.Decimals > 15)
            {
                ReportError(numberConfiguration.Id, new ConfigurationConfigurationError
                {
                    ErrorReason = ConfigurationConfigurationError.Reason.InvalidDecimals,
                    ErrorMessage = "Decimals must be between 0 and 15.",
                });

                return;
            }

            if (numberConfiguration.RangeMin.HasValue && Math.Abs(Math.Round((double)numberConfiguration.RangeMin, (int)numberConfiguration.Decimals) - (double)numberConfiguration.RangeMin) > double.Epsilon)
            {
                ReportError(numberConfiguration.Id, new ConfigurationConfigurationError
                {
                    ErrorReason = ConfigurationConfigurationError.Reason.InvalidRangeMin,
                    ErrorMessage = $"RangeMin has more decimal places than allowed by Decimals ({numberConfiguration.Decimals}).",
                });
            }

            if (numberConfiguration.RangeMax.HasValue && Math.Abs(Math.Round((double)numberConfiguration.RangeMax, (int)numberConfiguration.Decimals) - (double)numberConfiguration.RangeMax) > double.Epsilon)
            {
                ReportError(numberConfiguration.Id, new ConfigurationConfigurationError
                {
                    ErrorReason = ConfigurationConfigurationError.Reason.InvalidRangeMax,
                    ErrorMessage = $"RangeMax has more decimal places than allowed by Decimals ({numberConfiguration.Decimals}).",
                });
            }

            if (numberConfiguration.StepSize.HasValue && Math.Abs(Math.Round((double)numberConfiguration.StepSize, (int)numberConfiguration.Decimals) - (double)numberConfiguration.StepSize) > double.Epsilon)
            {
                ReportError(numberConfiguration.Id, new ConfigurationConfigurationError
                {
                    ErrorReason = ConfigurationConfigurationError.Reason.InvalidStepSize,
                    ErrorMessage = $"StepSize has more decimal places than allowed by Decimals ({numberConfiguration.Decimals}).",
                });
            }
        }
    }
}
