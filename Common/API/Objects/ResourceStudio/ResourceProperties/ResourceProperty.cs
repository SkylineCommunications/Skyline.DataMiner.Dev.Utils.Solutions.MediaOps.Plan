namespace Skyline.DataMiner.MediaOps.Plan.API
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
        }

        /// <summary>
        /// Gets or sets the name of the resource property.
        /// </summary>
        public override string Name
        {
            get => name;
            set
            {
                HasChanges = true;
                name = value;
            }
        }

        internal StorageResourceStudio.ResourcepropertyInstance OriginalInstance => originalInstance;

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
            HasChanges = false;
            IsNew = false;
        }

        private void ParseInstance(StorageResourceStudio.ResourcepropertyInstance instance)
        {
            this.originalInstance = instance ?? throw new ArgumentNullException(nameof(instance));

            name = instance.PropertyInfo.PropertyName;
        }
    }
}
