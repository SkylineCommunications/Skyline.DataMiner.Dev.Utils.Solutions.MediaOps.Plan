namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

    /// <summary>
    /// Represents the capability settings for a resource pool.
    /// </summary>
    public class ResourcePoolCapabilitySettings : TrackableObject
    {
        private StorageResourceStudio.ResourcePoolCapabilitiesSection orginalSection;

        private StorageResourceStudio.ResourcePoolCapabilitiesSection updatedSection;

        private HashSet<string> discretes = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourcePoolCapabilitySettings"/> class using the specified capability.
        /// </summary>
        /// <param name="capability">The capability to use for initializing the settings. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="capability"/> is <see langword="null"/>.</exception>
        public ResourcePoolCapabilitySettings(Capability capability)
            : this(capability?.Id ?? throw new ArgumentNullException(nameof(capability)))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourcePoolCapabilitySettings"/> class with the specified capability ID.
        /// </summary>
        /// <param name="capabilityId">The unique identifier for the capability. Must not be an empty GUID.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="capabilityId"/> is an empty GUID.</exception>
        public ResourcePoolCapabilitySettings(Guid capabilityId)
        {
            if (capabilityId == Guid.Empty)
            {
                throw new ArgumentException(nameof(capabilityId));
            }

            Id = capabilityId;

            IsNew = true;
        }

        internal ResourcePoolCapabilitySettings(StorageResourceStudio.ResourcePoolCapabilitiesSection section)
        {
            ParseSection(section);
        }

        internal EventHandler<EventArgs> ValueChanged;

        /// <summary>
        /// Gets the unique identifier of the capability.
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Gets the collection of discrete values.
        /// </summary>
        public IReadOnlyCollection<string> Discretes => discretes;

        /// <summary>
        /// Adds a discrete value to the collection if it is not already present.
        /// </summary>
        /// <param name="value">The discrete value to add to the collection. Cannot be null or empty.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <see langword="null"/> or empty.</exception>
        public void AddDiscrete(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (discretes.Add(value))
            {
                HasChanges = true;
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Removes the specified discrete value from the collection if it exists.
        /// </summary>
        /// <param name="value">The discrete value to remove from the collection. Cannot be null or empty.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <see langword="null"/> or empty.</exception>
        public void RemoveDiscrete(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (discretes.Remove(value))
            {
                HasChanges = true;
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Replaces the current set of discrete values with the specified collection.
        /// </summary>
        /// <param name="values">A collection of non-null, non-empty strings representing the discrete values to set.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="values"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="values"/> contains a <see langword="null"/> or empty string.</exception>
        public void SetDiscretes(ICollection<string> values)
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
        }

        internal StorageResourceStudio.ResourcePoolCapabilitiesSection OriginalSection => orginalSection;

        internal StorageResourceStudio.ResourcePoolCapabilitiesSection GetSectionWithChanges()
        {
            if (updatedSection == null)
            {
                updatedSection = IsNew ? new StorageResourceStudio.ResourcePoolCapabilitiesSection() : OriginalSection.Clone();
            }

            updatedSection.ProfileParameterId = Id;
            updatedSection.DiscreteValues = discretes;

            return updatedSection;
        }

        private void ParseSection(StorageResourceStudio.ResourcePoolCapabilitiesSection section)
        {
            orginalSection = section ?? throw new ArgumentNullException(nameof(section));

            Id = section.ProfileParameterId;
            discretes = new HashSet<string>(section.DiscreteValues);
        }
    }
}
