namespace RT_MediaOps.Plan.RST.Resources
{
    using System;
    using System.Linq;

    using RT_MediaOps.Plan.RegressionTests;

    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

    using Storage = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public sealed class CapacityAssignmentTests : IDisposable
    {
        private readonly TestObjectCreator objectCreator;

        public CapacityAssignmentTests()
        {
            objectCreator = new TestObjectCreator(TestContext);
        }

        private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

        public void Dispose()
        {
            objectCreator.Dispose();
        }

        [TestMethod]
        public void DraftResourceNumberCapacityCRUD()
        {
            var prefix = Guid.NewGuid();

            var capacity1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity()
            {
                Name = $"{prefix}_Capacity1",
                RangeMin = 100,
                RangeMax = 200,
                StepSize = 5,
            };
            objectCreator.CreateCapacity(capacity1);

            var capacity2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity()
            {
                Name = $"{prefix}_Capacity2",
                Decimals = 3,
            };
            objectCreator.CreateCapacity(capacity2);

            var capacitySettings1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacitySetting(capacity1.ID)
            {
                Value = 150,
            };

            var capacitySettings2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacitySetting(capacity2.ID)
            {
                Value = 75.123m,
            };

            // Create Resource with one capacity assigned
            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };
            unmanagedResource.AddCapacity(capacitySettings1);
            objectCreator.CreateResource(unmanagedResource);

            var resource = TestContext.Api.Resources.Read(unmanagedResource.ID);
            Assert.AreEqual(1, resource.Capacities.Count);
            var resourceCapacity1 = resource.Capacities.Single();
            Assert.AreEqual(capacitySettings1.Id, resourceCapacity1.Id);
            Assert.AreEqual(150, ((Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceNumberCapacitySetting)resourceCapacity1).Value);

            // Update capacity value
            capacitySettings1 = resource.Capacities.First(x => x.Id == capacity1.ID) as Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceNumberCapacitySetting;
            Assert.IsNotNull(capacitySettings1);
            capacitySettings1.Value = 180;
            TestContext.Api.Resources.Update(resource);

            resource = TestContext.Api.Resources.Read(unmanagedResource.ID);
            Assert.AreEqual(1, resource.Capacities.Count);
            resourceCapacity1 = resource.Capacities.Single();
            Assert.AreEqual(capacitySettings1.Id, resourceCapacity1.Id);
            Assert.AreEqual(180, ((Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceNumberCapacitySetting)resourceCapacity1).Value);

            // Update Resource to add second capacity
            resource.AddCapacity(capacitySettings2);
            TestContext.Api.Resources.Update(resource);

            resource = TestContext.Api.Resources.Read(unmanagedResource.ID);
            Assert.AreEqual(2, resource.Capacities.Count);

            resourceCapacity1 = resource.Capacities.SingleOrDefault(x => x.Id == capacity1.ID);
            Assert.AreEqual(capacitySettings1.Id, resourceCapacity1.Id);
            Assert.AreEqual(180, ((Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceNumberCapacitySetting)resourceCapacity1).Value);

            var resourceCapacity2 = resource.Capacities.SingleOrDefault(x => x.Id == capacity2.ID);
            Assert.AreEqual(capacitySettings2.Id, resourceCapacity2.Id);
            Assert.AreEqual(75.123m, ((Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceNumberCapacitySetting)resourceCapacity2).Value);

            // Update Resource to remove first capacity
            var capacitySettingsToRemove = resource.Capacities.First(x => x.Id == capacity1.ID);
            resource.RemoveCapacity(capacitySettingsToRemove);
            TestContext.Api.Resources.Update(resource);

            resource = TestContext.Api.Resources.Read(unmanagedResource.ID);
            Assert.AreEqual(1, resource.Capacities.Count);
            resourceCapacity2 = resource.Capacities.SingleOrDefault(x => x.Id == capacity2.ID);
            Assert.AreEqual(capacitySettings2.Id, resourceCapacity2.Id);
            Assert.AreEqual(75.123m, ((Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceNumberCapacitySetting)resourceCapacity2).Value);
        }

        [TestMethod]
        public void CompleteResourceNumberCapacityCRUD()
        {
            var prefix = Guid.NewGuid();

            var capacity1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity()
            {
                Name = $"{prefix}_Capacity1",
                RangeMin = 100,
                RangeMax = 200,
                StepSize = 5,
            };
            objectCreator.CreateCapacity(capacity1);

            var capacity2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity()
            {
                Name = $"{prefix}_Capacity2",
                Decimals = 3,
            };
            objectCreator.CreateCapacity(capacity2);

            var capacitySettings1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacitySetting(capacity1.ID)
            {
                Value = 150,
            };

            var capacitySettings2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacitySetting(capacity2.ID)
            {
                Value = 75.123m,
            };

            // Create Resource with one capacity assigned
            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };
            unmanagedResource.AddCapacity(capacitySettings1);
            objectCreator.CreateResource(unmanagedResource);

            // Move Resource to Completed state
            TestContext.Api.Resources.Complete(unmanagedResource.ID);

            var resource = TestContext.Api.Resources.Read(unmanagedResource.ID);
            Assert.AreEqual(1, resource.Capacities.Count);
            var resourceCapacity1 = resource.Capacities.Single();
            Assert.AreEqual(capacitySettings1.Id, resourceCapacity1.Id);
            Assert.AreEqual(150, ((Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceNumberCapacitySetting)resourceCapacity1).Value);

            Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);
            var coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);

            var coreResourceCapacity1 = coreResource.Capacities.SingleOrDefault(x => x.CapacityProfileID == capacity1.ID);
            Assert.IsNotNull(coreResourceCapacity1);
            Assert.AreEqual(150, coreResourceCapacity1.Value.MaxDecimalQuantity);

            // Update capacity value
            capacitySettings1 = resource.Capacities.First(x => x.Id == capacity1.ID) as Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceNumberCapacitySetting;
            Assert.IsNotNull(capacitySettings1);
            capacitySettings1.Value = 180;
            TestContext.Api.Resources.Update(resource);

            resource = TestContext.Api.Resources.Read(unmanagedResource.ID);
            Assert.AreEqual(1, resource.Capacities.Count);
            resourceCapacity1 = resource.Capacities.Single();
            Assert.AreEqual(capacitySettings1.Id, resourceCapacity1.Id);
            Assert.AreEqual(180, ((Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceNumberCapacitySetting)resourceCapacity1).Value);

            Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);
            coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);

            coreResourceCapacity1 = coreResource.Capacities.SingleOrDefault(x => x.CapacityProfileID == capacity1.ID);
            Assert.IsNotNull(coreResourceCapacity1);
            Assert.AreEqual(180, coreResourceCapacity1.Value.MaxDecimalQuantity);

            // Update Resource to add second capacity
            resource.AddCapacity(capacitySettings2);
            TestContext.Api.Resources.Update(resource);

            resource = TestContext.Api.Resources.Read(unmanagedResource.ID);
            Assert.AreEqual(2, resource.Capacities.Count);

            resourceCapacity1 = resource.Capacities.SingleOrDefault(x => x.Id == capacity1.ID);
            Assert.AreEqual(capacitySettings1.Id, resourceCapacity1.Id);
            Assert.AreEqual(180, ((Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceNumberCapacitySetting)resourceCapacity1).Value);

            var resourceCapacity2 = resource.Capacities.SingleOrDefault(x => x.Id == capacity2.ID);
            Assert.AreEqual(capacitySettings2.Id, resourceCapacity2.Id);
            Assert.AreEqual(75.123m, ((Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceNumberCapacitySetting)resourceCapacity2).Value);

            Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);
            coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);
            Assert.IsNotNull(coreResource);

            coreResourceCapacity1 = coreResource.Capacities.SingleOrDefault(x => x.CapacityProfileID == capacity1.ID);
            Assert.IsNotNull(coreResourceCapacity1);
            Assert.AreEqual(180, coreResourceCapacity1.Value.MaxDecimalQuantity);

            var coreResourceCapacity2 = coreResource.Capacities.SingleOrDefault(x => x.CapacityProfileID == capacity2.ID);
            Assert.IsNotNull(coreResourceCapacity2);
            Assert.AreEqual(75.123m, coreResourceCapacity2.Value.MaxDecimalQuantity);

            // Update Resource to remove first capacity
            var capacitySettingsToRemove = resource.Capacities.First(x => x.Id == capacity1.ID);
            resource.RemoveCapacity(capacitySettingsToRemove);
            TestContext.Api.Resources.Update(resource);

            resource = TestContext.Api.Resources.Read(unmanagedResource.ID);
            Assert.AreEqual(1, resource.Capacities.Count);
            resourceCapacity2 = resource.Capacities.SingleOrDefault(x => x.Id == capacity2.ID);
            Assert.AreEqual(capacitySettings2.Id, resourceCapacity2.Id);
            Assert.AreEqual(75.123m, ((Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceNumberCapacitySetting)resourceCapacity2).Value);

            Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);
            coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);

            coreResourceCapacity1 = coreResource.Capacities.SingleOrDefault(x => x.CapacityProfileID == capacity1.ID);
            Assert.IsNull(coreResourceCapacity1);

            coreResourceCapacity2 = coreResource.Capacities.SingleOrDefault(x => x.CapacityProfileID == capacity2.ID);
            Assert.IsNotNull(coreResourceCapacity2);
            Assert.AreEqual(75.123m, coreResourceCapacity2.Value.MaxDecimalQuantity);
        }

        [TestMethod]
        public void Dom_SingleResourceNumberCapacity()
        {
            var prefix = Guid.NewGuid();

            var capacity = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity()
            {
                Name = $"{prefix}_Capacity",
            };
            objectCreator.CreateCapacity(capacity);

            var capacitySettings = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacitySetting(capacity.ID)
            {
                Value = 10,
            };

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };
            unmanagedResource.AddCapacity(capacitySettings);
            objectCreator.CreateResource(unmanagedResource);

            var domResource = TestContext.ResourceStudioDomHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(unmanagedResource.ID)).SingleOrDefault();
            Assert.IsTrue(domResource.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourceCapacities.Id.Id));

            var domResourceCapacitiesSections = domResource.Sections.Where(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourceCapacities.Id.Id).ToList();
            Assert.AreEqual(1, domResourceCapacitiesSections.Count);

            var domResourceCapacity = domResourceCapacitiesSections.Single();
            var fdProfileParameterId = domResourceCapacity.FieldValues.SingleOrDefault(f => f.FieldDescriptorID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourceCapacities.ProfileParameterID.Id);
            Assert.IsNotNull(fdProfileParameterId);
            Assert.IsTrue(Guid.TryParse(Convert.ToString(fdProfileParameterId.Value.Value), out var profileParameterId));
            Assert.AreEqual(capacity.ID, profileParameterId);

            var fdDoubleMaxValue = domResourceCapacity.FieldValues.SingleOrDefault(f => f.FieldDescriptorID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourceCapacities.DoubleMaxValue.Id);
            Assert.IsNotNull(fdDoubleMaxValue);
            Assert.AreEqual(10m, Convert.ToDecimal(fdDoubleMaxValue.Value.Value));

            var fdDoubleMinValue = domResourceCapacity.FieldValues.SingleOrDefault(f => f.FieldDescriptorID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourceCapacities.DoubleMinValue.Id);
            Assert.IsNull(fdDoubleMinValue);
        }

        [TestMethod]
        public void Dom_SingleResourceRangeCapacity()
        {
            var prefix = Guid.NewGuid();

            var capacity = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.RangeCapacity()
            {
                Name = $"{prefix}_Capacity",
            };
            objectCreator.CreateCapacity(capacity);

            var capacitySettings = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.RangeCapacitySetting(capacity.ID)
            {
                MinValue = 10,
                MaxValue = 20,
            };

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };
            unmanagedResource.AddCapacity(capacitySettings);
            objectCreator.CreateResource(unmanagedResource);

            var domResource = TestContext.ResourceStudioDomHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(unmanagedResource.ID)).SingleOrDefault();
            Assert.IsTrue(domResource.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourceCapacities.Id.Id));

            var domResourceCapacitiesSections = domResource.Sections.Where(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourceCapacities.Id.Id).ToList();
            Assert.AreEqual(1, domResourceCapacitiesSections.Count);

            var domResourceCapacity = domResourceCapacitiesSections.Single();
            var fdProfileParameterId = domResourceCapacity.FieldValues.SingleOrDefault(f => f.FieldDescriptorID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourceCapacities.ProfileParameterID.Id);
            Assert.IsNotNull(fdProfileParameterId);
            Assert.IsTrue(Guid.TryParse(Convert.ToString(fdProfileParameterId.Value.Value), out var profileParameterId));
            Assert.AreEqual(capacity.ID, profileParameterId);

            var fdDoubleMaxValue = domResourceCapacity.FieldValues.SingleOrDefault(f => f.FieldDescriptorID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourceCapacities.DoubleMaxValue.Id);
            Assert.IsNotNull(fdDoubleMaxValue);
            Assert.AreEqual(20m, Convert.ToDecimal(fdDoubleMaxValue.Value.Value));

            var fdDoubleMinValue = domResourceCapacity.FieldValues.SingleOrDefault(f => f.FieldDescriptorID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourceCapacities.DoubleMinValue.Id);
            Assert.IsNotNull(fdDoubleMinValue);
            Assert.AreEqual(10m, Convert.ToDecimal(fdDoubleMinValue.Value.Value));
        }

        [TestMethod]
        public void Core_SingleResourceRangeCapacity()
        {
            var prefix = Guid.NewGuid();

            var capacity = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.RangeCapacity()
            {
                Name = $"{prefix}_Capacity",
            };
            objectCreator.CreateCapacity(capacity);

            var capacitySettings = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.RangeCapacitySetting(capacity.ID)
            {
                MinValue = 10,
                MaxValue = 20,
            };

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };
            unmanagedResource.AddCapacity(capacitySettings);
            objectCreator.CreateResource(unmanagedResource);
            TestContext.Api.Resources.Complete(unmanagedResource.ID);

            var resource = TestContext.Api.Resources.Read(unmanagedResource.ID);
            Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);

            var coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);
            Assert.IsNotNull(coreResource);

            var coreResourceCapacity = coreResource.Capacities.SingleOrDefault(x => x.CapacityProfileID == capacity.ID);
            Assert.IsNotNull(coreResourceCapacity);
            Assert.AreEqual(10m, coreResourceCapacity.Value.MinDecimalQuantity);
            Assert.AreEqual(20m, coreResourceCapacity.Value.MaxDecimalQuantity);
        }

        [TestMethod]
        public void AssignWithEmptyIdThrowsException()
        {
            var prefix = Guid.NewGuid();

            Assert.ThrowsException<ArgumentException>(() => new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacitySetting(Guid.Empty));
            Assert.ThrowsException<ArgumentException>(() => new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacitySetting(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity(Guid.Empty)));

            Assert.ThrowsException<ArgumentException>(() => new Skyline.DataMiner.Solutions.MediaOps.Plan.API.RangeCapacitySetting(Guid.Empty));
            Assert.ThrowsException<ArgumentException>(() => new Skyline.DataMiner.Solutions.MediaOps.Plan.API.RangeCapacitySetting(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.RangeCapacity(Guid.Empty)));
        }

        [TestMethod]
        public void AssignNotExistingNumberCapacityThrowsException()
        {
            var prefix = Guid.NewGuid();

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };

            var notExistingCapacityId = Guid.NewGuid();
            var capacitySettings = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacitySetting(notExistingCapacityId)
            {
                Value = 10,
            };
            unmanagedResource.AddCapacity(capacitySettings);

            try
            {
                objectCreator.CreateResource(unmanagedResource);
            }
            catch (MediaOpsException ex)
            {
                var errorMessage = $"Capacity with ID '{notExistingCapacityId}' not found.";
                Assert.AreEqual(errorMessage, ex.Message);

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var resourceConfigurationError = ex.TraceData.ErrorData.OfType<ResourceError>().SingleOrDefault();
                Assert.IsNotNull(resourceConfigurationError);

                var invalidResourceCapacitySettingsError = resourceConfigurationError as ResourceInvalidCapacitySettingsError;
                Assert.IsNotNull(invalidResourceCapacitySettingsError);
                Assert.AreEqual(errorMessage, invalidResourceCapacitySettingsError.ErrorMessage);
                Assert.AreEqual(notExistingCapacityId, invalidResourceCapacitySettingsError.CapacityId);

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void AssignNotExistingRangeCapacityThrowsException()
        {
            var prefix = Guid.NewGuid();

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };

            var notExistingCapacityId = Guid.NewGuid();
            var capacitySettings = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.RangeCapacitySetting(notExistingCapacityId)
            {
                MinValue = 10,
                MaxValue = 20,
            };
            unmanagedResource.AddCapacity(capacitySettings);

            try
            {
                objectCreator.CreateResource(unmanagedResource);
            }
            catch (MediaOpsException ex)
            {
                var errorMessage = $"Capacity with ID '{notExistingCapacityId}' not found.";
                Assert.AreEqual(errorMessage, ex.Message);

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var resourceConfigurationError = ex.TraceData.ErrorData.OfType<ResourceError>().SingleOrDefault();
                Assert.IsNotNull(resourceConfigurationError);

                var invalidResourceCapacitySettingsError = resourceConfigurationError as ResourceInvalidCapacitySettingsError;
                Assert.IsNotNull(invalidResourceCapacitySettingsError);
                Assert.AreEqual(errorMessage, invalidResourceCapacitySettingsError.ErrorMessage);
                Assert.AreEqual(notExistingCapacityId, invalidResourceCapacitySettingsError.CapacityId);

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void AssignWithOutOfRangeValueThrowsException()
        {
            var prefix = Guid.NewGuid();

            var capacity1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity()
            {
                Name = $"{prefix}_Capacity1",
                RangeMin = 10,
                RangeMax = 20,
            };
            objectCreator.CreateCapacity(capacity1);

            var capacity2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity()
            {
                Name = $"{prefix}_Capacity2",
                RangeMin = 10,
                RangeMax = 20,
            };
            objectCreator.CreateCapacity(capacity2);

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };

            var capacitySettings1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacitySetting(capacity1.ID)
            {
                Value = 5, // below min
            };
            unmanagedResource.AddCapacity(capacitySettings1);

            var capacitySettings2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacitySetting(capacity2.ID)
            {
                Value = 25, // above max
            };
            unmanagedResource.AddCapacity(capacitySettings2);

            try
            {
                objectCreator.CreateResource(unmanagedResource);
            }
            catch (MediaOpsException ex)
            {
                Assert.AreEqual(2, ex.TraceData.ErrorData.Count);
                var resourceConfigurationErrors = ex.TraceData.ErrorData.OfType<ResourceError>();
                Assert.AreEqual(2, resourceConfigurationErrors.Count());

                var invalidResourceCapacitySettingsErrors = resourceConfigurationErrors.OfType<ResourceInvalidCapacitySettingsError>();
                Assert.AreEqual(2, invalidResourceCapacitySettingsErrors.Count());

                var error1 = invalidResourceCapacitySettingsErrors.SingleOrDefault(e => e.CapacityId == capacity1.ID);
                Assert.IsNotNull(error1);
                Assert.AreEqual("Value '5' must be greater than or equal to '10'.", error1.ErrorMessage);

                var error2 = invalidResourceCapacitySettingsErrors.SingleOrDefault(e => e.CapacityId == capacity2.ID);
                Assert.IsNotNull(error2);
                Assert.AreEqual("Value '25' must be lower than or equal to '20'.", error2.ErrorMessage);

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void AssignWithInvalidDecimalsThrowsException()
        {
            var prefix = Guid.NewGuid();

            var capacity = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity()
            {
                Name = $"{prefix}_Capacity",
                RangeMin = 10,
                RangeMax = 20,
                Decimals = 3,
            };
            objectCreator.CreateCapacity(capacity);

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };

            var capacitySettings = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacitySetting(capacity.ID)
            {
                Value = 15.1234m,
            };
            unmanagedResource.AddCapacity(capacitySettings);

            try
            {
                objectCreator.CreateResource(unmanagedResource);
            }
            catch (MediaOpsException ex)
            {
                var errorMessage = $"Value '{capacitySettings.Value}' must contain less than '{capacity.Decimals}' decimals.";
                Assert.AreEqual(errorMessage, ex.Message);

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var resourceConfigurationError = ex.TraceData.ErrorData.OfType<ResourceError>().SingleOrDefault();
                Assert.IsNotNull(resourceConfigurationError);

                var invalidResourceCapacitySettingsError = resourceConfigurationError as ResourceInvalidCapacitySettingsError;
                Assert.IsNotNull(invalidResourceCapacitySettingsError);
                Assert.AreEqual(errorMessage, invalidResourceCapacitySettingsError.ErrorMessage);
                Assert.AreEqual(capacity.ID, invalidResourceCapacitySettingsError.CapacityId);

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void AssignWithInvalidStepSizeThrowsException()
        {
            var prefix = Guid.NewGuid();

            var capacity1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity()
            {
                Name = $"{prefix}_Capacity 1",
                RangeMin = 10,
                StepSize = 3,
            };
            objectCreator.CreateCapacity(capacity1);

            var capacity2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity()
            {
                Name = $"{prefix}_Capacity 2",
                RangeMax = 20,
                StepSize = 3,
            };
            objectCreator.CreateCapacity(capacity2);

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };

            var capacitySettings1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacitySetting(capacity1.ID)
            {
                Value = 15,
            };
            unmanagedResource.AddCapacity(capacitySettings1);

            var capacitySettings2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacitySetting(capacity2.ID)
            {
                Value = 15,
            };
            unmanagedResource.AddCapacity(capacitySettings2);

            try
            {
                objectCreator.CreateResource(unmanagedResource);
            }
            catch (MediaOpsException ex)
            {
                Assert.AreEqual(2, ex.TraceData.ErrorData.Count);
                var resourceConfigurationErrors = ex.TraceData.ErrorData.OfType<ResourceError>();
                Assert.AreEqual(2, resourceConfigurationErrors.Count());

                var invalidResourceCapacitySettingsErrors = resourceConfigurationErrors.OfType<ResourceInvalidCapacitySettingsError>();
                Assert.AreEqual(2, invalidResourceCapacitySettingsErrors.Count());

                var error1 = invalidResourceCapacitySettingsErrors.SingleOrDefault(e => e.CapacityId == capacity1.ID);
                Assert.IsNotNull(error1);
                Assert.AreEqual("Value '15' must align with the step size of '3'.", error1.ErrorMessage);

                var error2 = invalidResourceCapacitySettingsErrors.SingleOrDefault(e => e.CapacityId == capacity2.ID);
                Assert.IsNotNull(error2);
                Assert.AreEqual("Value '15' must align with the step size of '3'.", error2.ErrorMessage);

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void AssignWithWrongRangeThrowsException()
        {
            var prefix = Guid.NewGuid();

            var capacity = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.RangeCapacity()
            {
                Name = $"{prefix}_Capacity",
                RangeMin = 10,
                RangeMax = 20,
            };
            objectCreator.CreateCapacity(capacity);

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };

            var capacitySettings = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.RangeCapacitySetting(capacity.ID)
            {
                MinValue = 18,
                MaxValue = 12,
            };
            unmanagedResource.AddCapacity(capacitySettings);

            try
            {
                objectCreator.CreateResource(unmanagedResource);
            }
            catch (MediaOpsException ex)
            {
                var errorMessage = "Max value '12' must be greater than min value '18'.";
                Assert.AreEqual(errorMessage, ex.Message);

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var resourceConfigurationError = ex.TraceData.ErrorData.OfType<ResourceError>().SingleOrDefault();
                Assert.IsNotNull(resourceConfigurationError);

                var invalidResourceCapacitySettingsError = resourceConfigurationError as ResourceInvalidCapacitySettingsError;
                Assert.IsNotNull(invalidResourceCapacitySettingsError);
                Assert.AreEqual(errorMessage, invalidResourceCapacitySettingsError.ErrorMessage);
                Assert.AreEqual(capacity.ID, invalidResourceCapacitySettingsError.CapacityId);

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void CreateWithDuplicateSettingsThrowsException()
        {
            var prefix = Guid.NewGuid();

            var capacity1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity()
            {
                Name = $"{prefix}_Capacity1",
            };
            var capacity2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity()
            {
                Name = $"{prefix}_Capacity2",
            };
            objectCreator.CreateCapacities([capacity1, capacity2]);

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            }
            .AddCapacity(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacitySetting(capacity1.ID)
            {
                Value = 10,
            })
            .AddCapacity(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacitySetting(capacity2.ID)
            {
                Value = 20,
            })
            .AddCapacity(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacitySetting(capacity1.ID)
            {
                Value = 30,
            });

            try
            {
                objectCreator.CreateResource(unmanagedResource);
            }
            catch (MediaOpsException ex)
            {
                var errorMessage = $"Capacity with ID '{capacity1.ID}' is defined 2 times. Duplicate capacity settings are not allowed.";
                Assert.AreEqual(errorMessage, ex.Message);
                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);

                var resourceConfigurationError = ex.TraceData.ErrorData.OfType<ResourceError>().SingleOrDefault();
                Assert.IsNotNull(resourceConfigurationError);

                var invalidResourceCapacitySettingsError = resourceConfigurationError as ResourceInvalidCapacitySettingsError;
                Assert.IsNotNull(invalidResourceCapacitySettingsError);
                Assert.AreEqual(errorMessage, invalidResourceCapacitySettingsError.ErrorMessage);
                Assert.AreEqual(capacity1.ID, invalidResourceCapacitySettingsError.CapacityId);
                Assert.AreEqual(unmanagedResource.ID, invalidResourceCapacitySettingsError.Id);

                return;
            }

            Assert.Fail("Exception not thrown");
        }

        [TestMethod]
        public void UpdateWithDuplicateSettingsThrowsException()
        {
            var prefix = Guid.NewGuid();

            var capacity1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity()
            {
                Name = $"{prefix}_Capacity1",
            };
            var capacity2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity()
            {
                Name = $"{prefix}_Capacity2",
            };
            objectCreator.CreateCapacities([capacity1, capacity2]);

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            }
            .AddCapacity(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacitySetting(capacity1.ID)
            {
                Value = 10,
            })
            .AddCapacity(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacitySetting(capacity2.ID)
            {
                Value = 20,
            });
            objectCreator.CreateResource(unmanagedResource);

            var resource = TestContext.Api.Resources.Read(unmanagedResource.ID);
            resource.AddCapacity(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacitySetting(capacity1.ID)
            {
                Value = 30,
            });

            try
            {
                TestContext.Api.Resources.Update(resource);
            }
            catch (MediaOpsException ex)
            {
                var errorMessage = $"Capacity with ID '{capacity1.ID}' is defined 2 times. Duplicate capacity settings are not allowed.";
                Assert.AreEqual(errorMessage, ex.Message);
                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);

                var resourceConfigurationError = ex.TraceData.ErrorData.OfType<ResourceError>().SingleOrDefault();
                Assert.IsNotNull(resourceConfigurationError);

                var invalidResourceCapacitySettingsError = resourceConfigurationError as ResourceInvalidCapacitySettingsError;
                Assert.IsNotNull(invalidResourceCapacitySettingsError);
                Assert.AreEqual(errorMessage, invalidResourceCapacitySettingsError.ErrorMessage);
                Assert.AreEqual(capacity1.ID, invalidResourceCapacitySettingsError.CapacityId);
                Assert.AreEqual(unmanagedResource.ID, invalidResourceCapacitySettingsError.Id);

                return;
            }

            Assert.Fail("Exception not thrown");
        }

        [TestMethod]
        public void AssignCapacityFromExistingResourceToNewResource()
        {
            var prefix = Guid.NewGuid();

            var capacity = new NumberCapacity()
            {
                Name = $"{prefix}_Capacity",
            };
            objectCreator.CreateCapacity(capacity);

            var unmanagedResource1 = new UnmanagedResource()
            {
                Name = $"{prefix}_Resource1",
            }
            .AddCapacity(new NumberCapacitySetting(capacity.ID)
            {
                Value = 50,
            });

            objectCreator.CreateResource(unmanagedResource1);
            var resource1 = TestContext.Api.Resources.Read(unmanagedResource1.ID);

            var unmanagedResource2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resourc2",
            };

            foreach (var capacitySetting in resource1.Capacities)
            {
                unmanagedResource2.AddCapacity(capacitySetting);
            }

            objectCreator.CreateResource(unmanagedResource2);

            var resource2 = TestContext.Api.Resources.Read(unmanagedResource2.ID);
            Assert.IsNotNull(resource2);
        }

        [TestMethod]
        public void AddAndRemoveNumberCapacitySettingsOnDraftResource()
        {
            var prefix = Guid.NewGuid();

            var capacity = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity()
            {
                Name = $"{prefix}_Capacity",
                RangeMin = 0,
                RangeMax = 100,
                StepSize = 1,
            };
            objectCreator.CreateCapacity(capacity);

            var capacitySettings = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacitySetting(capacity.ID)
            {
                Value = 10,
            };

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };

            // Assign capacity settings on the draft resource object.
            unmanagedResource.AddCapacity(capacitySettings);
            Assert.AreEqual(1, unmanagedResource.Capacities.Count);

            // Remove the capacity settings again, still without any create/update call.
            unmanagedResource.RemoveCapacity(capacitySettings);

            // No call to CreateResource / Update here. We only validate in-memory behavior.
            Assert.AreEqual(0, unmanagedResource.Capacities.Count);
        }

        [TestMethod]
        public void AddAndRemoveRangeCapacitySettingsOnDraftResource()
        {
            var prefix = Guid.NewGuid();

            var capacity = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.RangeCapacity()
            {
                Name = $"{prefix}_Capacity",
            };
            objectCreator.CreateCapacity(capacity);

            var capacitySettings = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.RangeCapacitySetting(capacity.ID)
            {
                MinValue = 5,
                MaxValue = 15,
            };

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };

            // Assign capacity settings on the draft resource object.
            unmanagedResource.AddCapacity(capacitySettings);
            Assert.AreEqual(1, unmanagedResource.Capacities.Count);

            // Remove the capacity settings again, still without any create/update call.
            unmanagedResource.RemoveCapacity(capacitySettings);

            // No call to CreateResource / Update here. We only validate in-memory behavior.
            Assert.AreEqual(0, unmanagedResource.Capacities.Count);
        }
    }
}
