namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;

    using Skyline.DataMiner.Core.DataMinerSystem.Common;

    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

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

        internal VirtualFunctionResource(MediaOpsPlanApi planApi, StorageResourceStudio.ResourceInstance instance) : base(planApi, instance)
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
        /// Gets or sets the element ID associated with the resource link.
        /// </summary>
        public int ElementId
        {
            get => elementId;
            set
            {
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
                functionTableIndex = value;
            }
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = base.GetHashCode();
                hash = (hash * 23) + agentId.GetHashCode();
                hash = (hash * 23) + elementId.GetHashCode();
                hash = (hash * 23) + functionId.GetHashCode();
                hash = (hash * 23) + (functionTableIndex != null ? functionTableIndex.GetHashCode() : 0);

                return hash;
            }
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current VirtualFunctionResource instance.
        /// </summary>
        /// <param name="obj">The object to compare with the current VirtualFunctionResource instance.</param>
        /// <returns>true if the specified object is a VirtualFunctionResource and has the same values for all relevant fields;
        /// otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (obj is not VirtualFunctionResource other)
            {
                return false;
            }

            return base.Equals(other)
                   && agentId == other.agentId
                   && elementId == other.elementId
                   && functionId == other.functionId
                   && functionTableIndex == other.functionTableIndex;
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
