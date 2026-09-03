namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents a link between a job and another object, e.g. a booking or a reference in an external system.
	/// Only the linked object has to be described: the job side of the relationship is filled in automatically.
	/// </summary>
	public class JobLink
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="JobLink"/> class.
		/// </summary>
		/// <param name="objectType">The type of the object that is linked to the job.</param>
		/// <param name="objectId">The identifier of the object that is linked to the job. Optional: an object that has no identifier of its own can be described by its name and URL.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="objectType"/> is <see langword="null"/>.</exception>
		public JobLink(RelationshipObjectType objectType, string objectId)
			: this(objectType?.Id ?? throw new ArgumentNullException(nameof(objectType)), objectId)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="JobLink"/> class.
		/// </summary>
		/// <param name="objectTypeId">The unique identifier of the type of the object that is linked to the job.</param>
		/// <param name="objectId">The identifier of the object that is linked to the job. Optional: an object that has no identifier of its own can be described by its name and URL.</param>
		/// <exception cref="ArgumentException">Thrown when <paramref name="objectTypeId"/> is <see cref="Guid.Empty"/>.</exception>
		public JobLink(Guid objectTypeId, string objectId)
		{
			if (objectTypeId == Guid.Empty)
			{
				throw new ArgumentException($"'{nameof(objectTypeId)}' must be filled out.", nameof(objectTypeId));
			}

			ObjectTypeId = objectTypeId;
			ObjectId = objectId;
			JobIsParent = true;
		}

		// A copy is an unsaved link, so it follows the create convention instead of inheriting the original's storage side.
		internal JobLink(JobLink original)
		{
			ObjectTypeId = original.ObjectTypeId;
			ObjectId = original.ObjectId;
			ObjectName = original.ObjectName;
			Url = original.Url;
			JobIsParent = true;
		}

		// Links that are already stored bypass validation: the solution allows an empty object type and object id.
		internal JobLink(Guid objectTypeId, string objectId, Guid relationshipId, bool jobIsParent)
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
			// Only the object type takes part: the object id is optional and the relationship id changes when the link
			// is saved, so neither can contribute to a stable hash.
			return ObjectTypeId.GetHashCode();
		}

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			if (obj is not JobLink other)
			{
				return false;
			}

			if (ObjectTypeId != other.ObjectTypeId)
			{
				return false;
			}

			// Without an object id a link has no identity of its own, so several of them can coexist on a job and only
			// the stored relationship tells them apart.
			if (String.IsNullOrWhiteSpace(ObjectId) || String.IsNullOrWhiteSpace(other.ObjectId))
			{
				return Id != Guid.Empty && Id == other.Id;
			}

			return String.Equals(ObjectId, other.ObjectId, StringComparison.Ordinal);
		}
	}
}
