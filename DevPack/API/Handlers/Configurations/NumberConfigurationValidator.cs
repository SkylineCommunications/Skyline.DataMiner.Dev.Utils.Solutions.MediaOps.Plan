namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

    internal class NumberConfigurationValidator : ParameterApiObjectValidator
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
                ReportError(numberConfiguration.ID, new ConfigurationInvalidRangeError
                {
                    ErrorMessage = "RangeMax must be greater than RangeMin.",
                    RangeMin = numberConfiguration.RangeMin.Value,
                    RangeMax = numberConfiguration.RangeMax.Value,
                });
            }
        }

        private void ValidateStepSize()
        {
            if (numberConfiguration.StepSize.HasValue && numberConfiguration.StepSize <= 0)
            {
                ReportError(numberConfiguration.ID, new ConfigurationInvalidStepSizeError
                {
                    ErrorMessage = "StepSize must be greater than 0.",
                    StepSize = numberConfiguration.StepSize.Value,
                });
            }
        }

        private void ValidateDefaultNumberValue()
        {
            if (numberConfiguration.RangeMin.HasValue && numberConfiguration.DefaultValue < numberConfiguration.RangeMin)
            {
                ReportError(numberConfiguration.ID, new ConfigurationInvalidDefaultValueError
                {
                    ErrorMessage = "The default value cannot be lower than the minimum allowed value",
                    Id = numberConfiguration.ID,
                });
            }

            if (numberConfiguration.RangeMax.HasValue && numberConfiguration.DefaultValue > numberConfiguration.RangeMax)
            {
                ReportError(numberConfiguration.ID, new ConfigurationInvalidDefaultValueError
                {
                    ErrorMessage = "The default value cannot be higher than the maximum allowed value",
                    Id = numberConfiguration.ID,
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
                ReportError(numberConfiguration.ID, new ConfigurationInvalidDecimalsError
                {
                    ErrorMessage = "Decimals must be between 0 and 15.",
                    Id = numberConfiguration.ID,
                    Decimals = numberConfiguration.Decimals.Value,
                });

                return;
            }

            if (numberConfiguration.RangeMin.HasValue && (Math.Round(numberConfiguration.RangeMin.Value, numberConfiguration.Decimals.Value) - numberConfiguration.RangeMin.Value) != 0)
            {
                ReportError(numberConfiguration.ID, new ConfigurationInvalidRangeMinError
                {
                    ErrorMessage = $"RangeMin has more decimal places than allowed by Decimals ({numberConfiguration.Decimals}).",
                    Id = numberConfiguration.ID,
                    RangeMin = numberConfiguration.RangeMin.Value,
                });
            }

            if (numberConfiguration.RangeMax.HasValue && (Math.Round(numberConfiguration.RangeMax.Value, numberConfiguration.Decimals.Value) - numberConfiguration.RangeMax.Value) != 0)
            {
                ReportError(numberConfiguration.ID, new ConfigurationInvalidRangeMaxError
                {
                    ErrorMessage = $"RangeMax has more decimal places than allowed by Decimals ({numberConfiguration.Decimals}).",
                    Id = numberConfiguration.ID,
                    RangeMax = numberConfiguration.RangeMax.Value,
                });
            }

            if (numberConfiguration.StepSize.HasValue && (Math.Round(numberConfiguration.StepSize.Value, numberConfiguration.Decimals.Value) - numberConfiguration.StepSize.Value) != 0)
            {
                ReportError(numberConfiguration.ID, new ConfigurationInvalidStepSizeError
                {
                    ErrorMessage = $"StepSize has more decimal places than allowed by Decimals ({numberConfiguration.Decimals}).",
                    Id = numberConfiguration.ID,
                    StepSize = numberConfiguration.StepSize.Value,
                });
            }
        }
    }
}
