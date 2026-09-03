namespace RT_MediaOps.Plan.Relationships.Querying
{
	using System;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	/// <summary>
	/// Creates the relationship object types and relationships that are used to validate querying.
	/// </summary>
	internal sealed class RelationshipQueryingSetup
	{
		private readonly TestObjectCreator objectCreator;

		public RelationshipQueryingSetup(TestObjectCreator objectCreator)
		{
			this.objectCreator = objectCreator;

			Prefix = Guid.NewGuid().ToString();

			CreateObjectTypes();
			CreateRelationships();
		}

		public string Prefix { get; }

		public RelationshipObjectType[] ObjectTypes => new[]
		{
			ObjectTypeA!,
			ObjectTypeB!,
			ObjectTypeC!,
		};

		public Relationship[] Relationships => new[]
		{
			Relationship1!,
			Relationship2!,
			Relationship3!,
		};

		public RelationshipObjectType? ObjectTypeA { get; private set; }

		public RelationshipObjectType? ObjectTypeB { get; private set; }

		public RelationshipObjectType? ObjectTypeC { get; private set; }

		public Relationship? Relationship1 { get; private set; }

		public Relationship? Relationship2 { get; private set; }

		public Relationship? Relationship3 { get; private set; }

		public string ParentObjectId(int number) => $"Parent_{number}_{Prefix}";

		public string ChildObjectId(int number) => $"Child_{number}_{Prefix}";

		private void CreateObjectTypes()
		{
			ObjectTypeA = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"ObjectType_A_{Prefix}" });
			ObjectTypeB = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"ObjectType_B_{Prefix}" });
			ObjectTypeC = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"ObjectType_C_{Prefix}" });
		}

		private void CreateRelationships()
		{
			// The parent and child orders run in opposite directions so ordering on either side can be told apart.
			Relationship1 = objectCreator.CreateRelationship(new Relationship(new RelationshipData
			{
				Parent = new RelationshipEndpoint(ObjectTypeA!, ParentObjectId(1))
				{
					ObjectName = $"ParentName_A_{Prefix}",
					Url = $"https://example.invalid/parent/1/{Prefix}",
					Order = 1,
				},
				Child = new RelationshipEndpoint(ObjectTypeB!, ChildObjectId(1))
				{
					ObjectName = $"ChildName_C_{Prefix}",
					Url = $"https://example.invalid/child/1/{Prefix}",
					Order = 30,
				},
			}));

			Relationship2 = objectCreator.CreateRelationship(new Relationship(new RelationshipData
			{
				Parent = new RelationshipEndpoint(ObjectTypeA!, ParentObjectId(2))
				{
					ObjectName = $"ParentName_B_{Prefix}",
					Url = $"https://example.invalid/parent/2/{Prefix}",
					Order = 2,
				},
				Child = new RelationshipEndpoint(ObjectTypeC!, ChildObjectId(2))
				{
					ObjectName = $"ChildName_B_{Prefix}",
					Url = $"https://example.invalid/child/2/{Prefix}",
					Order = 20,
				},
			}));

			Relationship3 = objectCreator.CreateRelationship(new Relationship(new RelationshipData
			{
				Parent = new RelationshipEndpoint(ObjectTypeB!, ParentObjectId(3))
				{
					ObjectName = $"ParentName_C_{Prefix}",
					Url = $"https://example.invalid/parent/3/{Prefix}",
					Order = 3,
				},
				Child = new RelationshipEndpoint(ObjectTypeC!, ChildObjectId(3))
				{
					ObjectName = $"ChildName_A_{Prefix}",
					Url = $"https://example.invalid/child/3/{Prefix}",
					Order = 10,
				},
			}));
		}
	}
}
