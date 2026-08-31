namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	using StorageRelationships = Storage.DOM.SlcRelationships;

	/// <summary>
	/// Represents a directed relationship between two objects in the MediaOps Plan API.
	/// </summary>
	public class Relationship : ApiObject
	{
		private StorageRelationships.LinksInstance originalInstance;
		private StorageRelationships.LinksInstance updatedInstance;

		/// <summary>
		/// Initializes a new instance of the <see cref="Relationship"/> class.
		/// </summary>
		public Relationship() : base()
		{
			IsNew = true;
			Parent = new RelationshipEndpoint();
			Child = new RelationshipEndpoint();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Relationship"/> class.
		/// </summary>
		/// <param name="data">The data describing both sides of the relationship.</param>
		/// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException"><paramref name="data"/> is incomplete.</exception>
		public Relationship(RelationshipData data) : base()
		{
			if (data == null)
			{
				throw new ArgumentNullException(nameof(data));
			}

			data.Validate(nameof(data));

			IsNew = true;
			Parent = data.Parent;
			Child = data.Child;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Relationship"/> class with a user defined identifier.
		/// </summary>
		/// <param name="relationshipId">The unique identifier of the relationship.</param>
		/// <exception cref="ArgumentException"><paramref name="relationshipId"/> is empty.</exception>
		public Relationship(Guid relationshipId) : base(relationshipId)
		{
			IsNew = true;
			HasUserDefinedId = true;
			Parent = new RelationshipEndpoint();
			Child = new RelationshipEndpoint();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Relationship"/> class with a user defined identifier.
		/// </summary>
		/// <param name="relationshipId">The unique identifier of the relationship.</param>
		/// <param name="data">The data describing both sides of the relationship.</param>
		/// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
		/// <exception cref="ArgumentException"><paramref name="relationshipId"/> is empty or <paramref name="data"/> is incomplete.</exception>
		public Relationship(Guid relationshipId, RelationshipData data) : base(relationshipId)
		{
			if (data == null)
			{
				throw new ArgumentNullException(nameof(data));
			}

			data.Validate(nameof(data));

			IsNew = true;
			HasUserDefinedId = true;
			Parent = data.Parent;
			Child = data.Child;
		}

		internal Relationship(StorageRelationships.LinksInstance instance)
			: base(instance?.ID.Id ?? throw new ArgumentNullException(nameof(instance)))
		{
			ParseInstance(instance);
			InitTracking();
		}

		/// <summary>
		/// Gets or sets the parent side of the relationship.
		/// </summary>
		public RelationshipEndpoint Parent { get; set; }

		/// <summary>
		/// Gets or sets the child side of the relationship.
		/// </summary>
		public RelationshipEndpoint Child { get; set; }

		internal StorageRelationships.LinksInstance OriginalInstance => originalInstance;

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				var hash = 17;
				hash = (hash * 23) + Id.GetHashCode();
				hash = (hash * 23) + (Parent != null ? Parent.GetHashCode() : 0);
				hash = (hash * 23) + (Child != null ? Child.GetHashCode() : 0);

				return hash;
			}
		}

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			if (obj is not Relationship other)
			{
				return false;
			}

			return Id == other.Id
				&& Equals(Parent, other.Parent)
				&& Equals(Child, other.Child);
		}

		internal StorageRelationships.LinksInstance GetInstanceWithChanges()
		{
			if (updatedInstance == null)
			{
				updatedInstance = IsNew ? new StorageRelationships.LinksInstance(Id) : originalInstance.Clone();
			}

			var linkInfo = updatedInstance.LinkInfo;

			linkInfo.ParentObjectType = ToNullableGuid(Parent?.ObjectTypeId);
			linkInfo.ParentObjectID = ToNullableString(Parent?.ObjectId);
			linkInfo.ParentObjectName = ToNullableString(Parent?.ObjectName);
			linkInfo.ParentURL = ToNullableString(Parent?.Url);
			linkInfo.ParentOrder = Parent?.Order ?? 0;

			linkInfo.ChildObjectType = ToNullableGuid(Child?.ObjectTypeId);
			linkInfo.ChildObjectID = ToNullableString(Child?.ObjectId);
			linkInfo.ChildObjectName = ToNullableString(Child?.ObjectName);
			linkInfo.ChildURL = ToNullableString(Child?.Url);
			linkInfo.ChildOrder = Child?.Order ?? 0;

			return updatedInstance;
		}

		// The solution removes empty values from the section instead of storing them, so the DevPack does the same to stay compatible.
		private static Guid? ToNullableGuid(Guid? value)
		{
			return value.HasValue && value.Value != Guid.Empty ? value : null;
		}

		private static string ToNullableString(string value)
		{
			return string.IsNullOrEmpty(value) ? null : value;
		}

		private void ParseInstance(StorageRelationships.LinksInstance instance)
		{
			originalInstance = instance;

			var linkInfo = instance.LinkInfo;

			Parent = new RelationshipEndpoint
			{
				IsNew = false,
				ObjectTypeId = linkInfo.ParentObjectType ?? Guid.Empty,
				ObjectId = linkInfo.ParentObjectID,
				ObjectName = linkInfo.ParentObjectName,
				Url = linkInfo.ParentURL,
				Order = linkInfo.ParentOrder ?? 0,
			};

			Child = new RelationshipEndpoint
			{
				IsNew = false,
				ObjectTypeId = linkInfo.ChildObjectType ?? Guid.Empty,
				ObjectId = linkInfo.ChildObjectID,
				ObjectName = linkInfo.ChildObjectName,
				Url = linkInfo.ChildURL,
				Order = linkInfo.ChildOrder ?? 0,
			};
		}
	}
}
