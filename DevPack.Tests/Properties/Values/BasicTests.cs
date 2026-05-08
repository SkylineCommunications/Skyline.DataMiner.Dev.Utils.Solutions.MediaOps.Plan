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
                LinkedObjectId = "obj-1",
                Scope = "global",
            };
            collection.Add(new StringPropertyValue(property) { Value = "hello" });

            var created = objectCreator.CreatePropertyValueCollection(collection);

            Assert.AreEqual(collectionId, created.Id);
            Assert.AreEqual("obj-1", created.LinkedObjectId);
            Assert.AreEqual("global", created.Scope);
            Assert.AreEqual(1, created.StringValues.Count);
            Assert.AreEqual("hello", created.StringValues.First().Value);

            // Read
            var read = TestContext.Api.PropertyValueCollections.Read(collectionId);
            Assert.IsNotNull(read);
            Assert.AreEqual(collectionId, read.Id);

            // Delete
            TestContext.Api.PropertyValueCollections.Delete(read);

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
        public void CreateWithDuplicateLinkedObjectIdAndSubIdInBulkThrowsException()
        {
            var linkedObjectId = $"obj-{Guid.NewGuid()}";
            var subId = "sub-1";

            var collection1 = new PropertyValueCollection
            {
                Name = "Collection1",
                LinkedObjectId = linkedObjectId,
                SubId = subId,
                Scope = "global",
            };
            var collection2 = new PropertyValueCollection
            {
                Name = "Collection2",
                LinkedObjectId = linkedObjectId,
                SubId = subId,
                Scope = "global",
            };

            try
            {
                objectCreator.CreatePropertyValueCollections(new[] { collection1, collection2 });
            }
            catch (MediaOpsBulkException<Guid> ex)
            {
                var errors = ex.Result.TraceDataPerItem.Values
                    .SelectMany(x => x.ErrorData)
                    .OfType<PropertyValueCollectionDuplicateLinkedObjectIdAndSubIdError>()
                    .ToList();

                Assert.AreEqual(2, errors.Count);
                Assert.IsTrue(errors.All(e => e.LinkedObjectId == linkedObjectId && e.SubId == subId));

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void CreateWithLinkedObjectIdAndSubIdAlreadyInUseThrowsException()
        {
            var linkedObjectId = $"obj-{Guid.NewGuid()}";
            var subId = "sub-1";

            var existing = new PropertyValueCollection
            {
                Name = "Existing",
                LinkedObjectId = linkedObjectId,
                SubId = subId,
                Scope = "global",
            };
            objectCreator.CreatePropertyValueCollection(existing);

            var newCollection = new PropertyValueCollection
            {
                Name = "New",
                LinkedObjectId = linkedObjectId,
                SubId = subId,
                Scope = "global",
            };
            var ex = Assert.ThrowsException<MediaOpsException>(() => objectCreator.CreatePropertyValueCollection(newCollection));
            StringAssert.Contains(ex.Message, "already exists");

            var error = ex.TraceData.ErrorData.OfType<PropertyValueCollectionDuplicateLinkedObjectIdAndSubIdError>().SingleOrDefault();
            Assert.IsNotNull(error);
            Assert.AreEqual(linkedObjectId, error.LinkedObjectId);
            Assert.AreEqual(subId, error.SubId);
        }

        [TestMethod]
        public void CreateWithSameLinkedObjectIdAndDifferentSubIdSucceeds()
        {
            var linkedObjectId = $"obj-{Guid.NewGuid()}";

            var collections = new[]
            {
                new PropertyValueCollection
                {
                    Name = "Collection1",
                    LinkedObjectId = linkedObjectId,
                    SubId = "sub-1",
                    Scope = "global",
                },
                new PropertyValueCollection
                {
                    Name = "Collection2",
                    LinkedObjectId = linkedObjectId,
                    SubId = "sub-2",
                    Scope = "global",
                },
            };

            var created = objectCreator.CreatePropertyValueCollections(collections);

            Assert.AreEqual(2, created.Count);
            CollectionAssert.AreEquivalent(
                new[] { "sub-1", "sub-2" },
                created.Select(x => x.SubId).ToArray());
        }

        [TestMethod]
        public void CreateWithDifferentLinkedObjectIdAndSameSubIdSucceeds()
        {
            var subId = "shared-sub";

            var collections = new[]
            {
                new PropertyValueCollection
                {
                    Name = "Collection1",
                    LinkedObjectId = $"obj-{Guid.NewGuid()}",
                    SubId = subId,
                    Scope = "global",
                },
                new PropertyValueCollection
                {
                    Name = "Collection2",
                    LinkedObjectId = $"obj-{Guid.NewGuid()}",
                    SubId = subId,
                    Scope = "global",
                },
            };

            var created = objectCreator.CreatePropertyValueCollections(collections);

            Assert.AreEqual(2, created.Count);
            Assert.IsTrue(created.All(x => x.SubId == subId));
            Assert.AreEqual(2, created.Select(x => x.LinkedObjectId).Distinct().Count());
        }

        [TestMethod]
        public void CreateWithDuplicateLinkedObjectIdAndNullSubIdInBulkThrowsException()
        {
            var linkedObjectId = $"obj-{Guid.NewGuid()}";

            var collection1 = new PropertyValueCollection
            {
                Name = "Collection1",
                LinkedObjectId = linkedObjectId,
                Scope = "global",
            };
            var collection2 = new PropertyValueCollection
            {
                Name = "Collection2",
                LinkedObjectId = linkedObjectId,
                Scope = "global",
            };

            var ex = Assert.ThrowsException<MediaOpsBulkException<Guid>>(() => objectCreator.CreatePropertyValueCollections(new[] { collection1, collection2 }));
            var errors = ex.Result.TraceDataPerItem.Values
                .SelectMany(x => x.ErrorData)
                .OfType<PropertyValueCollectionDuplicateLinkedObjectIdAndSubIdError>()
                .ToList();

            Assert.AreEqual(2, errors.Count);
            Assert.IsTrue(errors.All(e => e.LinkedObjectId == linkedObjectId && e.SubId == null));
        }

        [TestMethod]
        public void CreateWithDuplicateCustomValueNamesThrowsException()
        {
            var customName = $"Custom_{Guid.NewGuid()}";

            var collection = new PropertyValueCollection
            {
                Name = "DuplicateCustomValues",
                LinkedObjectId = "obj-1",
                Scope = "global",
            };
            collection.Add(new CustomPropertyValue(customName) { Value = "A" });
            collection.Add(new CustomPropertyValue(customName) { Value = "B" });

            var ex = Assert.ThrowsException<MediaOpsException>(() => objectCreator.CreatePropertyValueCollection(collection));
            var errorMessage = $"Name '{customName}' is defined 2 times.";
            Assert.AreEqual(errorMessage, ex.Message);
            Assert.AreEqual(1, ex.TraceData.ErrorData.Count);

            var error = ex.TraceData.ErrorData.OfType<PropertyValueCollectionInvalidCustomSettingsError>().SingleOrDefault();
            Assert.IsNotNull(error);
            Assert.AreEqual(errorMessage, error.ErrorMessage);
            Assert.AreEqual(collection.Id, error.Id);
            Assert.AreEqual(customName, error.Name);
        }

        [TestMethod]
        public void CreateWithCustomValueNameMatchingPropertyNameInSameScopeThrowsException()
        {
            var propertyName = $"{Guid.NewGuid()}_Prop";
            var property = new StringProperty
            {
                Name = propertyName,
                Scope = "global",
                SectionName = "General",
            };
            objectCreator.CreateProperty(property);

            var collection = new PropertyValueCollection
            {
                Name = "ConflictingCustomValue",
                LinkedObjectId = "obj-1",
                Scope = "global",
            };
            collection.Add(new CustomPropertyValue(propertyName) { Value = "A" });

            var ex = Assert.ThrowsException<MediaOpsException>(() => objectCreator.CreatePropertyValueCollection(collection));
            var errorMessage = $"Name '{propertyName}' cannot be the same as a property name in the same scope.";
            Assert.AreEqual(errorMessage, ex.Message);
            Assert.AreEqual(1, ex.TraceData.ErrorData.Count);

            var error = ex.TraceData.ErrorData.OfType<PropertyValueCollectionInvalidCustomSettingsError>().SingleOrDefault();
            Assert.IsNotNull(error);
            Assert.AreEqual(errorMessage, error.ErrorMessage);
            Assert.AreEqual(collection.Id, error.Id);
            Assert.AreEqual(propertyName, error.Name);
        }

        [TestMethod]
        public void CreateWithDuplicatePropertyValueIdsThrowsException()
        {
            var property = new StringProperty
            {
                Name = $"{Guid.NewGuid()}_Prop",
                Scope = "global",
                SectionName = "General",
            };
            objectCreator.CreateProperty(property);

            var collection = new PropertyValueCollection
            {
                Name = "DuplicatePropertyValues",
                LinkedObjectId = "obj-1",
                Scope = "global",
            };
            collection.Add(new StringPropertyValue(property) { Value = "A" });
            collection.Add(new StringPropertyValue(property) { Value = "B" });

            var ex = Assert.ThrowsException<MediaOpsException>(() => objectCreator.CreatePropertyValueCollection(collection));
            var errorMessage = $"Property value collection contains 2 values with the same property ID '{property.Id}'.";
            Assert.AreEqual(errorMessage, ex.Message);
            Assert.AreEqual(1, ex.TraceData.ErrorData.Count);

            var error = ex.TraceData.ErrorData.OfType<PropertyValueCollectionInvalidPropertySettingsError>().SingleOrDefault();
            Assert.IsNotNull(error);
            Assert.AreEqual(errorMessage, error.ErrorMessage);
            Assert.AreEqual(collection.Id, error.Id);
            Assert.AreEqual(property.Id, error.PropertyId);
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

        [TestMethod]
        public void UpdateExistingWithCustomAndDoubleValues()
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
                LinkedObjectId = "obj-1",
                Scope = "global",
            };
            collection.Add(new StringPropertyValue(property) { Value = "hello" });

            var created = objectCreator.CreatePropertyValueCollection(collection);

            // Read
            var read = TestContext.Api.PropertyValueCollections.Read(collectionId);
            Assert.IsNotNull(read);
            Assert.AreEqual(collectionId, read.Id);

            // Arrange: create an additional boolean property definition
            var booleanProperty = new BooleanProperty
            {
                Name = $"{Guid.NewGuid()}_BooleanProp",
                Scope = "global",
                SectionName = "General",
            };
            objectCreator.CreateProperty(booleanProperty);

            // Update: add a custom value and a boolean property value
            read.Add(new CustomPropertyValue { Name = "CustomKey", Value = "CustomValue" });
            read.Add(new BooleanPropertyValue(booleanProperty) { Value = true });

            var updated = TestContext.Api.PropertyValueCollections.Update(read);

            Assert.AreEqual(1, updated.CustomValues.Count);
            Assert.AreEqual("CustomKey", updated.CustomValues.First().Name);
            Assert.AreEqual("CustomValue", updated.CustomValues.First().Value);
            Assert.AreEqual(1, updated.BooleanValues.Count);
            Assert.AreEqual(true, updated.BooleanValues.First().Value);

            var readAfterUpdate = TestContext.Api.PropertyValueCollections.Read(collectionId);
            Assert.IsNotNull(readAfterUpdate);
            Assert.AreEqual(1, readAfterUpdate.CustomValues.Count);
            Assert.AreEqual("CustomKey", readAfterUpdate.CustomValues.First().Name);
            Assert.AreEqual("CustomValue", readAfterUpdate.CustomValues.First().Value);
            Assert.AreEqual(1, readAfterUpdate.BooleanValues.Count);
            Assert.AreEqual(true, readAfterUpdate.BooleanValues.First().Value);

            // Delete
            TestContext.Api.PropertyValueCollections.Delete(updated);

            var readAfterDelete = TestContext.Api.PropertyValueCollections.Read(collectionId);
            Assert.IsNull(readAfterDelete);
        }
    }
}
