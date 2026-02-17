namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    /// <summary>
    /// Represents a single configurable value associated with a specific capability.
    /// </summary>
    public class CapabilitySetting : TrackableObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CapabilitySetting"/> class using the specified capability.
        /// </summary>
        /// <param name="capability">The capability to use for initializing the setting. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="capability"/> is <see langword="null"/>.</exception>
        public CapabilitySetting(Capability capability)
            : this(capability?.ID ?? throw new ArgumentNullException(nameof(capability)))
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
            Value = capabilitySetting.Value;

            IsNew = true;
        }

        /// <summary>
        /// Gets the unique identifier of the capability.
        /// </summary>
        public Guid Id { get; internal set; }

        /// <summary>
        /// Gets or sets the value associated with this capability.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Gets a value indicating whether this setting has a value defined.
        /// </summary>
        public bool HasValue => Value != null;

        internal virtual Storage.DOM.DomSectionBase OriginalSection { get; }

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
                hash = (hash * 23) + (Value != null ? Value.GetHashCode() : 0);

                return hash;
            }
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current <see cref="CapabilitySetting"/> instance.
        /// </summary>
        /// <param name="obj">The object to compare with the current <see cref="CapabilitySetting"/> instance.</param>
        /// <returns>
        /// <c>true</c> if the specified object is a <see cref="CapabilitySetting"/> and has the same <see cref="Id"/> and
        /// <see cref="Value"/> as the current instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj is not CapabilitySetting other)
            {
                return false;
            }

            return Id == other.Id && Value == other.Value;
        }
    }
}
