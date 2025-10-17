namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using Skyline.DataMiner.MediaOps.Plan.Storage.Core;

    /// <summary>
    /// Represents a configuration for discrete text settings, providing functionality to manage and parse text-based
    /// configurations.
    /// </summary>
    public class DiscreteTextConfiguration : Configuration
    {
        private string defaultValue;
        private Dictionary<string, string> discretes = new Dictionary<string, string>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscreteTextConfiguration"/> class.
        /// </summary>
        public DiscreteTextConfiguration() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscreteTextConfiguration"/> class with the specified unique
        /// identifier.
        /// </summary>
        /// <param name="id">The unique identifier for the configuration instance.</param>
        public DiscreteTextConfiguration(Guid id) : base(id)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscreteTextConfiguration"/> class using the specified
        /// parameter.
        /// </summary>
        /// <param name="parameter">The parameter used to configure the discrete text settings.</param>
        internal DiscreteTextConfiguration(Net.Profiles.Parameter parameter) : base(parameter)
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
        /// Gets a read-only dictionary of discrete values.
        /// </summary>
        public IReadOnlyDictionary<string, string> Discretes => discretes;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        protected internal override void InternalParseParameter(Net.Profiles.Parameter parameter)
        {
            if (!parameter.HasDefaultStringValue())
            {
                defaultValue = null;
            }
            else
            {
                int defaultIndex = parameter.Discretes.IndexOf(parameter.DefaultValue.StringValue);
                defaultValue = defaultIndex >= 0 ? parameter.DiscreetDisplayValues[defaultIndex] : null;
            }

            for (int i = 0; i < parameter.Discretes.Count; i++)
            {
                discretes.Add(parameter.DiscreetDisplayValues[i], parameter.Discretes[i]);
            }
        }
    }
}
