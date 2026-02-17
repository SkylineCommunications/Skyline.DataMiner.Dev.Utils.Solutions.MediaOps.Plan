namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

    /// <summary>
    /// Represents the settings for a resource property.
    /// </summary>
    public class ResourcePropertySettings : TrackableObject
    {
        private readonly StorageResourceStudio.ResourcePropertiesSection originalSection;

        private StorageResourceStudio.ResourcePropertiesSection updatedSection;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourcePropertySettings"/> class using the specified resource property.
        /// </summary>
        /// <param name="resourceProperty">The resource property to use for configuration. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="resourceProperty"/> is <see langword="null"/>.</exception>
        public ResourcePropertySettings(ResourceProperty resourceProperty)
            : this(resourceProperty?.ID ?? throw new ArgumentNullException(nameof(resourceProperty)))
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

        internal ResourcePropertySettings(ResourcePropertySettings resourcePropertySettings)
        {
            Id = resourcePropertySettings.Id;
            Value = resourcePropertySettings.Value;

            IsNew = true;
        }

        internal ResourcePropertySettings(StorageResourceStudio.ResourcePropertiesSection section)
        {
            originalSection = section ?? throw new ArgumentNullException(nameof(section));

            ParseSection();
            InitTracking();
        }

        /// <summary>
        /// Gets the unique identifier of the resource property.
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Gets or sets the property value.
        /// </summary>
        public string Value { get; set; }

        internal StorageResourceStudio.ResourcePropertiesSection OriginalSection => originalSection;

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 23) + Id.GetHashCode();
                hash = (hash * 23) + (Value != null ? Value.GetHashCode() : 0);
                return hash;
            }
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current ResourcePropertySettings instance.
        /// </summary>
        /// <param name="obj">The object to compare with the current ResourcePropertySettings instance.</param>
        /// <returns>true if the specified object is a ResourcePropertySettings instance and has the same Id and Value as the
        /// current instance; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (obj is not ResourcePropertySettings other)
            {
                return false;
            }

            return Id == other.Id && Value == other.Value;
        }

        internal StorageResourceStudio.ResourcePropertiesSection GetSectionWithChanges()
        {
            if (updatedSection == null)
            {
                updatedSection = IsNew ? new StorageResourceStudio.ResourcePropertiesSection() : originalSection.Clone();
            }

            updatedSection.Property = Id;
            updatedSection.PropertyValue = Value;

            return updatedSection;
        }

        private void ParseSection()
        {
            Id = originalSection.Property.HasValue ? originalSection.Property.Value : Guid.Empty;
            Value = originalSection.PropertyValue;
        }
    }
}
