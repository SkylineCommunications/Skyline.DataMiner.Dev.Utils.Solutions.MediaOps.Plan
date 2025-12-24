namespace RT_MediaOps.Plan.RST.ResourceProperties
{
    using System;
    using System.Linq;

    using RT_MediaOps.Plan.RegressionTests;

    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

    using Storage = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage;

    [TestClass]
    [TestCategory("IntegrationTest")]
    [DoNotParallelize]
    public sealed class BasicTests : IDisposable
    {
        private readonly ResourceStudioObjectCreator objectCreator;

        public BasicTests()
        {
            objectCreator = new ResourceStudioObjectCreator(TestContext.Api, TestContext.CategoriesApi);
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

            var property = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceProperty(propertyId)
            {
                Name = name,
            };

            objectCreator.CreateProperty(property);

            var returnedProperty = TestContext.Api.Properties.Read(propertyId);
            Assert.IsNotNull(returnedProperty);
            Assert.AreEqual(name, returnedProperty.Name);

            var domProperty = TestContext.ResourceStudioDomHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(propertyId)).SingleOrDefault();
            Assert.IsNotNull(domProperty);
            Assert.AreEqual(Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Definitions.Resourceproperty.Id, domProperty.DomDefinitionId.Id);

            Assert.IsTrue(domProperty.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.PropertyInfo.Id.Id));
            var domPropertyInfo = domProperty.Sections.Single(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.PropertyInfo.Id.Id);
            var fdName = domPropertyInfo.FieldValues.SingleOrDefault(f => f.FieldDescriptorID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.PropertyInfo.PropertyName.Id);
            Assert.IsNotNull(fdName);
            Assert.AreEqual(returnedProperty.Name, Convert.ToString(fdName.Value.Value));

            // Update
            var updatedName = name + "_Updated";
            returnedProperty.Name = updatedName;
            TestContext.Api.Properties.Update(returnedProperty);

            returnedProperty = TestContext.Api.Properties.Read(propertyId);
            Assert.IsNotNull(returnedProperty);
            Assert.AreEqual(updatedName, returnedProperty.Name);

            domProperty = TestContext.ResourceStudioDomHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(propertyId)).SingleOrDefault();
            Assert.IsNotNull(domProperty);
            Assert.AreEqual(Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Definitions.Resourceproperty.Id, domProperty.DomDefinitionId.Id);

            Assert.IsTrue(domProperty.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.PropertyInfo.Id.Id));
            domPropertyInfo = domProperty.Sections.Single(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.PropertyInfo.Id.Id);
            fdName = domPropertyInfo.FieldValues.SingleOrDefault(f => f.FieldDescriptorID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.PropertyInfo.PropertyName.Id);
            Assert.IsNotNull(fdName);
            Assert.AreEqual(returnedProperty.Name, Convert.ToString(fdName.Value.Value));

            // Delete
            TestContext.Api.Properties.Delete(returnedProperty);

            returnedProperty = TestContext.Api.Properties.Read(propertyId);
            Assert.IsNull(returnedProperty);

            domProperty = TestContext.ResourceStudioDomHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(propertyId)).SingleOrDefault();
            Assert.IsNull(domProperty);
        }

        [TestMethod]
        public void CreateWithExistingIdThrowsException()
        {
            var propertyId = Guid.NewGuid();

            var property1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceProperty(propertyId)
            {
                Name = $"{propertyId}_Property1",
            };
            var property2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceProperty(propertyId)
            {
                Name = $"{propertyId}_Property2",
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
                var propertyConfigurationError = ex.TraceData.ErrorData.OfType<ResourcePropertyError>().SingleOrDefault();
                Assert.IsNotNull(propertyConfigurationError);

                var propertyConfigurationIdInUseError = propertyConfigurationError as ResourcePropertyIdInUseError;
                Assert.IsNotNull(propertyConfigurationIdInUseError);
                Assert.AreEqual(propertyId, propertyConfigurationIdInUseError.Id);
                Assert.AreEqual("ID is already in use.", propertyConfigurationError.ErrorMessage);

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void CreateWithSameIdInBulkThrowsException()
        {
            var propertyId = Guid.NewGuid();

            var property1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceProperty(propertyId)
            {
                Name = $"{propertyId}_Property1",
            };
            var property2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceProperty(propertyId)
            {
                Name = $"{propertyId}_Property2",
            };

            try
            {
                objectCreator.CreateProperties(new[] { property1, property2 });
            }
            catch (MediaOpsBulkException<Guid> ex)
            {
                if (!ex.Result.TraceDataPerItem.TryGetValue(propertyId, out var traceData))
                {
                    Assert.Fail("No trace data found for the failed ID");
                }

                Assert.AreEqual(2, traceData.ErrorData.Count);
                var propertyConfigurationErrors = traceData.ErrorData.OfType<ResourcePropertyError>().ToList();
                Assert.AreEqual(2, propertyConfigurationErrors.Count());

                var errorMessages = new List<string>
                {
                   $"Resource property '{property1.Name}' has a duplicate ID.",
                   $"Resource property '{property2.Name}' has a duplicate ID."
                };

                foreach (var error in propertyConfigurationErrors)
                {
                    var propertyConfigurationDuplicateIdError = error as ResourcePropertyDuplicateIdError;
                    Assert.IsNotNull(propertyConfigurationDuplicateIdError);
                    Assert.AreEqual(propertyId, propertyConfigurationDuplicateIdError.Id);
                    Assert.IsTrue(errorMessages.Contains(error.ErrorMessage));

                    errorMessages.Remove(error.ErrorMessage);
                }

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void CreateWithExistingNameThrowsException()
        {
            var propertyId = Guid.NewGuid();

            var property1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceProperty()
            {
                Name = $"{propertyId}_Property",
            };
            var property2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceProperty()
            {
                Name = $"{propertyId}_Property",
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
                var propertyConfigurationError = ex.TraceData.ErrorData.OfType<ResourcePropertyError>().SingleOrDefault();
                Assert.IsNotNull(propertyConfigurationError);

                var propertyConfigurationNameExistsError = propertyConfigurationError as ResourcePropertyNameExistsError;
                Assert.IsNotNull(propertyConfigurationNameExistsError);
                Assert.AreEqual(property2.Id, propertyConfigurationNameExistsError.Id);
                Assert.AreEqual(property2.Name, propertyConfigurationNameExistsError.Name);
                Assert.AreEqual("Name is already in use.", propertyConfigurationError.ErrorMessage);

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void CreateWithSameNameInBulkThrowsException()
        {
            var propertyId = Guid.NewGuid();

            var property1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceProperty()
            {
                Name = $"{propertyId}_Property",
            };
            var property2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceProperty()
            {
                Name = $"{propertyId}_Property",
            };

            try
            {
                objectCreator.CreateProperties(new[] { property1, property2 });
            }
            catch (MediaOpsBulkException<Guid> ex)
            {
                Assert.AreEqual(2, ex.Result.TraceDataPerItem.Count);

                foreach (var traceData in ex.Result.TraceDataPerItem.Values)
                {
                    Assert.AreEqual(1, traceData.ErrorData.Count);
                    var propertyConfigurationError = traceData.ErrorData.OfType<ResourcePropertyError>().SingleOrDefault();
                    Assert.IsNotNull(propertyConfigurationError);

                    var propertyConfigurationDuplicateNameError = propertyConfigurationError as ResourcePropertyDuplicateNameError;
                    Assert.IsNotNull(propertyConfigurationDuplicateNameError);
                    Assert.AreEqual(property2.Name, propertyConfigurationDuplicateNameError.Name);
                    Assert.AreEqual($"Resource property '{property1.Name}' has a duplicate name.", propertyConfigurationError.ErrorMessage);
                }

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void UpdateToSameNameThrowsException()
        {
            var propertyId = Guid.NewGuid();

            var property1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceProperty()
            {
                Name = $"{propertyId}_Property_1",
            };
            var property2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceProperty()
            {
                Name = $"{propertyId}_Property_2",
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
                var propertyConfigurationError = ex.TraceData.ErrorData.OfType<ResourcePropertyError>().SingleOrDefault();
                Assert.IsNotNull(propertyConfigurationError);

                var propertyConfigurationNameExistsError = propertyConfigurationError as ResourcePropertyNameExistsError;
                Assert.IsNotNull(propertyConfigurationNameExistsError);
                Assert.AreEqual(toUpdate.Id, propertyConfigurationNameExistsError.Id);
                Assert.AreEqual(toUpdate.Name, propertyConfigurationNameExistsError.Name);
                Assert.AreEqual("Name is already in use.", propertyConfigurationError.ErrorMessage);

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void ReadWithEmptyListReturnsEmptyList()
        {
            var properties = TestContext.Api.Properties.Read(new List<Guid>());
            Assert.IsNotNull(properties);
            Assert.AreEqual(0, properties.Count());
        }
    }
}
