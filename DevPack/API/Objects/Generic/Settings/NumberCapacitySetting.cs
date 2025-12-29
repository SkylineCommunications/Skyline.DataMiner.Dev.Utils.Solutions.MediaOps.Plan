namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    /// <summary>
    /// Represents a capacity setting that uses a numeric value.
    /// </summary>
    public class NumberCapacitySetting : CapacitySetting
    {
        internal decimal value;

        /// <summary>
        /// Initializes a new instance of the <see cref="NumberCapacitySetting"/> class using the specified capacity.
        /// </summary>
        /// <param name="capacity">The capacity to use for initializing the settings. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="capacity"/> is <see langword="null"/>.</exception>
        public NumberCapacitySetting(NumberCapacity capacity)
            : base(capacity)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NumberCapacitySetting"/> class with the specified capacity ID.
        /// </summary>
        /// <param name="capacityId">The unique identifier for the capacity. Must not be an empty GUID.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="capacityId"/> is an empty GUID.</exception>
        public NumberCapacitySetting(Guid capacityId)
            : base(capacityId)
        {
        }

        internal NumberCapacitySetting() : base()
        {
        }

        internal NumberCapacitySetting(NumberCapacitySetting numberCapacitySetting)
            : base(numberCapacitySetting)
        {
            value = numberCapacitySetting.value;
            InitTracking();
        }

        /// <summary>
        /// Gets or sets the capacity value.
        /// </summary>
        public decimal Value
        {
            get => value;
            set
            {
                this.value = value;
            }
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 23) + Id.GetHashCode();
                hash = (hash * 23) + Value.GetHashCode();
                hash = (hash * 23) + (OriginalSection != null ? OriginalSection.ID.Id.GetHashCode() : 0);

                return hash;
            }
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is not NumberCapacitySetting other)
            {
                return false;
            }

            return Id == other.Id && Value == other.Value;
        }
    }
}
