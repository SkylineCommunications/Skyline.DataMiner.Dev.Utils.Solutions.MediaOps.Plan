namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    /// <summary>
    /// Represents an abstract base class for settings associated with a specific configuration.
    /// </summary>
    public abstract class ConfigurationSetting : Setting
    {
        private protected ConfigurationSetting(Configuration configuration)
            : this(configuration?.Id ?? throw new ArgumentNullException(nameof(configuration)))
        {
        }

        private protected ConfigurationSetting(Guid configurationId)
        {
            if (configurationId == Guid.Empty)
            {
                throw new ArgumentException(nameof(configurationId));
            }

            Id = configurationId;

            IsNew = true;
        }

        private protected ConfigurationSetting()
        {
        }

        private protected ConfigurationSetting(ConfigurationSetting configurationSetting)
            : base(configurationSetting)
        {
            Id = configurationSetting.Id;
        }

        /// <summary>
        /// Gets the unique identifier of the configuration.
        /// </summary>
        public Guid Id { get; internal set; }
    }
}
