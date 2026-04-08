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
		public int AgentId { get; set; }

		/// <summary>
		/// Gets or sets the service ID associated with the resource link.
		/// </summary>
		public int ServiceId { get; set; }

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				var hash = base.GetHashCode();
				hash = (hash * 23) + AgentId.GetHashCode();
				hash = (hash * 23) + ServiceId.GetHashCode();

				return hash;
			}
		}

		/// <summary>
		/// Determines whether the specified object is equal to the current ServiceResource instance.
		/// </summary>
		/// <param name="obj">The object to compare with the current ServiceResource instance.</param>
		/// <returns>true if the specified object is a ServiceResource and has the same values for all compared fields;
		/// otherwise, false.</returns>
		public override bool Equals(object obj)
		{
			if (obj is not ServiceResource other)
			{
				return false;
			}

			return base.Equals(other)
				&& AgentId == other.AgentId
				&& ServiceId == other.ServiceId;
		}

		internal override void ApplyChanges(StorageResourceStudio.ResourceInstance instance)
		{
			instance.ResourceInfo.Type = StorageResourceStudio.SlcResource_StudioIds.Enums.Type.Service;
			instance.ResourceInternalProperties.Metadata.LinkedServiceInfo = new DmsServiceId(AgentId, ServiceId).Value;
		}

		private void ParseInstance(StorageResourceStudio.ResourceInstance instance)
		{
			if (!string.IsNullOrWhiteSpace(instance.ResourceInternalProperties.Metadata.LinkedServiceInfo))
			{
				var serviceInfo = new DmsServiceId(instance.ResourceInternalProperties.Metadata.LinkedServiceInfo);
				AgentId = serviceInfo.AgentId;
				ServiceId = serviceInfo.ServiceId;
			}
		}
	}
}
