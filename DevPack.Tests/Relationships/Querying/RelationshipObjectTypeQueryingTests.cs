namespace RT_MediaOps.Plan.Relationships.Querying
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.SDM;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	using SLDataGateway.API.Querying;
	using SLDataGateway.API.Types.Querying;

	[TestClass]
	[TestCategory("IntegrationTest")]
	public sealed class RelationshipObjectTypeQueryingTests
	{
		private static TestObjectCreator? objectCreator;
		private static string? prefix;
		private static RelationshipObjectType[]? objectTypes;

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		private static string Prefix => prefix ?? throw new InvalidOperationException("Test setup was not initialized.");

		private static RelationshipObjectType[] ObjectTypes => objectTypes ?? throw new InvalidOperationException("Test setup was not initialized.");

		private static RelationshipObjectType ObjectTypeA => ObjectTypes[0];

		private static RelationshipObjectType ObjectTypeB => ObjectTypes[1];

		private static RelationshipObjectType ObjectTypeC => ObjectTypes[2];

		private FilterElement<RelationshipObjectType> ObjectTypeFilter => new ORFilterElement<RelationshipObjectType>(ObjectTypes.Select(x => RelationshipObjectTypeExposers.Id.Equal(x.Id)).ToArray());

		/// <summary>
		/// Gets the expected objects, in the expected order, mapped to the applied query.
		/// </summary>
		private Tuple<RelationshipObjectType[], IQuery<RelationshipObjectType>>[] QueryTestCases => new[]
		{
			new Tuple<RelationshipObjectType[], IQuery<RelationshipObjectType>>(
				[ObjectTypeA, ObjectTypeB, ObjectTypeC],
				ObjectTypeFilter.ToQuery().OrderBy(RelationshipObjectTypeExposers.Name)),
			new Tuple<RelationshipObjectType[], IQuery<RelationshipObjectType>>(
				[ObjectTypeC, ObjectTypeB, ObjectTypeA],
				ObjectTypeFilter.ToQuery().OrderByDescending(RelationshipObjectTypeExposers.Name)),
			new Tuple<RelationshipObjectType[], IQuery<RelationshipObjectType>>(
				[ObjectTypeB],
				ObjectTypeFilter.AND(RelationshipObjectTypeExposers.Name.Equal($"ObjectType_B_{Prefix}")).ToQuery().OrderBy(RelationshipObjectTypeExposers.Name)),
			new Tuple<RelationshipObjectType[], IQuery<RelationshipObjectType>>(
				[ObjectTypeA, ObjectTypeC],
				ObjectTypeFilter.AND(RelationshipObjectTypeExposers.Id.NotEqual(ObjectTypeB.Id)).ToQuery().OrderBy(RelationshipObjectTypeExposers.Name)),
			new Tuple<RelationshipObjectType[], IQuery<RelationshipObjectType>>(
				[],
				ObjectTypeFilter.AND(RelationshipObjectTypeExposers.Name.Equal($"Unknown_{Prefix}")).ToQuery().OrderBy(RelationshipObjectTypeExposers.Name)),
		};

		/// <summary>
		/// Gets the expected objects, in the expected order, mapped to the applied query with a limit.
		/// </summary>
		private Tuple<RelationshipObjectType[], IQuery<RelationshipObjectType>>[] LimitedQueryTestCases => new[]
		{
			new Tuple<RelationshipObjectType[], IQuery<RelationshipObjectType>>(
				[ObjectTypeA],
				ObjectTypeFilter.ToQuery().OrderBy(RelationshipObjectTypeExposers.Name).Limit(1)),
			new Tuple<RelationshipObjectType[], IQuery<RelationshipObjectType>>(
				[ObjectTypeC, ObjectTypeB],
				ObjectTypeFilter.ToQuery().OrderByDescending(RelationshipObjectTypeExposers.Name).Limit(2)),
			new Tuple<RelationshipObjectType[], IQuery<RelationshipObjectType>>(
				[ObjectTypeA, ObjectTypeB, ObjectTypeC],
				ObjectTypeFilter.ToQuery().OrderBy(RelationshipObjectTypeExposers.Name).Limit(10)),
		};

		[ClassInitialize]
		public static void ClassInitialize(TestContext context)
		{
			objectCreator = new TestObjectCreator(TestContext);
			prefix = Guid.NewGuid().ToString();
			objectTypes = new[]
			{
				objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"ObjectType_A_{prefix}" }),
				objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"ObjectType_B_{prefix}" }),
				objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"ObjectType_C_{prefix}" }),
			};
		}

		[ClassCleanup(ClassCleanupBehavior.EndOfClass)]
		public static void ClassCleanup()
		{
			objectCreator?.Dispose();
			objectCreator = null;
			objectTypes = null;
			prefix = null;
		}

		[TestMethod]
		public void ReadPartialObjectTypesWithQuery_ReturnsSameObjectTypesInSameOrder()
		{
			foreach (var (expectedObjects, query) in QueryTestCases.Concat(LimitedQueryTestCases))
			{
				var partialObjectTypes = TestContext.Api.RelationshipObjectTypes.ReadPartial(query.WithFields(RelationshipObjectTypeExposers.Name)).ToList();

				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), partialObjectTypes.Select(x => x.Id).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Name).ToList(), partialObjectTypes.Select(x => x.GetValue(RelationshipObjectTypeExposers.Name)).ToList(), $"Query: {query}");
			}
		}

		[TestMethod]
		public void ReadPartialPagedObjectTypesWithQuery_DefaultPageSize_ReturnsSameObjectTypesInOnePage()
		{
			foreach (var (expectedObjects, query) in QueryTestCases.Concat(LimitedQueryTestCases))
			{
				var pages = TestContext.Api.RelationshipObjectTypes.ReadPartialPaged(query.WithFields(RelationshipObjectTypeExposers.Name)).ToList();

				Assert.AreEqual(1, pages.Count, $"Query: {query}");
				Assert.IsFalse(pages[0].HasNextPage, $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), pages[0].Select(x => x.Id).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Name).ToList(), pages[0].Select(x => x.GetValue(RelationshipObjectTypeExposers.Name)).ToList(), $"Query: {query}");
			}
		}

		[TestMethod]
		public void ReadPartialPagedObjectTypesWithQuery_CustomPageSize_SplitsObjectTypesOverPages()
		{
			var repository = (RelationshipObjectTypesRepository)TestContext.Api.RelationshipObjectTypes;

			foreach (var (expectedObjects, query) in QueryTestCases.Concat(LimitedQueryTestCases))
			{
				var pages = repository.ReadPartialPaged(query.WithFields(RelationshipObjectTypeExposers.Name), 2).ToList();

				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), pages.SelectMany(x => x).Select(x => x.Id).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(Enumerable.Range(0, pages.Count).ToList(), pages.Select(x => x.PageNumber).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(pages.Select((_, i) => i < pages.Count - 1).ToList(), pages.Select(x => x.HasNextPage).ToList(), $"Query: {query}");
				Assert.IsTrue(pages.All(x => x.Count <= 2), $"Query: {query}");
			}

			Assert.IsTrue(QueryTestCases.Any(x => x.Item1.Length > 2), "No query returns more than one page.");
		}

		[TestMethod]
		public void ReadPartialPagedObjectTypesWithFilter_ReturnsAllObjectTypes()
		{
			var pages = TestContext.Api.RelationshipObjectTypes.ReadPartialPaged(ObjectTypeFilter.WithFields(RelationshipObjectTypeExposers.Name)).ToList();
			var objectTypesById = TestContext.Api.RelationshipObjectTypes.Read(ObjectTypeFilter).ToDictionary(x => x.Id);

			Assert.AreEqual(1, pages.Count);
			CollectionAssert.AreEquivalent(objectTypesById.Keys.ToList(), pages[0].Select(x => x.Id).ToList());

			foreach (var partialObjectType in pages[0])
			{
				Assert.AreEqual(objectTypesById[partialObjectType.Id].Name, partialObjectType.GetValue(RelationshipObjectTypeExposers.Name));
			}
		}

		[TestMethod]
		public void ReadPartialObjectTypesWithFilter_ReturnsSameValuesAsFullRead()
		{
			var partialObjectTypesById = TestContext.Api.RelationshipObjectTypes
				.ReadPartial(ObjectTypeFilter.WithFields(RelationshipObjectTypeExposers.Id, RelationshipObjectTypeExposers.Name))
				.ToDictionary(x => x.Id);
			var fullObjectTypes = TestContext.Api.RelationshipObjectTypes.Read(ObjectTypeFilter).ToList();

			Assert.AreEqual(ObjectTypes.Length, fullObjectTypes.Count);
			Assert.AreEqual(fullObjectTypes.Count, partialObjectTypesById.Count);

			foreach (var objectType in fullObjectTypes)
			{
				var partialObjectType = partialObjectTypesById[objectType.Id];

				Assert.AreEqual(objectType.Id, partialObjectType.GetValue(RelationshipObjectTypeExposers.Id));
				Assert.AreEqual(objectType.Name, partialObjectType.GetValue(RelationshipObjectTypeExposers.Name));
			}
		}

		[TestMethod]
		public void ReadPartialObjectTypes_OnlyId_ReturnsIds()
		{
			var partialObjectTypes = TestContext.Api.RelationshipObjectTypes.ReadPartial(ObjectTypeFilter.WithFields(RelationshipObjectTypeExposers.Id)).ToList();

			CollectionAssert.AreEquivalent(ObjectTypes.Select(x => x.Id).ToList(), partialObjectTypes.Select(x => x.GetValue(RelationshipObjectTypeExposers.Id)).ToList());
		}

		[TestMethod]
		public void ReadPartialObjectTypes_UnknownField_ThrowsBeforeReading()
		{
			var unknownField = new Exposer<RelationshipObjectType, string>(x => x.Name, "Unknown");

			Assert.ThrowsException<NotSupportedException>(() => TestContext.Api.RelationshipObjectTypes.ReadPartial(ObjectTypeFilter.WithFields(unknownField)));
		}
	}
}
