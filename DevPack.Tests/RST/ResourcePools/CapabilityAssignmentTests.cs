namespace RT_MediaOps.Plan.RST.ResourcePools
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
        private readonly TestObjectCreator objectCreator;

        public CapabilityAssignmentTests()
        {
            objectCreator = new TestObjectCreator(TestContext);
        }

        private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

        public void Dispose()
        {
            objectCreator.Dispose();
        }

        [TestMethod]
        public void DraftPoolCRUD()
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

            var capabilitySettings1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySettings(capability1.Id);
            capabilitySettings1.SetDiscretes(new[] { "Value 1", "Value 2" });

            var capabilitySettings2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySettings(capability2.Id);
            capabilitySettings2.SetDiscretes(new[] { "Value 2", "Value 3" });

            // Create Resource Pool with one capability assigned
            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_Resource Pool",
            };
            resourcePool.AddCapability(capabilitySettings1);
            objectCreator.CreateResourcePool(resourcePool);

            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);
            Assert.AreEqual(1, resourcePool.Capabilities.Count);
            var resourceCapbility = resourcePool.Capabilities.Single();
            Assert.AreEqual(capability1.Id, resourceCapbility.Id);
            Assert.AreEqual(2, resourceCapbility.Discretes.Count);
            Assert.IsTrue(resourceCapbility.Discretes.Contains("Value 1"));
            Assert.IsTrue(resourceCapbility.Discretes.Contains("Value 2"));

            // Update pool capability discretes
            capabilitySettings1 = resourcePool.Capabilities.First(x => x.Id == capability1.Id);
            capabilitySettings1.RemoveDiscrete("Value 1");
            TestContext.Api.ResourcePools.Update(resourcePool);

            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);
            Assert.AreEqual(1, resourcePool.Capabilities.Count);
            resourceCapbility = resourcePool.Capabilities.Single();
            Assert.AreEqual(capability1.Id, resourceCapbility.Id);
            Assert.AreEqual(1, resourceCapbility.Discretes.Count);
            Assert.IsTrue(resourceCapbility.Discretes.Contains("Value 2"));

            // Update Resource Pool to add second capability
            resourcePool.AddCapability(capabilitySettings2);
            TestContext.Api.ResourcePools.Update(resourcePool);

            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);
            Assert.AreEqual(2, resourcePool.Capabilities.Count);
            var expectedCapabilityData = new Dictionary<Guid, List<string>>()
            {
                { capability1.Id, new List<string> { "Value 2" } },
                { capability2.Id, new List<string> { "Value 2", "Value 3" } },
            };
            foreach (var capability in resourcePool.Capabilities)
            {
                Assert.IsTrue(expectedCapabilityData.ContainsKey(capability.Id));

                var expectedDiscretes = expectedCapabilityData[capability.Id];
                Assert.AreEqual(expectedDiscretes.Count, capability.Discretes.Count);
                foreach (var discrete in expectedDiscretes)
                {
                    Assert.IsTrue(capability.Discretes.Contains(discrete));
                }
            }

            // Update Resource Pool to remove first capability
            var capabilitySettingToRemove = resourcePool.Capabilities.First(c => c.Id == capability1.Id);
            resourcePool.RemoveCapability(capabilitySettingToRemove);
            TestContext.Api.ResourcePools.Update(resourcePool);

            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);
            Assert.AreEqual(1, resourcePool.Capabilities.Count);
            resourceCapbility = resourcePool.Capabilities.Single();
            Assert.AreEqual(capability2.Id, resourceCapbility.Id);
            Assert.AreEqual(2, resourceCapbility.Discretes.Count);
            Assert.IsTrue(resourceCapbility.Discretes.Contains("Value 2"));
            Assert.IsTrue(resourceCapbility.Discretes.Contains("Value 3"));
        }

        [TestMethod]
        public void CompletedPoolCRUD()
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

            var capabilitySettings1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySettings(capability1.Id);
            capabilitySettings1.SetDiscretes(new[] { "Value 1", "Value 2" });

            var capabilitySettings2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySettings(capability2.Id);
            capabilitySettings2.SetDiscretes(new[] { "Value 2", "Value 3" });

            // Create Resource Pool with one capability assigned
            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_Resource Pool",
            };
            resourcePool.AddCapability(capabilitySettings1);
            objectCreator.CreateResourcePool(resourcePool);

            // Move Resource Pool to Completed state
            TestContext.Api.ResourcePools.Complete(resourcePool.Id);

            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);
            Assert.AreEqual(1, resourcePool.Capabilities.Count);
            var resourceCapbility = resourcePool.Capabilities.Single();
            Assert.AreEqual(capability1.Id, resourceCapbility.Id);
            Assert.AreEqual(2, resourceCapbility.Discretes.Count);
            Assert.IsTrue(resourceCapbility.Discretes.Contains("Value 1"));
            Assert.IsTrue(resourceCapbility.Discretes.Contains("Value 2"));

            // Update pool capability discretes
            capabilitySettings1 = resourcePool.Capabilities.First(x => x.Id == capability1.Id);
            capabilitySettings1.RemoveDiscrete("Value 1");
            TestContext.Api.ResourcePools.Update(resourcePool);

            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);
            Assert.AreEqual(1, resourcePool.Capabilities.Count);
            resourceCapbility = resourcePool.Capabilities.Single();
            Assert.AreEqual(capability1.Id, resourceCapbility.Id);
            Assert.AreEqual(1, resourceCapbility.Discretes.Count);
            Assert.IsTrue(resourceCapbility.Discretes.Contains("Value 2"));

            // Update Resource Pool to add second capability
            resourcePool.AddCapability(capabilitySettings2);
            TestContext.Api.ResourcePools.Update(resourcePool);

            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);
            Assert.AreEqual(2, resourcePool.Capabilities.Count);
            var expectedCapabilityData = new Dictionary<Guid, List<string>>()
            {
                { capability1.Id, new List<string> { "Value 2" } },
                { capability2.Id, new List<string> { "Value 2", "Value 3" } },
            };
            foreach (var capability in resourcePool.Capabilities)
            {
                Assert.IsTrue(expectedCapabilityData.ContainsKey(capability.Id));

                var expectedDiscretes = expectedCapabilityData[capability.Id];
                Assert.AreEqual(expectedDiscretes.Count, capability.Discretes.Count);
                foreach (var discrete in expectedDiscretes)
                {
                    Assert.IsTrue(capability.Discretes.Contains(discrete));
                }
            }

            // Update Resource Pool to remove first capability
            var capabilitySettingToRemove = resourcePool.Capabilities.First(c => c.Id == capability1.Id);
            resourcePool.RemoveCapability(capabilitySettingToRemove);
            TestContext.Api.ResourcePools.Update(resourcePool);

            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);
            Assert.AreEqual(1, resourcePool.Capabilities.Count);
            resourceCapbility = resourcePool.Capabilities.Single();
            Assert.AreEqual(capability2.Id, resourceCapbility.Id);
            Assert.AreEqual(2, resourceCapbility.Discretes.Count);
            Assert.IsTrue(resourceCapbility.Discretes.Contains("Value 2"));
            Assert.IsTrue(resourceCapbility.Discretes.Contains("Value 3"));
        }

        [TestMethod]
        public void DraftPoolWithMixedResourcesCRUD()
        {
            var prefix = Guid.NewGuid();

            var capability = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
            {
                Name = $"{prefix}_Capability",
            };
            capability.SetDiscretes(new[] { "Value 1", "Value 2", "Value 3" });
            objectCreator.CreateCapability(capability);

            var capabilitySettings = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySettings(capability.Id);
            capabilitySettings.SetDiscretes(new[] { "Value 2", "Value 3" });

            var unmanagedResource1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource1",
            };
            var unmanagedResource2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource2",
            };
            var unmanagedResource3 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource3",
            };
            objectCreator.CreateResources([unmanagedResource1, unmanagedResource2, unmanagedResource3]);
            TestContext.Api.Resources.Complete(unmanagedResource1.Id);
            TestContext.Api.Resources.Complete(unmanagedResource3.Id);

            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_Resource Pool",
            };
            objectCreator.CreateResourcePool(resourcePool);

            var resources = TestContext.Api.Resources.Read([unmanagedResource1.Id, unmanagedResource2.Id, unmanagedResource3.Id]).ToArray();
            TestContext.Api.ResourcePools.AssignResourcesToPool(resourcePool.Id, resources);

            // Assign capability to pool
            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);
            resourcePool.AddCapability(capabilitySettings);
            TestContext.Api.ResourcePools.Update(resourcePool);

            var resource1 = resources.Single(r => r.Id == unmanagedResource1.Id);
            var resource2 = resources.Single(r => r.Id == unmanagedResource2.Id);
            var resource3 = resources.Single(r => r.Id == unmanagedResource3.Id);

            Assert.AreEqual(Guid.Empty, resourcePool.CoreResourcePoolId);
            Assert.AreEqual(Guid.Empty, resource2.CoreResourceId);

            foreach (var resource in new[] { resource1, resource3 })
            {
                Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);
                var coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);
                var resourceCapability = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == capability.Id);
                Assert.IsNull(resourceCapability);
            }
        }

        [TestMethod]
        public void CompletedPoolWithMixedResourcesCRUD()
        {
            var prefix = Guid.NewGuid();

            var capability = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
            {
                Name = $"{prefix}_Capability",
            };
            capability.SetDiscretes(new[] { "Value 1", "Value 2", "Value 3" });
            objectCreator.CreateCapability(capability);

            var capabilitySettings = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySettings(capability.Id);
            capabilitySettings.SetDiscretes(new[] { "Value 2", "Value 3" });

            var unmanagedResource1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource1",
            };
            var unmanagedResource2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource2",
            };
            var unmanagedResource3 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource3",
            };
            objectCreator.CreateResources([unmanagedResource1, unmanagedResource2, unmanagedResource3]);
            TestContext.Api.Resources.Complete(unmanagedResource1.Id);
            TestContext.Api.Resources.Complete(unmanagedResource3.Id);

            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_Resource Pool",
            };
            objectCreator.CreateResourcePool(resourcePool);
            TestContext.Api.ResourcePools.Complete(resourcePool.Id);

            var resources = TestContext.Api.Resources.Read([unmanagedResource1.Id, unmanagedResource2.Id, unmanagedResource3.Id]).ToList();
            TestContext.Api.ResourcePools.AssignResourcesToPool(resourcePool.Id, resources);

            // Assign capability to pool
            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);
            resourcePool.AddCapability(capabilitySettings);
            TestContext.Api.ResourcePools.Update(resourcePool);

            var resource1 = resources.Single(r => r.Id == unmanagedResource1.Id);
            var resource2 = resources.Single(r => r.Id == unmanagedResource2.Id);
            var resource3 = resources.Single(r => r.Id == unmanagedResource3.Id);

            Assert.AreNotEqual(Guid.Empty, resourcePool.CoreResourcePoolId);
            Assert.AreEqual(Guid.Empty, resource2.CoreResourceId);

            foreach (var resource in new[] { resource1, resource3 })
            {
                Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);
                var coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);

                // Expected capabilities + 1 > RST_ResourceType
                Assert.AreEqual(2, coreResource.Capabilities.Count);

                var resourceCapability = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == capability.Id);
                Assert.IsNotNull(resourceCapability);
                Assert.AreEqual(2, resourceCapability.Value.Discreets.Count);
                Assert.IsTrue(resourceCapability.Value.Discreets.Contains("Value 2"));
                Assert.IsTrue(resourceCapability.Value.Discreets.Contains("Value 3"));
            }

            // Update pool capability discretes
            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);
            capabilitySettings = resourcePool.Capabilities.First(x => x.Id == capability.Id);
            capabilitySettings.RemoveDiscrete("Value 3");
            TestContext.Api.ResourcePools.Update(resourcePool);

            foreach (var resource in new[] { resource1, resource3 })
            {
                Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);
                var coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);

                // Expected capabilities + 1 > RST_ResourceType
                Assert.AreEqual(2, coreResource.Capabilities.Count);

                var resourceCapability = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == capability.Id);
                Assert.IsNotNull(resourceCapability);
                Assert.AreEqual(1, resourceCapability.Value.Discreets.Count);
                Assert.IsTrue(resourceCapability.Value.Discreets.Contains("Value 2"));
            }

            // Remove capability from pool
            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);
            capabilitySettings = resourcePool.Capabilities.First(x => x.Id == capability.Id);
            resourcePool.RemoveCapability(capabilitySettings);
            TestContext.Api.ResourcePools.Update(resourcePool);

            foreach (var resource in new[] { resource1, resource3 })
            {
                Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);
                var coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);

                // Expected capabilities + 1 > RST_ResourceType
                Assert.AreEqual(1, coreResource.Capabilities.Count);

                var resourceCapability = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == capability.Id);
                Assert.IsNull(resourceCapability);
            }
        }

        [TestMethod]
        public void AssignToDraftPoolWithMixedResourcesAndMovePoolToCompleted()
        {
            var prefix = Guid.NewGuid();

            var capability = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
            {
                Name = $"{prefix}_Capability",
            };
            capability.SetDiscretes(new[] { "Value 1", "Value 2", "Value 3" });
            objectCreator.CreateCapability(capability);

            var capabilitySettings = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySettings(capability.Id);
            capabilitySettings.SetDiscretes(new[] { "Value 2", "Value 3" });

            var unmanagedResource1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource1",
            };
            var unmanagedResource2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource2",
            };
            var unmanagedResource3 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource3",
            };
            objectCreator.CreateResources([unmanagedResource1, unmanagedResource2, unmanagedResource3]);
            TestContext.Api.Resources.Complete(unmanagedResource1.Id);
            TestContext.Api.Resources.Complete(unmanagedResource3.Id);

            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_Resource Pool",
            };
            objectCreator.CreateResourcePool(resourcePool);

            var resources = TestContext.Api.Resources.Read([unmanagedResource1.Id, unmanagedResource2.Id, unmanagedResource3.Id]).ToList();
            TestContext.Api.ResourcePools.AssignResourcesToPool(resourcePool.Id, resources);

            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);
            Assert.AreEqual(Guid.Empty, resourcePool.CoreResourcePoolId);

            resourcePool.AddCapability(capabilitySettings);
            TestContext.Api.ResourcePools.Update(resourcePool);

            TestContext.Api.ResourcePools.Complete(resourcePool.Id);

            var resource1 = resources.Single(r => r.Id == unmanagedResource1.Id);
            var resource2 = resources.Single(r => r.Id == unmanagedResource2.Id);
            var resource3 = resources.Single(r => r.Id == unmanagedResource3.Id);

            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);
            Assert.AreNotEqual(Guid.Empty, resourcePool.CoreResourcePoolId);
            Assert.AreEqual(Guid.Empty, resource2.CoreResourceId);

            foreach (var resource in new[] { resource1, resource3 })
            {
                Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);
                var coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);

                // Expected capabilities + 1 > RST_ResourceType
                Assert.AreEqual(2, coreResource.Capabilities.Count);

                var resourceCapability = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == capability.Id);
                Assert.IsNotNull(resourceCapability);
                Assert.AreEqual(2, resourceCapability.Value.Discreets.Count);
                Assert.IsTrue(resourceCapability.Value.Discreets.Contains("Value 2"));
                Assert.IsTrue(resourceCapability.Value.Discreets.Contains("Value 3"));
            }
        }

        [TestMethod]
        public void AssignToCompletedPoolWithDraftResourcesAndMoveResourcesToCompleted()
        {
            var prefix = Guid.NewGuid();

            var capability = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
            {
                Name = $"{prefix}_Capability",
            };
            capability.SetDiscretes(new[] { "Value 1", "Value 2", "Value 3" });
            objectCreator.CreateCapability(capability);

            var capabilitySettings = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySettings(capability.Id);
            capabilitySettings.SetDiscretes(new[] { "Value 2", "Value 3" });

            var unmanagedResource1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource1",
            };
            var unmanagedResource2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource2",
            };
            var unmanagedResource3 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource3",
            };
            objectCreator.CreateResources([unmanagedResource1, unmanagedResource2, unmanagedResource3]);

            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_Resource Pool",
            };
            objectCreator.CreateResourcePool(resourcePool);
            TestContext.Api.ResourcePools.Complete(resourcePool.Id);

            var resources = TestContext.Api.Resources.Read([unmanagedResource1.Id, unmanagedResource2.Id, unmanagedResource3.Id]).ToList();
            TestContext.Api.ResourcePools.AssignResourcesToPool(resourcePool.Id, resources);

            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);
            Assert.AreNotEqual(Guid.Empty, resourcePool.CoreResourcePoolId);

            resourcePool.AddCapability(capabilitySettings);
            TestContext.Api.ResourcePools.Update(resourcePool);

            TestContext.Api.Resources.Complete(unmanagedResource1.Id);
            TestContext.Api.Resources.Complete(unmanagedResource3.Id);

            resources = TestContext.Api.Resources.Read([unmanagedResource1.Id, unmanagedResource2.Id, unmanagedResource3.Id]).ToList();
            var resource1 = resources.Single(r => r.Id == unmanagedResource1.Id);
            var resource2 = resources.Single(r => r.Id == unmanagedResource2.Id);
            var resource3 = resources.Single(r => r.Id == unmanagedResource3.Id);

            Assert.AreEqual(Guid.Empty, resource2.CoreResourceId);

            foreach (var resource in new[] { resource1, resource3 })
            {
                Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);
                var coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);

                // Expected capabilities + 1 > RST_ResourceType
                Assert.AreEqual(2, coreResource.Capabilities.Count);

                var resourceCapability = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == capability.Id);
                Assert.IsNotNull(resourceCapability);
                Assert.AreEqual(2, resourceCapability.Value.Discreets.Count);
                Assert.IsTrue(resourceCapability.Value.Discreets.Contains("Value 2"));
                Assert.IsTrue(resourceCapability.Value.Discreets.Contains("Value 3"));
            }
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

            var timeCapabilitySettings1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySettings(timeCapability1.Id);
            timeCapabilitySettings1.SetDiscretes(new[] { "Value 1", "Value 2" });

            var timeCapabilitySettings2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySettings(timeCapability2.Id);
            timeCapabilitySettings2.SetDiscretes(new[] { "Value 2", "Value 3" });

            var regularCapabilitySettings = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySettings(regularCapability.Id);
            regularCapabilitySettings.SetDiscretes(new[] { "Value 1", "Value 3" });

            var unmanagedResource1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource1",
            };
            var unmanagedResource2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource2",
            };
            var unmanagedResource3 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource3",
            };
            objectCreator.CreateResources([unmanagedResource1, unmanagedResource2, unmanagedResource3]);
            TestContext.Api.Resources.Complete(unmanagedResource1.Id);
            TestContext.Api.Resources.Complete(unmanagedResource3.Id);

            // Create Resource Pool with one time dependent capability assigned
            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_Resource Pool",
            };
            resourcePool.AddCapability(timeCapabilitySettings1);
            objectCreator.CreateResourcePool(resourcePool);

            // Move Resource Pool to Completed state
            TestContext.Api.ResourcePools.Complete(resourcePool.Id);

            var resources = TestContext.Api.Resources.Read([unmanagedResource1.Id, unmanagedResource2.Id, unmanagedResource3.Id]).ToList();
            TestContext.Api.ResourcePools.AssignResourcesToPool(resourcePool.Id, resources);

            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);
            Assert.AreEqual(1, resourcePool.Capabilities.Count);

            var resource1 = resources.Single(r => r.Id == unmanagedResource1.Id);
            var resource2 = resources.Single(r => r.Id == unmanagedResource2.Id);
            var resource3 = resources.Single(r => r.Id == unmanagedResource3.Id);

            var capabilities = TestContext.Api.Capabilities.Read([timeCapability1.Id, timeCapability2.Id, regularCapability.Id]);
            timeCapability1 = capabilities.Single(c => c.Id == timeCapability1.Id);
            timeCapability2 = capabilities.Single(c => c.Id == timeCapability2.Id);
            regularCapability = capabilities.Single(c => c.Id == regularCapability.Id);

            Assert.AreNotEqual(Guid.Empty, resourcePool.CoreResourcePoolId);
            Assert.AreEqual(Guid.Empty, resource2.CoreResourceId);

            foreach (var resource in new[] { resource1, resource3 })
            {
                Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);
                var coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);

                // Expected capabilities + 1 > RST_ResourceType
                Assert.AreEqual(3, coreResource.Capabilities.Count);

                var resourceCapability = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == timeCapability1.Id);
                Assert.IsNotNull(resourceCapability);
                Assert.AreEqual(2, resourceCapability.Value.Discreets.Count);
                Assert.IsTrue(resourceCapability.Value.Discreets.Contains("Value 1"));
                Assert.IsTrue(resourceCapability.Value.Discreets.Contains("Value 2"));

                var resourceTimeDependentCapability = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == timeCapability1.LinkedTimeDependentCapabilityId);
                Assert.IsNotNull(resourceTimeDependentCapability);
                Assert.IsTrue(resourceTimeDependentCapability.IsTimeDynamic);
            }

            // Update Resource Pool to add second time dependent capability
            resourcePool.AddCapability(timeCapabilitySettings2);
            TestContext.Api.ResourcePools.Update(resourcePool);

            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);
            Assert.AreEqual(2, resourcePool.Capabilities.Count);

            foreach (var resource in new[] { resource1, resource3 })
            {
                Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);
                var coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);

                // Expected capabilities + 1 > RST_ResourceType
                Assert.AreEqual(5, coreResource.Capabilities.Count);

                var resourceCapability1 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == timeCapability1.Id);
                Assert.IsNotNull(resourceCapability1);
                Assert.AreEqual(2, resourceCapability1.Value.Discreets.Count);
                Assert.IsTrue(resourceCapability1.Value.Discreets.Contains("Value 1"));
                Assert.IsTrue(resourceCapability1.Value.Discreets.Contains("Value 2"));

                var resourceTimeDependentCapability1 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == timeCapability1.LinkedTimeDependentCapabilityId);
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
            }

            // Update Resource Pool to add regular capability
            resourcePool.AddCapability(regularCapabilitySettings);
            TestContext.Api.ResourcePools.Update(resourcePool);

            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);
            Assert.AreEqual(3, resourcePool.Capabilities.Count);

            foreach (var resource in new[] { resource1, resource3 })
            {
                Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);
                var coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);

                // Expected capabilities + 1 > RST_ResourceType
                Assert.AreEqual(6, coreResource.Capabilities.Count);

                var resourceCapability1 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == timeCapability1.Id);
                Assert.IsNotNull(resourceCapability1);
                Assert.AreEqual(2, resourceCapability1.Value.Discreets.Count);
                Assert.IsTrue(resourceCapability1.Value.Discreets.Contains("Value 1"));
                Assert.IsTrue(resourceCapability1.Value.Discreets.Contains("Value 2"));

                var resourceTimeDependentCapability1 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == timeCapability1.LinkedTimeDependentCapabilityId);
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

                var resourceCapability3 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == regularCapability.Id);
                Assert.IsNotNull(resourceCapability3);
                Assert.AreEqual(2, resourceCapability3.Value.Discreets.Count);
                Assert.IsTrue(resourceCapability3.Value.Discreets.Contains("Value 1"));
                Assert.IsTrue(resourceCapability3.Value.Discreets.Contains("Value 3"));
            }

            // Remove first time dependent capability
            timeCapabilitySettings1 = resourcePool.Capabilities.First(x => x.Id == timeCapability1.Id);
            resourcePool.RemoveCapability(timeCapabilitySettings1);
            TestContext.Api.ResourcePools.Update(resourcePool);

            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);
            Assert.AreEqual(2, resourcePool.Capabilities.Count);

            foreach (var resource in new[] { resource1, resource3 })
            {
                Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);
                var coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);

                // Expected capabilities + 1 > RST_ResourceType
                Assert.AreEqual(4, coreResource.Capabilities.Count);

                var resourceCapability2 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == timeCapability2.Id);
                Assert.IsNotNull(resourceCapability2);
                Assert.AreEqual(2, resourceCapability2.Value.Discreets.Count);
                Assert.IsTrue(resourceCapability2.Value.Discreets.Contains("Value 2"));
                Assert.IsTrue(resourceCapability2.Value.Discreets.Contains("Value 3"));

                var resourceTimeDependentCapability2 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == timeCapability2.LinkedTimeDependentCapabilityId);
                Assert.IsNotNull(resourceTimeDependentCapability2);
                Assert.IsTrue(resourceTimeDependentCapability2.IsTimeDynamic);

                var resourceCapability3 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == regularCapability.Id);
                Assert.IsNotNull(resourceCapability3);
                Assert.AreEqual(2, resourceCapability3.Value.Discreets.Count);
                Assert.IsTrue(resourceCapability3.Value.Discreets.Contains("Value 1"));
                Assert.IsTrue(resourceCapability3.Value.Discreets.Contains("Value 3"));
            }
        }

        [TestMethod]
        public void Dom_SinglePoolCapability()
        {
            var prefix = Guid.NewGuid();

            var capability1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
            {
                Name = $"{prefix}_Capability 1",
            };
            capability1.SetDiscretes(new[] { "Value 1", "Value 2", "Value 3" });
            objectCreator.CreateCapability(capability1);

            var capabilitySettings1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySettings(capability1.Id);
            capabilitySettings1.SetDiscretes(new[] { "Value 1", "Value 2" });

            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_Resource Pool",
            };
            resourcePool.AddCapability(capabilitySettings1);
            objectCreator.CreateResourcePool(resourcePool);

            var domResourcePool = TestContext.ResourceStudioDomHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(resourcePool.Id)).SingleOrDefault();
            Assert.IsTrue(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolCapabilities.Id.Id));

            var domResourcePoolCapabilitiesSections = domResourcePool.Sections.Where(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolCapabilities.Id.Id).ToList();
            Assert.AreEqual(1, domResourcePoolCapabilitiesSections.Count);

            var domResourcePoolCapability = domResourcePoolCapabilitiesSections.Single();
            var fdProfileParameterId = domResourcePoolCapability.FieldValues.SingleOrDefault(f => f.FieldDescriptorID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolCapabilities.ProfileParameterID.Id);
            Assert.IsNotNull(fdProfileParameterId);
            Assert.IsTrue(Guid.TryParse(Convert.ToString(fdProfileParameterId.Value.Value), out var profileParameterId));
            Assert.AreEqual(capability1.Id, profileParameterId);

            var fdStringValue = domResourcePoolCapability.FieldValues.SingleOrDefault(f => f.FieldDescriptorID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolCapabilities.StringValue.Id);
            Assert.IsNotNull(fdStringValue);
            var discreteValues = Convert.ToString(fdStringValue.Value.Value).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            Assert.AreEqual(2, discreteValues.Count);
            Assert.IsTrue(discreteValues.Contains("Value 1"));
            Assert.IsTrue(discreteValues.Contains("Value 2"));
        }

        [TestMethod]
        public void MixedCRUD()
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

            var capabilityResourceSettings1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySettings(capability1.Id);
            capabilityResourceSettings1.SetDiscretes(new[] { "Value 1", "Value 2" });

            var capabilityResourceSettings2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySettings(capability2.Id);
            capabilityResourceSettings2.SetDiscretes(new[] { "Value 2", "Value 3" });

            var capabilityPoolSettings = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySettings(capability2.Id);
            capabilityPoolSettings.SetDiscretes(new[] { "Value 1", "Value 2" });

            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_Resource Pool",
            };
            resourcePool.AddCapability(capabilityPoolSettings);
            objectCreator.CreateResourcePool(resourcePool);
            TestContext.Api.ResourcePools.Complete(resourcePool.Id);

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };
            unmanagedResource.AssignToPool(resourcePool.Id);
            unmanagedResource.AddCapability(capabilityResourceSettings1);
            objectCreator.CreateResource(unmanagedResource);
            TestContext.Api.Resources.Complete(unmanagedResource.Id);

            // Validate capabilities on core resource
            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);
            Assert.AreEqual(1, resourcePool.Capabilities.Count);
            var resource = TestContext.Api.Resources.Read(unmanagedResource.Id);
            Assert.AreEqual(1, resource.Capabilities.Count);

            Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);
            var coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);

            // Expected capabilities + 1 > RST_ResourceType
            Assert.AreEqual(3, coreResource.Capabilities.Count);

            var resourceCapability1 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == capability1.Id);
            Assert.IsNotNull(resourceCapability1);
            Assert.AreEqual(2, resourceCapability1.Value.Discreets.Count);
            Assert.IsTrue(resourceCapability1.Value.Discreets.Contains("Value 1"));
            Assert.IsTrue(resourceCapability1.Value.Discreets.Contains("Value 2"));

            var resourceCapability2 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == capability2.Id);
            Assert.IsNotNull(resourceCapability2);
            Assert.AreEqual(2, resourceCapability2.Value.Discreets.Count);
            Assert.IsTrue(resourceCapability2.Value.Discreets.Contains("Value 1"));
            Assert.IsTrue(resourceCapability2.Value.Discreets.Contains("Value 2"));

            // Update resource with additional capability that overlaps with the one defined on pool
            resource.AddCapability(capabilityResourceSettings2);
            TestContext.Api.Resources.Update(resource);

            resource = TestContext.Api.Resources.Read(unmanagedResource.Id);
            Assert.AreEqual(2, resource.Capabilities.Count);

            Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);
            coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);

            // Expected capabilities + 1 > RST_ResourceType
            Assert.AreEqual(3, coreResource.Capabilities.Count);

            resourceCapability1 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == capability1.Id);
            Assert.IsNotNull(resourceCapability1);
            Assert.AreEqual(2, resourceCapability1.Value.Discreets.Count);
            Assert.IsTrue(resourceCapability1.Value.Discreets.Contains("Value 1"));
            Assert.IsTrue(resourceCapability1.Value.Discreets.Contains("Value 2"));

            resourceCapability2 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == capability2.Id);
            Assert.IsNotNull(resourceCapability2);
            Assert.AreEqual(3, resourceCapability2.Value.Discreets.Count);
            Assert.IsTrue(resourceCapability2.Value.Discreets.Contains("Value 1"));
            Assert.IsTrue(resourceCapability2.Value.Discreets.Contains("Value 2"));
            Assert.IsTrue(resourceCapability2.Value.Discreets.Contains("Value 3"));

            // Update pool capability discrete value
            capabilityPoolSettings = resourcePool.Capabilities.SingleOrDefault(x => x.Id == capability2.Id);
            capabilityPoolSettings.RemoveDiscrete("Value 2");
            TestContext.Api.ResourcePools.Update(resourcePool);

            // Expected capabilities + 1 > RST_ResourceType
            coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);

            resourceCapability1 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == capability1.Id);
            Assert.IsNotNull(resourceCapability1);
            Assert.AreEqual(2, resourceCapability1.Value.Discreets.Count);
            Assert.IsTrue(resourceCapability1.Value.Discreets.Contains("Value 1"));
            Assert.IsTrue(resourceCapability1.Value.Discreets.Contains("Value 2"));

            resourceCapability2 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == capability2.Id);
            Assert.IsNotNull(resourceCapability2);
            Assert.AreEqual(3, resourceCapability2.Value.Discreets.Count);
            Assert.IsTrue(resourceCapability2.Value.Discreets.Contains("Value 1"));
            Assert.IsTrue(resourceCapability2.Value.Discreets.Contains("Value 2"));
            Assert.IsTrue(resourceCapability2.Value.Discreets.Contains("Value 3"));

            // Remove pool capability
            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);
            capabilityPoolSettings = resourcePool.Capabilities.SingleOrDefault(x => x.Id == capability2.Id);
            resourcePool.RemoveCapability(capabilityPoolSettings);
            TestContext.Api.ResourcePools.Update(resourcePool);

            // Expected capabilities + 1 > RST_ResourceType
            coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);

            resourceCapability1 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == capability1.Id);
            Assert.IsNotNull(resourceCapability1);
            Assert.AreEqual(2, resourceCapability1.Value.Discreets.Count);
            Assert.IsTrue(resourceCapability1.Value.Discreets.Contains("Value 1"));
            Assert.IsTrue(resourceCapability1.Value.Discreets.Contains("Value 2"));

            resourceCapability2 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == capability2.Id);
            Assert.IsNotNull(resourceCapability2);
            Assert.AreEqual(2, resourceCapability2.Value.Discreets.Count);
            Assert.IsTrue(resourceCapability2.Value.Discreets.Contains("Value 2"));
            Assert.IsTrue(resourceCapability2.Value.Discreets.Contains("Value 3"));
        }

        [TestMethod]
        public void AssignWithEmptyIdThrowsException()
        {
            var prefix = Guid.NewGuid();

            Assert.ThrowsException<ArgumentException>(() => new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySettings(Guid.Empty));
            Assert.ThrowsException<ArgumentException>(() => new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySettings(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability(Guid.Empty)));
        }

        [TestMethod]
        public void AssignNotExistingCapabilityThrowsException()
        {
            var prefix = Guid.NewGuid();

            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_Resource Pool",
            };

            var notExistingCapabilityId = Guid.NewGuid();
            var capabilitySettings = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySettings(notExistingCapabilityId);
            capabilitySettings.SetDiscretes(new[] { "Value 1", "Value 2" });
            resourcePool.AddCapability(capabilitySettings);

            try
            {
                objectCreator.CreateResourcePool(resourcePool);
            }
            catch (MediaOpsException ex)
            {
                var errorMessage = $"Capability with ID '{notExistingCapabilityId}' not found.";
                Assert.AreEqual(errorMessage, ex.Message);

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var resourcePoolConfigurationError = ex.TraceData.ErrorData.OfType<ResourcePoolError>().SingleOrDefault();
                Assert.IsNotNull(resourcePoolConfigurationError);

                var invalidResourcePoolCapabilitySettingsError = resourcePoolConfigurationError as ResourcePoolInvalidCapabilitySettingsError;
                Assert.IsNotNull(invalidResourcePoolCapabilitySettingsError);
                Assert.AreEqual(errorMessage, invalidResourcePoolCapabilitySettingsError.ErrorMessage);
                Assert.AreEqual(notExistingCapabilityId, invalidResourcePoolCapabilitySettingsError.CapabilityId);

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

            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_Resource Pool",
            };

            var capabilitySettings = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySettings(capability.Id);
            resourcePool.AddCapability(capabilitySettings);

            try
            {
                objectCreator.CreateResourcePool(resourcePool);
            }
            catch (MediaOpsException ex)
            {
                var errorMessage = "At least one discrete value must be specified for the capability.";
                Assert.AreEqual(errorMessage, ex.Message);

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var resourcePoolConfigurationError = ex.TraceData.ErrorData.OfType<ResourcePoolError>().SingleOrDefault();
                Assert.IsNotNull(resourcePoolConfigurationError);

                var invalidResourcePoolCapabilitySettingsError = resourcePoolConfigurationError as ResourcePoolInvalidCapabilitySettingsError;
                Assert.IsNotNull(invalidResourcePoolCapabilitySettingsError);
                Assert.AreEqual(errorMessage, invalidResourcePoolCapabilitySettingsError.ErrorMessage);
                Assert.AreEqual(capability.Id, invalidResourcePoolCapabilitySettingsError.CapabilityId);

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

            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_Resource Pool",
            };

            var capabilitySettings = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySettings(capability.Id);
            capabilitySettings.SetDiscretes(new[] { "Value 1", "Value 5" });
            resourcePool.AddCapability(capabilitySettings);

            try
            {
                objectCreator.CreateResourcePool(resourcePool);
            }
            catch (MediaOpsException ex)
            {
                var errorMessage = $"Discrete value 'Value 5' is not valid for capability '{capability.Name}'.";
                Assert.AreEqual(errorMessage, ex.Message);

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var resourcePoolConfigurationError = ex.TraceData.ErrorData.OfType<ResourcePoolError>().SingleOrDefault();
                Assert.IsNotNull(resourcePoolConfigurationError);

                var invalidResourcePoolCapabilitySettingsError = resourcePoolConfigurationError as ResourcePoolInvalidCapabilitySettingsError;
                Assert.IsNotNull(invalidResourcePoolCapabilitySettingsError);
                Assert.AreEqual(errorMessage, invalidResourcePoolCapabilitySettingsError.ErrorMessage);
                Assert.AreEqual(capability.Id, invalidResourcePoolCapabilitySettingsError.CapabilityId);

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void CreateWithDuplicateSettingsThrowsException()
        {
            var prefix = Guid.NewGuid();

            var capability1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
            {
                Name = $"{prefix}_Capability1",
            }
            .SetDiscretes(new[] { "Value 1", "Value 2", "Value 3" });
            var capability2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
            {
                Name = $"{prefix}_Capability2",
            }
            .SetDiscretes(new[] { "Value 1", "Value 2", "Value 3" });
            objectCreator.CreateCapabilities([capability1, capability2]);

            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_Resource Pool",
            }
            .AddCapability(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySettings(capability1.Id).AddDiscrete("Value 1"))
            .AddCapability(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySettings(capability2.Id).AddDiscrete("Value 2"))
            .AddCapability(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySettings(capability1.Id).AddDiscrete("Value 3"));

            try
            {
                objectCreator.CreateResourcePool(resourcePool);
            }
            catch (MediaOpsException ex)
            {
                var errorMessage = $"Capability with ID '{capability1.Id}' is defined 2 times. Duplicate capability settings are not allowed.";
                Assert.AreEqual(errorMessage, ex.Message);
                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);

                var resourcePoolConfigurationError = ex.TraceData.ErrorData.OfType<ResourcePoolError>().SingleOrDefault();
                Assert.IsNotNull(resourcePoolConfigurationError);

                var invalidResourcePoolCapabilitySettingsError = resourcePoolConfigurationError as ResourcePoolInvalidCapabilitySettingsError;
                Assert.IsNotNull(invalidResourcePoolCapabilitySettingsError);
                Assert.AreEqual(errorMessage, invalidResourcePoolCapabilitySettingsError.ErrorMessage);
                Assert.AreEqual(capability1.Id, invalidResourcePoolCapabilitySettingsError.CapabilityId);
                Assert.AreEqual(resourcePool.Id, invalidResourcePoolCapabilitySettingsError.Id);

                return;
            }

            Assert.Fail("Exception not thrown");
        }

        [TestMethod]
        public void UpdateWithDuplicateSettingsThrowsException()
        {
            var prefix = Guid.NewGuid();

            var capability1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
            {
                Name = $"{prefix}_Capability1",
            }
            .SetDiscretes(new[] { "Value 1", "Value 2", "Value 3" });
            var capability2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
            {
                Name = $"{prefix}_Capability2",
            }
            .SetDiscretes(new[] { "Value 1", "Value 2", "Value 3" });
            objectCreator.CreateCapabilities([capability1, capability2]);

            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_Resource Pool",
            }
            .AddCapability(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySettings(capability1.Id).AddDiscrete("Value 1"))
            .AddCapability(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySettings(capability2.Id).AddDiscrete("Value 2"));
            objectCreator.CreateResourcePool(resourcePool);

            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);
            resourcePool.AddCapability(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySettings(capability1.Id).AddDiscrete("Value 3"));

            try
            {
                TestContext.Api.ResourcePools.Update(resourcePool);
            }
            catch (MediaOpsException ex)
            {
                var errorMessage = $"Capability with ID '{capability1.Id}' is defined 2 times. Duplicate capability settings are not allowed.";
                Assert.AreEqual(errorMessage, ex.Message);
                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);

                var resourcePoolConfigurationError = ex.TraceData.ErrorData.OfType<ResourcePoolError>().SingleOrDefault();
                Assert.IsNotNull(resourcePoolConfigurationError);

                var invalidResourcePoolCapabilitySettingsError = resourcePoolConfigurationError as ResourcePoolInvalidCapabilitySettingsError;
                Assert.IsNotNull(invalidResourcePoolCapabilitySettingsError);
                Assert.AreEqual(errorMessage, invalidResourcePoolCapabilitySettingsError.ErrorMessage);
                Assert.AreEqual(capability1.Id, invalidResourcePoolCapabilitySettingsError.CapabilityId);
                Assert.AreEqual(resourcePool.Id, invalidResourcePoolCapabilitySettingsError.Id);

                return;
            }

            Assert.Fail("Exception not thrown");
        }

        [TestMethod]
        public void AssignCapabilityFromExistingResourcePoolToNewResourcePool()
        {
            var prefix = Guid.NewGuid();

            var capability = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
            {
                Name = $"{prefix}_Capability",
            };
            capability.SetDiscretes(new[] { "Value 1", "Value 2", "Value 3" });
            objectCreator.CreateCapability(capability);

            var resourcePool1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool1",
            }
            .AddCapability(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySettings(capability.Id).AddDiscrete("Value 2"));

            objectCreator.CreateResourcePool(resourcePool1);
            resourcePool1 = TestContext.Api.ResourcePools.Read(resourcePool1.Id);

            var resourcePool2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool2",
            };

            foreach (var capabilitySetting in resourcePool1.Capabilities)
            {
                resourcePool2.AddCapability(capabilitySetting);
            }

            objectCreator.CreateResourcePool(resourcePool2);

            resourcePool2 = TestContext.Api.ResourcePools.Read(resourcePool2.Id);
            Assert.IsNotNull(resourcePool2);
        }
    }
}
