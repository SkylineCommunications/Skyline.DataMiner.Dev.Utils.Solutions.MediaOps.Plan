namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents one side of a <see cref="Relationship"/>.
	/// </summary>
	public class RelationshipEndpoint : TrackableObject
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RelationshipEndpoint"/> class.
		/// </summary>
		public RelationshipEndpoint()
		{
			IsNew = true;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RelationshipEndpoint"/> class for the specified object type.
		/// </summary>
		/// <param name="objectType">The type of the object this endpoint points to.</param>
		/// <param name="objectId">The identifier of the object this endpoint points to.</param>
		/// <exception cref="ArgumentNullException"><paramref name="objectType"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException"><paramref name="objectId"/> is <see langword="null"/> or whitespace.</exception>
		public RelationshipEndpoint(RelationshipObjectType objectType, string objectId)
		{
			if (objectType == null)
			{
				throw new ArgumentNullException(nameof(objectType));
			}

			if (string.IsNullOrWhiteSpace(objectId))
			{
				throw new ArgumentException($"'{nameof(objectId)}' must be filled out.", nameof(objectId));
			}

			IsNew = true;
			ObjectTypeId = objectType.Id;
			ObjectId = objectId;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RelationshipEndpoint"/> class for the specified object type identifier.
		/// </summary>
		/// <param name="objectTypeId">The unique identifier of the object type this endpoint points to.</param>
		/// <param name="objectId">The identifier of the object this endpoint points to.</param>
		/// <exception cref="ArgumentException"><paramref name="objectTypeId"/> is empty or <paramref name="objectId"/> is <see langword="null"/> or whitespace.</exception>
		public RelationshipEndpoint(Guid objectTypeId, string objectId)
		{
			if (objectTypeId == Guid.Empty)
			{
				throw new ArgumentException($"'{nameof(objectTypeId)}' must be filled out.", nameof(objectTypeId));
			}

			if (string.IsNullOrWhiteSpace(objectId))
			{
				throw new ArgumentException($"'{nameof(objectId)}' must be filled out.", nameof(objectId));
			}

			IsNew = true;
			ObjectTypeId = objectTypeId;
			ObjectId = objectId;
		}

		/// <summary>
		/// Gets the unique identifier of the object type this endpoint points to.
		/// </summary>
		public Guid ObjectTypeId { get; internal set; }

		/// <summary>
		/// Gets or sets the identifier of the object this endpoint points to. This is a free-form identifier and is not restricted to DOM instances.
		/// </summary>
		public string ObjectId { get; set; }

		/// <summary>
		/// Gets or sets the name of the object this endpoint points to. This is a snapshot taken when the relationship was written and is not kept in sync.
		/// </summary>
		public string ObjectName { get; set; }

		/// <summary>
		/// Gets or sets the URL that points to the object this endpoint points to.
		/// </summary>
		public string Url { get; set; }

		/// <summary>
		/// Gets or sets the order of this endpoint within the relationships of the object on the other side.
		/// </summary>
		public long Order { get; set; }

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				var hash = 17;
				hash = (hash * 23) + ObjectTypeId.GetHashCode();
				hash = (hash * 23) + (ObjectId != null ? ObjectId.GetHashCode() : 0);
				hash = (hash * 23) + (ObjectName != null ? ObjectName.GetHashCode() : 0);
				hash = (hash * 23) + (Url != null ? Url.GetHashCode() : 0);
				hash = (hash * 23) + Order.GetHashCode();

				return hash;
			}
		}

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			if (obj is not RelationshipEndpoint other)
			{
				return false;
			}

			return ObjectTypeId == other.ObjectTypeId
				&& ObjectId == other.ObjectId
				&& ObjectName == other.ObjectName
				&& Url == other.Url
				&& Order == other.Order;
		}
	}
}
