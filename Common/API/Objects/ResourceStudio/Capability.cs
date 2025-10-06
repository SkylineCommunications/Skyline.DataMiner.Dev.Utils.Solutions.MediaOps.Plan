namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using CoreParameter = Skyline.DataMiner.Net.Profiles.Parameter;

    /// <summary>
    /// Represents a capability in the MediaOps Plan API.
    /// </summary>
    public class Capability : Parameter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Capability"/> class.
        /// </summary>
        public Capability() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Capability"/> class with the specified unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier for the capability.</param>
        public Capability(Guid id) : base(id)
        {
        }

        internal Capability(CoreParameter parameter) : base(parameter)
        {
        }

        protected internal override Net.Profiles.ProfileParameterCategory Category => Net.Profiles.ProfileParameterCategory.Capability;

        protected internal override void InternalParseParameter(CoreParameter parameter)
        {

        }
    }
}
