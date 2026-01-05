namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core;

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
            InitTracking();
        }

        /// <summary>
        /// Gets or sets the default value of this <see cref="NumberConfiguration"/>.
        /// </summary>
        public decimal? DefaultValue { get; set; }

        /// <summary>
        /// Gets or sets the units of measurement for the <see cref="NumberConfiguration"/>.
        /// </summary>
        public string Units { get; set; }

        /// <summary>
        /// Gets or sets the minimum value for the <see cref="NumberConfiguration"/> range.
        /// </summary>
        public decimal? RangeMin { get; set; }

        /// <summary>
        /// Gets or sets the maximum value for the <see cref="NumberConfiguration"/> range.
        /// </summary>
        public decimal? RangeMax { get; set; }

        /// <summary>
        /// Gets or sets the step size for the <see cref="NumberConfiguration"/>.
        /// </summary>
        public decimal? StepSize { get; set; }

        /// <summary>
        /// Gets or sets the number of decimal places for the <see cref="NumberConfiguration"/> values.
        /// </summary>
        public int? Decimals { get; set; }

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
            if (obj is not NumberConfiguration other)
            {
                return false;
            }

            return base.Equals(other)
                && string.Equals(Units, other.Units, StringComparison.Ordinal)
                && RangeMin == other.RangeMin
                && RangeMax == other.RangeMax
                && StepSize == other.StepSize
                && Decimals == other.Decimals;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        protected internal override void InternalParseParameter(Net.Profiles.Parameter parameter)
        {
            DefaultValue = parameter.HasDefaultNumericValue() ? (decimal)parameter.DefaultValue.DoubleValue : null;
            Units = parameter.Units;
            RangeMin = parameter.HasMinRange() ? (decimal)parameter.RangeMin : null;
            RangeMax = parameter.HasMaxRange() ? (decimal)parameter.RangeMax : null;
            StepSize = parameter.HasStepSize() ? (decimal)parameter.Stepsize : null;
            Decimals = parameter.HasDecimals() ? parameter.Decimals : null;
        }
    }
}
