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
		public ResourcePropertyReference(Guid resourcePropertyId) : base(DataReferenceType.ResourceProperty)
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
				var hash = 17;
				hash = hash * 23 + Type.GetHashCode();
				hash = hash * 23 + ResourcePropertyId.GetHashCode();
				return hash;
			}
		}

		/// <inheritdoc/>
		public override bool Equals(DataReference other)
		{
			return other is ResourcePropertyReference rpr && rpr.ResourcePropertyId == ResourcePropertyId;
		}

		internal static ResourcePropertyReference ParseFromStorage(Storage.DOM.DataReference reference)
		{
			if (reference.ReferenceData == null || !reference.ReferenceData.TryGetValue(ResourcePropertyIdKey, out var raw))
			{
				return null;
			}

			return Guid.TryParse(raw, out var id) ? new ResourcePropertyReference(id) : null;
		}

		internal override Storage.DOM.DataReference ToStorage()
		{
			return new Storage.DOM.DataReference
			{
				ReferenceType = Type.ToString(),
				ReferenceData = new Dictionary<string, string>
				{
					[ResourcePropertyIdKey] = ResourcePropertyId.ToString(),
				},
			};
		}
	}
}
