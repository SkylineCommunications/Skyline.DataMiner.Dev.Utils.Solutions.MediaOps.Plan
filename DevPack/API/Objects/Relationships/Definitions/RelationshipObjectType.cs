namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	using StorageRelationships = Storage.DOM.SlcRelationships;

	/// <summary>
	/// Represents the type of an object that can take part in a relationship, e.g. a job or a booking.
	/// </summary>
	public class RelationshipObjectType : ApiNamedObject
	{
		// Owned by the MediaOps solution; the job repository resolves this object type on behalf of the consumer.
		internal const string JobObjectTypeName = "Job";

		internal static readonly IReadOnlyCollection<string> ReservedNames = [JobObjectTypeName];

		private StorageRelationships.ObjectTypesInstance originalInstance;
		private StorageRelationships.ObjectTypesInstance updatedInstance;

		/// <summary>
		/// Initializes a new instance of the <see cref="RelationshipObjectType"/> class.
		/// </summary>
		public RelationshipObjectType() : base()
		{
			IsNew = true;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RelationshipObjectType"/> class with a user defined identifier.
		/// </summary>
		/// <param name="objectTypeId">The unique identifier of the object type.</param>
		/// <exception cref="ArgumentException"><paramref name="objectTypeId"/> is empty.</exception>
		public RelationshipObjectType(Guid objectTypeId) : base(objectTypeId)
		{
			IsNew = true;
			HasUserDefinedId = true;
		}

		internal RelationshipObjectType(StorageRelationships.ObjectTypesInstance instance)
			: base(instance?.ID.Id ?? throw new ArgumentNullException(nameof(instance)))
		{
			ParseInstance(instance);
			InitTracking();
		}

		/// <summary>
		/// Gets or sets the name of the object type.
		/// </summary>
		public override string Name { get; set; }

		internal StorageRelationships.ObjectTypesInstance OriginalInstance => originalInstance;

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				var hash = 17;
				hash = (hash * 23) + Id.GetHashCode();
				hash = (hash * 23) + (Name != null ? Name.GetHashCode() : 0);

				return hash;
			}
		}

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			if (obj is not RelationshipObjectType other)
			{
				return false;
			}

			return Id == other.Id
				&& Name == other.Name;
		}

		internal StorageRelationships.ObjectTypesInstance GetInstanceWithChanges()
		{
			if (updatedInstance == null)
			{
				updatedInstance = IsNew ? new StorageRelationships.ObjectTypesInstance(Id) : originalInstance.Clone();
			}

			updatedInstance.ObjectTypeInfo.ObjectName = Name;

			return updatedInstance;
		}

		private void ParseInstance(StorageRelationships.ObjectTypesInstance instance)
		{
			originalInstance = instance;

			Name = instance.ObjectTypeInfo.ObjectName;
		}
	}
}
