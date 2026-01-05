namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    using CoreParameter = Net.Profiles.Parameter;

    /// <summary>
    /// Represents a Range Capacity in the MediaOps Plan API.
    /// </summary>
    public class RangeCapacity : Capacity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RangeCapacity"/> class.
        /// </summary>
        public RangeCapacity() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RangeCapacity"/> class with the specified unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier for the capacity.</param>
        public RangeCapacity(Guid id) : base(id)
        {
        }

        internal RangeCapacity(CoreParameter parameter) : base(parameter)
        {
            InitTracking();
        }

        /// <inheritdoc/>
        protected internal override CoreParameter.ParameterType ParameterType => CoreParameter.ParameterType.Range;
    }
}
