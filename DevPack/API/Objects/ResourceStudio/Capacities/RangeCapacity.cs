namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    using Skyline.DataMiner.Net.Profiles;

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
        }

        internal override CoreParameter GetParameterWithChanges()
        {
            var updatedParameter = IsNew ? new CoreParameter(Id) { Categories = ProfileParameterCategory.Capacity, Type = CoreParameter.ParameterType.Range } : new CoreParameter(CoreParameter) { Categories = ProfileParameterCategory.Capacity };

            updatedParameter.Name = Name;
            updatedParameter.IsOptional = !IsMandatory;

            updatedParameter.Units = !string.IsNullOrEmpty(units) ? units : null;
            updatedParameter.RangeMin = rangeMin.HasValue ? (double)rangeMin.Value : double.NaN;
            updatedParameter.RangeMax = rangeMax.HasValue ? (double)rangeMax.Value : double.NaN;
            updatedParameter.Stepsize = stepSize.HasValue ? (double)stepSize.Value : double.NaN;
            updatedParameter.Decimals = decimals.HasValue ? decimals.Value : int.MaxValue;

            return updatedParameter;
        }
    }
}
