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
        private string units;
        private decimal? rangeMin;
        private decimal? rangeMax;
        private decimal? stepSize;
        private int? decimals;

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
        /// Gets the category of the profile parameter, indicating its classification as a capacity.
        /// </summary>
        protected internal override ProfileParameterCategory Category => ProfileParameterCategory.Capacity;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        protected internal override void InternalParseParameter(CoreParameter parameter)
        {
            units = parameter.Units ?? string.Empty;
            rangeMin = double.IsNaN(parameter.RangeMin) ? null : (decimal)parameter.RangeMin;
            rangeMax = double.IsNaN(parameter.RangeMax) ? null : (decimal)parameter.RangeMax;
            stepSize = double.IsNaN(parameter.Stepsize) ? null : (decimal)parameter.Stepsize;
            decimals = (parameter.Decimals == int.MaxValue)? null : parameter.Decimals;
        }
    }
}
