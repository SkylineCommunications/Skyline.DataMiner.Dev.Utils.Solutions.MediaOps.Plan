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

	using SLDataGateway.API.Querying;

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
            var collection = new PropertySettingCollection(collectionId)
            {
                LinkedObjectId = "obj-1",
                Scope = "global",
            };
            collection.Add(new StringPropertySetting(property) { Value = "hello" });

            var created = objectCreator.CreatePropertySettingCollection(collection);

            Assert.AreEqual(collectionId, created.Id);
            Assert.AreEqual("obj-1", created.LinkedObjectId);
            Assert.AreEqual("global", created.Scope);
            Assert.AreEqual(1, created.StringSettings.Count);
            Assert.AreEqual("hello", created.StringSettings.First().Value);

            // Read
            var read = TestContext.Api.PropertySettingCollections.Read(collectionId);
            Assert.IsNotNull(read);
            Assert.AreEqual(collectionId, read.Id);

            // Delete
            TestContext.Api.PropertySettingCollections.Delete(read);

            var readAfterDelete = TestContext.Api.PropertySettingCollections.Read(collectionId);
            Assert.IsNull(readAfterDelete);
        }

        [TestMethod]
        public void CreateWithExistingIdThrowsException()
        {
            var collectionId = Guid.NewGuid();

            var collection1 = new PropertySettingCollection(collectionId)
            {
                LinkedObjectId = "obj-1",
                Scope = "global",
            };
            var collection2 = new PropertySettingCollection(collectionId)
            {
                LinkedObjectId = "obj-2",
                Scope = "global",
            };

            objectCreator.CreatePropertySettingCollection(collection1);

            try
            {
                objectCreator.CreatePropertySettingCollection(collection2);
            }
            catch (MediaOpsException ex)
            {
                StringAssert.Contains(ex.Message, "ID is already in use.");

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var error = ex.TraceData.ErrorData.OfType<PropertySettingCollectionIdInUseError>().SingleOrDefault();
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

            var collection1 = new PropertySettingCollection(collectionId)
            {
                LinkedObjectId = "obj-1",
                Scope = "global",
            };
            var collection2 = new PropertySettingCollection(collectionId)
            {
                LinkedObjectId = "obj-2",
                Scope = "global",
            };

            try
            {
                objectCreator.CreatePropertySettingCollections(new[] { collection1, collection2 });
            }
            catch (MediaOpsBulkException<Guid> ex)
            {
                if (!ex.Result.TraceDataPerItem.TryGetValue(collectionId, out var traceData))
                {
                    Assert.Fail("No trace data found for the failed ID.");
                }

                Assert.AreEqual(2, traceData.ErrorData.Count);
                var errors = traceData.ErrorData.OfType<PropertySettingCollectionDuplicateIdError>().ToList();
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

            var collection1 = new PropertySettingCollection
            {
                LinkedObjectId = linkedObjectId,
                SubId = subId,
                Scope = "global",
            };
            var collection2 = new PropertySettingCollection
            {
                LinkedObjectId = linkedObjectId,
                SubId = subId,
                Scope = "global",
            };

            try
            {
                objectCreator.CreatePropertySettingCollections(new[] { collection1, collection2 });
            }
            catch (MediaOpsBulkException<Guid> ex)
            {
                var errors = ex.Result.TraceDataPerItem.Values
                    .SelectMany(x => x.ErrorData)
                    .OfType<PropertySettingCollectionDuplicateLinkedObjectIdAndSubIdError>()
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

            var existing = new PropertySettingCollection
            {
                LinkedObjectId = linkedObjectId,
                SubId = subId,
                Scope = "global",
            };
            objectCreator.CreatePropertySettingCollection(existing);

            var newCollection = new PropertySettingCollection
            {
                LinkedObjectId = linkedObjectId,
                SubId = subId,
                Scope = "global",
            };
            var ex = Assert.ThrowsException<MediaOpsException>(() => objectCreator.CreatePropertySettingCollection(newCollection));
            StringAssert.Contains(ex.Message, "already exists");

            var error = ex.TraceData.ErrorData.OfType<PropertySettingCollectionDuplicateLinkedObjectIdAndSubIdError>().SingleOrDefault();
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
                new PropertySettingCollection
                {
                    LinkedObjectId = linkedObjectId,
                    SubId = "sub-1",
                    Scope = "global",
                },
                new PropertySettingCollection
                {
                    LinkedObjectId = linkedObjectId,
                    SubId = "sub-2",
                    Scope = "global",
                },
            };

            var created = objectCreator.CreatePropertySettingCollections(collections);

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
                new PropertySettingCollection
                {
                    LinkedObjectId = $"obj-{Guid.NewGuid()}",
                    SubId = subId,
                    Scope = "global",
                },
                new PropertySettingCollection
                {
                    LinkedObjectId = $"obj-{Guid.NewGuid()}",
                    SubId = subId,
                    Scope = "global",
                },
            };

            var created = objectCreator.CreatePropertySettingCollections(collections);

            Assert.AreEqual(2, created.Count);
            Assert.IsTrue(created.All(x => x.SubId == subId));
            Assert.AreEqual(2, created.Select(x => x.LinkedObjectId).Distinct().Count());
        }

        [TestMethod]
        public void CreateWithDuplicateLinkedObjectIdAndNullSubIdInBulkThrowsException()
        {
            var linkedObjectId = $"obj-{Guid.NewGuid()}";

            var collection1 = new PropertySettingCollection
            {
                LinkedObjectId = linkedObjectId,
                Scope = "global",
            };
            var collection2 = new PropertySettingCollection
            {
                LinkedObjectId = linkedObjectId,
                Scope = "global",
            };

            var ex = Assert.ThrowsException<MediaOpsBulkException<Guid>>(() => objectCreator.CreatePropertySettingCollections(new[] { collection1, collection2 }));
            var errors = ex.Result.TraceDataPerItem.Values
                .SelectMany(x => x.ErrorData)
                .OfType<PropertySettingCollectionDuplicateLinkedObjectIdAndSubIdError>()
                .ToList();

            Assert.AreEqual(2, errors.Count);
            Assert.IsTrue(errors.All(e => e.LinkedObjectId == linkedObjectId && e.SubId == null));
        }

        [TestMethod]
        public void CreateWithDuplicateCustomValueNamesThrowsException()
        {
            var customName = $"Custom_{Guid.NewGuid()}";

            var collection = new PropertySettingCollection
            {
                LinkedObjectId = "obj-1",
                Scope = "global",
            };
            collection.Add(new CustomPropertySetting(customName) { Value = "A" });
            collection.Add(new CustomPropertySetting(customName) { Value = "B" });

            var ex = Assert.ThrowsException<MediaOpsException>(() => objectCreator.CreatePropertySettingCollection(collection));
            var errorMessage = $"Name '{customName}' is defined 2 times.";
            Assert.AreEqual(errorMessage, ex.Message);
            Assert.AreEqual(1, ex.TraceData.ErrorData.Count);

            var error = ex.TraceData.ErrorData.OfType<PropertySettingCollectionInvalidCustomSettingsError>().SingleOrDefault();
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

            var collection = new PropertySettingCollection
            {
                LinkedObjectId = "obj-1",
                Scope = "global",
            };
            collection.Add(new CustomPropertySetting(propertyName) { Value = "A" });

            var ex = Assert.ThrowsException<MediaOpsException>(() => objectCreator.CreatePropertySettingCollection(collection));
            var errorMessage = $"Name '{propertyName}' cannot be the same as a property name in the same scope.";
            Assert.AreEqual(errorMessage, ex.Message);
            Assert.AreEqual(1, ex.TraceData.ErrorData.Count);

            var error = ex.TraceData.ErrorData.OfType<PropertySettingCollectionInvalidCustomSettingsError>().SingleOrDefault();
            Assert.IsNotNull(error);
            Assert.AreEqual(errorMessage, error.ErrorMessage);
            Assert.AreEqual(collection.Id, error.Id);
            Assert.AreEqual(propertyName, error.Name);
        }

        [TestMethod]
        public void CreateWithDuplicatePropertySettingIdsThrowsException()
        {
            var property = new StringProperty
            {
                Name = $"{Guid.NewGuid()}_Prop",
                Scope = "global",
                SectionName = "General",
            };
            objectCreator.CreateProperty(property);

            var collection = new PropertySettingCollection
            {
                LinkedObjectId = "obj-1",
                Scope = "global",
            };
            collection.Add(new StringPropertySetting(property) { Value = "A" });
            collection.Add(new StringPropertySetting(property) { Value = "B" });

            var ex = Assert.ThrowsException<MediaOpsException>(() => objectCreator.CreatePropertySettingCollection(collection));
            var errorMessage = $"Property value collection contains 2 values with the same property ID '{property.Id}'.";
            Assert.AreEqual(errorMessage, ex.Message);
            Assert.AreEqual(1, ex.TraceData.ErrorData.Count);

            var error = ex.TraceData.ErrorData.OfType<PropertySettingCollectionInvalidPropertySettingsError>().SingleOrDefault();
            Assert.IsNotNull(error);
            Assert.AreEqual(errorMessage, error.ErrorMessage);
            Assert.AreEqual(collection.Id, error.Id);
            Assert.AreEqual(property.Id, error.PropertyId);
        }

        [TestMethod]
        public void CreateWithEmptyLinkedObjectIdThrowsException()
        {
            var collection = new PropertySettingCollection
            {
                LinkedObjectId = string.Empty,
                Scope = "global",
            };

            try
            {
                objectCreator.CreatePropertySettingCollection(collection);
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
            var collection = new PropertySettingCollection
            {
                LinkedObjectId = "obj-1",
                Scope = string.Empty,
            };

            try
            {
                objectCreator.CreatePropertySettingCollection(collection);
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
            var collection = new PropertySettingCollection
            {
                LinkedObjectId = "obj-1",
                Scope = "global",
            };

            Assert.ThrowsException<InvalidOperationException>(() =>
                TestContext.Api.PropertySettingCollections.Update(collection));
        }

		[TestMethod]
        public void UpdateExistingThenCreateThrowsException()
		{
            var collection = new PropertySettingCollection
            {
                LinkedObjectId = "obj-1",
                Scope = "global",
            };
            var created = objectCreator.CreatePropertySettingCollection(collection);

            Assert.ThrowsException<InvalidOperationException>(() =>
                TestContext.Api.PropertySettingCollections.Create(created));
		}

		[TestMethod]
        public void CountReturnsCorrectNumber()
		{
            var countBefore = TestContext.Api.PropertySettingCollections.Count();

            var collection = new PropertySettingCollection
            {
                LinkedObjectId = "obj-count",
                Scope = "global",
            };
            objectCreator.CreatePropertySettingCollection(collection);

            var countAfter = TestContext.Api.PropertySettingCollections.Count();

            Assert.AreEqual(countBefore + 1, countAfter);
        }

        [TestMethod]
        public void ReadByLinkedObjectIdFilter()
        {
            var linkedObjectId = $"obj-{Guid.NewGuid()}";

            var collection = new PropertySettingCollection
            {
                LinkedObjectId = linkedObjectId,
                Scope = "global",
            };
            var created = objectCreator.CreatePropertySettingCollection(collection);

            var results = TestContext.Api.PropertySettingCollections
                .Read(PropertySettingCollectionExposers.Id.Equal(created.Id))
                .ToList();

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(linkedObjectId, results[0].LinkedObjectId);
        }

        [TestMethod]
        public void BulkCreateAndDelete()
        {
            var collections = new List<PropertySettingCollection>
            {
                new PropertySettingCollection { LinkedObjectId = "bulk-obj-1", Scope = "global" },
                new PropertySettingCollection { LinkedObjectId = "bulk-obj-2", Scope = "global" },
            };

            var created = objectCreator.CreatePropertySettingCollections(collections);

            Assert.AreEqual(2, created.Count);

            TestContext.Api.PropertySettingCollections.Delete(created);

            foreach (var item in created)
            {
                Assert.IsNull(TestContext.Api.PropertySettingCollections.Read(item.Id));
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
            var collection = new PropertySettingCollection(collectionId)
            {
                LinkedObjectId = "obj-1",
                Scope = "global",
            };
            collection.Add(new StringPropertySetting(property) { Value = "hello" });

            var created = objectCreator.CreatePropertySettingCollection(collection);

            // Read
            var read = TestContext.Api.PropertySettingCollections.Read(collectionId);
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
            read.Add(new CustomPropertySetting { Name = "CustomKey", Value = "CustomValue" });
            read.Add(new BooleanPropertySetting(booleanProperty) { Value = true });

            var updated = TestContext.Api.PropertySettingCollections.Update(read);

            Assert.AreEqual(1, updated.CustomSettings.Count);
            Assert.AreEqual("CustomKey", updated.CustomSettings.First().Name);
            Assert.AreEqual("CustomValue", updated.CustomSettings.First().Value);
            Assert.AreEqual(1, updated.BooleanSettings.Count);
            Assert.AreEqual(true, updated.BooleanSettings.First().Value);

            var readAfterUpdate = TestContext.Api.PropertySettingCollections.Read(collectionId);
            Assert.IsNotNull(readAfterUpdate);
            Assert.AreEqual(1, readAfterUpdate.CustomSettings.Count);
            Assert.AreEqual("CustomKey", readAfterUpdate.CustomSettings.First().Name);
            Assert.AreEqual("CustomValue", readAfterUpdate.CustomSettings.First().Value);
            Assert.AreEqual(1, readAfterUpdate.BooleanSettings.Count);
            Assert.AreEqual(true, readAfterUpdate.BooleanSettings.First().Value);

            // Delete
            TestContext.Api.PropertySettingCollections.Delete(updated);

            var readAfterDelete = TestContext.Api.PropertySettingCollections.Read(collectionId);
            Assert.IsNull(readAfterDelete);
		}

		[TestMethod]
		public void UpdateUnmodifiedPropertySettingCollection()
		{
			var property = new StringProperty
			{
				Name = $"{Guid.NewGuid()}_Property",
				Scope = "global",
				SectionName = "General",
			};
			objectCreator.CreateProperty(property);

			var collection = new PropertySettingCollection
			{
				LinkedObjectId = $"obj-{Guid.NewGuid()}",
				Scope = "global",
			};
			collection.Add(new StringPropertySetting(property) { Value = "value" });
			collection = objectCreator.CreatePropertySettingCollection(collection);

			var originalCollection = TestContext.Api.PropertySettingCollections.Read(collection.Id);
			var updatedCollection = TestContext.Api.PropertySettingCollections.Update(originalCollection);

			Assert.AreEqual(originalCollection, updatedCollection);
		}

		[TestMethod]
		public void BulkUpdateWithChangedAndUnchangedPropertySettingCollectionReturnsTwoCollections()
		{
			var property = new StringProperty
			{
				Name = $"{Guid.NewGuid()}_Property",
				Scope = "global",
				SectionName = "General",
			};
			objectCreator.CreateProperty(property);

			var changedCollection = new PropertySettingCollection
			{
				LinkedObjectId = $"obj-{Guid.NewGuid()}",
				Scope = "global",
			};
			changedCollection.Add(new StringPropertySetting(property) { Value = "value-1" });
			changedCollection = objectCreator.CreatePropertySettingCollection(changedCollection);

			var unchangedCollection = new PropertySettingCollection
			{
				LinkedObjectId = $"obj-{Guid.NewGuid()}",
				Scope = "global",
			};
			unchangedCollection.Add(new StringPropertySetting(property) { Value = "value-2" });
			unchangedCollection = objectCreator.CreatePropertySettingCollection(unchangedCollection);

			var changedToUpdate = TestContext.Api.PropertySettingCollections.Read(changedCollection.Id);
			var unchangedToUpdate = TestContext.Api.PropertySettingCollections.Read(unchangedCollection.Id);

			changedToUpdate.StringSettings.Single().Value = "value-1-updated";

			var updatedCollections = TestContext.Api.PropertySettingCollections.Update(new[] { changedToUpdate, unchangedToUpdate });

			Assert.AreEqual(2, updatedCollections.Count);
			Assert.IsTrue(updatedCollections.Any(x => x.Id == changedCollection.Id));
			Assert.IsTrue(updatedCollections.Any(x => x.Id == unchangedCollection.Id));

			var changedAfterUpdate = TestContext.Api.PropertySettingCollections.Read(changedCollection.Id);
			var unchangedAfterUpdate = TestContext.Api.PropertySettingCollections.Read(unchangedCollection.Id);

			Assert.AreEqual(changedToUpdate.StringSettings.Single().Value, changedAfterUpdate.StringSettings.Single().Value);
			Assert.AreEqual(unchangedCollection.StringSettings.Single().Value, unchangedAfterUpdate.StringSettings.Single().Value);
		}

		[TestMethod]
		public void BulkUpdateWithChangedInvalidAndUnchangedPropertySettingCollectionReturnsTwoSuccessfulIds()
		{
			var property = new StringProperty
			{
				Name = $"{Guid.NewGuid()}_Property",
				Scope = "global",
				SectionName = "General",
			};
			objectCreator.CreateProperty(property);

			var changedCollection = new PropertySettingCollection
			{
				LinkedObjectId = $"obj-{Guid.NewGuid()}",
				Scope = "global",
			};
			changedCollection.Add(new StringPropertySetting(property) { Value = "value-1" });
			changedCollection = objectCreator.CreatePropertySettingCollection(changedCollection);

			var invalidCollection = new PropertySettingCollection
			{
				LinkedObjectId = $"obj-{Guid.NewGuid()}",
				Scope = "global",
			};
			invalidCollection.Add(new StringPropertySetting(property) { Value = "value-2" });
			invalidCollection = objectCreator.CreatePropertySettingCollection(invalidCollection);

			var unchangedCollection = new PropertySettingCollection
			{
				LinkedObjectId = $"obj-{Guid.NewGuid()}",
				Scope = "global",
			};
			unchangedCollection.Add(new StringPropertySetting(property) { Value = "value-3" });
			unchangedCollection = objectCreator.CreatePropertySettingCollection(unchangedCollection);

			var changedToUpdate = TestContext.Api.PropertySettingCollections.Read(changedCollection.Id);
			var invalidToUpdate = TestContext.Api.PropertySettingCollections.Read(invalidCollection.Id);
			var unchangedToUpdate = TestContext.Api.PropertySettingCollections.Read(unchangedCollection.Id);

			changedToUpdate.StringSettings.Single().Value = "value-1-updated";
			invalidToUpdate.Add(new CustomPropertySetting { Name = "Duplicate", Value = "A" });
			invalidToUpdate.Add(new CustomPropertySetting { Name = "Duplicate", Value = "B" });

			var ex = Assert.ThrowsException<MediaOpsBulkException<Guid>>(() => TestContext.Api.PropertySettingCollections.Update(new[] { changedToUpdate, invalidToUpdate, unchangedToUpdate }));

			Assert.AreEqual(2, ex.Result.SuccessfulIds.Count);
			Assert.IsTrue(ex.Result.SuccessfulIds.Contains(changedCollection.Id));
			Assert.IsTrue(ex.Result.SuccessfulIds.Contains(unchangedCollection.Id));
			Assert.AreEqual(1, ex.Result.UnsuccessfulIds.Count);
			Assert.IsTrue(ex.Result.UnsuccessfulIds.Contains(invalidCollection.Id));

			var changedAfterUpdate = TestContext.Api.PropertySettingCollections.Read(changedCollection.Id);
			var invalidAfterUpdate = TestContext.Api.PropertySettingCollections.Read(invalidCollection.Id);
			var unchangedAfterUpdate = TestContext.Api.PropertySettingCollections.Read(unchangedCollection.Id);

			Assert.AreEqual(changedToUpdate.StringSettings.Single().Value, changedAfterUpdate.StringSettings.Single().Value);
			Assert.AreEqual(invalidCollection.CustomSettings.Count, invalidAfterUpdate.CustomSettings.Count);
			Assert.AreEqual(unchangedCollection.StringSettings.Single().Value, unchangedAfterUpdate.StringSettings.Single().Value);
		}

		[TestMethod]
		public void ReadWithEmptyFilterReturnsEmptyList()
		{
			var idsToRetrieve = new Guid[0];
			var emptyFilter = new ORFilterElement<PropertySettingCollection>(idsToRetrieve.Select(x => PropertySettingCollectionExposers.Id.Equal(x)).ToArray());

			var collections = TestContext.Api.PropertySettingCollections.Read(emptyFilter);
			Assert.IsNotNull(collections);
			Assert.AreEqual(0, collections.Count());
		}

		[TestMethod]
		public void CountWithEmptyFilterReturnsZero()
		{
			var idsToRetrieve = new Guid[0];
			var emptyFilter = new ORFilterElement<PropertySettingCollection>(idsToRetrieve.Select(x => PropertySettingCollectionExposers.Id.Equal(x)).ToArray());

			var count = TestContext.Api.PropertySettingCollections.Count(emptyFilter);
			Assert.AreEqual(0, count);
		}

		[TestMethod]
		public void ReadWithEmptyQueryReturnsEmptyList()
		{
			var idsToRetrieve = new Guid[0];
			var emptyFilter = new ORFilterElement<PropertySettingCollection>(idsToRetrieve.Select(x => PropertySettingCollectionExposers.Id.Equal(x)).ToArray());
			var queryWithEmptyFilter = emptyFilter.ToQuery();

			var collections = TestContext.Api.PropertySettingCollections.Read(queryWithEmptyFilter);
			Assert.IsNotNull(collections);
			Assert.AreEqual(0, collections.Count());
		}
	}
}
