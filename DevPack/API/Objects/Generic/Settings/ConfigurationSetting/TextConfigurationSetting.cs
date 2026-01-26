namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    /// <summary>
    /// Represents a configuration setting that uses a text value.
    /// </summary>
    public class TextConfigurationSetting : ConfigurationSetting
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TextConfigurationSetting"/> class using the specified text configuration.
        /// </summary>
        /// <param name="configuration">The text configuration to be used for this setting. Cannot be null.</param>
        public TextConfigurationSetting(TextConfiguration configuration)
            : base(configuration)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextConfigurationSetting"/> class with the specified configuration ID.
        /// </summary>
        /// <param name="configurationId">The unique identifier for the configuration. Must not be an empty GUID.</param>
        public TextConfigurationSetting(Guid configurationId)
            : base(configurationId)
        {
        }

        internal TextConfigurationSetting()
            : base()
        {
        }

        internal TextConfigurationSetting(TextConfigurationSetting textConfigurationSetting)
            : base(textConfigurationSetting)
        {
            Value = textConfigurationSetting.Value;
        }

        /// <summary>
        /// Gets or sets the configuration value.
        /// </summary>
        public string Value { get; set; }

        /// <inheritdoc/>
        public override bool HasValue => !string.IsNullOrEmpty(Value);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 23) + Id.GetHashCode();
                hash = (hash * 23) + (Value != null ? Value.GetHashCode() : 0);
                hash = (hash * 23) + (OriginalSection != null ? OriginalSection.ID.Id.GetHashCode() : 0);

                return hash;
            }
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is not TextConfigurationSetting other)
            {
                return false;
            }

            return Id == other.Id && Value == other.Value;
        }
    }
}
