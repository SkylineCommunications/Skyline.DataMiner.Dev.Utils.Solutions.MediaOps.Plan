namespace RT_MediaOps.Plan.RST.Resources
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using RT_MediaOps.Plan.RegressionTests;

    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Net.SLConfiguration;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

    using Storage = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public sealed class CapacityAssignmentTests : IDisposable
    {
        private readonly ResourceStudioObjectCreator objectCreator;

        public CapacityAssignmentTests()
        {
            objectCreator = new ResourceStudioObjectCreator(TestContext.Api);
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
                rangeMin = 100,
                rangeMax = 200,
                stepSize = 5,
            };
            objectCreator.CreateCapacity(capacity1);

            var capacity2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity()
            {
                Name = $"{prefix}_Capacity2",
                decimals = 3,
            };
            objectCreator.CreateCapacity(capacity2);

            var capacitySettings1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceNumberCapacitySettings(capacity1.Id)
            {
                Value = 150,
            };

            var capacitySettings2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceNumberCapacitySettings(capacity2.Id)
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

            var resource = TestContext.Api.Resources.Read(unmanagedResource.Id);
            Assert.AreEqual(1, resource.Capacities.Count);
            var resourceCapacity1 = resource.Capacities.Single();
            Assert.AreEqual(capacitySettings1.Id, resourceCapacity1.Id);
            Assert.AreEqual(150, ((Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceNumberCapacitySettings)resourceCapacity1).Value);

            // Update capacity value
            capacitySettings1 = resource.Capacities.First(x => x.Id == capacity1.Id) as Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceNumberCapacitySettings;
            Assert.IsNotNull(capacitySettings1);
            capacitySettings1.Value = 180;
            TestContext.Api.Resources.Update(resource);

            resource = TestContext.Api.Resources.Read(unmanagedResource.Id);
            Assert.AreEqual(1, resource.Capacities.Count);
            resourceCapacity1 = resource.Capacities.Single();
            Assert.AreEqual(capacitySettings1.Id, resourceCapacity1.Id);
            Assert.AreEqual(180, ((Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceNumberCapacitySettings)resourceCapacity1).Value);

            // Update Resource to add second capacity
            resource.AddCapacity(capacitySettings2);
            TestContext.Api.Resources.Update(resource);

            resource = TestContext.Api.Resources.Read(unmanagedResource.Id);
            Assert.AreEqual(2, resource.Capacities.Count);

            resourceCapacity1 = resource.Capacities.SingleOrDefault(x => x.Id == capacity1.Id);
            Assert.AreEqual(capacitySettings1.Id, resourceCapacity1.Id);
            Assert.AreEqual(180, ((Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceNumberCapacitySettings)resourceCapacity1).Value);

            var resourceCapacity2 = resource.Capacities.SingleOrDefault(x => x.Id == capacity2.Id);
            Assert.AreEqual(capacitySettings2.Id, resourceCapacity2.Id);
            Assert.AreEqual(75.123m, ((Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceNumberCapacitySettings)resourceCapacity2).Value);

            // Update Resource to remove first capacity
            var capacitySettingsToRemove = resource.Capacities.First(x => x.Id == capacity1.Id);
            resource.RemoveCapacity(capacitySettingsToRemove);
            TestContext.Api.Resources.Update(resource);

            resource = TestContext.Api.Resources.Read(unmanagedResource.Id);
            Assert.AreEqual(1, resource.Capacities.Count);
            resourceCapacity2 = resource.Capacities.SingleOrDefault(x => x.Id == capacity2.Id);
            Assert.AreEqual(capacitySettings2.Id, resourceCapacity2.Id);
            Assert.AreEqual(75.123m, ((Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceNumberCapacitySettings)resourceCapacity2).Value);
        }

        [TestMethod]
        public void CompleteResourceNumberCapacityCRUD()
        {
            var prefix = Guid.NewGuid();

            var capacity1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity()
            {
                Name = $"{prefix}_Capacity1",
                rangeMin = 100,
                rangeMax = 200,
                stepSize = 5,
            };
            objectCreator.CreateCapacity(capacity1);

            var capacity2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity()
            {
                Name = $"{prefix}_Capacity2",
                decimals = 3,
            };
            objectCreator.CreateCapacity(capacity2);

            var capacitySettings1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceNumberCapacitySettings(capacity1.Id)
            {
                Value = 150,
            };

            var capacitySettings2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceNumberCapacitySettings(capacity2.Id)
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
            TestContext.Api.Resources.MoveTo(unmanagedResource.Id, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceState.Complete);

            var resource = TestContext.Api.Resources.Read(unmanagedResource.Id);
            Assert.AreEqual(1, resource.Capacities.Count);
            var resourceCapacity1 = resource.Capacities.Single();
            Assert.AreEqual(capacitySettings1.Id, resourceCapacity1.Id);
            Assert.AreEqual(150, ((Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceNumberCapacitySettings)resourceCapacity1).Value);

            Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);
            var coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);

            var coreResourceCapacity1 = coreResource.Capacities.SingleOrDefault(x => x.CapacityProfileID == capacity1.Id);
            Assert.IsNotNull(coreResourceCapacity1);
            Assert.AreEqual(150, coreResourceCapacity1.Value.MaxDecimalQuantity);

            // Update capacity value
            capacitySettings1 = resource.Capacities.First(x => x.Id == capacity1.Id) as Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceNumberCapacitySettings;
            Assert.IsNotNull(capacitySettings1);
            capacitySettings1.Value = 180;
            TestContext.Api.Resources.Update(resource);

            resource = TestContext.Api.Resources.Read(unmanagedResource.Id);
            Assert.AreEqual(1, resource.Capacities.Count);
            resourceCapacity1 = resource.Capacities.Single();
            Assert.AreEqual(capacitySettings1.Id, resourceCapacity1.Id);
            Assert.AreEqual(180, ((Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceNumberCapacitySettings)resourceCapacity1).Value);

            Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);
            coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);

            coreResourceCapacity1 = coreResource.Capacities.SingleOrDefault(x => x.CapacityProfileID == capacity1.Id);
            Assert.IsNotNull(coreResourceCapacity1);
            Assert.AreEqual(180, coreResourceCapacity1.Value.MaxDecimalQuantity);

            // Update Resource to add second capacity
            resource.AddCapacity(capacitySettings2);
            TestContext.Api.Resources.Update(resource);

            resource = TestContext.Api.Resources.Read(unmanagedResource.Id);
            Assert.AreEqual(2, resource.Capacities.Count);

            resourceCapacity1 = resource.Capacities.SingleOrDefault(x => x.Id == capacity1.Id);
            Assert.AreEqual(capacitySettings1.Id, resourceCapacity1.Id);
            Assert.AreEqual(180, ((Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceNumberCapacitySettings)resourceCapacity1).Value);

            var resourceCapacity2 = resource.Capacities.SingleOrDefault(x => x.Id == capacity2.Id);
            Assert.AreEqual(capacitySettings2.Id, resourceCapacity2.Id);
            Assert.AreEqual(75.123m, ((Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceNumberCapacitySettings)resourceCapacity2).Value);

            Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);
            coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);
            Assert.IsNotNull(coreResource);

            coreResourceCapacity1 = coreResource.Capacities.SingleOrDefault(x => x.CapacityProfileID == capacity1.Id);
            Assert.IsNotNull(coreResourceCapacity1);
            Assert.AreEqual(180, coreResourceCapacity1.Value.MaxDecimalQuantity);

            var coreResourceCapacity2 = coreResource.Capacities.SingleOrDefault(x => x.CapacityProfileID == capacity2.Id);
            Assert.IsNotNull(coreResourceCapacity2);
            Assert.AreEqual(75.123m, coreResourceCapacity2.Value.MaxDecimalQuantity);

            // Update Resource to remove first capacity
            var capacitySettingsToRemove = resource.Capacities.First(x => x.Id == capacity1.Id);
            resource.RemoveCapacity(capacitySettingsToRemove);
            TestContext.Api.Resources.Update(resource);

            resource = TestContext.Api.Resources.Read(unmanagedResource.Id);
            Assert.AreEqual(1, resource.Capacities.Count);
            resourceCapacity2 = resource.Capacities.SingleOrDefault(x => x.Id == capacity2.Id);
            Assert.AreEqual(capacitySettings2.Id, resourceCapacity2.Id);
            Assert.AreEqual(75.123m, ((Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceNumberCapacitySettings)resourceCapacity2).Value);

            Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);
            coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);

            coreResourceCapacity1 = coreResource.Capacities.SingleOrDefault(x => x.CapacityProfileID == capacity1.Id);
            Assert.IsNull(coreResourceCapacity1);

            coreResourceCapacity2 = coreResource.Capacities.SingleOrDefault(x => x.CapacityProfileID == capacity2.Id);
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

            var capacitySettings = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceNumberCapacitySettings(capacity.Id)
            {
                Value = 10,
            };

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };
            unmanagedResource.AddCapacity(capacitySettings);
            objectCreator.CreateResource(unmanagedResource);

            var domResource = TestContext.ResourceStudioDomHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(unmanagedResource.Id)).SingleOrDefault();
            Assert.IsTrue(domResource.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourceCapacities.Id.Id));

            var domResourceCapacitiesSections = domResource.Sections.Where(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourceCapacities.Id.Id).ToList();
            Assert.AreEqual(1, domResourceCapacitiesSections.Count);

            var domResourceCapacity = domResourceCapacitiesSections.Single();
            var fdProfileParameterId = domResourceCapacity.FieldValues.SingleOrDefault(f => f.FieldDescriptorID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourceCapacities.ProfileParameterID.Id);
            Assert.IsNotNull(fdProfileParameterId);
            Assert.IsTrue(Guid.TryParse(Convert.ToString(fdProfileParameterId.Value.Value), out var profileParameterId));
            Assert.AreEqual(capacity.Id, profileParameterId);

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

            var capacitySettings = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceRangeCapacitySettings(capacity.Id)
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

            var domResource = TestContext.ResourceStudioDomHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(unmanagedResource.Id)).SingleOrDefault();
            Assert.IsTrue(domResource.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourceCapacities.Id.Id));

            var domResourceCapacitiesSections = domResource.Sections.Where(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourceCapacities.Id.Id).ToList();
            Assert.AreEqual(1, domResourceCapacitiesSections.Count);

            var domResourceCapacity = domResourceCapacitiesSections.Single();
            var fdProfileParameterId = domResourceCapacity.FieldValues.SingleOrDefault(f => f.FieldDescriptorID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourceCapacities.ProfileParameterID.Id);
            Assert.IsNotNull(fdProfileParameterId);
            Assert.IsTrue(Guid.TryParse(Convert.ToString(fdProfileParameterId.Value.Value), out var profileParameterId));
            Assert.AreEqual(capacity.Id, profileParameterId);

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

            var capacitySettings = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceRangeCapacitySettings(capacity.Id)
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
            TestContext.Api.Resources.MoveTo(unmanagedResource.Id, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceState.Complete);

            var resource = TestContext.Api.Resources.Read(unmanagedResource.Id);
            Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);

            var coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);
            Assert.IsNotNull(coreResource);

            var coreResourceCapacity = coreResource.Capacities.SingleOrDefault(x => x.CapacityProfileID == capacity.Id);
            Assert.IsNotNull(coreResourceCapacity);
            Assert.AreEqual(10m, coreResourceCapacity.Value.MinDecimalQuantity);
            Assert.AreEqual(20m, coreResourceCapacity.Value.MaxDecimalQuantity);
        }

        [TestMethod]
        public void AssignWithEmptyIdThrowsException()
        {
            var prefix = Guid.NewGuid();

            Assert.ThrowsException<ArgumentException>(() => new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceNumberCapacitySettings(Guid.Empty));
            Assert.ThrowsException<ArgumentException>(() => new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceNumberCapacitySettings(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity(Guid.Empty)));

            Assert.ThrowsException<ArgumentException>(() => new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceRangeCapacitySettings(Guid.Empty));
            Assert.ThrowsException<ArgumentException>(() => new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceRangeCapacitySettings(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.RangeCapacity(Guid.Empty)));
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
            var capacitySettings = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceNumberCapacitySettings(notExistingCapacityId)
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
                var resourceConfigurationError = ex.TraceData.ErrorData.OfType<ResourceConfigurationError>().SingleOrDefault();
                Assert.IsNotNull(resourceConfigurationError);

                var invalidResourceCapacitySettingsError = resourceConfigurationError as ResourceConfigurationInvalidCapacitySettingsError;
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
            var capacitySettings = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceRangeCapacitySettings(notExistingCapacityId)
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
                var resourceConfigurationError = ex.TraceData.ErrorData.OfType<ResourceConfigurationError>().SingleOrDefault();
                Assert.IsNotNull(resourceConfigurationError);

                var invalidResourceCapacitySettingsError = resourceConfigurationError as ResourceConfigurationInvalidCapacitySettingsError;
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
                rangeMin = 10,
                rangeMax = 20,
            };
            objectCreator.CreateCapacity(capacity1);

            var capacity2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity()
            {
                Name = $"{prefix}_Capacity2",
                rangeMin = 10,
                rangeMax = 20,
            };
            objectCreator.CreateCapacity(capacity2);

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };

            var capacitySettings1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceNumberCapacitySettings(capacity1.Id)
            {
                Value = 5, // below min
            };
            unmanagedResource.AddCapacity(capacitySettings1);

            var capacitySettings2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceNumberCapacitySettings(capacity2.Id)
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
                var resourceConfigurationErrors = ex.TraceData.ErrorData.OfType<ResourceConfigurationError>();
                Assert.AreEqual(2, resourceConfigurationErrors.Count());

                var invalidResourceCapacitySettingsErrors = resourceConfigurationErrors.OfType<ResourceConfigurationInvalidCapacitySettingsError>();
                Assert.AreEqual(2, invalidResourceCapacitySettingsErrors.Count());

                var error1 = invalidResourceCapacitySettingsErrors.SingleOrDefault(e => e.CapacityId == capacity1.Id);
                Assert.IsNotNull(error1);
                Assert.AreEqual("Value '5' must be greater than or equal to '10'.", error1.ErrorMessage);

                var error2 = invalidResourceCapacitySettingsErrors.SingleOrDefault(e => e.CapacityId == capacity2.Id);
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
                rangeMin = 10,
                rangeMax = 20,
                decimals = 3,
            };
            objectCreator.CreateCapacity(capacity);

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };

            var capacitySettings = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceNumberCapacitySettings(capacity.Id)
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
                var resourceConfigurationError = ex.TraceData.ErrorData.OfType<ResourceConfigurationError>().SingleOrDefault();
                Assert.IsNotNull(resourceConfigurationError);

                var invalidResourceCapacitySettingsError = resourceConfigurationError as ResourceConfigurationInvalidCapacitySettingsError;
                Assert.IsNotNull(invalidResourceCapacitySettingsError);
                Assert.AreEqual(errorMessage, invalidResourceCapacitySettingsError.ErrorMessage);
                Assert.AreEqual(capacity.Id, invalidResourceCapacitySettingsError.CapacityId);

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
                rangeMin = 10,
                stepSize = 3,
            };
            objectCreator.CreateCapacity(capacity1);

            var capacity2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity()
            {
                Name = $"{prefix}_Capacity 2",
                rangeMax = 20,
                stepSize = 3,
            };
            objectCreator.CreateCapacity(capacity2);

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };

            var capacitySettings1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceNumberCapacitySettings(capacity1.Id)
            {
                Value = 15,
            };
            unmanagedResource.AddCapacity(capacitySettings1);

            var capacitySettings2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceNumberCapacitySettings(capacity2.Id)
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
                var resourceConfigurationErrors = ex.TraceData.ErrorData.OfType<ResourceConfigurationError>();
                Assert.AreEqual(2, resourceConfigurationErrors.Count());

                var invalidResourceCapacitySettingsErrors = resourceConfigurationErrors.OfType<ResourceConfigurationInvalidCapacitySettingsError>();
                Assert.AreEqual(2, invalidResourceCapacitySettingsErrors.Count());

                var error1 = invalidResourceCapacitySettingsErrors.SingleOrDefault(e => e.CapacityId == capacity1.Id);
                Assert.IsNotNull(error1);
                Assert.AreEqual("Value '15' must align with the step size of '3'.", error1.ErrorMessage);

                var error2 = invalidResourceCapacitySettingsErrors.SingleOrDefault(e => e.CapacityId == capacity2.Id);
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
                rangeMin = 10,
                rangeMax = 20,
            };
            objectCreator.CreateCapacity(capacity);

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };

            var capacitySettings = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceRangeCapacitySettings(capacity.Id)
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
                var resourceConfigurationError = ex.TraceData.ErrorData.OfType<ResourceConfigurationError>().SingleOrDefault();
                Assert.IsNotNull(resourceConfigurationError);

                var invalidResourceCapacitySettingsError = resourceConfigurationError as ResourceConfigurationInvalidCapacitySettingsError;
                Assert.IsNotNull(invalidResourceCapacitySettingsError);
                Assert.AreEqual(errorMessage, invalidResourceCapacitySettingsError.ErrorMessage);
                Assert.AreEqual(capacity.Id, invalidResourceCapacitySettingsError.CapacityId);

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }
    }
}
