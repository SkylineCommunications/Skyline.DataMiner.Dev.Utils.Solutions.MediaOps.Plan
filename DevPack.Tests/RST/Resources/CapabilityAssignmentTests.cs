namespace RT_MediaOps.Plan.RST.Resources
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using RT_MediaOps.Plan.RegressionTests;

    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

    using Storage = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public sealed class CapabilityAssignmentTests : IDisposable
    {
        private readonly ResourceStudioObjectCreator objectCreator;

        public CapabilityAssignmentTests()
        {
            objectCreator = new ResourceStudioObjectCreator(TestContext.Api);
        }

        private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

        public void Dispose()
        {
            objectCreator.Dispose();
        }

        [TestMethod]
        public void DraftResourceCRUD()
        {
            var prefix = Guid.NewGuid();

            var capability1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
            {
                Name = $"{prefix}_Capability 1",
            };
            capability1.SetDiscretes(new[] { "Value 1", "Value 2", "Value 3" });
            objectCreator.CreateCapability(capability1);

            var capability2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
            {
                Name = $"{prefix}_Capability 2",
            };
            capability2.SetDiscretes(new[] { "Value 1", "Value 2", "Value 3" });
            objectCreator.CreateCapability(capability2);

            var capabilitySettings1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceCapabilitySettings(capability1.Id);
            capabilitySettings1.SetDiscretes(new[] { "Value 1", "Value 2" });

            var capabilitySettings2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceCapabilitySettings(capability2.Id);
            capabilitySettings2.SetDiscretes(new[] { "Value 2", "Value 3" });

            // Create Resource with one capability assigned
            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };
            unmanagedResource.AddCapability(capabilitySettings1);
            objectCreator.CreateResource(unmanagedResource);

            var resource = TestContext.Api.Resources.Read(unmanagedResource.Id);
            Assert.AreEqual(1, resource.Capabilities.Count);
            var resourceCapbility = resource.Capabilities.Single();
            Assert.AreEqual(capability1.Id, resourceCapbility.Id);
            Assert.AreEqual(2, resourceCapbility.Discretes.Count);
            Assert.IsTrue(resourceCapbility.Discretes.Contains("Value 1"));
            Assert.IsTrue(resourceCapbility.Discretes.Contains("Value 2"));

            // Update resource capability discretes
            capabilitySettings1 = resource.Capabilities.First(x => x.Id == capability1.Id);
            capabilitySettings1.RemoveDiscrete("Value 1");
            TestContext.Api.Resources.Update(resource);

            resource = TestContext.Api.Resources.Read(unmanagedResource.Id);
            Assert.AreEqual(1, resource.Capabilities.Count);
            resourceCapbility = resource.Capabilities.Single();
            Assert.AreEqual(capability1.Id, resourceCapbility.Id);
            Assert.AreEqual(1, resourceCapbility.Discretes.Count);
            Assert.IsTrue(resourceCapbility.Discretes.Contains("Value 2"));

            // Update Resource to add second capability
            resource.AddCapability(capabilitySettings2);
            TestContext.Api.Resources.Update(resource);

            resource = TestContext.Api.Resources.Read(unmanagedResource.Id);
            Assert.AreEqual(2, resource.Capabilities.Count);
            var expectedCapabilityData = new Dictionary<Guid, List<string>>()
            {
                { capability1.Id, new List<string> { "Value 2" } },
                { capability2.Id, new List<string> { "Value 2", "Value 3" } },
            };
            foreach (var capability in resource.Capabilities)
            {
                Assert.IsTrue(expectedCapabilityData.ContainsKey(capability.Id));

                var expectedDiscretes = expectedCapabilityData[capability.Id];
                Assert.AreEqual(expectedDiscretes.Count, capability.Discretes.Count);
                foreach (var discrete in expectedDiscretes)
                {
                    Assert.IsTrue(capability.Discretes.Contains(discrete));
                }
            }

            // Update Resource to remove first capability
            var capabilitySettingToRemove = resource.Capabilities.First(c => c.Id == capability1.Id);
            resource.RemoveCapability(capabilitySettingToRemove);
            TestContext.Api.Resources.Update(resource);

            resource = TestContext.Api.Resources.Read(unmanagedResource.Id);
            Assert.AreEqual(1, resource.Capabilities.Count);
            resourceCapbility = resource.Capabilities.Single();
            Assert.AreEqual(capability2.Id, resourceCapbility.Id);
            Assert.AreEqual(2, resourceCapbility.Discretes.Count);
            Assert.IsTrue(resourceCapbility.Discretes.Contains("Value 2"));
            Assert.IsTrue(resourceCapbility.Discretes.Contains("Value 3"));
        }

        [TestMethod]
        public void CompleteResourceCRUD()
        {
            var prefix = Guid.NewGuid();

            var capability1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
            {
                Name = $"{prefix}_Capability 1",
            };
            capability1.SetDiscretes(new[] { "Value 1", "Value 2", "Value 3" });
            objectCreator.CreateCapability(capability1);

            var capability2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
            {
                Name = $"{prefix}_Capability 2",
            };
            capability2.SetDiscretes(new[] { "Value 1", "Value 2", "Value 3" });
            objectCreator.CreateCapability(capability2);

            var capabilitySettings1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceCapabilitySettings(capability1.Id);
            capabilitySettings1.SetDiscretes(new[] { "Value 1", "Value 2" });

            var capabilitySettings2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceCapabilitySettings(capability2.Id);
            capabilitySettings2.SetDiscretes(new[] { "Value 2", "Value 3" });

            // Create Resource with one capability assigned
            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };
            unmanagedResource.AddCapability(capabilitySettings1);
            objectCreator.CreateResource(unmanagedResource);

            // Move Resource to Completed state
            TestContext.Api.Resources.MoveTo(unmanagedResource.Id, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceState.Complete);

            var resource = TestContext.Api.Resources.Read(unmanagedResource.Id);
            Assert.AreEqual(1, resource.Capabilities.Count);
            var resourceCapbility = resource.Capabilities.Single();
            Assert.AreEqual(capability1.Id, resourceCapbility.Id);
            Assert.AreEqual(2, resourceCapbility.Discretes.Count);
            Assert.IsTrue(resourceCapbility.Discretes.Contains("Value 1"));
            Assert.IsTrue(resourceCapbility.Discretes.Contains("Value 2"));

            Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);
            var coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);

            // Expected capabilities + 1 > RST_ResourceType
            Assert.AreEqual(2, coreResource.Capabilities.Count);

            var resourceCapability = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == capability1.Id);
            Assert.IsNotNull(resourceCapability);
            Assert.AreEqual(2, resourceCapability.Value.Discreets.Count);
            Assert.IsTrue(resourceCapability.Value.Discreets.Contains("Value 1"));
            Assert.IsTrue(resourceCapability.Value.Discreets.Contains("Value 2"));

            // Update capability discretes
            capabilitySettings1 = resource.Capabilities.First(x => x.Id == capability1.Id);
            capabilitySettings1.RemoveDiscrete("Value 1");
            TestContext.Api.Resources.Update(resource);

            resource = TestContext.Api.Resources.Read(unmanagedResource.Id);
            Assert.AreEqual(1, resource.Capabilities.Count);
            resourceCapbility = resource.Capabilities.Single();
            Assert.AreEqual(capability1.Id, resourceCapbility.Id);
            Assert.AreEqual(1, resourceCapbility.Discretes.Count);
            Assert.IsTrue(resourceCapbility.Discretes.Contains("Value 2"));

            Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);
            coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);

            // Expected capabilities + 1 > RST_ResourceType
            Assert.AreEqual(2, coreResource.Capabilities.Count);

            resourceCapability = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == capability1.Id);
            Assert.IsNotNull(resourceCapability);
            Assert.AreEqual(1, resourceCapability.Value.Discreets.Count);
            Assert.IsTrue(resourceCapability.Value.Discreets.Contains("Value 2"));

            // Update Resource to add second capability
            resource.AddCapability(capabilitySettings2);
            TestContext.Api.Resources.Update(resource);

            resource = TestContext.Api.Resources.Read(unmanagedResource.Id);
            Assert.AreEqual(2, resource.Capabilities.Count);
            var expectedCapabilityData = new Dictionary<Guid, List<string>>()
            {
                { capability1.Id, new List<string> { "Value 2" } },
                { capability2.Id, new List<string> { "Value 2", "Value 3" } },
            };
            foreach (var capability in resource.Capabilities)
            {
                Assert.IsTrue(expectedCapabilityData.ContainsKey(capability.Id));

                var expectedDiscretes = expectedCapabilityData[capability.Id];
                Assert.AreEqual(expectedDiscretes.Count, capability.Discretes.Count);
                foreach (var discrete in expectedDiscretes)
                {
                    Assert.IsTrue(capability.Discretes.Contains(discrete));
                }
            }

            Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);
            coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);

            // Expected capabilities + 1 > RST_ResourceType
            Assert.AreEqual(3, coreResource.Capabilities.Count);

            foreach (var capability in coreResource.Capabilities)
            {
                if (capability.CapabilityProfileID == Skyline.DataMiner.Solutions.MediaOps.Plan.API.CoreCapabilities.ResourceType.Id)
                {
                    continue;
                }

                Assert.IsTrue(expectedCapabilityData.ContainsKey(capability.CapabilityProfileID));

                var expectedDiscretes = expectedCapabilityData[capability.CapabilityProfileID];
                Assert.AreEqual(expectedDiscretes.Count, capability.Value.Discreets.Count);
                foreach (var discrete in expectedDiscretes)
                {
                    Assert.IsTrue(capability.Value.Discreets.Contains(discrete));
                }
            }

            // Update Resource to remove first capability
            var capabilitySettingToRemove = resource.Capabilities.First(c => c.Id == capability1.Id);
            resource.RemoveCapability(capabilitySettingToRemove);
            TestContext.Api.Resources.Update(resource);

            resource = TestContext.Api.Resources.Read(unmanagedResource.Id);
            Assert.AreEqual(1, resource.Capabilities.Count);
            resourceCapbility = resource.Capabilities.Single();
            Assert.AreEqual(capability2.Id, resourceCapbility.Id);
            Assert.AreEqual(2, resourceCapbility.Discretes.Count);
            Assert.IsTrue(resourceCapbility.Discretes.Contains("Value 2"));
            Assert.IsTrue(resourceCapbility.Discretes.Contains("Value 3"));

            Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);
            coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);

            // Expected capabilities + 1 > RST_ResourceType
            Assert.AreEqual(2, coreResource.Capabilities.Count);

            resourceCapability = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == capability2.Id);
            Assert.IsNotNull(resourceCapability);
            Assert.AreEqual(2, resourceCapability.Value.Discreets.Count);
            Assert.IsTrue(resourceCapability.Value.Discreets.Contains("Value 2"));
            Assert.IsTrue(resourceCapability.Value.Discreets.Contains("Value 3"));
        }

        [TestMethod]
        public void TimeDependentCapabilityCRUD()
        {
            var prefix = Guid.NewGuid();

            var timeCapability1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
            {
                Name = $"{prefix}_Time Capability 1",
                IsTimeDependent = true,
            };
            timeCapability1.SetDiscretes(new[] { "Value 1", "Value 2", "Value 3" });
            objectCreator.CreateCapability(timeCapability1);

            var timeCapability2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
            {
                Name = $"{prefix}_Time Capability 2",
                IsTimeDependent = true,
            };
            timeCapability2.SetDiscretes(new[] { "Value 1", "Value 2", "Value 3" });
            objectCreator.CreateCapability(timeCapability2);

            var regularCapability = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
            {
                Name = $"{prefix}_Capability"
            };
            regularCapability.SetDiscretes(new[] { "Value 1", "Value 2", "Value 3" });
            objectCreator.CreateCapability(regularCapability);

            var timeCapabilitySettings1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceCapabilitySettings(timeCapability1.Id);
            timeCapabilitySettings1.SetDiscretes(new[] { "Value 1", "Value 2" });

            var timeCapabilitySettings2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceCapabilitySettings(timeCapability2.Id);
            timeCapabilitySettings2.SetDiscretes(new[] { "Value 2", "Value 3" });

            var regularCapabilitySettings = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceCapabilitySettings(regularCapability.Id);
            regularCapabilitySettings.SetDiscretes(new[] { "Value 1", "Value 3" });

            // Create Resource with one time capability assigned
            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };
            unmanagedResource.AddCapability(timeCapabilitySettings1);
            objectCreator.CreateResource(unmanagedResource);

            // Move Resource to Completed state
            TestContext.Api.Resources.MoveTo(unmanagedResource.Id, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceState.Complete);

            var resource = TestContext.Api.Resources.Read(unmanagedResource.Id);
            Assert.AreEqual(1, resource.Capabilities.Count);

            var capabilities = TestContext.Api.Capabilities.Read([timeCapability1.Id, timeCapability2.Id, regularCapability.Id]);
            timeCapability1 = capabilities.Single(c => c.Id == timeCapability1.Id);
            timeCapability2 = capabilities.Single(c => c.Id == timeCapability2.Id);
            regularCapability = capabilities.Single(c => c.Id == regularCapability.Id);

            Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);
            var coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);

            // Expected capabilities + 1 > RST_ResourceType
            Assert.AreEqual(3, coreResource.Capabilities.Count);

            var resourceCapability1 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == timeCapability1.Id);
            Assert.IsNotNull(resourceCapability1);
            Assert.AreEqual(2, resourceCapability1.Value.Discreets.Count);
            Assert.IsTrue(resourceCapability1.Value.Discreets.Contains("Value 1"));
            Assert.IsTrue(resourceCapability1.Value.Discreets.Contains("Value 2"));

            var resourceTimeDependentCapability1 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == timeCapability1.LinkedTimeDependentCapabilityId);
            Assert.IsNotNull(resourceTimeDependentCapability1);
            Assert.IsTrue(resourceTimeDependentCapability1.IsTimeDynamic);

            // Update Resource to add second time capability
            resource.AddCapability(timeCapabilitySettings2);
            TestContext.Api.Resources.Update(resource);

            resource = TestContext.Api.Resources.Read(unmanagedResource.Id);
            Assert.AreEqual(2, resource.Capabilities.Count);

            Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);
            coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);

            // Expected capabilities + 1 > RST_ResourceType
            Assert.AreEqual(5, coreResource.Capabilities.Count);

            resourceCapability1 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == timeCapability1.Id);
            Assert.IsNotNull(resourceCapability1);
            Assert.AreEqual(2, resourceCapability1.Value.Discreets.Count);
            Assert.IsTrue(resourceCapability1.Value.Discreets.Contains("Value 1"));
            Assert.IsTrue(resourceCapability1.Value.Discreets.Contains("Value 2"));

            resourceTimeDependentCapability1 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == timeCapability1.LinkedTimeDependentCapabilityId);
            Assert.IsNotNull(resourceTimeDependentCapability1);
            Assert.IsTrue(resourceTimeDependentCapability1.IsTimeDynamic);

            var resourceCapability2 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == timeCapability2.Id);
            Assert.IsNotNull(resourceCapability2);
            Assert.AreEqual(2, resourceCapability2.Value.Discreets.Count);
            Assert.IsTrue(resourceCapability2.Value.Discreets.Contains("Value 2"));
            Assert.IsTrue(resourceCapability2.Value.Discreets.Contains("Value 3"));

            var resourceTimeDependentCapability2 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == timeCapability2.LinkedTimeDependentCapabilityId);
            Assert.IsNotNull(resourceTimeDependentCapability2);
            Assert.IsTrue(resourceTimeDependentCapability2.IsTimeDynamic);

            // Update Resource to add regular capability
            resource.AddCapability(regularCapabilitySettings);
            TestContext.Api.Resources.Update(resource);

            resource = TestContext.Api.Resources.Read(unmanagedResource.Id);
            Assert.AreEqual(3, resource.Capabilities.Count);

            Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);
            coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);

            // Expected capabilities + 1 > RST_ResourceType
            Assert.AreEqual(6, coreResource.Capabilities.Count);

            resourceCapability1 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == timeCapability1.Id);
            Assert.IsNotNull(resourceCapability1);
            Assert.AreEqual(2, resourceCapability1.Value.Discreets.Count);
            Assert.IsTrue(resourceCapability1.Value.Discreets.Contains("Value 1"));
            Assert.IsTrue(resourceCapability1.Value.Discreets.Contains("Value 2"));

            resourceTimeDependentCapability1 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == timeCapability1.LinkedTimeDependentCapabilityId);
            Assert.IsNotNull(resourceTimeDependentCapability1);
            Assert.IsTrue(resourceTimeDependentCapability1.IsTimeDynamic);

            resourceCapability2 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == timeCapability2.Id);
            Assert.IsNotNull(resourceCapability2);
            Assert.AreEqual(2, resourceCapability2.Value.Discreets.Count);
            Assert.IsTrue(resourceCapability2.Value.Discreets.Contains("Value 2"));
            Assert.IsTrue(resourceCapability2.Value.Discreets.Contains("Value 3"));

            resourceTimeDependentCapability2 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == timeCapability2.LinkedTimeDependentCapabilityId);
            Assert.IsNotNull(resourceTimeDependentCapability2);
            Assert.IsTrue(resourceTimeDependentCapability2.IsTimeDynamic);

            var resourceCapability3 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == regularCapability.Id);
            Assert.IsNotNull(resourceCapability3);
            Assert.AreEqual(2, resourceCapability3.Value.Discreets.Count);
            Assert.IsTrue(resourceCapability3.Value.Discreets.Contains("Value 1"));
            Assert.IsTrue(resourceCapability3.Value.Discreets.Contains("Value 3"));

            // Update Resource to remove first capability
            var capabilitySettingToRemove = resource.Capabilities.First(c => c.Id == timeCapability1.Id);
            resource.RemoveCapability(capabilitySettingToRemove);
            TestContext.Api.Resources.Update(resource);

            resource = TestContext.Api.Resources.Read(unmanagedResource.Id);
            Assert.AreEqual(2, resource.Capabilities.Count);

            Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);
            coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);

            // Expected capabilities + 1 > RST_ResourceType
            Assert.AreEqual(4, coreResource.Capabilities.Count);

            resourceCapability2 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == timeCapability2.Id);
            Assert.IsNotNull(resourceCapability2);
            Assert.AreEqual(2, resourceCapability2.Value.Discreets.Count);
            Assert.IsTrue(resourceCapability2.Value.Discreets.Contains("Value 2"));
            Assert.IsTrue(resourceCapability2.Value.Discreets.Contains("Value 3"));

            resourceTimeDependentCapability2 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == timeCapability2.LinkedTimeDependentCapabilityId);
            Assert.IsNotNull(resourceTimeDependentCapability2);
            Assert.IsTrue(resourceTimeDependentCapability2.IsTimeDynamic);

            resourceCapability3 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == regularCapability.Id);
            Assert.IsNotNull(resourceCapability3);
            Assert.AreEqual(2, resourceCapability3.Value.Discreets.Count);
            Assert.IsTrue(resourceCapability3.Value.Discreets.Contains("Value 1"));
            Assert.IsTrue(resourceCapability3.Value.Discreets.Contains("Value 3"));
        }

        [TestMethod]
        public void Dom_SingleResourceCapability()
        {
            var prefix = Guid.NewGuid();

            var capability = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
            {
                Name = $"{prefix}_Capability ",
            };
            capability.SetDiscretes(new[] { "Value 1", "Value 2", "Value 3" });
            objectCreator.CreateCapability(capability);

            var capabilitySettings = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceCapabilitySettings(capability.Id);
            capabilitySettings.SetDiscretes(new[] { "Value 1", "Value 2" });

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };
            unmanagedResource.AddCapability(capabilitySettings);
            objectCreator.CreateResource(unmanagedResource);

            var domResource = TestContext.ResourceStudioDomHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(unmanagedResource.Id)).SingleOrDefault();
            Assert.IsTrue(domResource.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourceCapabilities.Id.Id));

            var domResourceCapabilitiesSections = domResource.Sections.Where(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourceCapabilities.Id.Id).ToList();
            Assert.AreEqual(1, domResourceCapabilitiesSections.Count);

            var domResourceCapability = domResourceCapabilitiesSections.Single();
            var fdProfileParameterId = domResourceCapability.FieldValues.SingleOrDefault(f => f.FieldDescriptorID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourceCapabilities.ProfileParameterID.Id);
            Assert.IsNotNull(fdProfileParameterId);
            Assert.IsTrue(Guid.TryParse(Convert.ToString(fdProfileParameterId.Value.Value), out var profileParameterId));
            Assert.AreEqual(capability.Id, profileParameterId);

            var fdStringValue = domResourceCapability.FieldValues.SingleOrDefault(f => f.FieldDescriptorID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourceCapabilities.StringValue.Id);
            Assert.IsNotNull(fdStringValue);
            var discreteValues = Convert.ToString(fdStringValue.Value.Value).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            Assert.AreEqual(2, discreteValues.Count);
            Assert.IsTrue(discreteValues.Contains("Value 1"));
            Assert.IsTrue(discreteValues.Contains("Value 2"));
        }

        [TestMethod]
        public void AssignWithEmptyIdThrowsException()
        {
            var prefix = Guid.NewGuid();

            Assert.ThrowsException<ArgumentException>(() => new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceCapabilitySettings(Guid.Empty));
            Assert.ThrowsException<ArgumentException>(() => new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceCapabilitySettings(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability(Guid.Empty)));
        }

        [TestMethod]
        public void AssignNotExistingCapabilityThrowsException()
        {
            var prefix = Guid.NewGuid();

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };

            var notExistingCapabilityId = Guid.NewGuid();
            var capabilitySettings = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceCapabilitySettings(notExistingCapabilityId);
            capabilitySettings.SetDiscretes(new[] { "Value 1", "Value 2" });
            unmanagedResource.AddCapability(capabilitySettings);

            try
            {
                objectCreator.CreateResource(unmanagedResource);
            }
            catch (MediaOpsException ex)
            {
                var errorMessage = $"Capability with ID '{notExistingCapabilityId}' not found.";
                Assert.AreEqual(errorMessage, ex.Message);

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var resourceConfigurationError = ex.TraceData.ErrorData.OfType<ResourceError>().SingleOrDefault();
                Assert.IsNotNull(resourceConfigurationError);

                var invalidResourceCapabilitySettingsError = resourceConfigurationError as ResourceInvalidCapabilitySettingsError;
                Assert.IsNotNull(invalidResourceCapabilitySettingsError);
                Assert.AreEqual(errorMessage, invalidResourceCapabilitySettingsError.ErrorMessage);
                Assert.AreEqual(notExistingCapabilityId, invalidResourceCapabilitySettingsError.CapabilityId);

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void AssignWithNoDiscretesThrowsException()
        {
            var prefix = Guid.NewGuid();

            var capability = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
            {
                Name = $"{prefix}_Capability",
            };
            capability.SetDiscretes(new[] { "Value 1", "Value 2", "Value 3" });
            objectCreator.CreateCapability(capability);

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };

            var capabilitySettings = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceCapabilitySettings(capability.Id);
            unmanagedResource.AddCapability(capabilitySettings);

            try
            {
                objectCreator.CreateResource(unmanagedResource);
            }
            catch (MediaOpsException ex)
            {
                var errorMessage = "At least one discrete value must be specified for the capability.";
                Assert.AreEqual(errorMessage, ex.Message);

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var resourceConfigurationError = ex.TraceData.ErrorData.OfType<ResourceError>().SingleOrDefault();
                Assert.IsNotNull(resourceConfigurationError);

                var invalidResourceCapabilitySettingsError = resourceConfigurationError as ResourceInvalidCapabilitySettingsError;
                Assert.IsNotNull(invalidResourceCapabilitySettingsError);
                Assert.AreEqual(errorMessage, invalidResourceCapabilitySettingsError.ErrorMessage);
                Assert.AreEqual(capability.Id, invalidResourceCapabilitySettingsError.CapabilityId);

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void AssignNotExistingDiscreteThrowsException()
        {
            var prefix = Guid.NewGuid();

            var capability = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
            {
                Name = $"{prefix}_Capability",
            };
            capability.SetDiscretes(new[] { "Value 1", "Value 2", "Value 3" });
            objectCreator.CreateCapability(capability);

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };

            var capabilitySettings = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceCapabilitySettings(capability.Id);
            capabilitySettings.SetDiscretes(new[] { "Value 2", "Value 5" });
            unmanagedResource.AddCapability(capabilitySettings);

            try
            {
                objectCreator.CreateResource(unmanagedResource);
            }
            catch (MediaOpsException ex)
            {
                var errorMessage = $"Discrete value 'Value 5' is not valid for capability '{capability.Name}'.";
                Assert.AreEqual(errorMessage, ex.Message);

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var resourceConfigurationError = ex.TraceData.ErrorData.OfType<ResourceError>().SingleOrDefault();
                Assert.IsNotNull(resourceConfigurationError);

                var invalidResourceCapabilitySettingsError = resourceConfigurationError as ResourceInvalidCapabilitySettingsError;
                Assert.IsNotNull(invalidResourceCapabilitySettingsError);
                Assert.AreEqual(errorMessage, invalidResourceCapabilitySettingsError.ErrorMessage);
                Assert.AreEqual(capability.Id, invalidResourceCapabilitySettingsError.CapabilityId);

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }
    }
}
