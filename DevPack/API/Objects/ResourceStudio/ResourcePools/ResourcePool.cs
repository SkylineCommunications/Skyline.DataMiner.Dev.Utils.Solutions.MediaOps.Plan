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
        // Todo: add domain property?
        private StorageResourceStudio.ResourcepoolInstance originalInstance;

        private StorageResourceStudio.ResourcepoolInstance updatedInstance;

        private string name;

        private string iconImage;

        private string url;

        private readonly ICollection<LinkedResourcePool> linkedResourcepools = [];

        private readonly ICollection<ResourcePoolCapabilitySettings> capabilitySettings = [];

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
        }

        /// <summary>
        /// Gets or sets the name of the resource pool.
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
                HasChanges = true;
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
                HasChanges = true;
                url = value;
            }
        }

        /// <summary>
        /// Gets the collection of links associated with this resource pool.
        /// </summary>
        public IReadOnlyCollection<LinkedResourcePool> LinkedResourcePools => (IReadOnlyCollection<LinkedResourcePool>)linkedResourcepools;

        /// <summary>
        /// Gets the collection of capabilities assigned to this resource pool.
        /// </summary>
        public IReadOnlyCollection<ResourcePoolCapabilitySettings> Capabilities => (IReadOnlyCollection<ResourcePoolCapabilitySettings>)capabilitySettings;

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
            HasChanges = true;

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
            if (toRemove != null && linkedResourcepools.Remove(toRemove))
            {
                HasChanges = true;
            }

            return this;
        }

        /// <summary>
        /// Adds a new capability to the resource pool if it has not been previously added.
        /// </summary>
        /// <param name="capability">The capability settings to add. Must represent a new capability; otherwise, the method does not modify the collection.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="capability"/> is <see langword="null"/>.</exception>
        public ResourcePool AddCapability(ResourcePoolCapabilitySettings capability)
        {
            if (capability == null)
            {
                throw new ArgumentNullException(nameof(capability));
            }

            if (!capability.IsNew)
            {
                return this;
            }

            capabilitySettings.Add(capability);
            HasChanges = true;

            return this;
        }

        /// <summary>
        /// Removes the specified capability from the resource pool.
        /// </summary>
        /// <param name="capability">The capability to remove from the resource pool. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="capability"/> is <see langword="null"/>.</exception>
        public ResourcePool RemoveCapability(ResourcePoolCapabilitySettings capability)
        {
            if (capability == null)
            {
                throw new ArgumentNullException(nameof(capability));
            }

            var toRemove = capabilitySettings.SingleOrDefault(x => x.OriginalSection.ID == capability.OriginalSection.ID);
            if (toRemove != null && capabilitySettings.Remove(toRemove))
            {
                HasChanges = true;
            }

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
                    HasChanges = true;
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

            updatedInstance.ResourcePoolOther.IconImage = iconImage;
            updatedInstance.ResourcePoolOther.URL = url;

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
            State = EnumExtensions.MapEnum<StorageResourceStudio.SlcResource_StudioIds.Behaviors.Resourcepool_Behavior.StatusesEnum, ResourcePoolState>(instance.Status);
            coreResourcePoolId = instance.ResourcePoolInternalProperties.ResourcePoolId;

            iconImage = instance.ResourcePoolOther.IconImage;
            url = instance.ResourcePoolOther.URL;

            foreach (var section in instance.ResourcePoolLinks)
            {
                var link = new LinkedResourcePool(section);
                link.ValueChanged += (s, e) => { HasChanges = true; };
                linkedResourcepools.Add(link);
            }

            foreach (var section in instance.ResourcePoolCapabilities)
            {
                var capability = new ResourcePoolCapabilitySettings(section);
                capability.ValueChanged += (s, e) => { HasChanges = true; };
                capabilitySettings.Add(capability);
            }
        }
    }
}
