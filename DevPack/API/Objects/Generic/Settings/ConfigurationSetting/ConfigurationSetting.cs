namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    /// <summary>
    /// Represents an abstract base class for settings associated with a specific configuration.
    /// </summary>
    public abstract class ConfigurationSetting : TrackableObject
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
        {
            Id = configurationSetting.Id;
            IsNew = true;
        }

        /// <summary>
        /// Gets the unique identifier of the configuration.
        /// </summary>
        public Guid Id { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether this setting is fully defined.
        /// </summary>
        public abstract bool HasValue { get; }

        internal virtual Storage.DOM.DomSectionBase OriginalSection { get; }
    }
}
