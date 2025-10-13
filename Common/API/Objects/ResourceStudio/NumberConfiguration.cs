namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;

    /// <summary>
    /// Represents a configuration for numeric parameters, providing functionality to manage and parse numeric-related configurations.
    /// </summary>
    public class NumberConfiguration : Configuration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NumberConfiguration"/> class.
        /// </summary>
        public NumberConfiguration() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NumberConfiguration"/> class with the specified unique
        /// identifier.
        /// </summary>
        /// <param name="id">The unique identifier for the configuration.</param>
        public NumberConfiguration(Guid id) : base(id)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NumberConfiguration"/> class using the specified parameter.
        /// </summary>
        /// <param name="parameter">The parameter used to configure the number settings.</param>
        internal NumberConfiguration(Net.Profiles.Parameter parameter) : base(parameter)
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
