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
		public int AgentId { get; set; }

		/// <summary>
		/// Gets or sets the element ID associated with the resource link.
		/// </summary>
		public int ElementId { get; set; }

		/// <summary>
		/// Gets or sets the function ID associated with the resource link.
		/// </summary>
		public Guid FunctionId { get; set; }

		/// <summary>
		/// Gets or sets the function table index associated with the resource link.
		/// </summary>
		public string FunctionTableIndex { get; set; }

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				var hash = base.GetHashCode();
				hash = (hash * 23) + AgentId.GetHashCode();
				hash = (hash * 23) + ElementId.GetHashCode();
				hash = (hash * 23) + FunctionId.GetHashCode();
				hash = (hash * 23) + (FunctionTableIndex != null ? FunctionTableIndex.GetHashCode() : 0);

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
				   && AgentId == other.AgentId
				   && ElementId == other.ElementId
				   && FunctionId == other.FunctionId
				   && FunctionTableIndex == other.FunctionTableIndex;
		}

		internal override void ApplyChanges(StorageResourceStudio.ResourceInstance instance)
		{
			instance.ResourceInfo.Type = StorageResourceStudio.SlcResource_StudioIds.Enums.Type.VirtualFunction;
			instance.ResourceInternalProperties.Metadata.LinkedElementInfo = new DmsElementId(AgentId, ElementId).Value;
			instance.ResourceInternalProperties.Metadata.LinkedFunctionId = FunctionId;
			instance.ResourceInternalProperties.Metadata.LinkedFunctionTableIndex = FunctionTableIndex;
		}

		private void ParseInstance(StorageResourceStudio.ResourceInstance instance)
		{
			if (!string.IsNullOrWhiteSpace(instance.ResourceInternalProperties.Metadata.LinkedElementInfo))
			{
				var elementInfo = new DmsElementId(instance.ResourceInternalProperties.Metadata.LinkedElementInfo);
				AgentId = elementInfo.AgentId;
				ElementId = elementInfo.ElementId;
			}

			FunctionId = instance.ResourceInternalProperties.Metadata.LinkedFunctionId;
			FunctionTableIndex = instance.ResourceInternalProperties.Metadata.LinkedFunctionTableIndex;
		}
	}
}
