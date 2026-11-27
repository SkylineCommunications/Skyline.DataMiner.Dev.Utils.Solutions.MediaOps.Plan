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
		/// Gets the expected objects, in the expected order, mapped to the applied query with a limit.
		/// </summary>
		private Tuple<Relationship[], IQuery<Relationship>>[] RelationshipLimitedQueryTestCases => new[]
		{
			new Tuple<Relationship[], IQuery<Relationship>>(
				[Setup.Relationship1!],
				RelationshipFilter.ToQuery().OrderBy(RelationshipExposers.Parent.Order).Limit(1)),
			new Tuple<Relationship[], IQuery<Relationship>>(
				[Setup.Relationship1!, Setup.Relationship2!],
				RelationshipFilter.ToQuery().OrderBy(RelationshipExposers.Parent.Order).Limit(2)),
			new Tuple<Relationship[], IQuery<Relationship>>(
				[Setup.Relationship3!, Setup.Relationship2!],
				RelationshipFilter.ToQuery().OrderBy(RelationshipExposers.Child.Order).Limit(2)),
			new Tuple<Relationship[], IQuery<Relationship>>(
				[Setup.Relationship1!, Setup.Relationship2!, Setup.Relationship3!],
				RelationshipFilter.ToQuery().OrderBy(RelationshipExposers.Parent.ObjectName).Limit(10)),
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
		public void ReadRelationshipsPagedWithLimitedQuery()
		{
			var query = RelationshipFilter.ToQuery().OrderBy(RelationshipExposers.Parent.Order).Limit(2);

			QueryAssert.ReadPaged(TestContext.Api.Relationships, [Setup.Relationship1!, Setup.Relationship2!], query);
			QueryAssert.ReadPaged(TestContext.Api.Relationships, [Setup.Relationship1!, Setup.Relationship2!], query, 1);
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
		public void ReadPartialRelationshipsWithQuery_ReturnsSameRelationshipsInSameOrder()
		{
			foreach (var (expectedObjects, query) in RelationshipQueryTestCases.Concat(RelationshipLimitedQueryTestCases))
			{
				var partialRelationships = TestContext.Api.Relationships.ReadPartial(SelectIdAndParentName(query)).ToList();

				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), partialRelationships.Select(x => x.GetValue(RelationshipExposers.Id)).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Parent.ObjectName).ToList(), partialRelationships.Select(x => x.GetValue(RelationshipExposers.Parent.ObjectName)).ToList(), $"Query: {query}");
			}
		}

		[TestMethod]
		public void ReadPartialPagedRelationshipsWithQuery_DefaultPageSize_ReturnsSameRelationshipsInOnePage()
		{
			foreach (var (expectedObjects, query) in RelationshipQueryTestCases.Concat(RelationshipLimitedQueryTestCases))
			{
				var pages = TestContext.Api.Relationships.ReadPartialPaged(SelectIdAndParentName(query)).ToList();

				Assert.AreEqual(1, pages.Count, $"Query: {query}");
				Assert.IsFalse(pages[0].HasNextPage, $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), pages[0].Select(x => x.GetValue(RelationshipExposers.Id)).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Parent.ObjectName).ToList(), pages[0].Select(x => x.GetValue(RelationshipExposers.Parent.ObjectName)).ToList(), $"Query: {query}");
			}
		}

		[TestMethod]
		public void ReadPartialPagedRelationshipsWithQuery_CustomPageSize_SplitsRelationshipsOverPages()
		{
			foreach (var (expectedObjects, query) in RelationshipQueryTestCases.Concat(RelationshipLimitedQueryTestCases))
			{
				var pages = TestContext.Api.Relationships.ReadPartialPaged(SelectIdAndParentName(query), 2).ToList();

				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), pages.SelectMany(x => x).Select(x => x.GetValue(RelationshipExposers.Id)).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(Enumerable.Range(0, pages.Count).ToList(), pages.Select(x => x.PageNumber).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(pages.Select((_, i) => i < pages.Count - 1).ToList(), pages.Select(x => x.HasNextPage).ToList(), $"Query: {query}");
				Assert.IsTrue(pages.All(x => x.Count <= 2), $"Query: {query}");
			}

			Assert.IsTrue(RelationshipQueryTestCases.Any(x => x.Item1.Length > 2), "No query returns more than one page.");
		}

		[TestMethod]
		public void ReadPartialPagedRelationshipsWithFilter_ReturnsAllRelationships()
		{
			var partialQuery = new PartialQuery<Relationship>(RelationshipFilter.ToQuery())
				.Include(RelationshipExposers.Id)
				.Include(RelationshipExposers.Parent.ObjectId)
				.Include(RelationshipExposers.Child.ObjectId);

			var pages = TestContext.Api.Relationships.ReadPartialPaged(partialQuery).ToList();
			var relationshipsById = TestContext.Api.Relationships.Read(RelationshipFilter).ToDictionary(x => x.Id);

			Assert.AreEqual(1, pages.Count);
			CollectionAssert.AreEquivalent(relationshipsById.Keys.ToList(), pages[0].Select(x => x.GetValue(RelationshipExposers.Id)).ToList());

			foreach (var partialRelationship in pages[0])
			{
				var relationship = relationshipsById[partialRelationship.GetValue(RelationshipExposers.Id)];
				Assert.AreEqual(relationship.Parent.ObjectId, partialRelationship.GetValue(RelationshipExposers.Parent.ObjectId));
				Assert.AreEqual(relationship.Child.ObjectId, partialRelationship.GetValue(RelationshipExposers.Child.ObjectId));
			}
		}

		[TestMethod]
		public void ReadPartialRelationshipsWithFilter_ReturnsSameValuesAsFullRead()
		{
			var partialQuery = new PartialQuery<Relationship>(RelationshipFilter.ToQuery())
				.Include(RelationshipExposers.Id)
				.Include(RelationshipExposers.Parent.ObjectTypeId)
				.Include(RelationshipExposers.Parent.ObjectId)
				.Include(RelationshipExposers.Parent.ObjectName)
				.Include(RelationshipExposers.Parent.Url)
				.Include(RelationshipExposers.Parent.Order)
				.Include(RelationshipExposers.Child.ObjectTypeId)
				.Include(RelationshipExposers.Child.ObjectId)
				.Include(RelationshipExposers.Child.ObjectName)
				.Include(RelationshipExposers.Child.Url)
				.Include(RelationshipExposers.Child.Order);

			var partialRelationshipsById = TestContext.Api.Relationships.ReadPartial(partialQuery).ToDictionary(x => x.GetValue(RelationshipExposers.Id));
			var relationships = TestContext.Api.Relationships.Read(RelationshipFilter).ToList();

			Assert.AreEqual(Setup.Relationships.Length, relationships.Count);
			Assert.AreEqual(relationships.Count, partialRelationshipsById.Count);

			foreach (var relationship in relationships)
			{
				var partialRelationship = partialRelationshipsById[relationship.Id];

				Assert.AreEqual(relationship.Id, partialRelationship.GetValue(RelationshipExposers.Id));
				Assert.AreEqual(relationship.Parent.ObjectTypeId, partialRelationship.GetValue(RelationshipExposers.Parent.ObjectTypeId));
				Assert.AreEqual(relationship.Parent.ObjectId, partialRelationship.GetValue(RelationshipExposers.Parent.ObjectId));
				Assert.AreEqual(relationship.Parent.ObjectName, partialRelationship.GetValue(RelationshipExposers.Parent.ObjectName));
				Assert.AreEqual(relationship.Parent.Url, partialRelationship.GetValue(RelationshipExposers.Parent.Url));
				Assert.AreEqual(relationship.Parent.Order, partialRelationship.GetValue(RelationshipExposers.Parent.Order));
				Assert.AreEqual(relationship.Child.ObjectTypeId, partialRelationship.GetValue(RelationshipExposers.Child.ObjectTypeId));
				Assert.AreEqual(relationship.Child.ObjectId, partialRelationship.GetValue(RelationshipExposers.Child.ObjectId));
				Assert.AreEqual(relationship.Child.ObjectName, partialRelationship.GetValue(RelationshipExposers.Child.ObjectName));
				Assert.AreEqual(relationship.Child.Url, partialRelationship.GetValue(RelationshipExposers.Child.Url));
				Assert.AreEqual(relationship.Child.Order, partialRelationship.GetValue(RelationshipExposers.Child.Order));
			}
		}

		[TestMethod]
		public void ReadPartialRelationships_OnlyId_ReturnsIds()
		{
			var partialRelationships = TestContext.Api.Relationships.ReadPartial(new PartialQuery<Relationship>(RelationshipFilter.ToQuery()).Include(RelationshipExposers.Id)).ToList();

			CollectionAssert.AreEquivalent(Setup.Relationships.Select(x => x.Id).ToList(), partialRelationships.Select(x => x.GetValue(RelationshipExposers.Id)).ToList());
		}

		[TestMethod]
		public void ReadPartialRelationships_UnknownField_ThrowsBeforeReading()
		{
			var unknownField = new Exposer<Relationship, string>(x => x.Parent.ObjectId, "Unknown");

			Assert.ThrowsException<NotSupportedException>(() => TestContext.Api.Relationships.ReadPartial(new PartialQuery<Relationship>(RelationshipFilter.ToQuery()).Include(unknownField)));
		}

		private static IPartialQuery<Relationship> SelectIdAndParentName(IQuery<Relationship> query)
		{
			return new PartialQuery<Relationship>(query).Include(RelationshipExposers.Id).Include(RelationshipExposers.Parent.ObjectName);
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
		public void ReadObjectTypesPagedWithLimitedQuery()
		{
			var query = ObjectTypeFilter.ToQuery().OrderBy(RelationshipObjectTypeExposers.Name).Limit(2);

			QueryAssert.ReadPaged(TestContext.Api.RelationshipObjectTypes, [Setup.ObjectTypeA!, Setup.ObjectTypeB!], query);
			QueryAssert.ReadPaged(TestContext.Api.RelationshipObjectTypes, [Setup.ObjectTypeA!, Setup.ObjectTypeB!], query, 1);
		}

		[TestMethod]
		public void CountObjectTypesWithFilter()
		{
			Assert.AreEqual(3, TestContext.Api.RelationshipObjectTypes.Count(ObjectTypeFilter));
		}
	}
}
