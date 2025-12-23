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
                HasChanges = true;
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
