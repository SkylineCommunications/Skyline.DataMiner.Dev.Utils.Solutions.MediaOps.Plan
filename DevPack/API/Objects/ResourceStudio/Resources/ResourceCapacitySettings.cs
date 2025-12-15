namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

    /// <summary>
    /// Represents the capacity settings for a resource.
    /// </summary>
    public abstract class ResourceCapacitySettings : TrackableObject
    {
        internal StorageResourceStudio.ResourceCapacitiesSection originalSection;

        internal StorageResourceStudio.ResourceCapacitiesSection updatedSection;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceCapacitySettings"/> class using the specified capacity.
        /// </summary>
        /// <param name="capacity">The capacity to use for initializing the settings. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="capacity"/> is <see langword="null"/>.</exception>
        private protected ResourceCapacitySettings(Capacity capacity)
            : this(capacity?.Id ?? throw new ArgumentNullException(nameof(capacity)))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceCapacitySettings"/> class with the specified capacity ID.
        /// </summary>
        /// <param name="capacityId">The unique identifier for the capacity. Must not be an empty GUID.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="capacityId"/> is an empty GUID.</exception>
        private protected ResourceCapacitySettings(Guid capacityId)
        {
            if (capacityId == Guid.Empty)
            {
                throw new ArgumentException(nameof(capacityId));
            }

            Id = capacityId;

            IsNew = true;
        }

        private protected ResourceCapacitySettings(StorageResourceStudio.ResourceCapacitiesSection section)
        {
            ParseSection(section);
        }

        internal EventHandler<EventArgs> ValueChanged;

        /// <summary>
        /// Gets the unique identifier of the capacity.
        /// </summary>
        public Guid Id { get; private set; }

        internal StorageResourceStudio.ResourceCapacitiesSection OriginalSection => originalSection;

        internal abstract StorageResourceStudio.ResourceCapacitiesSection GetSectionWithChanges();

        private void ParseSection(StorageResourceStudio.ResourceCapacitiesSection section)
        {
            originalSection = section ?? throw new ArgumentNullException(nameof(section));

            Id = section.ProfileParameterId;
        }
    }
}