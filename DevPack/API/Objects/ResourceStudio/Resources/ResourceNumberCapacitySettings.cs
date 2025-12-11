namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

    /// <summary>
    /// Represents the number capacity settings for a resource.
    /// </summary>
    public class ResourceNumberCapacitySettings : ResourceCapacitySettings
    {
        private decimal value;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceNumberCapacitySettings"/> class using the specified capacity.
        /// </summary>
        /// <param name="capacity">The capacity to use for initializing the settings. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="capacity"/> is <see langword="null"/>.</exception>
        public ResourceNumberCapacitySettings(NumberCapacity capacity)
            : base(capacity)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceNumberCapacitySettings"/> class with the specified capacity ID.
        /// </summary>
        /// <param name="capacityId">The unique identifier for the capacity. Must not be an empty GUID.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="capacityId"/> is an empty GUID.</exception>
        public ResourceNumberCapacitySettings(Guid capacityId)
            : base(capacityId)
        {
        }

        internal ResourceNumberCapacitySettings(StorageResourceStudio.ResourceCapacitiesSection section)
            : base(section)
        {
            ParseSection(section);
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

        internal override StorageResourceStudio.ResourceCapacitiesSection GetSectionWithChanges()
        {
            if (updatedSection == null)
            {
                updatedSection = IsNew ? new StorageResourceStudio.ResourceCapacitiesSection() : originalSection.Clone();
            }

            updatedSection.ProfileParameterId = Id;
            updatedSection.DoubleMaxValue = (double)value;
            updatedSection.DoubleMinValue = null;

            return updatedSection;
        }

        private void ParseSection(StorageResourceStudio.ResourceCapacitiesSection section)
        {
            value = (decimal)section.DoubleMaxValue.Value;
        }
    }
}