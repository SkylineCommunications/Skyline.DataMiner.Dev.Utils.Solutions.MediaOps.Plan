namespace RT_MediaOps.Plan.Properties.Definitions
{
	using System;
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
			// Create
			var propertyId = Guid.NewGuid();
			var name = $"{propertyId}_Property";

			var property = new BooleanProperty(propertyId)
			{
				Name = name,
				Scope = "global",
				SectionName = "General",
				DefaultValue = true,
			};

			objectCreator.CreateProperty(property);

			var returnedProperty = TestContext.Api.Properties.Read(propertyId) as BooleanProperty;
			Assert.IsNotNull(returnedProperty);
			Assert.AreEqual(name, returnedProperty.Name);
			Assert.AreEqual("global", returnedProperty.Scope);
			Assert.AreEqual("General", returnedProperty.SectionName);
			Assert.IsTrue(returnedProperty.DefaultValue);

			// Update
			var updatedName = name + "_Updated";
			returnedProperty.Name = updatedName;
			returnedProperty.DefaultValue = false;
			TestContext.Api.Properties.Update(returnedProperty);

			returnedProperty = TestContext.Api.Properties.Read(propertyId) as BooleanProperty;
			Assert.IsNotNull(returnedProperty);
			Assert.AreEqual(updatedName, returnedProperty.Name);
			Assert.IsFalse(returnedProperty.DefaultValue);

			// Delete
			TestContext.Api.Properties.Delete(returnedProperty);

			returnedProperty = TestContext.Api.Properties.Read(propertyId) as BooleanProperty;
			Assert.IsNull(returnedProperty);
		}

		[TestMethod]
		public void CreateWithExistingIdThrowsException()
		{
			var propertyId = Guid.NewGuid();

			var property1 = new BooleanProperty(propertyId)
			{
				Name = $"{propertyId}_Property1",
				Scope = "global",
				SectionName = "General",
			};
			var property2 = new BooleanProperty(propertyId)
			{
				Name = $"{propertyId}_Property2",
				Scope = "global",
				SectionName = "General",
			};

			objectCreator.CreateProperty(property1);
			try
			{
				objectCreator.CreateProperty(property2);
			}
			catch (MediaOpsException ex)
			{
				StringAssert.Contains(ex.Message, "ID is already in use.");

				Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
				var propertyError = ex.TraceData.ErrorData.OfType<PropertyError>().SingleOrDefault();
				Assert.IsNotNull(propertyError);

				var propertyIdInUseError = propertyError as PropertyIdInUseError;
				Assert.IsNotNull(propertyIdInUseError);
				Assert.AreEqual(propertyId, propertyIdInUseError.Id);
				Assert.AreEqual("ID is already in use.", propertyError.ErrorMessage);

				return;
			}

			Assert.Fail("Expected exception was not thrown.");
		}

		[TestMethod]
		public void CreateWithSameIdInBulkThrowsException()
		{
			var propertyId = Guid.NewGuid();

			var property1 = new BooleanProperty(propertyId)
			{
				Name = $"{propertyId}_Property1",
				Scope = "global",
				SectionName = "General",
			};
			var property2 = new BooleanProperty(propertyId)
			{
				Name = $"{propertyId}_Property2",
				Scope = "global",
				SectionName = "General",
			};

			try
			{
				objectCreator.CreateProperties(new Property[] { property1, property2 });
			}
			catch (MediaOpsBulkException<Guid> ex)
			{
				if (!ex.Result.TraceDataPerItem.TryGetValue(propertyId, out var traceData))
				{
					Assert.Fail("No trace data found for the failed ID");
				}

				Assert.AreEqual(2, traceData.ErrorData.Count);
				var propertyErrors = traceData.ErrorData.OfType<PropertyError>().ToList();
				Assert.AreEqual(2, propertyErrors.Count);

				var errorMessages = new List<string>
				{
					$"Property '{property1.Name}' has a duplicate ID.",
					$"Property '{property2.Name}' has a duplicate ID.",
				};

				foreach (var error in propertyErrors)
				{
					var duplicateIdError = error as PropertyDuplicateIdError;
					Assert.IsNotNull(duplicateIdError);
					Assert.AreEqual(propertyId, duplicateIdError.Id);
					Assert.IsTrue(errorMessages.Contains(error.ErrorMessage));

					errorMessages.Remove(error.ErrorMessage);
				}

				return;
			}

			Assert.Fail("Expected exception was not thrown.");
		}

		[TestMethod]
		public void UpdateToSameNameThrowsException()
		{
			var prefix = Guid.NewGuid();

			var property1 = new BooleanProperty()
			{
				Name = $"{prefix}_Property_1",
				Scope = "global",
				SectionName = "General",
			};
			var property2 = new BooleanProperty()
			{
				Name = $"{prefix}_Property_2",
				Scope = "global",
				SectionName = "General",
			};

			objectCreator.CreateProperty(property1);
			objectCreator.CreateProperty(property2);

			var toUpdate = TestContext.Api.Properties.Read(property2.Id);
			toUpdate.Name = property1.Name;

			try
			{
				TestContext.Api.Properties.Update(toUpdate);
			}
			catch (MediaOpsException ex)
			{
				StringAssert.Contains(ex.Message, "Name is already in use.");

				Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
				var propertyError = ex.TraceData.ErrorData.OfType<PropertyError>().SingleOrDefault();
				Assert.IsNotNull(propertyError);

				var nameExistsError = propertyError as PropertyNameExistsError;
				Assert.IsNotNull(nameExistsError);
				Assert.AreEqual(toUpdate.Id, nameExistsError.Id);
				Assert.AreEqual(toUpdate.Name, nameExistsError.Name);
				Assert.AreEqual("Name is already in use.", propertyError.ErrorMessage);

				return;
			}

			Assert.Fail("Expected exception was not thrown.");
		}

		[TestMethod]
		public void UpdateUnmodifiedProperty()
		{
			var property = new BooleanProperty
			{
				Name = $"{Guid.NewGuid()}_Property",
				Scope = "global",
				SectionName = "General",
			};

			property = objectCreator.CreateProperty(property);

			var originalProperty = TestContext.Api.Properties.Read(property.Id);
			var updatedProperty = TestContext.Api.Properties.Update(originalProperty);

			Assert.AreEqual(originalProperty, updatedProperty);
		}

		[TestMethod]
		public void BulkUpdateWithChangedAndUnchangedPropertyReturnsTwoProperties()
		{
			var prefix = Guid.NewGuid();

			var changedProperty = new BooleanProperty
			{
				Name = $"{prefix}_Changed",
				Scope = "global",
				SectionName = "General",
			};
			var unchangedProperty = new BooleanProperty
			{
				Name = $"{prefix}_Unchanged",
				Scope = "global",
				SectionName = "General",
			};

			changedProperty = (BooleanProperty)objectCreator.CreateProperty(changedProperty);
			unchangedProperty = (BooleanProperty)objectCreator.CreateProperty(unchangedProperty);

			var changedToUpdate = TestContext.Api.Properties.Read(changedProperty.Id);
			var unchangedToUpdate = TestContext.Api.Properties.Read(unchangedProperty.Id);

			changedToUpdate.Name = $"{prefix}_Changed_Updated";

			var updatedProperties = TestContext.Api.Properties.Update(new[] { changedToUpdate, unchangedToUpdate });

			Assert.AreEqual(2, updatedProperties.Count);
			Assert.IsTrue(updatedProperties.Any(x => x.Id == changedProperty.Id));
			Assert.IsTrue(updatedProperties.Any(x => x.Id == unchangedProperty.Id));

			var changedAfterUpdate = TestContext.Api.Properties.Read(changedProperty.Id);
			var unchangedAfterUpdate = TestContext.Api.Properties.Read(unchangedProperty.Id);

			Assert.AreEqual(changedToUpdate.Name, changedAfterUpdate.Name);
			Assert.AreEqual(unchangedProperty.Name, unchangedAfterUpdate.Name);
		}

		[TestMethod]
		public void BulkUpdateWithChangedInvalidAndUnchangedPropertyReturnsTwoSuccessfulIds()
		{
			var prefix = Guid.NewGuid();

			var changedProperty = new BooleanProperty
			{
				Name = $"{prefix}_Changed",
				Scope = "global",
				SectionName = "General",
			};
			var invalidProperty = new BooleanProperty
			{
				Name = $"{prefix}_Invalid",
				Scope = "global",
				SectionName = "General",
			};
			var unchangedProperty = new BooleanProperty
			{
				Name = $"{prefix}_Unchanged",
				Scope = "global",
				SectionName = "General",
			};

			changedProperty = (BooleanProperty)objectCreator.CreateProperty(changedProperty);
			invalidProperty = (BooleanProperty)objectCreator.CreateProperty(invalidProperty);
			unchangedProperty = (BooleanProperty)objectCreator.CreateProperty(unchangedProperty);

			var changedToUpdate = TestContext.Api.Properties.Read(changedProperty.Id);
			var invalidToUpdate = TestContext.Api.Properties.Read(invalidProperty.Id);
			var unchangedToUpdate = TestContext.Api.Properties.Read(unchangedProperty.Id);

			changedToUpdate.Name = $"{prefix}_Changed_Updated";
			invalidToUpdate.Name = String.Empty;

			var ex = Assert.ThrowsException<MediaOpsBulkException<Guid>>(() => TestContext.Api.Properties.Update(new[] { changedToUpdate, invalidToUpdate, unchangedToUpdate }));

			Assert.AreEqual(2, ex.Result.SuccessfulIds.Count);
			Assert.IsTrue(ex.Result.SuccessfulIds.Contains(changedProperty.Id));
			Assert.IsTrue(ex.Result.SuccessfulIds.Contains(unchangedProperty.Id));
			Assert.AreEqual(1, ex.Result.UnsuccessfulIds.Count);
			Assert.IsTrue(ex.Result.UnsuccessfulIds.Contains(invalidProperty.Id));

			var changedAfterUpdate = TestContext.Api.Properties.Read(changedProperty.Id);
			var invalidAfterUpdate = TestContext.Api.Properties.Read(invalidProperty.Id);
			var unchangedAfterUpdate = TestContext.Api.Properties.Read(unchangedProperty.Id);

			Assert.AreEqual(changedToUpdate.Name, changedAfterUpdate.Name);
			Assert.AreEqual(invalidProperty.Name, invalidAfterUpdate.Name);
			Assert.AreEqual(unchangedProperty.Name, unchangedAfterUpdate.Name);
		}

		[TestMethod]
		public void CreateWithSameNameInDifferentScopesSucceeds()
		{
			var prefix = Guid.NewGuid();
			var sharedName = $"{prefix}_Property";

			var property1 = new BooleanProperty()
			{
				Name = sharedName,
				Scope = "scope1",
				SectionName = "General",
			};
			var property2 = new BooleanProperty()
			{
				Name = sharedName,
				Scope = "scope2",
				SectionName = "General",
			};

			objectCreator.CreateProperty(property1);
			objectCreator.CreateProperty(property2);

			var returnedProperty1 = TestContext.Api.Properties.Read(property1.Id);
			var returnedProperty2 = TestContext.Api.Properties.Read(property2.Id);

			Assert.IsNotNull(returnedProperty1);
			Assert.IsNotNull(returnedProperty2);
			Assert.AreEqual(sharedName, returnedProperty1.Name);
			Assert.AreEqual(sharedName, returnedProperty2.Name);
			Assert.AreEqual("scope1", returnedProperty1.Scope);
			Assert.AreEqual("scope2", returnedProperty2.Scope);
		}

		[TestMethod]
		public void CreateWithSameNameInDifferentScopesInBulkSucceeds()
		{
			var prefix = Guid.NewGuid();
			var sharedName = $"{prefix}_Property";

			var property1 = new BooleanProperty()
			{
				Name = sharedName,
				Scope = "scope1",
				SectionName = "General",
			};
			var property2 = new BooleanProperty()
			{
				Name = sharedName,
				Scope = "scope2",
				SectionName = "General",
			};

			objectCreator.CreateProperties(new Property[] { property1, property2 });

			var returnedProperty1 = TestContext.Api.Properties.Read(property1.Id);
			var returnedProperty2 = TestContext.Api.Properties.Read(property2.Id);

			Assert.IsNotNull(returnedProperty1);
			Assert.IsNotNull(returnedProperty2);
			Assert.AreEqual(sharedName, returnedProperty1.Name);
			Assert.AreEqual(sharedName, returnedProperty2.Name);
			Assert.AreEqual("scope1", returnedProperty1.Scope);
			Assert.AreEqual("scope2", returnedProperty2.Scope);
		}

		[TestMethod]
		public void CreateWithSameNameInSameScopeThrowsException()
		{
			var prefix = Guid.NewGuid();

			var property1 = new BooleanProperty()
			{
				Name = $"{prefix}_Property",
				Scope = "scope1",
				SectionName = "General",
			};
			var property2 = new BooleanProperty()
			{
				Name = $"{prefix}_Property",
				Scope = "scope1",
				SectionName = "General",
			};

			objectCreator.CreateProperty(property1);
			try
			{
				objectCreator.CreateProperty(property2);
			}
			catch (MediaOpsException ex)
			{
				StringAssert.Contains(ex.Message, "Name is already in use.");

				Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
				var propertyError = ex.TraceData.ErrorData.OfType<PropertyError>().SingleOrDefault();
				Assert.IsNotNull(propertyError);

				var nameExistsError = propertyError as PropertyNameExistsError;
				Assert.IsNotNull(nameExistsError);
				Assert.AreEqual(property2.Id, nameExistsError.Id);
				Assert.AreEqual(property2.Name, nameExistsError.Name);
				Assert.AreEqual("Name is already in use.", propertyError.ErrorMessage);

				return;
			}

			Assert.Fail("Expected exception was not thrown.");
		}

		[TestMethod]
		public void CreateWithSameNameInSameScopeInBulkThrowsException()
		{
			var prefix = Guid.NewGuid();

			var property1 = new BooleanProperty()
			{
				Name = $"{prefix}_Property",
				Scope = "scope1",
				SectionName = "General",
			};
			var property2 = new BooleanProperty()
			{
				Name = $"{prefix}_Property",
				Scope = "scope1",
				SectionName = "General",
			};

			try
			{
				objectCreator.CreateProperties(new Property[] { property1, property2 });
			}
			catch (MediaOpsBulkException<Guid> ex)
			{
				Assert.AreEqual(2, ex.Result.TraceDataPerItem.Count);

				foreach (var traceData in ex.Result.TraceDataPerItem.Values)
				{
					Assert.AreEqual(1, traceData.ErrorData.Count);
					var propertyError = traceData.ErrorData.OfType<PropertyError>().SingleOrDefault();
					Assert.IsNotNull(propertyError);

					var duplicateNameError = propertyError as PropertyDuplicateNameError;
					Assert.IsNotNull(duplicateNameError);
					Assert.AreEqual(property2.Name, duplicateNameError.Name);
					Assert.AreEqual($"Property '{property1.Name}' has a duplicate name in scope '{property1.Scope}'.", propertyError.ErrorMessage);
				}

				return;
			}

			Assert.Fail("Expected exception was not thrown.");
		}

		[TestMethod]
		public void UpdateToSameNameInDifferentScopeSucceeds()
		{
			var prefix = Guid.NewGuid();

			var property1 = new BooleanProperty()
			{
				Name = $"{prefix}_Property_1",
				Scope = "scope1",
				SectionName = "General",
			};
			var property2 = new BooleanProperty()
			{
				Name = $"{prefix}_Property_2",
				Scope = "scope2",
				SectionName = "General",
			};

			objectCreator.CreateProperty(property1);
			objectCreator.CreateProperty(property2);

			var toUpdate = TestContext.Api.Properties.Read(property2.Id);
			toUpdate.Name = property1.Name;

			TestContext.Api.Properties.Update(toUpdate);

			var returnedProperty = TestContext.Api.Properties.Read(property2.Id);
			Assert.IsNotNull(returnedProperty);
			Assert.AreEqual(property1.Name, returnedProperty.Name);
		}

		[TestMethod]
		public void ReadWithEmptyListReturnsEmptyList()
		{
			var properties = TestContext.Api.Properties.Read(new List<Guid>());
			Assert.IsNotNull(properties);
			Assert.AreEqual(0, properties.Count());
		}

		[TestMethod]
		public void ReadWithEmptyFilterReturnsEmptyList()
		{
			var idsToRetrieve = new Guid[0];
			var emptyFilter = new ORFilterElement<Property>(idsToRetrieve.Select(x => Skyline.DataMiner.Solutions.MediaOps.Plan.API.PropertyExposers.Id.Equal(x)).ToArray());

			var properties = TestContext.Api.Properties.Read(emptyFilter);
			Assert.IsNotNull(properties);
			Assert.AreEqual(0, properties.Count());
		}

		[TestMethod]
		public void CountWithEmptyFilterReturnsZero()
		{
			var idsToRetrieve = new Guid[0];
			var emptyFilter = new ORFilterElement<Property>(idsToRetrieve.Select(x => Skyline.DataMiner.Solutions.MediaOps.Plan.API.PropertyExposers.Id.Equal(x)).ToArray());

			var count = TestContext.Api.Properties.Count(emptyFilter);
			Assert.AreEqual(0, count);
		}

		[TestMethod]
		public void ReadWithEmptyQueryReturnsEmptyList()
		{
			var idsToRetrieve = new Guid[0];
			var emptyFilter = new ORFilterElement<Property>(idsToRetrieve.Select(x => Skyline.DataMiner.Solutions.MediaOps.Plan.API.PropertyExposers.Id.Equal(x)).ToArray());
			var queryWithEmptyFilter = emptyFilter.ToQuery();

			var properties = TestContext.Api.Properties.Read(queryWithEmptyFilter);
			Assert.IsNotNull(properties);
			Assert.AreEqual(0, properties.Count());
		}

		[TestMethod]
		public void CreateWithNullNameThrowsException()
		{
			var property = new BooleanProperty()
			{
				Name = null,
				Scope = "global",
				SectionName = "General",
			};

			try
			{
				objectCreator.CreateProperty(property);
			}
			catch (MediaOpsException ex)
			{
				var invalidNameError = ex.TraceData.ErrorData.OfType<PropertyInvalidNameError>().SingleOrDefault();
				Assert.IsNotNull(invalidNameError);
				Assert.AreEqual("Name cannot be empty.", invalidNameError.ErrorMessage);
				return;
			}

			Assert.Fail("Expected exception was not thrown.");
		}

		[TestMethod]
		public void CreateWithEmptyNameThrowsException()
		{
			var property = new BooleanProperty()
			{
				Name = string.Empty,
				Scope = "global",
				SectionName = "General",
			};

			try
			{
				objectCreator.CreateProperty(property);
			}
			catch (MediaOpsException ex)
			{
				var invalidNameError = ex.TraceData.ErrorData.OfType<PropertyInvalidNameError>().SingleOrDefault();
				Assert.IsNotNull(invalidNameError);
				Assert.AreEqual("Name cannot be empty.", invalidNameError.ErrorMessage);
				return;
			}

			Assert.Fail("Expected exception was not thrown.");
		}
	}
}
