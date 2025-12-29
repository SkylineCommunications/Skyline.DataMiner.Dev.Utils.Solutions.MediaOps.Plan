namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.Core;

    /// <summary>
    /// Represents a configuration for numeric parameters, providing functionality to manage and parse numeric-related configurations.
    /// </summary>
    public class NumberConfiguration : Configuration
    {
        private decimal? defaultValue;
        private string units;
        private decimal? rangeMin;
        private decimal? rangeMax;
        private decimal? stepSize;
        private int? decimals;

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
        public decimal? DefaultValue
        {
            get => defaultValue;
            set
            {
                defaultValue = value;
            }
        }

        /// <summary>
        /// Gets or sets the units of measurement for the <see cref="NumberConfiguration"/>.
        /// </summary>
        public string Units
        {
            get => units;
            set
            {
                units = value;
            }
        }

        /// <summary>
        /// Gets or sets the minimum value for the <see cref="NumberConfiguration"/> range.
        /// </summary>
        public decimal? RangeMin
        {
            get => rangeMin;
            set
            {
                rangeMin = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum value for the <see cref="NumberConfiguration"/> range.
        /// </summary>
        public decimal? RangeMax
        {
            get => rangeMax;
            set
            {
                rangeMax = value;
            }
        }

        /// <summary>
        /// Gets or sets the step size for the <see cref="NumberConfiguration"/>.
        /// </summary>
        public decimal? StepSize
        {
            get => stepSize;
            set
            {
                stepSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the number of decimal places for the <see cref="NumberConfiguration"/> values.
        /// </summary>
        public int? Decimals
        {
            get => decimals;
            set
            {
                decimals = value;
            }
        }

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

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        protected internal override void InternalParseParameter(Net.Profiles.Parameter parameter)
        {
            defaultValue = parameter.HasDefaultNumericValue() ? (decimal)parameter.DefaultValue.DoubleValue : null;
            units = parameter.Units;
            rangeMin = parameter.HasMinRange() ? (decimal)parameter.RangeMin : null;
            rangeMax = parameter.HasMaxRange() ? (decimal)parameter.RangeMax : null;
            stepSize = parameter.HasStepSize() ? (decimal)parameter.Stepsize : null;
            decimals = parameter.HasDecimals() ? parameter.Decimals : null;
        }
    }
}
