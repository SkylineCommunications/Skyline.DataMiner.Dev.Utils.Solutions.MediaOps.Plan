namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;

    /// <summary>
    /// Represents a configuration for discrete text settings, providing functionality to manage and parse text-based
    /// configurations.
    /// </summary>
    public class DiscreteTextConfiguration : Configuration
    {
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

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        protected internal override void InternalParseParameter(Net.Profiles.Parameter parameter)
        {
            throw new NotImplementedException();
        }
    }
}
