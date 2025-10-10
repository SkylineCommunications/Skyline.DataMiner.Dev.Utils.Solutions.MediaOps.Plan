namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.MediaOps.Plan.Extensions;

    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

    /// <summary>
    /// Represents a resource in the MediaOps Plan API.
    /// </summary>
    public abstract class Resource : ApiObject
    {
        private StorageResourceStudio.ResourceInstance originalInstance;

        private StorageResourceStudio.ResourceInstance updatedInstance;

        private string name;

        private bool isFavorite;

        private int concurrency;

        private Guid coreResourceId;

        private protected Resource() : base()
        {
            IsNew = true;

            SetDefaultValues();
        }

        private protected Resource(Guid resourceId) : base(resourceId)
        {
            IsNew = true;
            HasUserDefinedId = true;

            SetDefaultValues();
        }

        private protected Resource(StorageResourceStudio.ResourceInstance instance) : base(instance.ID.Id)
        {
            ParseInstance(instance);
        }

        /// <summary>
        /// Gets or sets the name of the resource.
        /// </summary>
        public override string Name
        {
            get => name;
            set
            {
                HasChanges |= !String.Equals(name, value);
                name = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the resource is a favorite.
        /// </summary>
        public bool IsFavorite
        {
            get => isFavorite;
            set
            {
                HasChanges |= isFavorite != value;
                isFavorite = value;
            }
        }

        /// <summary>
        /// Gets or sets the concurrency of the resource.
        /// </summary>
        public int Concurrency
        {
            get => concurrency;
            set
            {
                HasChanges |= concurrency != value;
                concurrency = value;
            }
        }

        /// <summary>
        /// Gets the state of the resource.
        /// </summary>
        public ResourceState State { get; private set; }

        internal abstract void ApplyChanges(StorageResourceStudio.ResourceInstance instance);

        internal static IEnumerable<Resource> InstantiateResources(IEnumerable<StorageResourceStudio.ResourceInstance> instances)
        {
            if (instances == null)
            {
                throw new ArgumentNullException(nameof(instances));
            }

            if (!instances.Any())
            {
                return [];
            }

            return InstantiateResourcesIterator(instances);
        }

        internal StorageResourceStudio.ResourceInstance OriginalInstance => originalInstance;

        internal Guid CoreResourceId => coreResourceId;

        internal StorageResourceStudio.ResourceInstance GetInstanceWithChanges()
        {
            if (updatedInstance == null)
            {
                updatedInstance = IsNew ? new StorageResourceStudio.ResourceInstance(Id) : originalInstance.Clone();
            }

            updatedInstance.ResourceInfo.Name = name;
            updatedInstance.ResourceInfo.Favorite = isFavorite;
            updatedInstance.ResourceInfo.Concurrency = concurrency;

            ApplyChanges(updatedInstance);

            return updatedInstance;
        }

        private static IEnumerable<Resource> InstantiateResourcesIterator(IEnumerable<StorageResourceStudio.ResourceInstance> instances)
        {
            foreach (var instance in instances)
            {
                if (!instance.ResourceInfo.Type.HasValue)
                {
                    continue;
                }

                switch (instance.ResourceInfo.Type.Value)
                {
                    case StorageResourceStudio.SlcResource_StudioIds.Enums.Type.Unmanaged: yield return new UnmanagedResource(instance); break;
                    case StorageResourceStudio.SlcResource_StudioIds.Enums.Type.Element: yield return new ElementResource(instance); break;
                    case StorageResourceStudio.SlcResource_StudioIds.Enums.Type.Service: yield return new ServiceResource(instance); break;
                    case StorageResourceStudio.SlcResource_StudioIds.Enums.Type.VirtualFunction: yield return new VirtualFunctionResource(instance); break;

                    default:
                        continue;
                }
            }
        }

        private void SetDefaultValues()
        {
            concurrency = 1;
        }

        private void ParseInstance(StorageResourceStudio.ResourceInstance instance)
        {
            this.originalInstance = instance ?? throw new ArgumentNullException(nameof(instance));

            name = instance.ResourceInfo.Name;
            isFavorite = instance.ResourceInfo.Favorite.HasValue ? instance.ResourceInfo.Favorite.Value : false;
            concurrency = instance.ResourceInfo.Concurrency.HasValue ? (int)instance.ResourceInfo.Concurrency.Value : 1;
            coreResourceId = instance.ResourceInternalProperties.Resource_Id ?? Guid.Empty;

            State = EnumExtensions.MapEnum<StorageResourceStudio.SlcResource_StudioIds.Behaviors.Resource_Behavior.StatusesEnum, ResourceState>(instance.Status);
        }

        // TODO: should we support this? OR should a user read the created/updated instances after pushing their changes?
        internal void UpdateInstance(StorageResourceStudio.ResourceInstance instance)
        {
            ParseInstance(instance);

            updatedInstance = null;
            HasChanges = false;
            IsNew = false;
        }
    }
}
