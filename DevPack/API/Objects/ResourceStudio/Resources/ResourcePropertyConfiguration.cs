namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

    /// <summary>
    /// Represents the configuration for a resource property.
    /// </summary>
    public class ResourcePropertyConfiguration : TrackableObject
    {
        private StorageResourceStudio.ResourcePropertiesSection originalSection;

        private StorageResourceStudio.ResourcePropertiesSection updatedSection;

        private string value;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourcePropertyConfiguration"/> class using the specified resource property.
        /// </summary>
        /// <param name="resourceProperty">The resource property to use for configuration. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if resourceProperty is null.</exception>
        public ResourcePropertyConfiguration(ResourceProperty resourceProperty)
            : this(resourceProperty?.Id ?? throw new ArgumentNullException(nameof(resourceProperty)))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourcePropertyConfiguration"/> class with the specified resource property ID.
        /// </summary>
        /// <param name="resourcePropertyId">The unique identifier for the resource property. Must not be an empty GUID.</param>
        /// <exception cref="ArgumentException">Thrown if resourcePropertyId is an empty GUID.</exception>
        public ResourcePropertyConfiguration(Guid resourcePropertyId)
        {
            if (resourcePropertyId == Guid.Empty)
            {
                throw new ArgumentException(nameof(resourcePropertyId));
            }

            Id = resourcePropertyId;

            IsNew = true;
        }

        internal ResourcePropertyConfiguration(StorageResourceStudio.ResourcePropertiesSection section)
        {
            ParseSection(section);
        }

        internal EventHandler<EventArgs> ValueChanged;

        /// <summary>
        /// Gets the unique identifier of the resource property.
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Gets or sets the property value.
        /// </summary>
        public string Value
        {
            get => value;
            set
            {
                HasChanges = true;
                ValueChanged?.Invoke(this, EventArgs.Empty);
                this.value = value;
            }
        }

        internal StorageResourceStudio.ResourcePropertiesSection OriginalSection => originalSection;

        internal StorageResourceStudio.ResourcePropertiesSection GetSectionWithChanges()
        {
            if (updatedSection == null)
            {
                updatedSection = IsNew ? new StorageResourceStudio.ResourcePropertiesSection() : originalSection.Clone();
            }

            updatedSection.Property = Id;
            updatedSection.PropertyValue = value;

            return updatedSection;
        }

        private void ParseSection(StorageResourceStudio.ResourcePropertiesSection section)
        {
            originalSection = section ?? throw new ArgumentNullException(nameof(section));

            Id = originalSection.Property.Value;
            value = originalSection.PropertyValue;
        }
    }
}
