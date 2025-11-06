namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using Skyline.DataMiner.MediaOps.Plan.Storage.Core;

    /// <summary>
    /// Represents a configuration for text-based settings, providing functionality to manage and parse text-related configurations.
    /// </summary>
    public class TextConfiguration : Configuration
    {
        private string defaultValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextConfiguration"/> class.
        /// </summary>
        public TextConfiguration() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextConfiguration"/> class with the specified unique
        /// identifier.
        /// </summary>
        /// <param name="id">The unique identifier for the text configuration.</param>
        public TextConfiguration(Guid id) : base(id)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextConfiguration"/> class using the specified profile.
        /// </summary>
        /// <param name="profile">The profile containing parameters used to configure the text settings.</param>
        internal TextConfiguration(Net.Profiles.Parameter profile) : base(profile)
        {
        }

        public string DefaultValue
        {
            get => defaultValue;
            set
            {
                HasChanges = true;
                defaultValue = value;
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        protected internal override void InternalParseParameter(Net.Profiles.Parameter parameter)
        {
            defaultValue = parameter.HasDefaultStringValue() ? parameter.DefaultValue.StringValue : null;
        }
    }
}
