namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    using Skyline.DataMiner.Core.DataMinerSystem.Common;

    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

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

        internal ServiceResource(StorageResourceStudio.ResourceInstance instance) : base(instance)
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
}
