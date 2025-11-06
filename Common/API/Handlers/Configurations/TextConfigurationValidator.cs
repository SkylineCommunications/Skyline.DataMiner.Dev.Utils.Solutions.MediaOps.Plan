namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using Skyline.DataMiner.MediaOps.Plan.Exceptions;

    internal class TextConfigurationValidator : ApiObjectValidator<Guid>
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
                ReportError(textConfiguration.Id, new ConfigurationConfigurationError
                {
                    ErrorReason = ConfigurationConfigurationError.Reason.InvalidDefaultValue,
                    ErrorMessage = "A default value for a text configuration cannot be null",
                });
            }
            else
            {
                // valid default string value
            }
        }
    }
}
