namespace RT_MediaOps.Plan.Relationships.Links
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
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
			var parentType = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{Guid.NewGuid()}_Parent" });
			var childType = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{Guid.NewGuid()}_Child" });

			var relationshipId = Guid.NewGuid();
			var parentObjectId = Guid.NewGuid().ToString();
			var childObjectId = Guid.NewGuid().ToString();

			var relationship = new Relationship(relationshipId, new RelationshipData
			{
				Parent = new RelationshipEndpoint(parentType, parentObjectId)
				{
					ObjectName = "Parent object",
					Url = "https://example.invalid/parent",
					Order = 1,
				},
				Child = new RelationshipEndpoint(childType, childObjectId)
				{
					ObjectName = "Child object",
					Url = "https://example.invalid/child",
					Order = 2,
				},
			});

			objectCreator.CreateRelationship(relationship);

			var returned = TestContext.Api.Relationships.Read(relationshipId);
			Assert.IsNotNull(returned);
			Assert.AreEqual(parentType.Id, returned.Parent.ObjectTypeId);
			Assert.AreEqual(parentObjectId, returned.Parent.ObjectId);
			Assert.AreEqual("Parent object", returned.Parent.ObjectName);
			Assert.AreEqual("https://example.invalid/parent", returned.Parent.Url);
			Assert.AreEqual(1, returned.Parent.Order);
			Assert.AreEqual(childType.Id, returned.Child.ObjectTypeId);
			Assert.AreEqual(childObjectId, returned.Child.ObjectId);
			Assert.AreEqual(2, returned.Child.Order);

			returned.Child.ObjectName = "Renamed child";
			returned.Child.Order = 5;
			TestContext.Api.Relationships.Update(returned);

			returned = TestContext.Api.Relationships.Read(relationshipId);
			Assert.IsNotNull(returned);
			Assert.AreEqual("Renamed child", returned.Child.ObjectName);
			Assert.AreEqual(5, returned.Child.Order);

			TestContext.Api.Relationships.Delete(returned);

			Assert.IsNull(TestContext.Api.Relationships.Read(relationshipId));
		}

		[TestMethod]
		public void ReadKeepsObjectTypeIds()
		{
			var parentType = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{Guid.NewGuid()}_Parent" });
			var childType = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{Guid.NewGuid()}_Child" });

			var relationship = objectCreator.CreateRelationship(new Relationship(new RelationshipData
			{
				Parent = new RelationshipEndpoint(parentType, Guid.NewGuid().ToString()),
				Child = new RelationshipEndpoint(childType, Guid.NewGuid().ToString()),
			}));

			var returned = TestContext.Api.Relationships.Read(relationship.Id);
			Assert.IsNotNull(returned);
			Assert.AreEqual(parentType.Id, returned.Parent.ObjectTypeId);
			Assert.AreEqual(childType.Id, returned.Child.ObjectTypeId);
		}

		[TestMethod]
		public void ReadByParentObjectId()
		{
			var objectType = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{Guid.NewGuid()}_ObjectType" });
			var parentObjectId = Guid.NewGuid().ToString();

			var relationship = objectCreator.CreateRelationship(new Relationship(new RelationshipData
			{
				Parent = new RelationshipEndpoint(objectType, parentObjectId),
				Child = new RelationshipEndpoint(objectType, Guid.NewGuid().ToString()),
			}));

			var filter = RelationshipExposers.Parent.ObjectTypeId.Equal(objectType.Id)
				.AND(RelationshipExposers.Parent.ObjectId.Equal(parentObjectId));

			var results = TestContext.Api.Relationships.Read(filter).ToList();
			Assert.AreEqual(1, results.Count);
			Assert.AreEqual(relationship.Id, results[0].Id);
		}

		[TestMethod]
		public void CreateWithUnknownObjectTypeThrowsException()
		{
			var objectType = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{Guid.NewGuid()}_ObjectType" });
			var unknownObjectTypeId = Guid.NewGuid();

			var exception = Assert.ThrowsException<MediaOpsException>(() => objectCreator.CreateRelationship(new Relationship(new RelationshipData
			{
				Parent = new RelationshipEndpoint(objectType, Guid.NewGuid().ToString()),
				Child = new RelationshipEndpoint(unknownObjectTypeId, Guid.NewGuid().ToString()),
			})));

			var error = exception.TraceData.ErrorData.OfType<RelationshipInvalidObjectTypeError>().SingleOrDefault();
			Assert.IsNotNull(error);
			StringAssert.Contains(error.ErrorMessage, unknownObjectTypeId.ToString());
		}

		[TestMethod]
		public void CreateWithIdOfExistingObjectTypeThrowsException()
		{
			var objectType = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{Guid.NewGuid()}_ObjectType" });

			// DOM instance IDs are unique per module, so an object type ID collides with a link ID.
			var exception = Assert.ThrowsException<MediaOpsException>(() => objectCreator.CreateRelationship(new Relationship(objectType.Id, new RelationshipData
			{
				Parent = new RelationshipEndpoint(objectType, Guid.NewGuid().ToString()),
				Child = new RelationshipEndpoint(objectType, Guid.NewGuid().ToString()),
			})));

			var error = exception.TraceData.ErrorData.OfType<RelationshipIdInUseError>().SingleOrDefault();
			Assert.IsNotNull(error);
			Assert.AreEqual(objectType.Id, error.Id);
		}

		[TestMethod]
		public void CreateWithoutObjectIdThrowsException()
		{
			var objectType = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{Guid.NewGuid()}_ObjectType" });

			var relationship = new Relationship();
			relationship.Parent.ObjectTypeId = objectType.Id;
			relationship.Child.ObjectTypeId = objectType.Id;

			var exception = Assert.ThrowsException<MediaOpsException>(() => objectCreator.CreateRelationship(relationship));

			Assert.IsNotNull(exception.TraceData.ErrorData.OfType<RelationshipInvalidEndpointError>().SingleOrDefault());
		}

		[TestMethod]
		public void CreateAndUpdateInBulk()
		{
			var objectType = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{Guid.NewGuid()}_ObjectType" });

			var created = objectCreator.CreateRelationships(
			[
				NewRelationship(objectType, 1),
				NewRelationship(objectType, 2),
			]);

			Assert.AreEqual(2, created.Count);

			var toUpdate = created.ToList();
			foreach (var relationship in toUpdate)
			{
				relationship.Child.ObjectName = "Bulk renamed";
			}

			TestContext.Api.Relationships.Update(toUpdate);

			var reread = TestContext.Api.Relationships.Read(toUpdate.Select(x => x.Id)).ToList();
			Assert.AreEqual(2, reread.Count);
			Assert.IsTrue(reread.All(x => x.Child.ObjectName == "Bulk renamed"));
		}

		[TestMethod]
		public void DeleteInBulk()
		{
			var objectType = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{Guid.NewGuid()}_ObjectType" });

			var created = objectCreator.CreateRelationships(
			[
				NewRelationship(objectType, 1),
				NewRelationship(objectType, 2),
			]);

			TestContext.Api.Relationships.Delete(created.Select(x => x.Id).ToList());

			Assert.AreEqual(0, TestContext.Api.Relationships.Read(created.Select(x => x.Id)).Count());
		}

		[TestMethod]
		public void ReadUnknownRelationshipReturnsNull()
		{
			Assert.IsNull(TestContext.Api.Relationships.Read(Guid.NewGuid()));
		}

		[TestMethod]
		public void EmptyOptionalFieldsRoundTripAsNull()
		{
			var objectType = objectCreator.CreateRelationshipObjectType(new RelationshipObjectType { Name = $"{Guid.NewGuid()}_ObjectType" });

			var created = objectCreator.CreateRelationship(new Relationship(new RelationshipData
			{
				Parent = new RelationshipEndpoint(objectType, Guid.NewGuid().ToString()),
				Child = new RelationshipEndpoint(objectType, Guid.NewGuid().ToString()),
			}));

			var returned = TestContext.Api.Relationships.Read(created.Id);
			Assert.IsNotNull(returned);
			Assert.IsNull(returned.Parent.ObjectName);
			Assert.IsNull(returned.Parent.Url);
			Assert.IsNull(returned.Child.ObjectName);
			Assert.IsNull(returned.Child.Url);
			Assert.AreEqual(0, returned.Parent.Order);
			Assert.AreEqual(0, returned.Child.Order);
		}

		private static Relationship NewRelationship(RelationshipObjectType objectType, int order)
		{
			return new Relationship(new RelationshipData
			{
				Parent = new RelationshipEndpoint(objectType, Guid.NewGuid().ToString()) { Order = order },
				Child = new RelationshipEndpoint(objectType, Guid.NewGuid().ToString()) { Order = order },
			});
		}
	}
}
