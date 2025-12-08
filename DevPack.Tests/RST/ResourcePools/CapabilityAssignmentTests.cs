namespace RT_MediaOps.Plan.RST.ResourcePools
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using RT_MediaOps.Plan.RegressionTests;

    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

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
        public void DraftPoolCRUD()
        {
            var prefix = Guid.NewGuid();

            var capability1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
            {
                Name = $"{prefix}_Capability 1",
            };
            capability1.SetDiscretes(new[] { "Value 1", "Value 2", "Value 3" });
            var capabilityId1 = objectCreator.CreateCapability(capability1);

            var capability2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
            {
                Name = $"{prefix}_Capability 2",
            };
            capability2.SetDiscretes(new[] { "Value 1", "Value 2", "Value 3" });
            var capabilityId2 = objectCreator.CreateCapability(capability2);

            var capabilitySettings1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePoolCapabilitySettings(capabilityId1);
            capabilitySettings1.SetDiscretes(new[] { "Value 1", "Value 2" });

            var capabilitySettings2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePoolCapabilitySettings(capabilityId2);
            capabilitySettings2.SetDiscretes(new[] { "Value 2", "Value 3" });

            // Create Resource Pool with one capability assigned
            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_Resource Pool",
            };
            resourcePool.AddCapability(capabilitySettings1);
            var poolId = objectCreator.CreateResourcePool(resourcePool);

            resourcePool = TestContext.Api.ResourcePools.Read(poolId);
            Assert.AreEqual(1, resourcePool.Capabilities.Count);
            var resourceCapbility = resourcePool.Capabilities.Single();
            Assert.AreEqual(capabilityId1, resourceCapbility.Id);
            Assert.AreEqual(2, resourceCapbility.Discretes.Count);
            Assert.IsTrue(resourceCapbility.Discretes.Contains("Value 1"));
            Assert.IsTrue(resourceCapbility.Discretes.Contains("Value 2"));

            // Update pool capability discretes
            capabilitySettings1 = resourcePool.Capabilities.First(x => x.Id == capability1.Id);
            capabilitySettings1.RemoveDiscrete("Value 1");
            TestContext.Api.ResourcePools.Update(resourcePool);

            resourcePool = TestContext.Api.ResourcePools.Read(poolId);
            Assert.AreEqual(1, resourcePool.Capabilities.Count);
            resourceCapbility = resourcePool.Capabilities.Single();
            Assert.AreEqual(capabilityId1, resourceCapbility.Id);
            Assert.AreEqual(1, resourceCapbility.Discretes.Count);
            Assert.IsTrue(resourceCapbility.Discretes.Contains("Value 2"));

            // Update Resource Pool to add second capability
            resourcePool.AddCapability(capabilitySettings2);
            TestContext.Api.ResourcePools.Update(resourcePool);

            resourcePool = TestContext.Api.ResourcePools.Read(poolId);
            Assert.AreEqual(2, resourcePool.Capabilities.Count);
            var expectedCapabilityData = new Dictionary<Guid, List<string>>()
            {
                { capabilityId1, new List<string> { "Value 2" } },
                { capabilityId2, new List<string> { "Value 2", "Value 3" } },
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
            var capabilitySettingToRemove = resourcePool.Capabilities.First(c => c.Id == capabilityId1);
            resourcePool.RemoveCapability(capabilitySettingToRemove);
            TestContext.Api.ResourcePools.Update(resourcePool);

            resourcePool = TestContext.Api.ResourcePools.Read(poolId);
            Assert.AreEqual(1, resourcePool.Capabilities.Count);
            resourceCapbility = resourcePool.Capabilities.Single();
            Assert.AreEqual(capabilityId2, resourceCapbility.Id);
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
            var capabilityId1 = objectCreator.CreateCapability(capability1);

            var capability2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
            {
                Name = $"{prefix}_Capability 2",
            };
            capability2.SetDiscretes(new[] { "Value 1", "Value 2", "Value 3" });
            var capabilityId2 = objectCreator.CreateCapability(capability2);

            var capabilitySettings1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePoolCapabilitySettings(capabilityId1);
            capabilitySettings1.SetDiscretes(new[] { "Value 1", "Value 2" });

            var capabilitySettings2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePoolCapabilitySettings(capabilityId2);
            capabilitySettings2.SetDiscretes(new[] { "Value 2", "Value 3" });

            // Create Resource Pool with one capability assigned
            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_Resource Pool",
            };
            resourcePool.AddCapability(capabilitySettings1);
            var poolId = objectCreator.CreateResourcePool(resourcePool);

            // Move Resource Pool to Completed state
            TestContext.Api.ResourcePools.MoveTo(poolId, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePoolState.Complete);

            resourcePool = TestContext.Api.ResourcePools.Read(poolId);
            Assert.AreEqual(1, resourcePool.Capabilities.Count);
            var resourceCapbility = resourcePool.Capabilities.Single();
            Assert.AreEqual(capabilityId1, resourceCapbility.Id);
            Assert.AreEqual(2, resourceCapbility.Discretes.Count);
            Assert.IsTrue(resourceCapbility.Discretes.Contains("Value 1"));
            Assert.IsTrue(resourceCapbility.Discretes.Contains("Value 2"));

            // Update pool capability discretes
            capabilitySettings1 = resourcePool.Capabilities.First(x => x.Id == capability1.Id);
            capabilitySettings1.RemoveDiscrete("Value 1");
            TestContext.Api.ResourcePools.Update(resourcePool);

            resourcePool = TestContext.Api.ResourcePools.Read(poolId);
            Assert.AreEqual(1, resourcePool.Capabilities.Count);
            resourceCapbility = resourcePool.Capabilities.Single();
            Assert.AreEqual(capabilityId1, resourceCapbility.Id);
            Assert.AreEqual(1, resourceCapbility.Discretes.Count);
            Assert.IsTrue(resourceCapbility.Discretes.Contains("Value 2"));

            // Update Resource Pool to add second capability
            resourcePool.AddCapability(capabilitySettings2);
            TestContext.Api.ResourcePools.Update(resourcePool);

            resourcePool = TestContext.Api.ResourcePools.Read(poolId);
            Assert.AreEqual(2, resourcePool.Capabilities.Count);
            var expectedCapabilityData = new Dictionary<Guid, List<string>>()
            {
                { capabilityId1, new List<string> { "Value 2" } },
                { capabilityId2, new List<string> { "Value 2", "Value 3" } },
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
            var capabilitySettingToRemove = resourcePool.Capabilities.First(c => c.Id == capabilityId1);
            resourcePool.RemoveCapability(capabilitySettingToRemove);
            TestContext.Api.ResourcePools.Update(resourcePool);

            resourcePool = TestContext.Api.ResourcePools.Read(poolId);
            Assert.AreEqual(1, resourcePool.Capabilities.Count);
            resourceCapbility = resourcePool.Capabilities.Single();
            Assert.AreEqual(capabilityId2, resourceCapbility.Id);
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
            var capabilityId = objectCreator.CreateCapability(capability);

            var capabilitySettings = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePoolCapabilitySettings(capabilityId);
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
            var resourceIds = objectCreator.CreateResources([unmanagedResource1, unmanagedResource2, unmanagedResource3]).ToArray();
            TestContext.Api.Resources.MoveTo(unmanagedResource1.Id, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceState.Complete);
            TestContext.Api.Resources.MoveTo(unmanagedResource3.Id, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceState.Complete);

            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_Resource Pool",
            };
            var poolId = objectCreator.CreateResourcePool(resourcePool);

            var resources = TestContext.Api.Resources.Read(resourceIds).Values;
            TestContext.Api.ResourcePools.AssignResourcesToPool(poolId, resources);

            // Assign capability to pool
            resourcePool = TestContext.Api.ResourcePools.Read(poolId);
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
                var resourceCapability = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == capabilityId);
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
            var capabilityId = objectCreator.CreateCapability(capability);

            var capabilitySettings = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePoolCapabilitySettings(capabilityId);
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
            var resourceIds = objectCreator.CreateResources([unmanagedResource1, unmanagedResource2, unmanagedResource3]).ToArray();
            TestContext.Api.Resources.MoveTo(unmanagedResource1.Id, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceState.Complete);
            TestContext.Api.Resources.MoveTo(unmanagedResource3.Id, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceState.Complete);

            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_Resource Pool",
            };
            var poolId = objectCreator.CreateResourcePool(resourcePool);
            TestContext.Api.ResourcePools.MoveTo(poolId, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePoolState.Complete);

            var resources = TestContext.Api.Resources.Read(resourceIds).Values;
            TestContext.Api.ResourcePools.AssignResourcesToPool(poolId, resources);

            // Assign capability to pool
            resourcePool = TestContext.Api.ResourcePools.Read(poolId);
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

                var resourceCapability = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == capabilityId);
                Assert.IsNotNull(resourceCapability);
                Assert.AreEqual(2, resourceCapability.Value.Discreets.Count);
                Assert.IsTrue(resourceCapability.Value.Discreets.Contains("Value 2"));
                Assert.IsTrue(resourceCapability.Value.Discreets.Contains("Value 3"));
            }

            // Update pool capability discretes
            resourcePool = TestContext.Api.ResourcePools.Read(poolId);
            capabilitySettings = resourcePool.Capabilities.First(x => x.Id == capability.Id);
            capabilitySettings.RemoveDiscrete("Value 3");
            TestContext.Api.ResourcePools.Update(resourcePool);

            foreach (var resource in new[] { resource1, resource3 })
            {
                Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);
                var coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);

                // Expected capabilities + 1 > RST_ResourceType
                Assert.AreEqual(2, coreResource.Capabilities.Count);

                var resourceCapability = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == capabilityId);
                Assert.IsNotNull(resourceCapability);
                Assert.AreEqual(1, resourceCapability.Value.Discreets.Count);
                Assert.IsTrue(resourceCapability.Value.Discreets.Contains("Value 2"));
            }

            // Remove capability from pool
            resourcePool = TestContext.Api.ResourcePools.Read(poolId);
            capabilitySettings = resourcePool.Capabilities.First(x => x.Id == capability.Id);
            resourcePool.RemoveCapability(capabilitySettings);
            TestContext.Api.ResourcePools.Update(resourcePool);

            foreach (var resource in new[] { resource1, resource3 })
            {
                Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);
                var coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);

                // Expected capabilities + 1 > RST_ResourceType
                Assert.AreEqual(1, coreResource.Capabilities.Count);

                var resourceCapability = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == capabilityId);
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
            var capabilityId = objectCreator.CreateCapability(capability);

            var capabilitySettings = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePoolCapabilitySettings(capabilityId);
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
            var resourceIds = objectCreator.CreateResources([unmanagedResource1, unmanagedResource2, unmanagedResource3]).ToArray();
            TestContext.Api.Resources.MoveTo(unmanagedResource1.Id, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceState.Complete);
            TestContext.Api.Resources.MoveTo(unmanagedResource3.Id, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceState.Complete);

            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_Resource Pool",
            };
            var poolId = objectCreator.CreateResourcePool(resourcePool);

            var resources = TestContext.Api.Resources.Read(resourceIds).Values;
            TestContext.Api.ResourcePools.AssignResourcesToPool(poolId, resources);

            resourcePool = TestContext.Api.ResourcePools.Read(poolId);
            Assert.AreEqual(Guid.Empty, resourcePool.CoreResourcePoolId);

            resourcePool.AddCapability(capabilitySettings);
            TestContext.Api.ResourcePools.Update(resourcePool);

            TestContext.Api.ResourcePools.MoveTo(poolId, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePoolState.Complete);

            var resource1 = resources.Single(r => r.Id == unmanagedResource1.Id);
            var resource2 = resources.Single(r => r.Id == unmanagedResource2.Id);
            var resource3 = resources.Single(r => r.Id == unmanagedResource3.Id);

            resourcePool = TestContext.Api.ResourcePools.Read(poolId);
            Assert.AreNotEqual(Guid.Empty, resourcePool.CoreResourcePoolId);
            Assert.AreEqual(Guid.Empty, resource2.CoreResourceId);

            foreach (var resource in new[] { resource1, resource3 })
            {
                Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);
                var coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);

                // Expected capabilities + 1 > RST_ResourceType
                Assert.AreEqual(2, coreResource.Capabilities.Count);

                var resourceCapability = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == capabilityId);
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
            var capabilityId = objectCreator.CreateCapability(capability);

            var capabilitySettings = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePoolCapabilitySettings(capabilityId);
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
            var resourceIds = objectCreator.CreateResources([unmanagedResource1, unmanagedResource2, unmanagedResource3]).ToArray();

            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_Resource Pool",
            };
            var poolId = objectCreator.CreateResourcePool(resourcePool);
            TestContext.Api.ResourcePools.MoveTo(poolId, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePoolState.Complete);

            var resources = TestContext.Api.Resources.Read(resourceIds).Values;
            TestContext.Api.ResourcePools.AssignResourcesToPool(poolId, resources);

            resourcePool = TestContext.Api.ResourcePools.Read(poolId);
            Assert.AreNotEqual(Guid.Empty, resourcePool.CoreResourcePoolId);

            resourcePool.AddCapability(capabilitySettings);
            TestContext.Api.ResourcePools.Update(resourcePool);

            TestContext.Api.Resources.MoveTo(unmanagedResource1.Id, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceState.Complete);
            TestContext.Api.Resources.MoveTo(unmanagedResource3.Id, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceState.Complete);

            resources = TestContext.Api.Resources.Read(resourceIds).Values;
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

                var resourceCapability = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == capabilityId);
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
            var timeCapabilityId1 = objectCreator.CreateCapability(timeCapability1);

            var timeCapability2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
            {
                Name = $"{prefix}_Time Capability 2",
                IsTimeDependent = true,
            };
            timeCapability2.SetDiscretes(new[] { "Value 1", "Value 2", "Value 3" });
            var timeCapabilityId2 = objectCreator.CreateCapability(timeCapability2);

            var regularCapability = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
            {
                Name = $"{prefix}_Capability"
            };
            regularCapability.SetDiscretes(new[] { "Value 1", "Value 2", "Value 3" });
            var regularCapabilityId = objectCreator.CreateCapability(regularCapability);

            var timeCapabilitySettings1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePoolCapabilitySettings(timeCapabilityId1);
            timeCapabilitySettings1.SetDiscretes(new[] { "Value 1", "Value 2" });

            var timeCapabilitySettings2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePoolCapabilitySettings(timeCapabilityId2);
            timeCapabilitySettings2.SetDiscretes(new[] { "Value 2", "Value 3" });

            var regularCapabilitySettings = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePoolCapabilitySettings(regularCapabilityId);
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
            var resourceIds = objectCreator.CreateResources([unmanagedResource1, unmanagedResource2, unmanagedResource3]).ToArray();
            TestContext.Api.Resources.MoveTo(unmanagedResource1.Id, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceState.Complete);
            TestContext.Api.Resources.MoveTo(unmanagedResource3.Id, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceState.Complete);

            // Create Resource Pool with one time dependent capability assigned
            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_Resource Pool",
            };
            resourcePool.AddCapability(timeCapabilitySettings1);
            var poolId = objectCreator.CreateResourcePool(resourcePool);

            // Move Resource Pool to Completed state
            TestContext.Api.ResourcePools.MoveTo(poolId, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePoolState.Complete);

            var resources = TestContext.Api.Resources.Read(resourceIds).Values;
            TestContext.Api.ResourcePools.AssignResourcesToPool(poolId, resources);

            resourcePool = TestContext.Api.ResourcePools.Read(poolId);
            Assert.AreEqual(1, resourcePool.Capabilities.Count);

            var resource1 = resources.Single(r => r.Id == unmanagedResource1.Id);
            var resource2 = resources.Single(r => r.Id == unmanagedResource2.Id);
            var resource3 = resources.Single(r => r.Id == unmanagedResource3.Id);

            var capabilities = TestContext.Api.Capabilities.Read([timeCapabilityId1, timeCapabilityId2, regularCapabilityId]).Values;
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

                var resourceCapability = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == timeCapabilityId1);
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

            resourcePool = TestContext.Api.ResourcePools.Read(poolId);
            Assert.AreEqual(2, resourcePool.Capabilities.Count);

            foreach (var resource in new[] { resource1, resource3 })
            {
                Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);
                var coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);

                // Expected capabilities + 1 > RST_ResourceType
                Assert.AreEqual(5, coreResource.Capabilities.Count);

                var resourceCapability1 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == timeCapabilityId1);
                Assert.IsNotNull(resourceCapability1);
                Assert.AreEqual(2, resourceCapability1.Value.Discreets.Count);
                Assert.IsTrue(resourceCapability1.Value.Discreets.Contains("Value 1"));
                Assert.IsTrue(resourceCapability1.Value.Discreets.Contains("Value 2"));

                var resourceTimeDependentCapability1 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == timeCapability1.LinkedTimeDependentCapabilityId);
                Assert.IsNotNull(resourceTimeDependentCapability1);
                Assert.IsTrue(resourceTimeDependentCapability1.IsTimeDynamic);

                var resourceCapability2 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == timeCapabilityId2);
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

            resourcePool = TestContext.Api.ResourcePools.Read(poolId);
            Assert.AreEqual(3, resourcePool.Capabilities.Count);

            foreach (var resource in new[] { resource1, resource3 })
            {
                Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);
                var coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);

                // Expected capabilities + 1 > RST_ResourceType
                Assert.AreEqual(6, coreResource.Capabilities.Count);

                var resourceCapability1 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == timeCapabilityId1);
                Assert.IsNotNull(resourceCapability1);
                Assert.AreEqual(2, resourceCapability1.Value.Discreets.Count);
                Assert.IsTrue(resourceCapability1.Value.Discreets.Contains("Value 1"));
                Assert.IsTrue(resourceCapability1.Value.Discreets.Contains("Value 2"));

                var resourceTimeDependentCapability1 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == timeCapability1.LinkedTimeDependentCapabilityId);
                Assert.IsNotNull(resourceTimeDependentCapability1);
                Assert.IsTrue(resourceTimeDependentCapability1.IsTimeDynamic);

                var resourceCapability2 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == timeCapabilityId2);
                Assert.IsNotNull(resourceCapability2);
                Assert.AreEqual(2, resourceCapability2.Value.Discreets.Count);
                Assert.IsTrue(resourceCapability2.Value.Discreets.Contains("Value 2"));
                Assert.IsTrue(resourceCapability2.Value.Discreets.Contains("Value 3"));

                var resourceTimeDependentCapability2 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == timeCapability2.LinkedTimeDependentCapabilityId);
                Assert.IsNotNull(resourceTimeDependentCapability2);
                Assert.IsTrue(resourceTimeDependentCapability2.IsTimeDynamic);

                var resourceCapability3 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == regularCapabilityId);
                Assert.IsNotNull(resourceCapability3);
                Assert.AreEqual(2, resourceCapability3.Value.Discreets.Count);
                Assert.IsTrue(resourceCapability3.Value.Discreets.Contains("Value 1"));
                Assert.IsTrue(resourceCapability3.Value.Discreets.Contains("Value 3"));
            }

            // Remove first time dependent capability
            timeCapabilitySettings1 = resourcePool.Capabilities.First(x => x.Id == timeCapability1.Id);
            resourcePool.RemoveCapability(timeCapabilitySettings1);
            TestContext.Api.ResourcePools.Update(resourcePool);

            resourcePool = TestContext.Api.ResourcePools.Read(poolId);
            Assert.AreEqual(2, resourcePool.Capabilities.Count);

            foreach (var resource in new[] { resource1, resource3 })
            {
                Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);
                var coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);

                // Expected capabilities + 1 > RST_ResourceType
                Assert.AreEqual(4, coreResource.Capabilities.Count);

                var resourceCapability2 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == timeCapabilityId2);
                Assert.IsNotNull(resourceCapability2);
                Assert.AreEqual(2, resourceCapability2.Value.Discreets.Count);
                Assert.IsTrue(resourceCapability2.Value.Discreets.Contains("Value 2"));
                Assert.IsTrue(resourceCapability2.Value.Discreets.Contains("Value 3"));

                var resourceTimeDependentCapability2 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == timeCapability2.LinkedTimeDependentCapabilityId);
                Assert.IsNotNull(resourceTimeDependentCapability2);
                Assert.IsTrue(resourceTimeDependentCapability2.IsTimeDynamic);

                var resourceCapability3 = coreResource.Capabilities.SingleOrDefault(x => x.CapabilityProfileID == regularCapabilityId);
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
            var capabilityId1 = objectCreator.CreateCapability(capability1);

            var capabilitySettings1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePoolCapabilitySettings(capabilityId1);
            capabilitySettings1.SetDiscretes(new[] { "Value 1", "Value 2" });

            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_Resource Pool",
            };
            resourcePool.AddCapability(capabilitySettings1);
            var poolId = objectCreator.CreateResourcePool(resourcePool);

            var domResourcePool = TestContext.ResourceStudioDomHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(poolId)).SingleOrDefault();
            Assert.IsTrue(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolCapabilities.Id.Id));

            var domResourcePoolCapabilitiesSections = domResourcePool.Sections.Where(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolCapabilities.Id.Id).ToList();
            Assert.AreEqual(1, domResourcePoolCapabilitiesSections.Count);

            var domResourcePoolCapability = domResourcePoolCapabilitiesSections.Single();
            var fdProfileParameterId = domResourcePoolCapability.FieldValues.SingleOrDefault(f => f.FieldDescriptorID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolCapabilities.ProfileParameterID.Id);
            Assert.IsNotNull(fdProfileParameterId);
            Assert.IsTrue(Guid.TryParse(Convert.ToString(fdProfileParameterId.Value.Value), out var profileParameterId));
            Assert.AreEqual(capabilityId1, profileParameterId);

            var fdStringValue = domResourcePoolCapability.FieldValues.SingleOrDefault(f => f.FieldDescriptorID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolCapabilities.StringValue.Id);
            Assert.IsNotNull(fdStringValue);
            var discreteValues = Convert.ToString(fdStringValue.Value.Value).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            Assert.AreEqual(2, discreteValues.Count);
            Assert.IsTrue(discreteValues.Contains("Value 1"));
            Assert.IsTrue(discreteValues.Contains("Value 2"));
        }
    }
}
