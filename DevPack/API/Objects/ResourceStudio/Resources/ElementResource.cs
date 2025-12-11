namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    using Skyline.DataMiner.Core.DataMinerSystem.Common;

    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

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

        internal ElementResource(MediaOpsPlanApi planApi, StorageResourceStudio.ResourceInstance instance) : base(planApi, instance)
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
            DmsElementId elementInfo;
            if (!string.IsNullOrEmpty(instance.ResourceInternalProperties?.Metadata?.LinkedElementInfo))
            {
                elementInfo = new DmsElementId(instance.ResourceInternalProperties.Metadata.LinkedElementInfo);
                agentId = elementInfo.AgentId;
                elementId = elementInfo.ElementId;
            }
            else if (!string.IsNullOrEmpty(instance.ResourceInfo?.Element))
            {
                elementInfo = new DmsElementId(instance.ResourceInfo.Element);
                agentId = elementInfo.AgentId;
                elementId = elementInfo.ElementId;
            }
        }
    }
}
