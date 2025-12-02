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
    public sealed class BasicTests
    {
        private readonly IntegrationTestContext testContext;
        private readonly ResourceStudioObjectCreator objectCreator;

        public BasicTests()
        {
            testContext = new IntegrationTestContext();
            objectCreator = new ResourceStudioObjectCreator(testContext.Api);
        }

        public void Dispose()
        {
            objectCreator.Dispose();
            testContext.Dispose();
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

            var returnedId = objectCreator.CreateProperty(property);
            Assert.AreEqual(propertyId, returnedId);

            var returnedProperty = testContext.Api.Properties.Read(propertyId);
            Assert.IsNotNull(returnedProperty);
            Assert.AreEqual(name, returnedProperty.Name);

            var domProperty = testContext.ResourceStudioDomHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(propertyId)).SingleOrDefault();
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
            testContext.Api.Properties.Update(returnedProperty);

            returnedProperty = testContext.Api.Properties.Read(propertyId);
            Assert.IsNotNull(returnedProperty);
            Assert.AreEqual(updatedName, returnedProperty.Name);

            domProperty = testContext.ResourceStudioDomHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(propertyId)).SingleOrDefault();
            Assert.IsNotNull(domProperty);
            Assert.AreEqual(Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Definitions.Resourceproperty.Id, domProperty.DomDefinitionId.Id);

            Assert.IsTrue(domProperty.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.PropertyInfo.Id.Id));
            domPropertyInfo = domProperty.Sections.Single(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.PropertyInfo.Id.Id);
            fdName = domPropertyInfo.FieldValues.SingleOrDefault(f => f.FieldDescriptorID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.PropertyInfo.PropertyName.Id);
            Assert.IsNotNull(fdName);
            Assert.AreEqual(returnedProperty.Name, Convert.ToString(fdName.Value.Value));

            // Delete
            testContext.Api.Properties.Delete(returnedProperty);

            returnedProperty = testContext.Api.Properties.Read(propertyId);
            Assert.IsNull(returnedProperty);

            domProperty = testContext.ResourceStudioDomHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(propertyId)).SingleOrDefault();
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
                var propertyConfigurationError = ex.TraceData.ErrorData.OfType<ResourcePropertyConfigurationError>().SingleOrDefault();
                Assert.IsNotNull(propertyConfigurationError);

                Assert.ReferenceEquals(ResourcePropertyConfigurationError.Reason.IdInUse, propertyConfigurationError.ErrorReason);
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
                var propertyConfigurationErrors = traceData.ErrorData.OfType<ResourcePropertyConfigurationError>().ToList();
                Assert.AreEqual(2, propertyConfigurationErrors.Count());

                var errorMessages = new List<string>
                {
                   $"Resource property '{property1.Name}' has a duplicate ID.",
                   $"Resource property '{property2.Name}' has a duplicate ID."
                };

                foreach (var error in propertyConfigurationErrors)
                {
                    Assert.AreEqual(ResourcePropertyConfigurationError.Reason.DuplicateId, error.ErrorReason);
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
                var propertyConfigurationError = ex.TraceData.ErrorData.OfType<ResourcePropertyConfigurationError>().SingleOrDefault();
                Assert.IsNotNull(propertyConfigurationError);

                Assert.AreEqual(ResourcePropertyConfigurationError.Reason.NameExists, propertyConfigurationError.ErrorReason);
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
                    var propertyConfigurationError = traceData.ErrorData.OfType<ResourcePropertyConfigurationError>().SingleOrDefault();
                    Assert.IsNotNull(propertyConfigurationError);

                    Assert.AreEqual(ResourcePropertyConfigurationError.Reason.DuplicateName, propertyConfigurationError.ErrorReason);
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

            var id1 = objectCreator.CreateProperty(property1);
            var id2 = objectCreator.CreateProperty(property2);

            var toUpdate = testContext.Api.Properties.Read(id2);
            toUpdate.Name = property1.Name;

            try
            {
                testContext.Api.Properties.Update(toUpdate);
            }
            catch (MediaOpsException ex)
            {
                StringAssert.Contains(ex.Message, "Name is already in use.");

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var propertyConfigurationError = ex.TraceData.ErrorData.OfType<ResourcePropertyConfigurationError>().SingleOrDefault();
                Assert.IsNotNull(propertyConfigurationError);

                Assert.AreEqual(ResourcePropertyConfigurationError.Reason.NameExists, propertyConfigurationError.ErrorReason);
                Assert.AreEqual("Name is already in use.", propertyConfigurationError.ErrorMessage);

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }
    }
}
