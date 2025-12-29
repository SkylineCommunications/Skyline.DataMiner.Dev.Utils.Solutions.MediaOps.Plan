namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

    /// <summary>
    /// Represents a resource property in the MediaOps Plan API.
    /// </summary>
    public class ResourceProperty : ApiObject
    {
        private readonly StorageResourceStudio.ResourcepropertyInstance originalInstance;

        private StorageResourceStudio.ResourcepropertyInstance updatedInstance;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceProperty"/> class.
        /// </summary>
        public ResourceProperty() : base()
        {
            IsNew = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceProperty"/> class with a specific resource property ID.
        /// </summary>
        /// <param name="resourcePropertyId">The unique identifier of the resource property.</param>
        public ResourceProperty(Guid resourcePropertyId) : base(resourcePropertyId)
        {
            IsNew = true;
            HasUserDefinedId = true;
        }

        internal ResourceProperty(StorageResourceStudio.ResourcepropertyInstance instance) : base(instance.ID.Id)
        {
            originalInstance = instance ?? throw new ArgumentNullException(nameof(instance));

            ParseInstance();
            InitTracking();
        }

        /// <summary>
        /// Gets or sets the name of the resource property.
        /// </summary>
        public override string Name { get; set; }

        internal StorageResourceStudio.ResourcepropertyInstance OriginalInstance => originalInstance;

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 23) + Id.GetHashCode();
                hash = (hash * 23) + (Name != null ? Name.GetHashCode() : 0);
                return hash;
            }
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current ResourceProperty instance.
        /// </summary>
        /// <param name="obj">The object to compare with the current ResourceProperty instance.</param>
        /// <returns>true if the specified object is a ResourceProperty and has the same Id and Name as the current instance;
        /// otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (obj is not ResourceProperty other)
            {
                return false;
            }

            return Id == other.Id && Name == other.Name;
        }

        internal StorageResourceStudio.ResourcepropertyInstance GetInstanceWithChanges()
        {
            if (updatedInstance == null)
            {
                updatedInstance = IsNew ? new StorageResourceStudio.ResourcepropertyInstance(Id) : originalInstance.Clone();
            }

            updatedInstance.PropertyInfo.PropertyName = Name;

            return updatedInstance;
        }

        private void ParseInstance()
        {
            Name = OriginalInstance.PropertyInfo.PropertyName;
        }
    }
}
