namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    using Skyline.DataMiner.Net.Profiles;

    using CoreParameter = Net.Profiles.Parameter;

    /// <summary>
    /// Represents a Number Capacity in the MediaOps Plan API.
    /// </summary>
    public class NumberCapacity : Capacity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NumberCapacity"/> class.
        /// </summary>
        public NumberCapacity() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NumberCapacity"/> class with the specified unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier for the capacity.</param>
        public NumberCapacity(Guid id) : base(id)
        {
        }

        internal NumberCapacity(CoreParameter parameter) : base(parameter)
        {
        }

        internal override CoreParameter GetParameterWithChanges()
        {
            var updatedParameter = IsNew ? new CoreParameter(Id) { Categories = ProfileParameterCategory.Capacity, Type = CoreParameter.ParameterType.Number } : new CoreParameter(CoreParameter) { Categories = ProfileParameterCategory.Capacity };

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
