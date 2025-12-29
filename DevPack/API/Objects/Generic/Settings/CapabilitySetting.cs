namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Represents a configurable set of discrete values associated with a specific capability.
    /// </summary>
    public class CapabilitySetting : TrackableObject
    {
        internal HashSet<string> discretes = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="CapabilitySetting"/> class using the specified capability.
        /// </summary>
        /// <param name="capability">The capability to use for initializing the settings. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="capability"/> is <see langword="null"/>.</exception>
        public CapabilitySetting(Capability capability)
            : this(capability?.Id ?? throw new ArgumentNullException(nameof(capability)))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CapabilitySetting"/> class with the specified capability ID.
        /// </summary>
        /// <param name="capabilityId">The unique identifier for the capability. Must not be an empty GUID.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="capabilityId"/> is an empty GUID.</exception>
        public CapabilitySetting(Guid capabilityId)
        {
            if (capabilityId == Guid.Empty)
            {
                throw new ArgumentException(nameof(capabilityId));
            }

            Id = capabilityId;

            IsNew = true;
        }

        internal CapabilitySetting()
        {
        }

        internal CapabilitySetting(CapabilitySetting capabilitySetting)
        {
            Id = capabilitySetting.Id;
            discretes = new HashSet<string>(capabilitySetting.Discretes);

            IsNew = capabilitySetting.IsNew;
            InitTracking();
        }

        /// <summary>
        /// Gets the unique identifier of the capability.
        /// </summary>
        public Guid Id { get; internal set; }

        /// <summary>
        /// Gets the collection of discrete values.
        /// </summary>
        public IReadOnlyCollection<string> Discretes => discretes;

        internal virtual Storage.DOM.DomSectionBase OriginalSection { get; }

        /// <summary>
        /// Adds a discrete value to the collection if it is not already present.
        /// </summary>
        /// <param name="value">The discrete value to add to the collection. Cannot be null or empty.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <see langword="null"/> or empty.</exception>
        public CapabilitySetting AddDiscrete(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(nameof(value));
            }

            discretes.Add(value);
            return this;
        }

        /// <summary>
        /// Removes the specified discrete value from the collection if it exists.
        /// </summary>
        /// <param name="value">The discrete value to remove from the collection. Cannot be null or empty.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <see langword="null"/> or empty.</exception>
        public CapabilitySetting RemoveDiscrete(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(nameof(value));
            }

            discretes.Remove(value);
            return this;
        }

        /// <summary>
        /// Replaces the current set of discrete values with the specified collection.
        /// </summary>
        /// <param name="values">A collection of non-null, non-empty strings representing the discrete values to set.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="values"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="values"/> contains a <see langword="null"/> or empty string.</exception>
        public CapabilitySetting SetDiscretes(ICollection<string> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            if (values.Any(x => string.IsNullOrEmpty(x)))
            {
                throw new ArgumentException("The collection contains null or empty values.", nameof(values));
            }

            discretes = new HashSet<string>(values);
            return this;
        }

        /// <summary>
        /// Generates the hash code for the object.
        /// </summary>
        /// <returns>Hash code representing the current object.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 23) + Id.GetHashCode();
                hash = (hash * 23) + (OriginalSection != null ? OriginalSection.ID.Id.GetHashCode() : 0);
                foreach (var discreet in discretes.OrderBy(x => x))
                {
                    hash = (hash * 23) + (discreet != null ? discreet.GetHashCode() : 0);
                }

                return hash;
            }
        }
    }
}
