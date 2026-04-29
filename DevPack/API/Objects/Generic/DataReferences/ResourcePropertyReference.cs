namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Represents a reference to a specific resource property.
	/// </summary>
	public sealed class ResourcePropertyReference : DataReference
	{
		private const string ResourcePropertyIdKey = "ResourcePropertyId";

		/// <summary>
		/// Initializes a new instance of the <see cref="ResourcePropertyReference"/> class.
		/// </summary>
		/// <param name="resourcePropertyId">The unique identifier of the resource property.</param>
		/// <param name="nodeId">
		/// Optional identifier of the workflow node whose resource is referenced.
		/// When <see langword="null"/> the reference targets the resource of the current node.
		/// </param>
		public ResourcePropertyReference(Guid resourcePropertyId, string nodeId = null) : base(DataReferenceType.ResourceProperty, nodeId)
		{
			ResourcePropertyId = resourcePropertyId;
		}

		/// <summary>
		/// Gets the unique identifier of the resource property.
		/// </summary>
		public Guid ResourcePropertyId { get; }

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				var hash = base.GetHashCode();
				hash = hash * 23 + ResourcePropertyId.GetHashCode();
				return hash;
			}
		}

		/// <inheritdoc/>
		public override bool Equals(DataReference other)
		{
			return base.Equals(other)
				&& other is ResourcePropertyReference rpr
				&& rpr.ResourcePropertyId == ResourcePropertyId;
		}

		internal static ResourcePropertyReference ParseFromStorage(Storage.DOM.DataReferenceStorage reference, string nodeId)
		{
			if (reference.ReferenceData == null || !reference.ReferenceData.TryGetValue(ResourcePropertyIdKey, out var raw))
			{
				return null;
			}

			return Guid.TryParse(raw, out var id) ? new ResourcePropertyReference(id, nodeId) : null;
		}

      internal override Dictionary<string, string> BuildReferenceData()
		{
			var data = base.BuildReferenceData() ?? new Dictionary<string, string>();
			data[ResourcePropertyIdKey] = ResourcePropertyId.ToString();
			return data;
		}
	}
}
