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

        internal ServiceResource(MediaOpsPlanApi planApi, StorageResourceStudio.ResourceInstance instance) : base(planApi, instance)
        {
            ParseInstance(instance);
            InitTracking();
        }

        /// <summary>
        /// Gets or sets the agent ID associated with the resource link.
        /// </summary>
        public int AgentId
        {
            get => agentId;
            set
            {
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
                serviceId = value;
            }
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = base.GetHashCode();
                hash = (hash * 23) + agentId.GetHashCode();
                hash = (hash * 23) + serviceId.GetHashCode();

                return hash;
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
