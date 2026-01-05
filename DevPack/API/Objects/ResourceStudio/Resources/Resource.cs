namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;

    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

    /// <summary>
    /// Represents a resource in the MediaOps Plan API.
    /// </summary>
    public abstract class Resource : ApiObject
    {
        private StorageResourceStudio.ResourceInstance originalInstance;

        private StorageResourceStudio.ResourceInstance updatedInstance;

        private Guid coreResourceId;

        private HashSet<Guid> resourcePoolIds = new HashSet<Guid>();

        private readonly List<ResourceCapabilitySetting> capabilitySettings = [];

        private readonly List<ResourceNumberCapacitySetting> numberCapacitySettings = [];

        private readonly List<ResourceRangeCapacitySetting> rangeCapacitySettings = [];

        private readonly List<ResourcePropertySettings> propertySettings = [];

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

        private protected Resource(MediaOpsPlanApi planApi, StorageResourceStudio.ResourceInstance instance) : base(instance.ID.Id)
        {
            ParseInstance(planApi, instance);
        }

        /// <summary>
        /// Gets or sets the name of the resource.
        /// </summary>
        public override string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the resource is a favorite.
        /// </summary>
        public bool IsFavorite { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the resource is managed by an external system.
        /// </summary>
        public bool IsExternallyManaged { get; set; }

        /// <summary>
        /// Gets or sets the concurrency of the resource.
        /// </summary>
        public int Concurrency { get; set; }

        /// <summary>
        /// Gets the state of the resource.
        /// </summary>
        public ResourceState State { get; private set; }

        /// <summary>
        /// Gets or sets the icon of the resource.
        /// </summary>
        public string IconImage { get; set; }

        /// <summary>
        /// Gets or sets the URL of the resource.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets the collection of resource pool identifiers assigned to the resource.
        /// </summary>
        public IReadOnlyCollection<Guid> ResourcePoolIds => resourcePoolIds;

        /// <summary>
        /// Gets the collection of capabilities assigned to this resource.
        /// </summary>
        public IReadOnlyCollection<CapabilitySetting> Capabilities => capabilitySettings;

        /// <summary>
        /// Gets the collection of capacities assigned to this resource.
        /// </summary>
        public IReadOnlyCollection<CapacitySetting> Capacities => numberCapacitySettings.Concat<CapacitySetting>(rangeCapacitySettings).ToList();

        /// <summary>
        /// Gets the collection of property settings associated with this resource.
        /// </summary>
        public IReadOnlyCollection<ResourcePropertySettings> Properties => propertySettings;

        /// <summary>
        /// Gets or sets the unique identifier for the Live virtual signal group input associated with the resource.
        /// </summary>
        public Guid VirtualSignalGroupInputId { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier for the Live virtual signal group output associated with the resource.
        /// </summary>
        public Guid VirtualSignalGroupOutputId { get; set; }

        /// <summary>
        /// Assigns the current resource to the specified resource pool.
        /// </summary>
        /// <param name="resourcePool">The resource pool to which the resource will be assigned. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="resourcePool"/> is <see langword="null"/>.</exception>
        public Resource AssignToPool(ResourcePool resourcePool)
        {
            if (resourcePool == null)
            {
                throw new ArgumentNullException(nameof(resourcePool));
            }

            return AssignToPool(resourcePool.Id);
        }

        /// <summary>
        /// Assigns the current resource to the specified resource pool.
        /// </summary>
        /// <param name="resourcePoolId">The unique identifier of the resource pool to which the resource will be assigned. Cannot be <see
        /// langword="Guid.Empty"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="resourcePoolId"/> is <see langword="Guid.Empty"/>.</exception>
        public Resource AssignToPool(Guid resourcePoolId)
        {
            if (resourcePoolId == Guid.Empty)
            {
                throw new ArgumentException(nameof(resourcePoolId));
            }

            resourcePoolIds.Add(resourcePoolId);
            return this;
        }

        /// <summary>
        /// Configures the collection of resource pools to which this resource belongs.
        /// </summary>
        /// <param name="resourcePools">A collection of <see cref="ResourcePool"/> objects representing the resource pools to configure.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="resourcePools"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="resourcePools"/> contains a null element.</exception>
        public Resource SetPools(IEnumerable<ResourcePool> resourcePools)
        {
            if (resourcePools == null)
            {
                throw new ArgumentNullException(nameof(resourcePools));
            }

            if (resourcePools.Any(rp => rp == null))
            {
                throw new ArgumentException("The collection contains a null resource pool.", nameof(resourcePools));
            }

            return SetPools(resourcePools.Select(rp => rp.Id));
        }

        /// <summary>
        /// Configures the collection of resource pools to which this resource belongs.
        /// </summary>
        /// <param name="resourcePoolIds">A collection of <see cref="Guid"/> values representing the resource pool IDs to assign.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="resourcePoolIds"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="resourcePoolIds"/> contains an empty <see cref="Guid"/> value.</exception>
        public Resource SetPools(IEnumerable<Guid> resourcePoolIds)
        {
            if (resourcePoolIds == null)
            {
                throw new ArgumentNullException(nameof(resourcePoolIds));
            }

            if (resourcePoolIds.Any(id => id == Guid.Empty))
            {
                throw new ArgumentException("The collection contains an empty resource pool ID.", nameof(resourcePoolIds));
            }

            this.resourcePoolIds = new HashSet<Guid>(resourcePoolIds);
            return this;
        }

        /// <summary>
        /// Removes the current resource from the specified resource pool.
        /// </summary>
        /// <param name="resourcePool">The resource pool from which the resource will be unassigned. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="resourcePool"/> is <see langword="null"/>.</exception>
        public Resource UnassignFromPool(ResourcePool resourcePool)
        {
            if (resourcePool == null)
            {
                throw new ArgumentNullException(nameof(resourcePool));
            }

            return UnassignFromPool(resourcePool.Id);
        }

        /// <summary>
        /// Removes the current resource from the specified resource pool.
        /// </summary>
        /// <param name="resourcePoolId">The unique identifier of the resource pool to unassign from this resource. Cannot be <see
        /// langword="Guid.Empty"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="resourcePoolId"/> is <see langword="Guid.Empty"/>.</exception>
        public Resource UnassignFromPool(Guid resourcePoolId)
        {
            if (resourcePoolId == Guid.Empty)
            {
                throw new ArgumentException(nameof(resourcePoolId));
            }

            resourcePoolIds.Remove(resourcePoolId);
            return this;
        }

        /// <summary>
        /// Adds a new capability to the resource.
        /// </summary>
        /// <param name="capabilitySetting">The capability setting to add.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="capabilitySetting"/> is <see langword="null"/>.</exception>
        public Resource AddCapability(CapabilitySetting capabilitySetting)
        {
            if (capabilitySetting == null)
            {
                throw new ArgumentNullException(nameof(capabilitySetting));
            }

            capabilitySettings.Add(new ResourceCapabilitySetting(capabilitySetting));
            return this;
        }

        /// <summary>
        /// Removes the specified capability from the resource.
        /// </summary>
        /// <param name="capabilitySetting">The capability to remove from the resource. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="capabilitySetting"/> is <see langword="null"/>.</exception>
        public Resource RemoveCapability(CapabilitySetting capabilitySetting)
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

        /// <summary>
        /// Adds a new capacity to the resource.
        /// </summary>
        /// <param name="capacitySetting">The capacity settings to add.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="capacitySetting"/> is <see langword="null"/>.</exception>
        public Resource AddCapacity(CapacitySetting capacitySetting)
        {
            if (capacitySetting == null)
            {
                throw new ArgumentNullException(nameof(capacitySetting));
            }

            if (capacitySetting is NumberCapacitySetting numberCapacity)
            {
                numberCapacitySettings.Add(new ResourceNumberCapacitySetting(numberCapacity));
            }
            else if (capacitySetting is RangeCapacitySetting rangeCapacity)
            {
                rangeCapacitySettings.Add(new ResourceRangeCapacitySetting(rangeCapacity));
            }
            else
            {
                throw new ArgumentException("The capacity setting type is not supported.", nameof(capacitySetting));
            }

            return this;
        }

        /// <summary>
        /// Removes the specified capacity from the resource.
        /// </summary>
        /// <param name="capacitySetting">The capacity to remove from the resource. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="capacitySetting"/> is <see langword="null"/>.</exception>
        public Resource RemoveCapacity(CapacitySetting capacitySetting)
        {
            if (capacitySetting == null)
            {
                throw new ArgumentNullException(nameof(capacitySetting));
            }

            if (capacitySetting.OriginalSection == null)
            {
                return this;
            }

            if (capacitySetting is NumberCapacitySetting)
            {
                var toRemoveNumber = numberCapacitySettings.SingleOrDefault(x => x.OriginalSection.ID == capacitySetting.OriginalSection.ID);
                if (toRemoveNumber != null)
                {
                    numberCapacitySettings.Remove(toRemoveNumber);
                }
            }
            else if (capacitySetting is RangeCapacitySetting)
            {
                var toRemoveRange = rangeCapacitySettings.SingleOrDefault(x => x.OriginalSection.ID == capacitySetting.OriginalSection.ID);
                if (toRemoveRange != null)
                {
                    rangeCapacitySettings.Remove(toRemoveRange);
                }
            }

            return this;
        }

        /// <summary>
        /// Adds the specified property to the resource.
        /// </summary>
        /// <param name="property">The property to add.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="property"/> is <see langword="null"/>.</exception>
        public Resource AddProperty(ResourcePropertySettings property)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            if (!property.IsNew)
            {
                return this;
            }

            propertySettings.Add(property);
            return this;
        }

        /// <summary>
        /// Removes the specified property from the resource.
        /// </summary>
        /// <param name="property">The property to remove.</param>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="property"/> parameter is <see langword="null"/>.</exception>
        public Resource RemoveProperty(ResourcePropertySettings property)
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            var toRemove = propertySettings.SingleOrDefault(x => x.OriginalSection.ID == property.OriginalSection.ID);
            if (toRemove != null)
            {
                propertySettings.Remove(toRemove);
            }

            return this;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 23) + Id.GetHashCode();
                hash = (hash * 23) + (Name != null ? Name.GetHashCode() : 0);
                hash = (hash * 23) + IsExternallyManaged.GetHashCode();
                hash = (hash * 23) + (IconImage != null ? IconImage.GetHashCode() : 0);
                hash = (hash * 23) + (Url != null ? Url.GetHashCode() : 0);
                hash = (hash * 23) + Concurrency.GetHashCode();
                hash = (hash * 23) + State.GetHashCode();
                hash = (hash * 23) + IsFavorite.GetHashCode();
                hash = (hash * 23) + VirtualSignalGroupInputId.GetHashCode();
                hash = (hash * 23) + VirtualSignalGroupOutputId.GetHashCode();

                foreach (var poolId in ResourcePoolIds.OrderBy(x => x).ToArray())
                {
                    hash = (hash * 23) + poolId.GetHashCode();
                }

                foreach (var setting in Capabilities.OrderBy(x => x.Id).ToArray())
                {
                    hash = (hash * 23) + setting.GetHashCode();
                }

                foreach (var setting in Capacities.OrderBy(x => x.Id).ToArray())
                {
                    hash = (hash * 23) + setting.GetHashCode();
                }

                foreach (var setting in Properties.OrderBy(x => x.Id).ToArray())
                {
                    hash = (hash * 23) + setting.GetHashCode();
                }

                return hash;
            }
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current Resource instance.
        /// </summary>
        /// <remarks>Equality is determined by comparing the values of all significant properties and
        /// collections of the Resource. This method performs a deep comparison, including the contents of collections
        /// such as Capabilities, Capacities, Properties, and resourcePoolIds.</remarks>
        /// <param name="obj">The object to compare with the current Resource instance.</param>
        /// <returns>true if the specified object is a Resource and has the same values for all relevant properties and
        /// collections; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (obj is not Resource other)
            {
                return false;
            }

            if (Id != other.Id ||
                Name != other.Name ||
                IsExternallyManaged != other.IsExternallyManaged ||
                IconImage != other.IconImage ||
                Url != other.Url ||
                Concurrency != other.Concurrency ||
                State != other.State ||
                IsFavorite != other.IsFavorite ||
                VirtualSignalGroupInputId != other.VirtualSignalGroupInputId ||
                VirtualSignalGroupOutputId != other.VirtualSignalGroupOutputId)
            {
                return false;
            }

            if (!resourcePoolIds.SetEquals(other.resourcePoolIds))
            {
                return false;
            }

            if (Capabilities.Count != other.Capabilities.Count ||
                Capacities.Count != other.Capacities.Count ||
                Properties.Count != other.Properties.Count)
            {
                return false;
            }

            foreach (var capability in Capabilities)
            {
                if (!other.Capabilities.Contains(capability))
                {
                    return false;
                }
            }

            foreach (var capacity in Capacities)
            {
                if (!other.Capacities.Contains(capacity))
                {
                    return false;
                }
            }

            foreach (var property in Properties)
            {
                if (!other.Properties.Contains(property))
                {
                    return false;
                }
            }

            return true;
        }

        internal abstract void ApplyChanges(StorageResourceStudio.ResourceInstance instance);

        internal static IEnumerable<Resource> InstantiateResources(MediaOpsPlanApi planApi, IEnumerable<StorageResourceStudio.ResourceInstance> instances)
        {
            if (planApi == null)
            {
                throw new ArgumentNullException(nameof(planApi));
            }

            if (instances == null)
            {
                throw new ArgumentNullException(nameof(instances));
            }

            if (!instances.Any())
            {
                return [];
            }

            return InstantiateResourcesIterator(planApi, instances);
        }

        internal StorageResourceStudio.ResourceInstance OriginalInstance => originalInstance;

        internal Guid CoreResourceId => coreResourceId;

        internal StorageResourceStudio.ResourceInstance GetInstanceWithChanges()
        {
            if (updatedInstance == null)
            {
                updatedInstance = IsNew ? new StorageResourceStudio.ResourceInstance(Id) : originalInstance.Clone();
            }

            updatedInstance.ResourceInfo.Name = Name;
            updatedInstance.ResourceInfo.Favorite = IsFavorite;
            updatedInstance.ResourceInfo.Concurrency = Concurrency;
            updatedInstance.ResourceInternalProperties.PoolIds = resourcePoolIds.ToList();
            updatedInstance.ResourceOther.IconImage = IconImage;
            updatedInstance.ResourceOther.URL = Url;

            // Setting to null will not create a DOM section in storage.
            updatedInstance.ExternalMetadata.ExternallyManaged = IsExternallyManaged ? true : null;

            updatedInstance.ResourceConnectionManagement.VirtualSignalGroupInputId = VirtualSignalGroupInputId;
            updatedInstance.ResourceConnectionManagement.VirtualSignalGroupOutputId = VirtualSignalGroupOutputId;

            updatedInstance.ResourceCapabilities.Clear();
            foreach (var capability in capabilitySettings)
            {
                updatedInstance.ResourceCapabilities.Add(capability.GetSectionWithChanges());
            }

            updatedInstance.ResourceCapacities.Clear();
            foreach (var capacity in numberCapacitySettings)
            {
                updatedInstance.ResourceCapacities.Add(capacity.GetSectionWithChanges());
            }
            foreach (var capacity in rangeCapacitySettings)
            {
                updatedInstance.ResourceCapacities.Add(capacity.GetSectionWithChanges());
            }

            updatedInstance.ResourceProperties.Clear();
            foreach (var property in propertySettings)
            {
                updatedInstance.ResourceProperties.Add(property.GetSectionWithChanges());
            }

            ApplyChanges(updatedInstance);

            return updatedInstance;
        }

        private static IEnumerable<Resource> InstantiateResourcesIterator(MediaOpsPlanApi planApi, IEnumerable<StorageResourceStudio.ResourceInstance> instances)
        {
            foreach (var instance in instances)
            {
                if (!instance.ResourceInfo.Type.HasValue)
                {
                    continue;
                }

                switch (instance.ResourceInfo.Type.Value)
                {
                    case StorageResourceStudio.SlcResource_StudioIds.Enums.Type.Unmanaged: yield return new UnmanagedResource(planApi, instance); break;
                    case StorageResourceStudio.SlcResource_StudioIds.Enums.Type.Element: yield return new ElementResource(planApi, instance); break;
                    case StorageResourceStudio.SlcResource_StudioIds.Enums.Type.Service: yield return new ServiceResource(planApi, instance); break;
                    case StorageResourceStudio.SlcResource_StudioIds.Enums.Type.VirtualFunction: yield return new VirtualFunctionResource(planApi, instance); break;

                    default:
                        continue;
                }
            }
        }

        private void SetDefaultValues()
        {
            Concurrency = 1;
        }

        private void ParseInstance(MediaOpsPlanApi planApi, StorageResourceStudio.ResourceInstance instance)
        {
            this.originalInstance = instance ?? throw new ArgumentNullException(nameof(instance));

            Name = instance.ResourceInfo.Name;
            IsFavorite = instance.ResourceInfo.Favorite ?? false;
            Concurrency = instance.ResourceInfo.Concurrency.HasValue ? (int)instance.ResourceInfo.Concurrency.Value : 1;
            resourcePoolIds = new HashSet<Guid>(instance.ResourceInternalProperties.PoolIds);
            coreResourceId = instance.ResourceInternalProperties.Resource_Id ?? Guid.Empty;
            IsExternallyManaged = instance.ExternalMetadata?.ExternallyManaged ?? false;
            IconImage = instance.ResourceOther.IconImage;
            Url = instance.ResourceOther.URL;
            VirtualSignalGroupInputId = instance.ResourceConnectionManagement.VirtualSignalGroupInputId;
            VirtualSignalGroupOutputId = instance.ResourceConnectionManagement.VirtualSignalGroupOutputId;

            State = EnumExtensions.MapEnum<StorageResourceStudio.SlcResource_StudioIds.Behaviors.Resource_Behavior.StatusesEnum, ResourceState>(instance.Status);

            foreach (var section in instance.ResourceCapabilities)
            {
                var capability = new ResourceCapabilitySetting(section);
                capabilitySettings.Add(capability);
            }

            foreach (var section in instance.ResourceProperties)
            {
                var propertyConfiguration = new ResourcePropertySettings(section);
                propertySettings.Add(propertyConfiguration);
            }

            ParseResourceCapacities(planApi, instance.ResourceCapacities);
        }

        private void ParseResourceCapacities(MediaOpsPlanApi planApi, IList<StorageResourceStudio.ResourceCapacitiesSection> resourceCapacities)
        {
            if (resourceCapacities == null || resourceCapacities.Count == 0)
            {
                return;
            }

            var capacityIds = resourceCapacities.Select(rc => rc.ProfileParameterId).Distinct();
            var capacityById = planApi.Capacities.Read(capacityIds).ToDictionary(x => x.Id);

            foreach (var section in resourceCapacities)
            {
                if (capacityById.TryGetValue(section.ProfileParameterId, out var capacity)
                    && capacity is RangeCapacity)
                {
                    var resourceCapacitySetting = new ResourceRangeCapacitySetting(section);
                    rangeCapacitySettings.Add(resourceCapacitySetting);
                }
                else
                {
                    var resourceCapacitySetting = new ResourceNumberCapacitySetting(section);
                    numberCapacitySettings.Add(resourceCapacitySetting);
                }
            }
        }
    }
}
