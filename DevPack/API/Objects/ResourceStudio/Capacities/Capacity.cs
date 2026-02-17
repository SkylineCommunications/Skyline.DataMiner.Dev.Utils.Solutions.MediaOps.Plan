namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.Net.Profiles;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core;

    using CoreParameter = Net.Profiles.Parameter;

    /// <summary>
    /// Represents a Capacity in the MediaOps Plan API.
    /// </summary>
    public abstract class Capacity : Parameter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Capacity"/> class.
        /// </summary>
        private protected Capacity() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Capacity"/> class with the specified unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier for the capacity.</param>
        private protected Capacity(Guid id) : base(id)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Capacity"/> class using the specified core parameter.
        /// </summary>
        /// <param name="parameter">The core parameter used to initialize the capacity. Must not be null.</param>
        private protected Capacity(CoreParameter parameter) : base(parameter)
        {
        }

        /// <summary>
        /// Defines an implicit conversion from a Capacity instance to its underlying Guid identifier.
        /// </summary>
        /// <remarks>This operator enables a Capacity object to be used wherever a Guid is expected,
        /// returning the value of its Id property. If the Capacity instance is null, a NullReferenceException will be
        /// thrown.</remarks>
        /// <param name="capacity">The Capacity instance to convert to a Guid.</param>
        public static implicit operator Guid(Capacity capacity) => capacity.ID;

        /// <summary>
        /// Gets or sets the units of measurement for the capacity.
        /// </summary>
        public string Units { get; set; }

        /// <summary>
        /// Gets or sets the minimum value for the capacity range.
        /// </summary>
        public decimal? RangeMin { get; set; }

        /// <summary>
        /// Gets or sets the maximum value for the capacity range.
        /// </summary>
        public decimal? RangeMax { get; set; }

        /// <summary>
        /// Gets or sets the step size for the capacity.
        /// </summary>
        public decimal? StepSize { get; set; }

        /// <summary>
        /// Gets or sets the number of decimal places for the capacity values.
        /// </summary>
        public int? Decimals { get; set; }

        /// <summary>
        /// Gets the category of the profile parameter, indicating its classification as a capacity.
        /// </summary>
        protected internal override ProfileParameterCategory Category => ProfileParameterCategory.Capacity;

        /// <summary>
        /// Gets the internal type of the parameter.
        /// </summary>
        protected internal abstract CoreParameter.ParameterType ParameterType { get; }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = base.GetHashCode();
                hash = (hash * 23) + (Units != null ? Units.GetHashCode() : 0);
                hash = (hash * 23) + (RangeMin.HasValue ? RangeMin.Value.GetHashCode() : 0);
                hash = (hash * 23) + (RangeMax.HasValue ? RangeMax.Value.GetHashCode() : 0);
                hash = (hash * 23) + (StepSize.HasValue ? StepSize.Value.GetHashCode() : 0);
                hash = (hash * 23) + (Decimals.HasValue ? Decimals.Value.GetHashCode() : 0);

                return hash;
            }
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is not Capacity other)
            {
                return false;
            }

            return base.Equals(other)
                && Units == other.Units
                && RangeMin == other.RangeMin
                && RangeMax == other.RangeMax
                && StepSize == other.StepSize
                && Decimals == other.Decimals;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        protected internal override void InternalParseParameter(CoreParameter parameter)
        {
            Units = parameter.Units ?? string.Empty;
            RangeMin = parameter.HasMinRange() ? (decimal)parameter.RangeMin : null;
            RangeMax = parameter.HasMaxRange() ? (decimal)parameter.RangeMax : null;
            StepSize = parameter.HasStepSize() ? (decimal)parameter.Stepsize : null;
            Decimals = parameter.HasDecimals() ? parameter.Decimals : null;
        }

        internal virtual CoreParameter GetParameterWithChanges()
        {
            var updatedParameter = IsNew ?
                new CoreParameter(ID) { Categories = ProfileParameterCategory.Capacity, Type = ParameterType } :
                new CoreParameter(CoreParameter) { Categories = ProfileParameterCategory.Capacity };

            updatedParameter.Name = Name;
            updatedParameter.IsOptional = !IsMandatory;

            updatedParameter.Units = Units ?? String.Empty;
            updatedParameter.RangeMin = RangeMin.HasValue ? (double)RangeMin.Value : double.NaN;
            updatedParameter.RangeMax = RangeMax.HasValue ? (double)RangeMax.Value : double.NaN;
            updatedParameter.Stepsize = StepSize.HasValue ? (double)StepSize.Value : double.NaN;
            updatedParameter.Decimals = Decimals.HasValue ? Decimals.Value : int.MaxValue;

            return updatedParameter;
        }

        internal static Capacity InstantiateCapacity(CoreParameter parameter)
        {
            if (parameter == null)
            {
                throw new ArgumentNullException(nameof(parameter));
            }

            return InstantiateCapacities([parameter]).FirstOrDefault();
        }

        internal static IEnumerable<Capacity> InstantiateCapacities(IEnumerable<CoreParameter> parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (!parameters.Any())
            {
                return [];
            }

            return InstantiateCapacitiesIterator(parameters);
        }

        private static IEnumerable<Capacity> InstantiateCapacitiesIterator(IEnumerable<CoreParameter> parameters)
        {
            foreach (var parameter in parameters)
            {
                if (!parameter.IsCapacity())
                {
                    continue;
                }

                if (parameter.IsNumber())
                {
                    yield return new NumberCapacity(parameter);
                }
                else if (parameter.IsRange())
                {
                    yield return new RangeCapacity(parameter);
                }
                else
                {
                    // continue
                }
            }
        }
    }
}
