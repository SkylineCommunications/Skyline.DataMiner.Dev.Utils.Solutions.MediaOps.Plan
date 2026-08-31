namespace RT_MediaOps.Plan.Relationships.Definitions
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	[TestClass]
	[TestCategory("IntegrationTest")]
	[DoNotParallelize]
	public sealed class BasicTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public BasicTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void BasicCrudActions()
		{
			var objectTypeId = Guid.NewGuid();
			var name = $"{objectTypeId}_ObjectType";

			var objectType = new RelationshipObjectType(objectTypeId)
			{
				Name = name,
			};

			objectCreator.CreateRelationshipObjectType(objectType);

			var returned = TestContext.Api.RelationshipObjectTypes.Read(objectTypeId);
			Assert.IsNotNull(returned);
			Assert.AreEqual(name, returned.Name);

			var updatedName = name + "_Updated";
			returned.Name = updatedName;
			TestContext.Api.RelationshipObjectTypes.Update(returned);

			returned = TestContext.Api.RelationshipObjectTypes.Read(objectTypeId);
			Assert.IsNotNull(returned);
			Assert.AreEqual(updatedName, returned.Name);

			TestContext.Api.RelationshipObjectTypes.Delete(returned);

			Assert.IsNull(TestContext.Api.RelationshipObjectTypes.Read(objectTypeId));
		}

		[TestMethod]
		public void CreateWithExistingIdThrowsException()
		{
			var objectTypeId = Guid.NewGuid();

			objectCreator.CreateRelationshipObjectType(new RelationshipObjectType(objectTypeId) { Name = $"{objectTypeId}_A" });

			var exception = Assert.ThrowsException<MediaOpsException>(
				() => objectCreator.CreateRelationshipObjectType(new RelationshipObjectType(objectTypeId) { Name = $"{objectTypeId}_B" }));

			var error = exception.TraceData.ErrorData.OfType<RelationshipObjectTypeIdInUseError>().SingleOrDefault();
			Assert.IsNotNull(error);
			Assert.AreEqual(objectTypeId, error.Id);
		}

		[TestMethod]
		public void CreateWithIdOfExistingRelationshipThrowsException()
		{
			var objectType = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{Guid.NewGuid()}_ObjectType" });

			var relationship = objectCreator.CreateRelationship(new Relationship(new RelationshipData
			{
				Parent = new RelationshipEndpoint(objectType, Guid.NewGuid().ToString()),
				Child = new RelationshipEndpoint(objectType, Guid.NewGuid().ToString()),
			}));

			// DOM instance IDs are unique per module, so a link ID collides with an object type ID.
			var exception = Assert.ThrowsException<MediaOpsException>(
				() => objectCreator.CreateRelationshipObjectType(new RelationshipObjectType(relationship.Id) { Name = $"{Guid.NewGuid()}_ObjectType" }));

			var error = exception.TraceData.ErrorData.OfType<RelationshipObjectTypeIdInUseError>().SingleOrDefault();
			Assert.IsNotNull(error);
			Assert.AreEqual(relationship.Id, error.Id);
		}

		[TestMethod]
		public void CreateWithExistingNameThrowsException()
		{
			var name = $"{Guid.NewGuid()}_ObjectType";

			objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = name });

			var exception = Assert.ThrowsException<MediaOpsException>(
				() => objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = name }));

			var error = exception.TraceData.ErrorData.OfType<RelationshipObjectTypeNameExistsError>().SingleOrDefault();
			Assert.IsNotNull(error);
			Assert.AreEqual(name, error.Name);
		}

		[TestMethod]
		public void CreateWithEmptyNameThrowsException()
		{
			var exception = Assert.ThrowsException<MediaOpsException>(
				() => objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = " " }));

			Assert.IsNotNull(exception.TraceData.ErrorData.OfType<RelationshipObjectTypeInvalidNameError>().SingleOrDefault());
		}

		[TestMethod]
		public void CreateWithReservedNameThrowsException()
		{
			var exception = Assert.ThrowsException<MediaOpsException>(
				() => objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = RelationshipObjectType.JobObjectTypeName }));

			var error = exception.TraceData.ErrorData.OfType<RelationshipObjectTypeReservedNameError>().SingleOrDefault();
			Assert.IsNotNull(error);
			Assert.AreEqual(RelationshipObjectType.JobObjectTypeName, error.Name);
		}

		[TestMethod]
		public void CreateAndUpdateInBulk()
		{
			var prefix = Guid.NewGuid().ToString();

			var created = objectCreator.CreateRelationshipObjectTypes(
			[
				new RelationshipObjectType { Name = $"{prefix}_A" },
				new RelationshipObjectType { Name = $"{prefix}_B" },
			]);

			Assert.AreEqual(2, created.Count);

			foreach (var objectType in created)
			{
				objectType.Name += "_Updated";
			}

			TestContext.Api.RelationshipObjectTypes.Update(created);

			var reread = TestContext.Api.RelationshipObjectTypes.Read(created.Select(x => x.Id)).ToList();
			Assert.AreEqual(2, reread.Count);
			Assert.IsTrue(reread.All(x => x.Name.EndsWith("_Updated")));
		}

		[TestMethod]
		public void CreateDuplicateNamesInSameBatchThrowsException()
		{
			var name = $"{Guid.NewGuid()}_ObjectType";

			var exception = Assert.ThrowsException<MediaOpsBulkException<Guid>>(() => objectCreator.CreateRelationshipObjectTypes(
			[
				new RelationshipObjectType { Name = name },
				new RelationshipObjectType { Name = name },
			]));

			Assert.AreEqual(2, exception.Result.UnsuccessfulIds.Count);
			Assert.IsTrue(exception.Result.TraceDataPerItem.Values.SelectMany(x => x.ErrorData).OfType<RelationshipObjectTypeDuplicateNameError>().Any());
		}

		[TestMethod]
		public void ReadUnknownObjectTypeReturnsNull()
		{
			Assert.IsNull(TestContext.Api.RelationshipObjectTypes.Read(Guid.NewGuid()));
		}
	}
}
