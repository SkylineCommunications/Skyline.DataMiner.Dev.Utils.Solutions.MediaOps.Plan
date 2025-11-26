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

        internal VirtualFunctionResource(StorageResourceStudio.ResourceInstance instance) : base(instance)
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
