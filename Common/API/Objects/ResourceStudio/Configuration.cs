namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using Skyline.DataMiner.Net.Profiles;
    using CoreParameter = Net.Profiles.Parameter;

    /// <summary>
    /// Represents a Configuration in the MediaOps.
    /// </summary>
    public abstract class Configuration : Parameter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Configuration"/> class.
        /// </summary>
        public Configuration() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Configuration"/> class with the specified unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier for the configuration.</param>
        public Configuration(Guid id) : base(id)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Configuration"/> class using the specified core parameter.
        /// </summary>
        /// <param name="parameter">The core parameter used to configure the instance. Must not be <see langword="null"/>.</param>
        internal protected Configuration(CoreParameter parameter) : base(parameter)
        {
        }

        protected internal override ProfileParameterCategory Category => ProfileParameterCategory.Configuration;
    }
}
