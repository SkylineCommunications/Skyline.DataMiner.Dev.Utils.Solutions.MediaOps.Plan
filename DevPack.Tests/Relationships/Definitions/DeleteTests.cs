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
	public sealed class DeleteTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public DeleteTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void DeleteObjectTypeInUseThrowsException()
		{
			var objectType = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{Guid.NewGuid()}_ObjectType" });

			var relationship = objectCreator.CreateRelationship(new Relationship(new RelationshipData
			{
				Parent = new RelationshipEndpoint(objectType, Guid.NewGuid().ToString()),
				Child = new RelationshipEndpoint(objectType, Guid.NewGuid().ToString()),
			}));

			var exception = Assert.ThrowsException<MediaOpsException>(() => TestContext.Api.RelationshipObjectTypes.Delete(objectType));

			var error = exception.TraceData.ErrorData.OfType<RelationshipObjectTypeInUseError>().SingleOrDefault();
			Assert.IsNotNull(error);
			Assert.AreEqual(objectType.Id, error.Id);
			CollectionAssert.AreEquivalent(new[] { relationship.Id }, error.RelationshipIds.ToArray());

			Assert.IsNotNull(TestContext.Api.RelationshipObjectTypes.Read(objectType.Id), "The object type must survive a rejected delete.");
			Assert.IsNotNull(TestContext.Api.Relationships.Read(relationship.Id), "The relationship must not be touched by a rejected delete.");
		}

		[TestMethod]
		public void DeleteObjectTypeSucceedsAfterRelationshipIsRemoved()
		{
			var objectType = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{Guid.NewGuid()}_ObjectType" });

			var relationship = objectCreator.CreateRelationship(new Relationship(new RelationshipData
			{
				Parent = new RelationshipEndpoint(objectType, Guid.NewGuid().ToString()),
				Child = new RelationshipEndpoint(objectType, Guid.NewGuid().ToString()),
			}));

			Assert.ThrowsException<MediaOpsException>(() => TestContext.Api.RelationshipObjectTypes.Delete(objectType));

			TestContext.Api.Relationships.Delete(relationship);
			TestContext.Api.RelationshipObjectTypes.Delete(objectType);

			Assert.IsNull(TestContext.Api.RelationshipObjectTypes.Read(objectType.Id));
		}

		[TestMethod]
		public void DeleteUnusedObjectTypeSucceeds()
		{
			var objectType = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{Guid.NewGuid()}_ObjectType" });

			TestContext.Api.RelationshipObjectTypes.Delete(objectType);

			Assert.IsNull(TestContext.Api.RelationshipObjectTypes.Read(objectType.Id));
		}

		[TestMethod]
		public void DeleteUnusedObjectTypesInBulkSucceeds()
		{
			var objectTypeA = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{Guid.NewGuid()}_ObjectTypeA" });
			var objectTypeB = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{Guid.NewGuid()}_ObjectTypeB" });

			TestContext.Api.RelationshipObjectTypes.Delete([objectTypeA, objectTypeB]);

			Assert.IsNull(TestContext.Api.RelationshipObjectTypes.Read(objectTypeA.Id));
			Assert.IsNull(TestContext.Api.RelationshipObjectTypes.Read(objectTypeB.Id));
		}

		[TestMethod]
		public void DeleteObjectTypesInBulkReportsOnlyTheOneInUse()
		{
			var used = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{Guid.NewGuid()}_Used" });
			var unused = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{Guid.NewGuid()}_Unused" });

			objectCreator.CreateRelationship(new Relationship(new RelationshipData
			{
				Parent = new RelationshipEndpoint(used, Guid.NewGuid().ToString()),
				Child = new RelationshipEndpoint(used, Guid.NewGuid().ToString()),
			}));

			var exception = Assert.ThrowsException<MediaOpsBulkException<Guid>>(() => TestContext.Api.RelationshipObjectTypes.Delete([used, unused]));

			CollectionAssert.AreEquivalent(new[] { used.Id }, exception.Result.UnsuccessfulIds.ToArray());
			Assert.IsNotNull(TestContext.Api.RelationshipObjectTypes.Read(used.Id));
			Assert.IsNull(TestContext.Api.RelationshipObjectTypes.Read(unused.Id));
		}
	}
}
