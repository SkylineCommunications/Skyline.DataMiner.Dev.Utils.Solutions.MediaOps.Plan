namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    /// <summary>
    /// Represents a configuration setting that uses a numeric value.
    /// </summary>
    public class NumberConfigurationSetting : ConfigurationSetting
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NumberConfigurationSetting"/> class using the specified number configuration.
        /// </summary>
        /// <param name="configuration">The number configuration to be used for this setting. Cannot be null.</param>
        public NumberConfigurationSetting(NumberConfiguration configuration)
            : base(configuration)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NumberConfigurationSetting"/> class with the specified configuration ID.
        /// </summary>
        /// <param name="configurationId">The unique identifier for the configuration. Must not be an empty GUID.</param>
        public NumberConfigurationSetting(Guid configurationId)
            : base(configurationId)
        {
        }

        internal NumberConfigurationSetting()
            : base()
        {
        }

        internal NumberConfigurationSetting(NumberConfigurationSetting numberConfigurationSetting)
            : base(numberConfigurationSetting)
        {
            Value = numberConfigurationSetting.Value;
        }

        /// <summary>
        /// Gets or sets the configuration value.
        /// </summary>
        public decimal? Value { get; set; }

        /// <inheritdoc/>
        public override bool HasValue => Value.HasValue;

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 23) + Id.GetHashCode();
                hash = (hash * 23) + Value.GetHashCode();
                hash = (hash * 23) + (OriginalSection != null ? OriginalSection.ID.Id.GetHashCode() : 0);
                return hash;
            }
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is not NumberConfigurationSetting other)
            {
                return false;
            }
            return Id == other.Id && Value == other.Value;
        }
    }
}
