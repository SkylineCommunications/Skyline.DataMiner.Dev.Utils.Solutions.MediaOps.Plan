namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

    /// <summary>
    /// Represents a resource property in the MediaOps Plan API.
    /// </summary>
    public class ResourceProperty : ApiObject
    {
        private StorageResourceStudio.ResourcepropertyInstance originalInstance;
        private StorageResourceStudio.ResourcepropertyInstance updatedInstance;
        private string name;

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
            ParseInstance(instance);
            InitTracking();
        }

        /// <summary>
        /// Gets or sets the name of the resource property.
        /// </summary>
        public override string Name
        {
            get => name;
            set
            {
                name = value;
            }
        }

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

        internal StorageResourceStudio.ResourcepropertyInstance GetInstanceWithChanges()
        {
            if (updatedInstance == null)
            {
                updatedInstance = IsNew ? new StorageResourceStudio.ResourcepropertyInstance(Id) : originalInstance.Clone();
            }

            updatedInstance.PropertyInfo.PropertyName = name;

            return updatedInstance;
        }

        internal void UpdateInstance(StorageResourceStudio.ResourcepropertyInstance instance)
        {
            ParseInstance(instance);

            updatedInstance = null;
            IsNew = false;
        }

        private void ParseInstance(StorageResourceStudio.ResourcepropertyInstance instance)
        {
            this.originalInstance = instance ?? throw new ArgumentNullException(nameof(instance));

            name = instance.PropertyInfo.PropertyName;
        }
    }
}
