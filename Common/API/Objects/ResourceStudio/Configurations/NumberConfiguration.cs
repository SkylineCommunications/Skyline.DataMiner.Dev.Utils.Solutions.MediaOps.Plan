namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using Skyline.DataMiner.MediaOps.Plan.Storage.Core;

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
        }

        public decimal? DefaultValue
        {
            get => defaultValue;
            set
            {
                HasChanges = true;
                defaultValue = value;
            }
        }

        /// <summary>
        /// Gets or sets the units of measurement for the capacity.
        /// </summary>
        public string Units
        {
            get => units;
            set
            {
                HasChanges = true;
                units = value;
            }
        }

        /// <summary>
        /// Gets or sets the minimum value for the capacity range.
        /// </summary>
        public decimal? RangeMin
        {
            get => rangeMin;
            set
            {
                HasChanges = true;
                rangeMin = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum value for the capacity range.
        /// </summary>
        public decimal? RangeMax
        {
            get => rangeMax;
            set
            {
                HasChanges = true;
                rangeMax = value;
            }
        }

        /// <summary>
        /// Gets or sets the step size for the capacity.
        /// </summary>
        public decimal? StepSize
        {
            get => stepSize;
            set
            {
                HasChanges = true;
                stepSize = value;
            }
        }

        /// <summary>
        /// Gets or sets the number of decimal places for the capacity values.
        /// </summary>
        public int? Decimals
        {
            get => decimals;
            set
            {
                HasChanges = true;
                decimals = value;
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        protected internal override void InternalParseParameter(Net.Profiles.Parameter parameter)
        {
            defaultValue = parameter.HasDefaultNumericValue() ? null : (decimal)parameter.DefaultValue.DoubleValue;
            units = parameter.Units ?? string.Empty;
            rangeMin = parameter.HasMinRange() ? null : (decimal)parameter.RangeMin;
            rangeMax = parameter.HasMaxRange() ? null : (decimal)parameter.RangeMax;
            stepSize = parameter.HasStepSize() ? null : (decimal)parameter.Stepsize;
            decimals = parameter.HasDecimals() ? null : parameter.Decimals;
        }
    }
}
