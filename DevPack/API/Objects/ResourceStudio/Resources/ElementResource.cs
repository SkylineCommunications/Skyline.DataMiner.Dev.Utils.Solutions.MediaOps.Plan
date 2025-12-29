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
            InitTracking();
        }

        /// <summary>
        /// Gets or sets the agent ID associated with the resource link.
        /// </summary>
        public int AgentId { get; set; }

        /// <summary>
        /// Gets or sets the element ID associated with the resource link.
        /// </summary>
        public int ElementId { get; set; }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = base.GetHashCode();
                hash = (hash * 23) + AgentId.GetHashCode();
                hash = (hash * 23) + ElementId.GetHashCode();

                return hash;
            }
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is not ElementResource other)
            {
                return false;
            }

            return base.Equals(other) &&
                   AgentId == other.AgentId &&
                   ElementId == other.ElementId;
        }

        internal override void ApplyChanges(StorageResourceStudio.ResourceInstance instance)
        {
            instance.ResourceInfo.Type = StorageResourceStudio.SlcResource_StudioIds.Enums.Type.Element;
            instance.ResourceInternalProperties.Metadata.LinkedElementInfo = new DmsElementId(AgentId, ElementId).Value;
        }

        private void ParseInstance(StorageResourceStudio.ResourceInstance instance)
        {
            DmsElementId elementInfo;
            if (!string.IsNullOrEmpty(instance.ResourceInternalProperties?.Metadata?.LinkedElementInfo))
            {
                elementInfo = new DmsElementId(instance.ResourceInternalProperties.Metadata.LinkedElementInfo);
                AgentId = elementInfo.AgentId;
                ElementId = elementInfo.ElementId;
            }
            else if (!string.IsNullOrEmpty(instance.ResourceInfo?.Element))
            {
                elementInfo = new DmsElementId(instance.ResourceInfo.Element);
                AgentId = elementInfo.AgentId;
                ElementId = elementInfo.ElementId;
            }
        }
    }
}
