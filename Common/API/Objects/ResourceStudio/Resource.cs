namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.Core.DataMinerSystem.Common;
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
        public string Name
        {
            get => name;
            set
            {
                HasChanges = true;
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
                HasChanges = true;
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
                HasChanges = true;
                concurrency = value;
            }
        }

        /// <summary>
        /// Gets the state of the resource.
        /// </summary>
        public ResourceState State { get; private set; }

        internal override bool IsNew { get; set; }

        internal override bool HasUserDefinedId { get; set; } = false;

        internal override bool HasChanges { get; set; } = false;

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

            State = EnumExtensions.MapEnum<StorageResourceStudio.SlcResource_StudioIds.Behaviors.Resource_Behavior.StatusesEnum, ResourceState>(instance.Status);
        }
    }

    /// <summary>
    /// Represents an unmanaged resource in the MediaOps Plan API.
    /// </summary>
    public class UnmanagedResource : Resource
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnmanagedResource"/> class.
        /// </summary>
        public UnmanagedResource() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnmanagedResource"/> class with a specific resource ID.
        /// </summary>
        /// <param name="resourceId">The unique identifier of the resource.</param>
        public UnmanagedResource(Guid resourceId) : base(resourceId)
        {
        }

        internal UnmanagedResource(StorageResourceStudio.ResourceInstance instance) : base(instance.ID.Id)
        {
        }

        internal override void ApplyChanges(StorageResourceStudio.ResourceInstance instance)
        {
            instance.ResourceInfo.Type = StorageResourceStudio.SlcResource_StudioIds.Enums.Type.Unmanaged;
        }
    }

    /// <summary>
    /// Represents an element resource in the MediaOps Plan API.
    /// </summary>
    public class ElementResource : Resource
    {
        private int agentId;

        private int elementId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ElementResource"/> class.
        /// </summary>
        public ElementResource() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ElementResource"/> class with a specific resource ID.
        /// </summary>
        /// <param name="resourceId">The unique identifier of the resource.</param>
        public ElementResource(Guid resourceId) : base(resourceId)
        {
        }

        internal ElementResource(StorageResourceStudio.ResourceInstance instance) : base(instance.ID.Id)
        {
            ParseInstance(instance);
        }

        /// <summary>
        /// Gets or sets the agent ID associated with the resource link.
        /// </summary>
        public int AgentId
        {
            get => agentId;
            set
            {
                HasChanges = true;
                agentId = value;
            }
        }

        /// <summary>
        /// Gets or sets the element ID associated with the resource link.
        /// </summary>
        public int ElementId
        {
            get => elementId;
            set
            {
                HasChanges = true;
                elementId = value;
            }
        }

        internal override void ApplyChanges(StorageResourceStudio.ResourceInstance instance)
        {
            instance.ResourceInfo.Type = StorageResourceStudio.SlcResource_StudioIds.Enums.Type.Element;
            instance.ResourceInternalProperties.Metadata.LinkedElementInfo = new DmsElementId(agentId, elementId).Value;
        }

        private void ParseInstance(StorageResourceStudio.ResourceInstance instance)
        {
            if (!string.IsNullOrWhiteSpace(instance.ResourceInternalProperties.Metadata.LinkedElementInfo))
            {
                var elementInfo = new DmsElementId(instance.ResourceInternalProperties.Metadata.LinkedElementInfo);
                agentId = elementInfo.AgentId;
                elementId = elementInfo.ElementId;
            }
        }
    }

    /// <summary>
    /// Represents a service resource in the MediaOps Plan API.
    /// </summary>
    public class ServiceResource : Resource
    {
        private int agentId;

        private int serviceId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceResource"/> class.
        /// </summary>
        public ServiceResource() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceResource"/> class with a specific resource ID.
        /// </summary>
        /// <param name="resourceId">The unique identifier of the resource.</param>
        public ServiceResource(Guid resourceId) : base(resourceId)
        {
        }

        internal ServiceResource(StorageResourceStudio.ResourceInstance instance) : base(instance.ID.Id)
        {
            ParseInstance(instance);
        }

        /// <summary>
        /// Gets or sets the agent ID associated with the resource link.
        /// </summary>
        public int AgentId
        {
            get => agentId;
            set
            {
                HasChanges = true;
                agentId = value;
            }
        }

        /// <summary>
        /// Gets or sets the service ID associated with the resource link.
        /// </summary>
        public int ServiceId
        {
            get => serviceId;
            set
            {
                HasChanges = true;
                serviceId = value;
            }
        }

        internal override void ApplyChanges(StorageResourceStudio.ResourceInstance instance)
        {
            instance.ResourceInfo.Type = StorageResourceStudio.SlcResource_StudioIds.Enums.Type.Service;
            instance.ResourceInternalProperties.Metadata.LinkedServiceInfo = new DmsServiceId(agentId, serviceId).Value;
        }

        private void ParseInstance(StorageResourceStudio.ResourceInstance instance)
        {
            if (!string.IsNullOrWhiteSpace(instance.ResourceInternalProperties.Metadata.LinkedServiceInfo))
            {
                var serviceInfo = new DmsServiceId(instance.ResourceInternalProperties.Metadata.LinkedServiceInfo);
                agentId = serviceInfo.AgentId;
                serviceId = serviceInfo.ServiceId;
            }
        }
    }

    /// <summary>
    /// Represents a virtual function resource in the MediaOps Plan API.
    /// </summary>
    public class VirtualFunctionResource : Resource
    {
        private int agentId;

        private int elementId;

        private Guid functionId;

        private string functionTableIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualFunctionResource"/> class.
        /// </summary>
        public VirtualFunctionResource() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualFunctionResource"/> class with a specific resource ID.
        /// </summary>
        /// <param name="resourceId">The unique identifier of the resource.</param>
        public VirtualFunctionResource(Guid resourceId) : base(resourceId)
        {
        }

        internal VirtualFunctionResource(StorageResourceStudio.ResourceInstance instance) : base(instance.ID.Id)
        {
            ParseInstance(instance);
        }

        /// <summary>
        /// Gets or sets the agent ID associated with the resource link.
        /// </summary>
        public int AgentId
        {
            get => agentId;
            set
            {
                HasChanges = true;
                agentId = value;
            }
        }

        /// <summary>
        /// Gets or sets the element ID associated with the resource link.
        /// </summary>
        public int ElementId
        {
            get => elementId;
            set
            {
                HasChanges = true;
                elementId = value;
            }
        }

        /// <summary>
        /// Gets or sets the function ID associated with the resource link.
        /// </summary>
        public Guid FunctionId
        {
            get => functionId;
            set
            {
                HasChanges = true;
                functionId = value;
            }
        }

        /// <summary>
        /// Gets or sets the function table index associated with the resource link.
        /// </summary>
        public string FunctionTableIndex
        {
            get => functionTableIndex;
            set
            {
                HasChanges = true;
                functionTableIndex = value;
            }
        }

        internal override void ApplyChanges(StorageResourceStudio.ResourceInstance instance)
        {
            instance.ResourceInfo.Type = StorageResourceStudio.SlcResource_StudioIds.Enums.Type.VirtualFunction;
            instance.ResourceInternalProperties.Metadata.LinkedElementInfo = new DmsElementId(agentId, elementId).Value;
            instance.ResourceInternalProperties.Metadata.LinkedFunctionId = functionId;
            instance.ResourceInternalProperties.Metadata.LinkedFunctionTableIndex = functionTableIndex;
        }

        private void ParseInstance(StorageResourceStudio.ResourceInstance instance)
        {
            if (!string.IsNullOrWhiteSpace(instance.ResourceInternalProperties.Metadata.LinkedElementInfo))
            {
                var elementInfo = new DmsElementId(instance.ResourceInternalProperties.Metadata.LinkedElementInfo);
                agentId = elementInfo.AgentId;
                elementId = elementInfo.ElementId;
            }

            functionId = instance.ResourceInternalProperties.Metadata.LinkedFunctionId;
            functionTableIndex = instance.ResourceInternalProperties.Metadata.LinkedFunctionTableIndex;
        }
    }
}
