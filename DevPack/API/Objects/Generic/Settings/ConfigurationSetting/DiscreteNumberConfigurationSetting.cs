namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    /// <summary>
    /// Represents a configuration setting that uses a discrete numeric value.
    /// </summary>
    public class DiscreteNumberConfigurationSetting : ConfigurationSetting
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DiscreteNumberConfigurationSetting"/> class using the specified discrete number configuration.
        /// </summary>
        /// <param name="configuration">The discrete number configuration to be used for this setting. Cannot be null.</param>
        public DiscreteNumberConfigurationSetting(DiscreteNumberConfiguration configuration)
            : base(configuration)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscreteNumberConfigurationSetting"/> class with the specified configuration ID.
        /// </summary>
        /// <param name="configurationId">The unique identifier for the configuration. Must not be an empty GUID.</param>
        public DiscreteNumberConfigurationSetting(Guid configurationId)
            : base(configurationId)
        {
        }

        internal DiscreteNumberConfigurationSetting()
            : base()
        {
        }

        internal DiscreteNumberConfigurationSetting(DiscreteNumberConfigurationSetting discreteNumberConfigurationSetting)
            : base(discreteNumberConfigurationSetting)
        {
            Value = discreteNumberConfigurationSetting.Value;
            InitTracking();
        }

        /// <summary>
        /// Gets or sets the configuration value.
        /// </summary>
        public NumberDiscreet Value { get; set; }

        /// <inheritdoc/>
        public override bool HasValue => Value != null && !string.IsNullOrWhiteSpace(Value.DisplayName);

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
            if (obj is not DiscreteNumberConfigurationSetting other)
            {
                return false;
            }
            return Id == other.Id && Value == other.Value;
        }
    }
}
