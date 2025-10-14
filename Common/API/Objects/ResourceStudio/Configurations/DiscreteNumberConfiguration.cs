namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;

    /// <summary>
    /// Represents a configuration for discrete numerical values.
    /// </summary>
    public class DiscreteNumberConfiguration : Configuration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DiscreteNumberConfiguration"/> class.
        /// </summary>
        public DiscreteNumberConfiguration() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscreteNumberConfiguration"/> class with the specified unique
        /// identifier.
        /// </summary>
        /// <param name="id">The unique identifier for the configuration instance.</param>
        public DiscreteNumberConfiguration(Guid id) : base(id)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscreteNumberConfiguration"/> class with the specified
        /// parameter.
        /// </summary>
        /// <param name="parameter">The parameter used to configure the discrete number settings.</param>
        internal DiscreteNumberConfiguration(Net.Profiles.Parameter parameter) : base(parameter)
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
