namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

    internal class TextConfigurationValidator : ApiObjectValidator
    {
        private readonly TextConfiguration textConfiguration;

        private TextConfigurationValidator(TextConfiguration apiConfiguration)
        {
            this.textConfiguration = apiConfiguration ?? throw new ArgumentNullException(nameof(apiConfiguration));
        }

        public static TextConfigurationValidator Validate(TextConfiguration apiConfiguration)
        {
            var validator = new TextConfigurationValidator(apiConfiguration);
            validator.ValidateDefaultTextValue();
            return validator;
        }

        private void ValidateDefaultTextValue()
        {
            if (textConfiguration.DefaultValue == null)
            {
                // valid default string value
            }
            else if (!InputValidator.ValidateTextLength(textConfiguration.DefaultValue))
            {
                ReportError(textConfiguration.Id, new ConfigurationInvalidDefaultValueError
                {
                    ErrorMessage = $"The default value of the text configuration exceeds {InputValidator.DefaultMaxTextLength} characters",
                });
            }
            else
            {
                // valid default string value
            }
        }
    }
}
