namespace RT_MediaOps.Plan.Properties.Values
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using RT_MediaOps.Plan.RegressionTests;
    using RT_MediaOps.Plan.RST;

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
            // Arrange: create a property definition to link values against
            var property = new StringProperty
            {
                Name = $"{Guid.NewGuid()}_Prop",
                Scope = "global",
                SectionName = "General",
            };
            objectCreator.CreateProperty(property);

            // Create
            var collectionId = Guid.NewGuid();
            var collection = new PropertyValueCollection(collectionId)
            {
                Name = "TestCollection",
                LinkedObjectId = "obj-1",
                Scope = "global",
            };
            collection.Add(new StringPropertyValue(property) { Value = "hello" });

            var created = objectCreator.CreatePropertyValueCollection(collection);

            Assert.AreEqual(collectionId, created.Id);
            Assert.AreEqual("TestCollection", created.Name);
            Assert.AreEqual("obj-1", created.LinkedObjectId);
            Assert.AreEqual("global", created.Scope);
            Assert.AreEqual(1, created.StringValues.Count);
            Assert.AreEqual("hello", created.StringValues.First().Value);

            // Read
            var read = TestContext.Api.PropertyValueCollections.Read(collectionId);
            Assert.IsNotNull(read);
            Assert.AreEqual(collectionId, read.Id);
            Assert.AreEqual("TestCollection", read.Name);

            // Update
            read.Name = "TestCollection_Updated";
            var updated = TestContext.Api.PropertyValueCollections.Update(read);

            Assert.AreEqual("TestCollection_Updated", updated.Name);

            var readAfterUpdate = TestContext.Api.PropertyValueCollections.Read(collectionId);
            Assert.IsNotNull(readAfterUpdate);
            Assert.AreEqual("TestCollection_Updated", readAfterUpdate.Name);

            // Delete
            TestContext.Api.PropertyValueCollections.Delete(updated);

            var readAfterDelete = TestContext.Api.PropertyValueCollections.Read(collectionId);
            Assert.IsNull(readAfterDelete);
        }

        [TestMethod]
        public void CreateWithExistingIdThrowsException()
        {
            var collectionId = Guid.NewGuid();

            var collection1 = new PropertyValueCollection(collectionId)
            {
                Name = "Collection1",
                LinkedObjectId = "obj-1",
                Scope = "global",
            };
            var collection2 = new PropertyValueCollection(collectionId)
            {
                Name = "Collection2",
                LinkedObjectId = "obj-2",
                Scope = "global",
            };

            objectCreator.CreatePropertyValueCollection(collection1);

            try
            {
                objectCreator.CreatePropertyValueCollection(collection2);
            }
            catch (MediaOpsException ex)
            {
                StringAssert.Contains(ex.Message, "ID is already in use.");

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var error = ex.TraceData.ErrorData.OfType<PropertyValueCollectionIdInUseError>().SingleOrDefault();
                Assert.IsNotNull(error);
                Assert.AreEqual(collectionId, error.Id);

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void CreateWithSameIdInBulkThrowsException()
        {
            var collectionId = Guid.NewGuid();

            var collection1 = new PropertyValueCollection(collectionId)
            {
                Name = "Collection1",
                LinkedObjectId = "obj-1",
                Scope = "global",
            };
            var collection2 = new PropertyValueCollection(collectionId)
            {
                Name = "Collection2",
                LinkedObjectId = "obj-2",
                Scope = "global",
            };

            try
            {
                objectCreator.CreatePropertyValueCollections(new[] { collection1, collection2 });
            }
            catch (MediaOpsBulkException<Guid> ex)
            {
                if (!ex.Result.TraceDataPerItem.TryGetValue(collectionId, out var traceData))
                {
                    Assert.Fail("No trace data found for the failed ID.");
                }

                Assert.AreEqual(2, traceData.ErrorData.Count);
                var errors = traceData.ErrorData.OfType<PropertyValueCollectionDuplicateIdError>().ToList();
                Assert.AreEqual(2, errors.Count);

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void CreateWithEmptyLinkedObjectIdThrowsException()
        {
            var collection = new PropertyValueCollection
            {
                Name = "BadCollection",
                LinkedObjectId = string.Empty,
                Scope = "global",
            };

            try
            {
                objectCreator.CreatePropertyValueCollection(collection);
            }
            catch (MediaOpsException ex)
            {
                StringAssert.Contains(ex.Message, "Linked object ID cannot be empty.");
                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void CreateWithEmptyScopeThrowsException()
        {
            var collection = new PropertyValueCollection
            {
                Name = "BadCollection",
                LinkedObjectId = "obj-1",
                Scope = string.Empty,
            };

            try
            {
                objectCreator.CreatePropertyValueCollection(collection);
            }
            catch (MediaOpsException ex)
            {
                StringAssert.Contains(ex.Message, "Scope cannot be empty.");
                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void CreateNewThenUpdateNewThrowsException()
        {
            var collection = new PropertyValueCollection
            {
                Name = "NewCollection",
                LinkedObjectId = "obj-1",
                Scope = "global",
            };

            Assert.ThrowsException<InvalidOperationException>(() =>
                TestContext.Api.PropertyValueCollections.Update(collection));
        }

        [TestMethod]
        public void UpdateExistingThenCreateThrowsException()
        {
            var collection = new PropertyValueCollection
            {
                Name = "NewCollection",
                LinkedObjectId = "obj-1",
                Scope = "global",
            };
            var created = objectCreator.CreatePropertyValueCollection(collection);

            Assert.ThrowsException<InvalidOperationException>(() =>
                TestContext.Api.PropertyValueCollections.Create(created));
        }

        [TestMethod]
        public void DeleteNewCollectionThrowsException()
        {
            var collection = new PropertyValueCollection
            {
                Name = "NewCollection",
                LinkedObjectId = "obj-1",
                Scope = "global",
            };

            try
            {
                TestContext.Api.PropertyValueCollections.Delete(collection);
            }
            catch (MediaOpsException ex)
            {
                StringAssert.Contains(ex.Message, "A property value collection that was not saved cannot be removed.");
                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void CountReturnsCorrectNumber()
        {
            var countBefore = TestContext.Api.PropertyValueCollections.Count();

            var collection = new PropertyValueCollection
            {
                Name = "CountTestCollection",
                LinkedObjectId = "obj-count",
                Scope = "global",
            };
            objectCreator.CreatePropertyValueCollection(collection);

            var countAfter = TestContext.Api.PropertyValueCollections.Count();

            Assert.AreEqual(countBefore + 1, countAfter);
        }

        [TestMethod]
        public void ReadByLinkedObjectIdFilter()
        {
            var linkedObjectId = $"obj-{Guid.NewGuid()}";

            var collection = new PropertyValueCollection
            {
                Name = "FilterTestCollection",
                LinkedObjectId = linkedObjectId,
                Scope = "global",
            };
            var created = objectCreator.CreatePropertyValueCollection(collection);

            var results = TestContext.Api.PropertyValueCollections
                .Read(PropertyValueCollectionExposers.Id.Equal(created.Id))
                .ToList();

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(linkedObjectId, results[0].LinkedObjectId);
        }

        [TestMethod]
        public void BulkCreateAndDelete()
        {
            var collections = new List<PropertyValueCollection>
            {
                new PropertyValueCollection { Name = "Bulk1", LinkedObjectId = "bulk-obj-1", Scope = "global" },
                new PropertyValueCollection { Name = "Bulk2", LinkedObjectId = "bulk-obj-2", Scope = "global" },
            };

            var created = objectCreator.CreatePropertyValueCollections(collections);

            Assert.AreEqual(2, created.Count);

            TestContext.Api.PropertyValueCollections.Delete(created);

            foreach (var item in created)
            {
                Assert.IsNull(TestContext.Api.PropertyValueCollections.Read(item.Id));
            }
        }
    }
}
