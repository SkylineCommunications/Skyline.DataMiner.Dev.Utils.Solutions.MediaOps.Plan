namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.Core.InterAppCalls.Common.Shared;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;

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

        private HashSet<Guid> assignedPoolIds = new HashSet<Guid>();

        private readonly ICollection<ResourcePropertySettings> propertySettings = [];

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

        /// <summary>
        /// Gets the collection of resource pool identifiers assigned to the resource.
        /// </summary>
        public IReadOnlyCollection<Guid> AssignedResourcePoolIds => (IReadOnlyCollection<Guid>)assignedPoolIds;

        /// <summary>
        /// Gets the collection of property configurations associated with this resource.
        /// </summary>
        public IReadOnlyCollection<ResourcePropertySettings> Properties => (IReadOnlyCollection<ResourcePropertySettings>)propertySettings;

        /// <summary>
        /// Assigns the current resource to the specified resource pool.
        /// </summary>
        /// <param name="resourcePool">The resource pool to which the resource will be assigned. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="resourcePool"/> is <see langword="null"/>.</exception>
        public void AssignToPool(ResourcePool resourcePool)
        {
            if (resourcePool == null)
            {
                throw new ArgumentNullException(nameof(resourcePool));
            }

            AssignToPool(resourcePool.Id);
        }

        /// <summary>
        /// Assigns the current resource to the specified resource pool.
        /// </summary>
        /// <param name="resourcePoolId">The unique identifier of the resource pool to which the resource will be assigned. Cannot be <see
        /// langword="Guid.Empty"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="resourcePoolId"/> is <see langword="Guid.Empty"/>.</exception>
        public void AssignToPool(Guid resourcePoolId)
        {
            if (resourcePoolId == Guid.Empty)
            {
                throw new ArgumentException(nameof(resourcePoolId));
            }

            assignedPoolIds.Add(resourcePoolId);
            HasChanges = true;
        }

        /// <summary>
        /// Configures the collection of resource pools to which this resource belongs.
        /// </summary>
        /// <param name="resourcePools">A collection of <see cref="ResourcePool"/> objects representing the resource pools to configure.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="resourcePools"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="resourcePools"/> contains a null element.</exception>
        public void SetPools(IEnumerable<ResourcePool> resourcePools)
        {
            if (resourcePools == null)
            {
                throw new ArgumentNullException(nameof(resourcePools));
            }

            if (resourcePools.Any(rp => rp == null))
            {
                throw new ArgumentException("The collection contains a null resource pool.", nameof(resourcePools));
            }

            SetPools(resourcePools.Select(rp => rp.Id));
        }

        /// <summary>
        /// Configures the collection of resource pools to which this resource belongs.
        /// </summary>
        /// <param name="resourcePoolIds">A collection of <see cref="Guid"/> values representing the resource pool IDs to assign.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="resourcePoolIds"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="resourcePoolIds"/> contains an empty <see cref="Guid"/> value.</exception>
        public void SetPools(IEnumerable<Guid> resourcePoolIds)
        {
            if (resourcePoolIds == null)
            {
                throw new ArgumentNullException(nameof(resourcePoolIds));
            }

            if (resourcePoolIds.Any(id => id == Guid.Empty))
            {
                throw new ArgumentException("The collection contains an empty resource pool ID.", nameof(resourcePoolIds));
            }

            assignedPoolIds = new HashSet<Guid>(resourcePoolIds);
            HasChanges = true;
        }

        /// <summary>
        /// Removes the current resource from the specified resource pool.
        /// </summary>
        /// <param name="resourcePool">The resource pool from which the resource will be unassigned. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="resourcePool"/> is <see langword="null"/>.</exception>
        public void UnassignFromPool(ResourcePool resourcePool)
        {
            if (resourcePool == null)
            {
                throw new ArgumentNullException(nameof(resourcePool));
            }

            UnassignFromPool(resourcePool.Id);
        }

        /// <summary>
        /// Removes the current resource from the specified resource pool.
        /// </summary>
        /// <param name="resourcePoolId">The unique identifier of the resource pool to unassign from this resource. Cannot be <see
        /// langword="Guid.Empty"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="resourcePoolId"/> is <see langword="Guid.Empty"/>.</exception>
        public void UnassignFromPool(Guid resourcePoolId)
        {
            if (resourcePoolId == Guid.Empty)
            {
                throw new ArgumentException(nameof(resourcePoolId));
            }

            if (assignedPoolIds.Remove(resourcePoolId))
            {
                HasChanges = true;
            }
        }

        /// <summary>
        /// Adds the specified property to the resource.
        /// </summary>
        /// <param name="property">The property to add.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="property"/> is <see langword="null"/>.</exception>
        public void AddProperty(ResourcePropertySettings property)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            if (!property.IsNew)
            {
                return;
            }
            
            propertySettings.Add(property);
            HasChanges = true;
        }

        /// <summary>
        /// Removes the specified property from the resource.
        /// </summary>
        /// <param name="property">The property to remove.</param>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="property"/> parameter is <see langword="null"/>.</exception>
        public void RemoveProperty(ResourcePropertySettings property)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            var toRemove = propertySettings.SingleOrDefault(x => x.OriginalSection.ID == property.OriginalSection.ID);
            if (toRemove != null && propertySettings.Remove(toRemove))
            {
                HasChanges = true;
            }
        }

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
            updatedInstance.ResourceInternalProperties.PoolIds = assignedPoolIds.ToList();

            updatedInstance.ResourceProperties.Clear();
            foreach (var property in propertySettings)
            {
                updatedInstance.ResourceProperties.Add(property.GetSectionWithChanges());
            }

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
            isFavorite = instance.ResourceInfo.Favorite ?? false;
            concurrency = instance.ResourceInfo.Concurrency.HasValue ? (int)instance.ResourceInfo.Concurrency.Value : 1;
            assignedPoolIds = new HashSet<Guid>(instance.ResourceInternalProperties.PoolIds);
            coreResourceId = instance.ResourceInternalProperties.Resource_Id ?? Guid.Empty;

            State = EnumExtensions.MapEnum<StorageResourceStudio.SlcResource_StudioIds.Behaviors.Resource_Behavior.StatusesEnum, ResourceState>(instance.Status);

            foreach (var section in instance.ResourceProperties)
            {
                var propertyConfiguration = new ResourcePropertySettings(section);
                propertyConfiguration.ValueChanged += (s, e) => { HasChanges = true; };
                propertySettings.Add(propertyConfiguration);
            }
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
