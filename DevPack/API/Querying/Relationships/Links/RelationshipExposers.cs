namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	/// <summary>
	/// Provides exposers for querying and filtering <see cref="Relationship"/> objects.
	/// </summary>
	public static class RelationshipExposers
	{
		/// <summary>
		/// Gets an exposer for the <see cref="ApiObject.Id"/> property.
		/// </summary>
		public static readonly Exposer<Relationship, Guid> Id = new Exposer<Relationship, Guid>((obj) => obj.Id, "Id");

		/// <summary>
		/// Provides exposers for querying and filtering the parent side of a relationship.
		/// </summary>
		public static class Parent
		{
			/// <summary>
			/// Gets an exposer for the <see cref="RelationshipEndpoint.ObjectTypeId"/> property of the parent.
			/// </summary>
			public static readonly Exposer<Relationship, Guid> ObjectTypeId = new Exposer<Relationship, Guid>((obj) => obj.Parent != null ? obj.Parent.ObjectTypeId : Guid.Empty, "Parent.ObjectTypeId");

			/// <summary>
			/// Gets an exposer for the <see cref="RelationshipEndpoint.ObjectId"/> property of the parent.
			/// </summary>
			public static readonly Exposer<Relationship, string> ObjectId = new Exposer<Relationship, string>((obj) => obj.Parent?.ObjectId, "Parent.ObjectId");

			/// <summary>
			/// Gets an exposer for the <see cref="RelationshipEndpoint.ObjectName"/> property of the parent.
			/// </summary>
			public static readonly Exposer<Relationship, string> ObjectName = new Exposer<Relationship, string>((obj) => obj.Parent?.ObjectName, "Parent.ObjectName");

			/// <summary>
			/// Gets an exposer for the <see cref="RelationshipEndpoint.Url"/> property of the parent.
			/// </summary>
			public static readonly Exposer<Relationship, string> Url = new Exposer<Relationship, string>((obj) => obj.Parent?.Url, "Parent.Url");

			/// <summary>
			/// Gets an exposer for the <see cref="RelationshipEndpoint.Order"/> property of the parent.
			/// </summary>
			public static readonly Exposer<Relationship, long> Order = new Exposer<Relationship, long>((obj) => obj.Parent != null ? obj.Parent.Order : 0, "Parent.Order");
		}

		/// <summary>
		/// Provides exposers for querying and filtering the child side of a relationship.
		/// </summary>
		public static class Child
		{
			/// <summary>
			/// Gets an exposer for the <see cref="RelationshipEndpoint.ObjectTypeId"/> property of the child.
			/// </summary>
			public static readonly Exposer<Relationship, Guid> ObjectTypeId = new Exposer<Relationship, Guid>((obj) => obj.Child != null ? obj.Child.ObjectTypeId : Guid.Empty, "Child.ObjectTypeId");

			/// <summary>
			/// Gets an exposer for the <see cref="RelationshipEndpoint.ObjectId"/> property of the child.
			/// </summary>
			public static readonly Exposer<Relationship, string> ObjectId = new Exposer<Relationship, string>((obj) => obj.Child?.ObjectId, "Child.ObjectId");

			/// <summary>
			/// Gets an exposer for the <see cref="RelationshipEndpoint.ObjectName"/> property of the child.
			/// </summary>
			public static readonly Exposer<Relationship, string> ObjectName = new Exposer<Relationship, string>((obj) => obj.Child?.ObjectName, "Child.ObjectName");

			/// <summary>
			/// Gets an exposer for the <see cref="RelationshipEndpoint.Url"/> property of the child.
			/// </summary>
			public static readonly Exposer<Relationship, string> Url = new Exposer<Relationship, string>((obj) => obj.Child?.Url, "Child.Url");

			/// <summary>
			/// Gets an exposer for the <see cref="RelationshipEndpoint.Order"/> property of the child.
			/// </summary>
			public static readonly Exposer<Relationship, long> Order = new Exposer<Relationship, long>((obj) => obj.Child != null ? obj.Child.Order : 0, "Child.Order");
		}
	}
}
