namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core;

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
            InitTracking();
        }

        /// <summary>
        /// Gets or sets the default value of this <see cref="TextConfiguration"/>.
        /// </summary>
        public string DefaultValue
        {
            get => defaultValue;
            set
            {
                defaultValue = value;
            }
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = base.GetHashCode();
                hash = (hash * 23) + (DefaultValue != null ? DefaultValue.GetHashCode() : 0);

                return hash;
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
