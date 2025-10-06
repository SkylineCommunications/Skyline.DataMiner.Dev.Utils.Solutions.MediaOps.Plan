namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using CoreParameter = Skyline.DataMiner.Net.Profiles.Parameter;

    /// <summary>
    /// Represents a Configuration in the MediaOps.
    /// </summary>
    public class Configuration : Parameter
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

        internal Configuration(CoreParameter parameter) : base(parameter)
        {
        }

        protected internal override Net.Profiles.ProfileParameterCategory Category => Net.Profiles.ProfileParameterCategory.Configuration;

        protected internal override void InternalParseParameter(CoreParameter parameter)
        {

        }
    }
}
