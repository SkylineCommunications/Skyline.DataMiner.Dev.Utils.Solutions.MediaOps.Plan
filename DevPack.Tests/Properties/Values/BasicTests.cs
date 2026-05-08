namespace RT_MediaOps.Plan.Properties.Values
{
	using System;

	using RT_MediaOps.Plan.RegressionTests;
	using RT_MediaOps.Plan.RST;

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
			// Arrange – create the property definition used in the collection
			var prefix = Guid.NewGuid();
			var property = new BooleanProperty
			{
				Name = $"{prefix}_Property",
				Scope = "global",
				SectionName = "General",
			};
			objectCreator.CreateProperty(property);

			// Create
			var collection = new PropertyValueCollection
			{
				LinkedObjectId = $"{prefix}_Object",
				Scope = "global",
			};
			collection.Add(new BooleanPropertyValue(property) { Value = true });

			var createdCollection = objectCreator.CreatePropertyValueCollection(collection);

			Assert.IsNotNull(createdCollection);
			Assert.AreNotEqual(Guid.Empty, createdCollection.Id);

			var returnedCollection = TestContext.Api.PropertyValueCollections.Read(createdCollection.Id);
			Assert.IsNotNull(returnedCollection);
			Assert.AreEqual(createdCollection.Id, returnedCollection.Id);
			Assert.AreEqual("global", returnedCollection.Scope);
			Assert.AreEqual(1, returnedCollection.BooleanValues.Count);

			// Update
			var boolVal = returnedCollection.BooleanValues.GetEnumerator();
			boolVal.MoveNext();
			boolVal.Current.Value = false;

			var updatedCollection = TestContext.Api.PropertyValueCollections.Update(returnedCollection);
			Assert.IsNotNull(updatedCollection);

			// Delete
			TestContext.Api.PropertyValueCollections.Delete(updatedCollection);

			var deletedCollection = TestContext.Api.PropertyValueCollections.Read(updatedCollection.Id);
			Assert.IsNull(deletedCollection);
		}

		[TestMethod]
		public void CreateWithDuplicatePropertyValueThrowsException()
		{
			var prefix = Guid.NewGuid();
			var property = new BooleanProperty
			{
				Name = $"{prefix}_Property",
				Scope = "global",
				SectionName = "General",
			};
			objectCreator.CreateProperty(property);

			var collection = new PropertyValueCollection
			{
				LinkedObjectId = $"{prefix}_Object",
				Scope = "global",
			};

			// Add the same property twice (allowed at the model level; the handler must reject it)
			collection.Add(new BooleanPropertyValue(property) { Value = true });
			collection.Add(new BooleanPropertyValue(property) { Value = false });

			try
			{
				objectCreator.CreatePropertyValueCollection(collection);
			}
			catch (MediaOpsException)
			{
				return;
			}

			Assert.Fail("Expected exception was not thrown.");
		}

		[TestMethod]
		public void CreateWithDuplicatePropertyValueInBulkThrowsException()
		{
			var prefix = Guid.NewGuid();
			var property = new BooleanProperty
			{
				Name = $"{prefix}_Property",
				Scope = "global",
				SectionName = "General",
			};
			objectCreator.CreateProperty(property);

			var collection = new PropertyValueCollection
			{
				LinkedObjectId = $"{prefix}_Object",
				Scope = "global",
			};

			// Add the same property twice (allowed at the model level; the handler must reject it)
			collection.Add(new BooleanPropertyValue(property) { Value = true });
			collection.Add(new BooleanPropertyValue(property) { Value = false });

			try
			{
				objectCreator.CreatePropertyValueCollections(new[] { collection });
			}
			catch (MediaOpsBulkException<Guid>)
			{
				return;
			}

			Assert.Fail("Expected exception was not thrown.");
		}

		[TestMethod]
		public void UpdateWithDuplicatePropertyValueThrowsException()
		{
			var prefix = Guid.NewGuid();
			var property1 = new BooleanProperty
			{
				Name = $"{prefix}_Property1",
				Scope = "global",
				SectionName = "General",
			};
			var property2 = new BooleanProperty
			{
				Name = $"{prefix}_Property2",
				Scope = "global",
				SectionName = "General",
			};
			objectCreator.CreateProperty(property1);
			objectCreator.CreateProperty(property2);

			var collection = new PropertyValueCollection
			{
				LinkedObjectId = $"{prefix}_Object",
				Scope = "global",
			};
			collection.Add(new BooleanPropertyValue(property1) { Value = true });
			collection.Add(new BooleanPropertyValue(property2) { Value = false });
			objectCreator.CreatePropertyValueCollection(collection);

			var savedCollection = TestContext.Api.PropertyValueCollections.Read(collection.Id);

			// Add property1 a second time to create a duplicate
			savedCollection.Add(new BooleanPropertyValue(property1) { Value = false });

			try
			{
				TestContext.Api.PropertyValueCollections.Update(savedCollection);
			}
			catch (MediaOpsException)
			{
				return;
			}

			Assert.Fail("Expected exception was not thrown.");
		}
	}
}
