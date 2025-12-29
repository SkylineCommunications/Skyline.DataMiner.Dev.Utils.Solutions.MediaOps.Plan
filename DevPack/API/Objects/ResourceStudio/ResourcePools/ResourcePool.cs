namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;

    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

    /// <summary>
    /// Represents a resource pool in the MediaOps.
    /// </summary>
    public class ResourcePool : ApiObject
    {
        private readonly List<LinkedResourcePool> linkedResourcepools = [];
        private readonly List<ResourcePoolCapabilitySetting> capabilitySettings = [];

        private StorageResourceStudio.ResourcepoolInstance originalInstance;
        private StorageResourceStudio.ResourcepoolInstance updatedInstance;

        private string name;
        private bool isExternallyManaged;
        private string iconImage;
        private string url;
        private string categoryId;
        private Guid coreResourcePoolId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourcePool"/> class.
        /// </summary>
        public ResourcePool() : base()
        {
            IsNew = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourcePool"/> class with a specific resource pool ID.
        /// </summary>
        /// <param name="resourcePoolId">The unique identifier of the resource pool.</param>
        public ResourcePool(Guid resourcePoolId) : base(resourcePoolId)
        {
            IsNew = true;
            HasUserDefinedId = true;
        }

        internal ResourcePool(StorageResourceStudio.ResourcepoolInstance instance) : base(instance.ID.Id)
        {
            ParseInstance(instance);
            InitTracking();
        }

        /// <summary>
        /// Gets or sets the name of the resource pool.
        /// </summary>
        public override string Name
        {
            get => name;
            set
            {
                name = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the resource pool is managed by an external system.
        /// </summary>
        public bool IsExternallyManaged
        {
            get => isExternallyManaged;
            set
            {
                isExternallyManaged = value;
            }
        }

        /// <summary>
        /// Gets the state of the resource pool.
        /// </summary>
        public ResourcePoolState State { get; private set; }

        /// <summary>
        /// Gets or sets the icon of the resource pool.
        /// </summary>
        public string IconImage
        {
            get => iconImage;
            set
            {
                iconImage = value;
            }
        }

        /// <summary>
        /// Gets or sets the URL of the resource pool.
        /// </summary>
        public string Url
        {
            get => url;
            set
            {
                url = value;
            }
        }

        /// <summary>
        /// Gets or sets the unique identifier of the associated category.
        /// </summary>
        public string CategoryId
        {
            get => categoryId;
            set
            {
                categoryId = value;
            }
        }

        /// <summary>
        /// Gets the collection of links associated with this resource pool.
        /// </summary>
        public IReadOnlyCollection<LinkedResourcePool> LinkedResourcePools => linkedResourcepools;

        /// <summary>
        /// Gets the collection of capabilities assigned to this resource pool.
        /// </summary>
        public IReadOnlyCollection<CapabilitySetting> Capabilities => capabilitySettings;

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 23) + Id.GetHashCode();
                hash = (hash * 23) + (Name != null ? Name.GetHashCode() : 0);
                hash = (hash * 23) + IsExternallyManaged.GetHashCode();
                hash = (hash * 23) + (IconImage != null ? iconImage.GetHashCode() : 0);
                hash = (hash * 23) + (Url != null ? Url.GetHashCode() : 0);
                hash = (hash * 23) + (CategoryId != null ? CategoryId.GetHashCode() : 0);
                hash = (hash * 23) + State.GetHashCode();

                foreach (var linkedResourcePool in LinkedResourcePools.OrderBy(x => x.LinkedResourcePoolId))
                {
                    hash = (hash * 23) + linkedResourcePool.GetHashCode();
                }

                foreach (var setting in Capabilities.OrderBy(x => x.Id))
                {
                    hash = (hash * 23) + setting.GetHashCode();
                }

                return hash;
            }
        }

        internal Guid CoreResourcePoolId => coreResourcePoolId;

        internal StorageResourceStudio.ResourcepoolInstance OriginalInstance => originalInstance;

        /// <summary>
        /// Adds a link to another resource pool.
        /// </summary>
        /// <param name="linkedResourcePool">The resource pool link to add.</param>
        public ResourcePool AddLinkedResourcePool(LinkedResourcePool linkedResourcePool)
        {
            if (linkedResourcePool == null)
            {
                throw new ArgumentNullException(nameof(linkedResourcePool));
            }

            if (!linkedResourcePool.IsNew)
            {
                return this;
            }

            linkedResourcepools.Add(linkedResourcePool);

            return this;
        }

        /// <summary>
        /// Removes the specified resource pool link from the collection, if it exists.
        /// </summary>
        /// <param name="linkedResourcePool">The resource pool link to remove.</param>
        public ResourcePool RemoveLinkedResourcePool(LinkedResourcePool linkedResourcePool)
        {
            if (linkedResourcePool == null)
            {
                throw new ArgumentNullException(nameof(linkedResourcePool));
            }

            var toRemove = linkedResourcepools.SingleOrDefault(x => x.OriginalSection.ID == linkedResourcePool.OriginalSection.ID);
            if (toRemove == null)
            {
                return this;
            }

            linkedResourcepools.Remove(linkedResourcePool);
            return this;
        }

        /// <summary>
        /// Adds a new capability to the resource pool.
        /// </summary>
        /// <param name="capabilitySetting">The capability setting to add.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="capabilitySetting"/> is <see langword="null"/>.</exception>
        public ResourcePool AddCapability(CapabilitySetting capabilitySetting)
        {
            if (capabilitySetting == null)
            {
                throw new ArgumentNullException(nameof(capabilitySetting));
            }

            capabilitySettings.Add(new ResourcePoolCapabilitySetting(capabilitySetting));

            return this;
        }

        /// <summary>
        /// Removes the specified capability from the resource pool.
        /// </summary>
        /// <param name="capabilitySetting">The capability to remove from the resource pool. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="capabilitySetting"/> is <see langword="null"/>.</exception>
        public ResourcePool RemoveCapability(CapabilitySetting capabilitySetting)
        {
            if (capabilitySetting == null)
            {
                throw new ArgumentNullException(nameof(capabilitySetting));
            }

            if (capabilitySetting.OriginalSection == null)
            {
                return this;
            }

            var toRemove = capabilitySettings.SingleOrDefault(x => x.OriginalSection.ID == capabilitySetting.OriginalSection.ID);
            if (toRemove == null)
            {
                return this;
            }

            capabilitySettings.Remove(toRemove);
            return this;
        }

        internal ResourcePool RemoveLinkedResourcePool(ResourcePool resourcePool)
        {
            if (resourcePool == null)
            {
                throw new ArgumentNullException(nameof(resourcePool));
            }

            if (resourcePool.IsNew)
            {
                return this;
            }

            var toRemove = linkedResourcepools.Where(x => x.LinkedResourcePoolId == resourcePool.Id).ToList();
            if (toRemove.Count > 0)
            {
                foreach (var item in toRemove)
                {
                    linkedResourcepools.Remove(item);
                }
            }

            return this;
        }

        internal StorageResourceStudio.ResourcepoolInstance GetInstanceWithChanges()
        {
            if (updatedInstance == null)
            {
                updatedInstance = IsNew ? new StorageResourceStudio.ResourcepoolInstance(Id) : originalInstance.Clone();
            }

            updatedInstance.ResourcePoolInfo.Name = Name;
            updatedInstance.ResourcePoolInfo.Category = categoryId;
            updatedInstance.ResourcePoolOther.IconImage = iconImage;
            updatedInstance.ResourcePoolOther.URL = url;

            // Setting to null will not create a DOM section in storage.
            updatedInstance.ExternalMetadata.ExternallyManaged = IsExternallyManaged ? true : null;

            updatedInstance.ResourcePoolLinks.Clear();
            foreach (var link in linkedResourcepools)
            {
                updatedInstance.ResourcePoolLinks.Add(link.GetSectionWithChanges());
            }

            updatedInstance.ResourcePoolCapabilities.Clear();
            foreach (var capability in capabilitySettings)
            {
                updatedInstance.ResourcePoolCapabilities.Add(capability.GetSectionWithChanges());
            }

            return updatedInstance;
        }

        private void ParseInstance(StorageResourceStudio.ResourcepoolInstance instance)
        {
            this.originalInstance = instance ?? throw new ArgumentNullException(nameof(instance));

            name = instance.ResourcePoolInfo.Name;
            categoryId = instance.ResourcePoolInfo.Category;
            isExternallyManaged = instance.ExternalMetadata?.ExternallyManaged ?? false;
            State = EnumExtensions.MapEnum<StorageResourceStudio.SlcResource_StudioIds.Behaviors.Resourcepool_Behavior.StatusesEnum, ResourcePoolState>(instance.Status);
            coreResourcePoolId = instance.ResourcePoolInternalProperties.ResourcePoolId;

            iconImage = instance.ResourcePoolOther.IconImage;
            url = instance.ResourcePoolOther.URL;

            foreach (var section in instance.ResourcePoolLinks)
            {
                var link = new LinkedResourcePool(section);
                linkedResourcepools.Add(link);
            }

            foreach (var section in instance.ResourcePoolCapabilities)
            {
                var capability = new ResourcePoolCapabilitySetting(section);
                capabilitySettings.Add(capability);
            }
        }
    }
}
