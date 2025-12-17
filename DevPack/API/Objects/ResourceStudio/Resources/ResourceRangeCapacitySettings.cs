namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

    /// <summary>
    /// Represents the range capacity settings for a resource.
    /// </summary>
    public class ResourceRangeCapacitySettings : ResourceCapacitySettings
    {
        private decimal minValue;
        private decimal maxValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceRangeCapacitySettings"/> class using the specified capacity.
        /// </summary>
        /// <param name="capacity">The capacity to use for initializing the settings. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="capacity"/> is <see langword="null"/>.</exception>
        public ResourceRangeCapacitySettings(RangeCapacity capacity)
            : base(capacity)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceRangeCapacitySettings"/> class with the specified capacity ID.
        /// </summary>
        /// <param name="capacityId">The unique identifier for the capacity. Must not be an empty GUID.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="capacityId"/> is an empty GUID.</exception>
        public ResourceRangeCapacitySettings(Guid capacityId)
            : base(capacityId)
        {
        }

        internal ResourceRangeCapacitySettings(StorageResourceStudio.ResourceCapacitiesSection section)
            : base(section)
        {
            ParseSection(section);
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
                HasChanges = true;
                ValueChanged?.Invoke(this, EventArgs.Empty);
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
            updatedSection.DoubleMinValue = (double)minValue;
            updatedSection.DoubleMaxValue = (double)maxValue;

            return updatedSection;
        }

        private void ParseSection(StorageResourceStudio.ResourceCapacitiesSection section)
        {
            minValue = (decimal)section.DoubleMinValue.Value;
            maxValue = (decimal)section.DoubleMaxValue.Value;
        }
    }
}