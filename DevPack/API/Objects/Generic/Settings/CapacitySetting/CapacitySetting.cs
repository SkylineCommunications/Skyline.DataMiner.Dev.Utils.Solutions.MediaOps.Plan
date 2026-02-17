namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    /// <summary>
    /// Represents an abstract base class for settings associated with a specific capacity.
    /// </summary>
    public abstract class CapacitySetting : TrackableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CapacitySetting"/> class using the specified capacity.
        /// </summary>
        /// <param name="capacity">The capacity to use for initializing the settings. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="capacity"/> is <see langword="null"/>.</exception>
        private protected CapacitySetting(Capacity capacity)
            : this(capacity?.ID ?? throw new ArgumentNullException(nameof(capacity)))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CapacitySetting"/> class with the specified capacity ID.
        /// </summary>
        /// <param name="capacityId">The unique identifier for the capacity. Must not be an empty GUID.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="capacityId"/> is an empty GUID.</exception>
        private protected CapacitySetting(Guid capacityId)
        {
            if (capacityId == Guid.Empty)
            {
                throw new ArgumentException(nameof(capacityId));
            }

            Id = capacityId;

            IsNew = true;
        }

        private protected CapacitySetting()
        {
        }

        private protected CapacitySetting(CapacitySetting capacitySetting)
        {
            Id = capacitySetting.Id;
            IsNew = true;
        }

        /// <summary>
        /// Gets the unique identifier of the capacity.
        /// </summary>
        public Guid Id { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether this setting is fully defined.
        /// </summary>
        public abstract bool HasValue { get; }

        internal virtual Storage.DOM.DomSectionBase OriginalSection { get; }
    }
}
