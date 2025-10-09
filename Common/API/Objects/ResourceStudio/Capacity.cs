namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using Skyline.DataMiner.Net.Profiles;
    using CoreParameter = Net.Profiles.Parameter;

    /// <summary>
    /// Represents a Capacity in the MediaOps Plan API.
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

        /// <summary>
        /// Initializes a new instance of the <see cref="Capacity"/> class using the specified core parameter.
        /// </summary>
        /// <param name="parameter">The core parameter used to initialize the capacity. Must not be null.</param>
        internal protected Capacity(CoreParameter parameter) : base(parameter)
        {
        }

        /// <summary>
        /// Gets the category of the profile parameter, indicating its classification as a capacity.
        /// </summary>
        protected internal override ProfileParameterCategory Category => ProfileParameterCategory.Capacity;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        protected internal override void InternalParseParameter(CoreParameter parameter)
        {
            throw new NotImplementedException();
        }
    }
}
