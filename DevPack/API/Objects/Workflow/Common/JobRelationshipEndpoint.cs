namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Describes the object a job is linked to, e.g. a booking or a reference in an external system.
	/// Only this side of the relationship has to be described: the job side is filled in automatically.
	/// </summary>
	public class JobRelationshipEndpoint
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="JobRelationshipEndpoint"/> class.
		/// </summary>
		/// <param name="objectType">The type of the object that is linked to the job.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="objectType"/> is <see langword="null"/>.</exception>
		public JobRelationshipEndpoint(RelationshipObjectType objectType)
			: this(objectType?.Id ?? throw new ArgumentNullException(nameof(objectType)))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="JobRelationshipEndpoint"/> class.
		/// </summary>
		/// <param name="objectTypeId">The unique identifier of the type of the object that is linked to the job.</param>
		/// <exception cref="ArgumentException">Thrown when <paramref name="objectTypeId"/> is <see cref="Guid.Empty"/>.</exception>
		public JobRelationshipEndpoint(Guid objectTypeId)
		{
			if (objectTypeId == Guid.Empty)
			{
				throw new ArgumentException($"'{nameof(objectTypeId)}' must be filled out.", nameof(objectTypeId));
			}

			ObjectTypeId = objectTypeId;
			JobIsParent = true;
		}

		// A copy is an unsaved link, so it follows the create convention instead of inheriting the original's storage side.
		internal JobRelationshipEndpoint(JobRelationshipEndpoint original)
		{
			ObjectTypeId = original.ObjectTypeId;
			ObjectId = original.ObjectId;
			ObjectName = original.ObjectName;
			Url = original.Url;
			JobIsParent = true;
		}

		// Links that are already stored bypass validation: the solution allows an empty object type and object id.
		internal JobRelationshipEndpoint(Guid objectTypeId, string objectId, Guid relationshipId, bool jobIsParent)
		{
			ObjectTypeId = objectTypeId;
			ObjectId = objectId;
			Id = relationshipId;
			JobIsParent = jobIsParent;
		}

		/// <summary>
		/// Gets the unique identifier of the relationship that stores this link. This is <see cref="Guid.Empty"/> as long as the link has not been saved.
		/// </summary>
		public Guid Id { get; internal set; }

		/// <summary>
		/// Gets the unique identifier of the type of the linked object.
		/// </summary>
		public Guid ObjectTypeId { get; internal set; }

		/// <summary>
		/// Gets or sets the identifier of the linked object. This is a free-form identifier and is not restricted to DOM instances.
		/// </summary>
		public string ObjectId { get; set; }

		/// <summary>
		/// Gets or sets the name of the linked object. This is a snapshot and is not kept in sync with the linked object.
		/// </summary>
		public string ObjectName { get; set; }

		/// <summary>
		/// Gets or sets the URL that points to the linked object.
		/// </summary>
		public string Url { get; set; }

		// New links always put the job on the parent side; existing links keep the side they were stored on.
		internal bool JobIsParent { get; set; }

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				var hash = 17;
				hash = (hash * 23) + Id.GetHashCode();
				hash = (hash * 23) + ObjectTypeId.GetHashCode();
				hash = (hash * 23) + (ObjectId != null ? ObjectId.GetHashCode() : 0);
				hash = (hash * 23) + (ObjectName != null ? ObjectName.GetHashCode() : 0);
				hash = (hash * 23) + (Url != null ? Url.GetHashCode() : 0);

				return hash;
			}
		}

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			if (obj is not JobRelationshipEndpoint other)
			{
				return false;
			}

			return Id == other.Id
				&& ObjectTypeId == other.ObjectTypeId
				&& String.Equals(ObjectId, other.ObjectId, StringComparison.Ordinal)
				&& String.Equals(ObjectName, other.ObjectName, StringComparison.Ordinal)
				&& String.Equals(Url, other.Url, StringComparison.Ordinal);
		}
	}
}
