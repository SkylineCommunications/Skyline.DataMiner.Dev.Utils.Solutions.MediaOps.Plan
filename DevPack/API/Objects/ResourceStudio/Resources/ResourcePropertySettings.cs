namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

    /// <summary>
    /// Represents the settings for a resource property.
    /// </summary>
    public class ResourcePropertySettings : TrackableObject
    {
        private StorageResourceStudio.ResourcePropertiesSection originalSection;

        private StorageResourceStudio.ResourcePropertiesSection updatedSection;

        private string value;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourcePropertySettings"/> class using the specified resource property.
        /// </summary>
        /// <param name="resourceProperty">The resource property to use for configuration. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="resourceProperty"/> is <see langword="null"/>.</exception>
        public ResourcePropertySettings(ResourceProperty resourceProperty)
            : this(resourceProperty?.Id ?? throw new ArgumentNullException(nameof(resourceProperty)))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourcePropertySettings"/> class with the specified resource property ID.
        /// </summary>
        /// <param name="resourcePropertyId">The unique identifier for the resource property. Must not be an empty GUID.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="resourcePropertyId"/> is an empty GUID.</exception>
        public ResourcePropertySettings(Guid resourcePropertyId)
        {
            if (resourcePropertyId == Guid.Empty)
            {
                throw new ArgumentException(nameof(resourcePropertyId));
            }

            Id = resourcePropertyId;

            IsNew = true;
        }

        internal ResourcePropertySettings(StorageResourceStudio.ResourcePropertiesSection section)
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
                this.value = value;
                HasChanges = true;
                ValueChanged?.Invoke(this, EventArgs.Empty);
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

            Id = section.Property.Value;
            value = section.PropertyValue;
        }
    }
}
