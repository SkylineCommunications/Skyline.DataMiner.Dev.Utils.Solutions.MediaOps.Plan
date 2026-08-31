namespace RT_MediaOps.Plan.Relationships.Querying
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.Querying;
	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.SDM;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	using SLDataGateway.API.Querying;
	using SLDataGateway.API.Types.Querying;

	[TestClass]
	[TestCategory("IntegrationTest")]
	public sealed class RelationshipQueryingTests
	{
		private static TestObjectCreator? objectCreator;
		private static RelationshipQueryingSetup? setup;

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		private static RelationshipQueryingSetup Setup => setup ?? throw new InvalidOperationException("Test setup was not initialized.");

		[ClassInitialize]
		public static void ClassInitialize(TestContext context)
		{
			objectCreator = new TestObjectCreator(TestContext);
			setup = new RelationshipQueryingSetup(objectCreator);
		}

		[ClassCleanup(ClassCleanupBehavior.EndOfClass)]
		public static void ClassCleanup()
		{
			objectCreator?.Dispose();
			objectCreator = null;
			setup = null;
		}

		private FilterElement<Relationship> RelationshipFilter => new ORFilterElement<Relationship>(Setup.Relationships.Select(x => RelationshipExposers.Id.Equal(x.Id)).ToArray());

		private FilterElement<RelationshipObjectType> ObjectTypeFilter => new ORFilterElement<RelationshipObjectType>(Setup.ObjectTypes.Select(x => RelationshipObjectTypeExposers.Id.Equal(x.Id)).ToArray());

		/// <summary>
		/// Gets the expected objects, in the expected order, mapped to the applied query.
		/// </summary>
		private Tuple<Relationship[], IQuery<Relationship>>[] RelationshipQueryTestCases => new[]
		{
			new Tuple<Relationship[], IQuery<Relationship>>(
				[Setup.Relationship1!, Setup.Relationship2!, Setup.Relationship3!],
				RelationshipFilter.ToQuery().OrderBy(RelationshipExposers.Parent.Order)),
			new Tuple<Relationship[], IQuery<Relationship>>(
				[Setup.Relationship3!, Setup.Relationship2!, Setup.Relationship1!],
				RelationshipFilter.ToQuery().OrderByDescending(RelationshipExposers.Parent.Order)),

			new Tuple<Relationship[], IQuery<Relationship>>(
				[Setup.Relationship3!, Setup.Relationship2!, Setup.Relationship1!],
				RelationshipFilter.ToQuery().OrderBy(RelationshipExposers.Child.Order)),

			new Tuple<Relationship[], IQuery<Relationship>>(
				[Setup.Relationship1!, Setup.Relationship2!, Setup.Relationship3!],
				RelationshipFilter.ToQuery().OrderBy(RelationshipExposers.Parent.ObjectName)),
			new Tuple<Relationship[], IQuery<Relationship>>(
				[Setup.Relationship3!, Setup.Relationship2!, Setup.Relationship1!],
				RelationshipFilter.ToQuery().OrderBy(RelationshipExposers.Child.ObjectName)),

			new Tuple<Relationship[], IQuery<Relationship>>(
				[Setup.Relationship1!, Setup.Relationship2!],
				RelationshipFilter.AND(RelationshipExposers.Parent.ObjectTypeId.Equal(Setup.ObjectTypeA!.Id)).ToQuery().OrderBy(RelationshipExposers.Parent.Order)),
			new Tuple<Relationship[], IQuery<Relationship>>(
				[Setup.Relationship2!, Setup.Relationship3!],
				RelationshipFilter.AND(RelationshipExposers.Child.ObjectTypeId.Equal(Setup.ObjectTypeC!.Id)).ToQuery().OrderBy(RelationshipExposers.Parent.Order)),

			new Tuple<Relationship[], IQuery<Relationship>>(
				[Setup.Relationship1!],
				RelationshipFilter.AND(RelationshipExposers.Parent.ObjectId.Equal(Setup.ParentObjectId(1))).ToQuery().OrderBy(RelationshipExposers.Parent.Order)),
			new Tuple<Relationship[], IQuery<Relationship>>(
				[Setup.Relationship3!],
				RelationshipFilter.AND(RelationshipExposers.Child.ObjectId.Equal(Setup.ChildObjectId(3))).ToQuery().OrderBy(RelationshipExposers.Parent.Order)),

			new Tuple<Relationship[], IQuery<Relationship>>(
				[Setup.Relationship2!],
				RelationshipFilter.AND(RelationshipExposers.Parent.ObjectName.Equal($"ParentName_B_{Setup.Prefix}")).ToQuery().OrderBy(RelationshipExposers.Parent.Order)),
			new Tuple<Relationship[], IQuery<Relationship>>(
				[Setup.Relationship2!],
				RelationshipFilter.AND(RelationshipExposers.Child.Url.Equal($"https://example.invalid/child/2/{Setup.Prefix}")).ToQuery().OrderBy(RelationshipExposers.Parent.Order)),

			new Tuple<Relationship[], IQuery<Relationship>>(
				[Setup.Relationship3!],
				RelationshipFilter.AND(RelationshipExposers.Parent.Order.Equal(3L)).ToQuery().OrderBy(RelationshipExposers.Parent.Order)),

			new Tuple<Relationship[], IQuery<Relationship>>(
				[],
				RelationshipFilter.AND(RelationshipExposers.Parent.ObjectId.Equal($"Unknown_{Setup.Prefix}")).ToQuery().OrderBy(RelationshipExposers.Parent.Order)),
		};

		/// <summary>
		/// Gets the expected objects, in the expected order, mapped to the applied query.
		/// </summary>
		private Tuple<RelationshipObjectType[], IQuery<RelationshipObjectType>>[] ObjectTypeQueryTestCases => new[]
		{
			new Tuple<RelationshipObjectType[], IQuery<RelationshipObjectType>>(
				[Setup.ObjectTypeA!, Setup.ObjectTypeB!, Setup.ObjectTypeC!],
				ObjectTypeFilter.ToQuery().OrderBy(RelationshipObjectTypeExposers.Name)),
			new Tuple<RelationshipObjectType[], IQuery<RelationshipObjectType>>(
				[Setup.ObjectTypeC!, Setup.ObjectTypeB!, Setup.ObjectTypeA!],
				ObjectTypeFilter.ToQuery().OrderByDescending(RelationshipObjectTypeExposers.Name)),

			new Tuple<RelationshipObjectType[], IQuery<RelationshipObjectType>>(
				[Setup.ObjectTypeB!],
				ObjectTypeFilter.AND(RelationshipObjectTypeExposers.Name.Equal($"ObjectType_B_{Setup.Prefix}")).ToQuery().OrderBy(RelationshipObjectTypeExposers.Name)),

			new Tuple<RelationshipObjectType[], IQuery<RelationshipObjectType>>(
				[],
				ObjectTypeFilter.AND(RelationshipObjectTypeExposers.Name.Equal($"Unknown_{Setup.Prefix}")).ToQuery().OrderBy(RelationshipObjectTypeExposers.Name)),
		};

		[TestMethod]
		public void ReadRelationshipsWithQuery()
		{
			foreach (var (expectedObjects, query) in RelationshipQueryTestCases)
			{
				QueryAssert.Read(TestContext.Api.Relationships, expectedObjects, query);
			}
		}

		[TestMethod]
		public void CountRelationshipsWithQuery()
		{
			foreach (var (expectedObjects, query) in RelationshipQueryTestCases)
			{
				QueryAssert.Count(TestContext.Api.Relationships, expectedObjects, query);
			}
		}

		[TestMethod]
		public void ReadRelationshipsPagedWithQuery_DefaultPageSize()
		{
			foreach (var (expectedObjects, query) in RelationshipQueryTestCases)
			{
				QueryAssert.ReadPaged(TestContext.Api.Relationships, expectedObjects, query);
			}
		}

		[TestMethod]
		public void ReadRelationshipsPagedWithQuery_CustomPageSize()
		{
			foreach (var (expectedObjects, query) in RelationshipQueryTestCases)
			{
				QueryAssert.ReadPaged(TestContext.Api.Relationships, expectedObjects, query, 2);
			}
		}

		[TestMethod]
		public void ReadRelationshipsWithLimitedQuery()
		{
			var query = RelationshipFilter.ToQuery().OrderBy(RelationshipExposers.Parent.Order).Limit(2);

			QueryAssert.Read(TestContext.Api.Relationships, [Setup.Relationship1!, Setup.Relationship2!], query);
		}

		[TestMethod]
		public void ReadRelationshipsWithFilter()
		{
			var filter = RelationshipFilter.AND(RelationshipExposers.Parent.ObjectTypeId.Equal(Setup.ObjectTypeA!.Id));

			var results = TestContext.Api.Relationships.Read(filter).ToList();

			Assert.AreEqual(2, results.Count);
			Assert.IsTrue(results.All(x => x.Parent.ObjectTypeId == Setup.ObjectTypeA!.Id));
		}

		[TestMethod]
		public void CountRelationshipsWithFilter()
		{
			Assert.AreEqual(3, TestContext.Api.Relationships.Count(RelationshipFilter));
			Assert.AreEqual(2, TestContext.Api.Relationships.Count(RelationshipFilter.AND(RelationshipExposers.Parent.ObjectTypeId.Equal(Setup.ObjectTypeA!.Id))));
		}

		[TestMethod]
		public void ReadObjectTypesWithQuery()
		{
			foreach (var (expectedObjects, query) in ObjectTypeQueryTestCases)
			{
				QueryAssert.Read(TestContext.Api.RelationshipObjectTypes, expectedObjects, query);
			}
		}

		[TestMethod]
		public void CountObjectTypesWithQuery()
		{
			foreach (var (expectedObjects, query) in ObjectTypeQueryTestCases)
			{
				QueryAssert.Count(TestContext.Api.RelationshipObjectTypes, expectedObjects, query);
			}
		}

		[TestMethod]
		public void ReadObjectTypesPagedWithQuery_DefaultPageSize()
		{
			foreach (var (expectedObjects, query) in ObjectTypeQueryTestCases)
			{
				QueryAssert.ReadPaged(TestContext.Api.RelationshipObjectTypes, expectedObjects, query);
			}
		}

		[TestMethod]
		public void ReadObjectTypesPagedWithQuery_CustomPageSize()
		{
			foreach (var (expectedObjects, query) in ObjectTypeQueryTestCases)
			{
				QueryAssert.ReadPaged(TestContext.Api.RelationshipObjectTypes, expectedObjects, query, 2);
			}
		}

		[TestMethod]
		public void ReadObjectTypesWithLimitedQuery()
		{
			var query = ObjectTypeFilter.ToQuery().OrderBy(RelationshipObjectTypeExposers.Name).Limit(2);

			QueryAssert.Read(TestContext.Api.RelationshipObjectTypes, [Setup.ObjectTypeA!, Setup.ObjectTypeB!], query);
		}

		[TestMethod]
		public void CountObjectTypesWithFilter()
		{
			Assert.AreEqual(3, TestContext.Api.RelationshipObjectTypes.Count(ObjectTypeFilter));
		}
	}
}
