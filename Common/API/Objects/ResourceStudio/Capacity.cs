namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using CoreParameter = Skyline.DataMiner.Net.Profiles.Parameter;

    /// <summary>
    /// Represents a Capacity in the MediaOps.
    /// </summary>
    public class Capacity : Parameter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Capacity"/> class.
        /// </summary>
        public Capacity() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Capacity"/> class with the specified unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier for the capacity.</param>
        public Capacity(Guid id) : base(id)
        {
        }

        internal Capacity(CoreParameter parameter) : base(parameter)
        {
        }

        protected internal override Net.Profiles.ProfileParameterCategory Category => Net.Profiles.ProfileParameterCategory.Capacity;

        protected internal override void InternalParseParameter(CoreParameter parameter)
        {

        }
    }
}
