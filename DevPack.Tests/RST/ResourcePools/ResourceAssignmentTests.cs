namespace RT_MediaOps.Plan.RST.ResourcePools
{
    using System;
    using System.Linq;

    using RT_MediaOps.Plan.RegressionTests;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public sealed class ResourceAssignmentTests : IDisposable
    {
        private readonly ResourceStudioObjectCreator objectCreator;

        public ResourceAssignmentTests()
        {
            objectCreator = new ResourceStudioObjectCreator(TestContext.Api);
        }

        private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

        public void Dispose()
        {
            objectCreator.Dispose();
        }

        [TestMethod]
        public void HappyPathWithDraftObjects()
        {
            var prefix = Guid.NewGuid();

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
            var resources = TestContext.Api.Resources.Read([unmanagedResource1.Id, unmanagedResource2.Id, unmanagedResource3.Id]).Values;

            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool",
            };
            objectCreator.CreateResourcePool(resourcePool);

            // Assign resources to pool.
            TestContext.Api.ResourcePools.AssignResourcesToPool(resourcePool.Id, resources);

            resources = TestContext.Api.Resources.Read([unmanagedResource1.Id, unmanagedResource2.Id, unmanagedResource3.Id]).Values;
            foreach (var resource in resources)
            {
                Assert.AreEqual(1, resource.AssignedResourcePoolIds.Count);
                Assert.IsTrue(resource.AssignedResourcePoolIds.Contains(resourcePool.Id));
                Assert.AreEqual(Guid.Empty, resource.CoreResourceId);
            }

            // Remove resources from pool.
            TestContext.Api.ResourcePools.UnassignResourcesFromPool(resourcePool.Id, resources.Skip(1));

            resources = TestContext.Api.Resources.Read([unmanagedResource1.Id, unmanagedResource2.Id, unmanagedResource3.Id]).Values;
            int resourceIndex = 0;
            foreach (var resource in resources)
            {
                if (resourceIndex == 0)
                {
                    Assert.AreEqual(1, resource.AssignedResourcePoolIds.Count);
                    Assert.IsTrue(resource.AssignedResourcePoolIds.Contains(resourcePool.Id));
                }
                else
                {
                    Assert.AreEqual(0, resource.AssignedResourcePoolIds.Count);
                    Assert.IsFalse(resource.AssignedResourcePoolIds.Contains(resourcePool.Id));
                }

                Assert.AreEqual(Guid.Empty, resource.CoreResourceId);

                resourceIndex++;
            }
        }

        [TestMethod]
        public void HappyPathWithCompletedObjects()
        {
            // THIS METHOD WILL FAIL AS LONG AS RESOURCE MANAGEMENT CRUD IS PRESENT.
            var prefix = Guid.NewGuid();

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

            TestContext.Api.Resources.MoveTo(unmanagedResource1.Id, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceState.Complete);
            TestContext.Api.Resources.MoveTo(unmanagedResource2.Id, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceState.Complete);
            TestContext.Api.Resources.MoveTo(unmanagedResource3.Id, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceState.Complete);

            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool",
            };
            objectCreator.CreateResourcePool(resourcePool);
            TestContext.Api.ResourcePools.MoveTo(resourcePool.Id, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePoolState.Complete);

            var resources = TestContext.Api.Resources.Read([unmanagedResource1.Id, unmanagedResource2.Id, unmanagedResource3.Id]).Values;
            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);
            Assert.AreNotEqual(Guid.Empty, resourcePool.CoreResourcePoolId);

            // Assign resources to pool.
            TestContext.Api.ResourcePools.AssignResourcesToPool(resourcePool.Id, resources);

            resources = TestContext.Api.Resources.Read([unmanagedResource1.Id, unmanagedResource2.Id, unmanagedResource3.Id]).Values;
            foreach (var resource in resources)
            {
                Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);
                var coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);
                Assert.AreEqual(1, coreResource.PoolGUIDs.Count);
                Assert.IsTrue(coreResource.PoolGUIDs.Contains(resourcePool.CoreResourcePoolId));
            }

            // Remove resources from pool.
            TestContext.Api.ResourcePools.UnassignResourcesFromPool(resourcePool.Id, resources.Skip(1));

            resources = TestContext.Api.Resources.Read([unmanagedResource1.Id, unmanagedResource2.Id, unmanagedResource3.Id]).Values;
            int resourceIndex = 0;
            foreach (var resource in resources)
            {
                Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);
                var coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);

                if (resourceIndex == 0)
                {
                    Assert.AreEqual(1, coreResource.PoolGUIDs.Count);
                    Assert.IsTrue(coreResource.PoolGUIDs.Contains(resourcePool.CoreResourcePoolId));
                }
                else
                {
                    Assert.AreEqual(0, coreResource.PoolGUIDs.Count);
                    Assert.IsFalse(coreResource.PoolGUIDs.Contains(resourcePool.CoreResourcePoolId));
                }

                resourceIndex++;
            }
        }

        [TestMethod]
        public void DraftPoolWithMixedResourcesAndMovePoolToCompleted()
        {
            var prefix = Guid.NewGuid();

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
            TestContext.Api.Resources.MoveTo(unmanagedResource1.Id, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceState.Complete);
            TestContext.Api.Resources.MoveTo(unmanagedResource3.Id, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceState.Complete);

            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_Resource Pool",
            };
            objectCreator.CreateResourcePool(resourcePool);

            var resources = TestContext.Api.Resources.Read([unmanagedResource1.Id, unmanagedResource2.Id, unmanagedResource3.Id]).Values;
            TestContext.Api.ResourcePools.AssignResourcesToPool(resourcePool.Id, resources);

            // Set pool to completed.
            TestContext.Api.ResourcePools.MoveTo(resourcePool.Id, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePoolState.Complete);

            resources = TestContext.Api.Resources.Read([unmanagedResource1.Id, unmanagedResource2.Id, unmanagedResource3.Id]).Values;
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
                Assert.AreEqual(1, coreResource.PoolGUIDs.Count);
                Assert.IsTrue(coreResource.PoolGUIDs.Contains(resourcePool.CoreResourcePoolId));
            }
        }
    }
}
