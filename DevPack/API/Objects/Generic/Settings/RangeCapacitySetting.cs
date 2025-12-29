namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    /// <summary>
    /// Represents a capacity setting that defines a range with minimum and maximum values.
    /// </summary>
    public class RangeCapacitySetting : CapacitySetting
    {
        internal decimal minValue;
        internal decimal maxValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="RangeCapacitySetting"/> class using the specified capacity.
        /// </summary>
        /// <param name="capacity">The capacity to use for initializing the settings. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="capacity"/> is <see langword="null"/>.</exception>
        public RangeCapacitySetting(RangeCapacity capacity)
            : base(capacity)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RangeCapacitySetting"/> class with the specified capacity ID.
        /// </summary>
        /// <param name="capacityId">The unique identifier for the capacity. Must not be an empty GUID.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="capacityId"/> is an empty GUID.</exception>
        public RangeCapacitySetting(Guid capacityId)
            : base(capacityId)
        {
        }

        internal RangeCapacitySetting() : base()
        {
        }

        internal RangeCapacitySetting(RangeCapacitySetting rangeCapacitySetting)
            : base(rangeCapacitySetting)
        {
            minValue = rangeCapacitySetting.minValue;
            maxValue = rangeCapacitySetting.maxValue;
            InitTracking();
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 23) + Id.GetHashCode();
                hash = (hash * 23) + minValue.GetHashCode();
                hash = (hash * 23) + maxValue.GetHashCode();
                hash = (hash * 23) + (OriginalSection != null ? OriginalSection.ID.Id.GetHashCode() : 0);

                return hash;
            }
        }

        /// <summary>
        /// Gets or sets the minimum capacity value.
        /// </summary>
        public decimal MinValue
        {
            get => minValue;
            set
            {
                minValue = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum capacity value.
        /// </summary>
        public decimal MaxValue
        {
            get => maxValue;
            set
            {
                maxValue = value;
            }
        }
    }
}
