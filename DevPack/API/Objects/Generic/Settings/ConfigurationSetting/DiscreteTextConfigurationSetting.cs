namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    /// <summary>
    /// Represents a configuration setting that uses a discrete text value.
    /// </summary>
    public class DiscreteTextConfigurationSetting : ConfigurationSetting
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DiscreteTextConfigurationSetting"/> class using the specified discrete text configuration.
        /// </summary>
        /// <param name="discreteTextConfiguration">The discrete text configuration to be used for this setting. Cannot be null.</param>
        public DiscreteTextConfigurationSetting(DiscreteTextConfiguration discreteTextConfiguration)
            : base(discreteTextConfiguration)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscreteTextConfigurationSetting"/> class with the specified configuration ID.
        /// </summary>
        /// <param name="configurationId">The unique identifier for the configuration. Must not be an empty GUID.</param>
        public DiscreteTextConfigurationSetting(Guid configurationId)
            : base(configurationId)
        {
        }

        internal DiscreteTextConfigurationSetting()
            : base()
        {
        }

        internal DiscreteTextConfigurationSetting(DiscreteTextConfigurationSetting discreteTextConfigurationSetting)
            : base(discreteTextConfigurationSetting)
        {
            Value = discreteTextConfigurationSetting.Value;
        }

        /// <summary>
        /// Gets or sets the configuration value.
        /// </summary>
        public TextDiscreet Value { get; set; }

        /// <inheritdoc/>
        public override bool HasValue => Value != null && !string.IsNullOrWhiteSpace(Value.Value);

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
            if (obj is not DiscreteTextConfigurationSetting other)
            {
                return false;
            }

            return Id == other.Id && Value == other.Value;
        }
    }
}
