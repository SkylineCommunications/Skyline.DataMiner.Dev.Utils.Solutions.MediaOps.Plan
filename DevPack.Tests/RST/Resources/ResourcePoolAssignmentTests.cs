namespace RT_MediaOps.Plan.RST.Resources
{
    using System;
    using System.Linq;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using RT_MediaOps.Plan.RegressionTests;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public sealed class ResourcePoolAssignmentTests : IDisposable
    {
        private readonly IntegrationTestContext testContext;
        private readonly ResourceStudioObjectCreator objectCreator;

        public ResourcePoolAssignmentTests()
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
        public void HappyPathWithDraftObjects()
        {
            var prefix = Guid.NewGuid().ToString();

            var resourcePool1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool1",
            };
            var resourcePool2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool2",
            };
            var resourcePool3 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool3",
            };

            var poolIds = objectCreator.CreateResourcePools(new[] { resourcePool1, resourcePool2, resourcePool3 }).ToArray();

            // Create resource with 2 pools assigned
            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };
            unmanagedResource.SetPools([resourcePool1, resourcePool2]);
            objectCreator.CreateResource(unmanagedResource);

            var resource = testContext.Api.Resources.Read(unmanagedResource.Id);
            Assert.AreEqual(2, resource.AssignedResourcePoolIds.Count);
            Assert.IsTrue(resource.AssignedResourcePoolIds.Contains(resourcePool1.Id));
            Assert.IsTrue(resource.AssignedResourcePoolIds.Contains(resourcePool2.Id));

            // Update resource with additional pool assignment
            resource.AssignToPool(resourcePool3);
            testContext.Api.Resources.Update(resource);

            resource = testContext.Api.Resources.Read(unmanagedResource.Id);
            Assert.AreEqual(3, resource.AssignedResourcePoolIds.Count);
            Assert.IsTrue(resource.AssignedResourcePoolIds.Contains(resourcePool1.Id));
            Assert.IsTrue(resource.AssignedResourcePoolIds.Contains(resourcePool2.Id));
            Assert.IsTrue(resource.AssignedResourcePoolIds.Contains(resourcePool3.Id));

            // Remove pool
            resource.UnassignFromPool(resourcePool2);
            testContext.Api.Resources.Update(resource);

            resource = testContext.Api.Resources.Read(unmanagedResource.Id);
            Assert.AreEqual(2, resource.AssignedResourcePoolIds.Count);
            Assert.IsTrue(resource.AssignedResourcePoolIds.Contains(resourcePool1.Id));
            Assert.IsTrue(resource.AssignedResourcePoolIds.Contains(resourcePool3.Id));
        }

        [TestMethod]
        public void HappyPathWithCompleteObjects()
        {
            var prefix = Guid.NewGuid().ToString();

            var resourcePool1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool1",
            };
            var resourcePool2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool2",
            };
            var resourcePool3 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool3",
            };

            var poolIds = objectCreator.CreateResourcePools(new[] { resourcePool1, resourcePool2, resourcePool3 }).ToArray();
            foreach (var poolId in poolIds)
            {
                testContext.Api.ResourcePools.MoveTo(poolId, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePoolState.Complete);
            }

            // Create resource with 2 pools assigned
            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };
            unmanagedResource.SetPools([resourcePool1, resourcePool2]);
            objectCreator.CreateResource(unmanagedResource);

            var resource = testContext.Api.Resources.Read(unmanagedResource.Id);
            Assert.AreEqual(2, resource.AssignedResourcePoolIds.Count);
            Assert.IsTrue(resource.AssignedResourcePoolIds.Contains(resourcePool1.Id));
            Assert.IsTrue(resource.AssignedResourcePoolIds.Contains(resourcePool2.Id));

            // Set resource to complete
            testContext.Api.Resources.MoveTo(resource.Id, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceState.Complete);

            // Update resource with additional pool assignment
            resource.AssignToPool(resourcePool3);
            testContext.Api.Resources.Update(resource);

            resource = testContext.Api.Resources.Read(unmanagedResource.Id);
            Assert.AreEqual(3, resource.AssignedResourcePoolIds.Count);
            Assert.IsTrue(resource.AssignedResourcePoolIds.Contains(resourcePool1.Id));
            Assert.IsTrue(resource.AssignedResourcePoolIds.Contains(resourcePool2.Id));
            Assert.IsTrue(resource.AssignedResourcePoolIds.Contains(resourcePool3.Id));

            // Remove pool
            resource.UnassignFromPool(resourcePool2);
            testContext.Api.Resources.Update(resource);

            resource = testContext.Api.Resources.Read(unmanagedResource.Id);
            Assert.AreEqual(2, resource.AssignedResourcePoolIds.Count);
            Assert.IsTrue(resource.AssignedResourcePoolIds.Contains(resourcePool1.Id));
            Assert.IsTrue(resource.AssignedResourcePoolIds.Contains(resourcePool3.Id));
        }

        [TestMethod]
        public void SetPools_NullCollection()
        {
            var prefix = Guid.NewGuid().ToString();

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };

            Assert.ThrowsException<ArgumentNullException>(() => unmanagedResource.SetPools((Guid[]?)null));
            Assert.ThrowsException<ArgumentNullException>(() => unmanagedResource.SetPools((Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool[]?)null));
        }

        [TestMethod]
        public void SetPools_EmptyCollection()
        {
            var prefix = Guid.NewGuid().ToString();

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };
            unmanagedResource.SetPools(new Guid[] { });
            Assert.AreEqual(0, unmanagedResource.AssignedResourcePoolIds.Count);

            unmanagedResource.SetPools(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool[] { });
            Assert.AreEqual(0, unmanagedResource.AssignedResourcePoolIds.Count);
        }

        [TestMethod]
        public void SetPools_CollectionWithNulls()
        {
            var prefix = Guid.NewGuid().ToString();

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };

            Assert.ThrowsException<ArgumentException>(() => unmanagedResource.SetPools(new Guid[] { Guid.NewGuid(), Guid.Empty }));
            Assert.ThrowsException<ArgumentException>(() => unmanagedResource.SetPools(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool?[] { new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool(), null }));
        }

        [TestMethod]
        public void AssignToPool_Null()
        {
            var prefix = Guid.NewGuid().ToString();

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };

            Assert.ThrowsException<ArgumentNullException>(() => unmanagedResource.AssignToPool(null));
            Assert.ThrowsException<ArgumentException>(() => unmanagedResource.AssignToPool(Guid.Empty));
        }

        [TestMethod]
        public void UnassignFromPool_Null()
        {
            var prefix = Guid.NewGuid().ToString();

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };

            Assert.ThrowsException<ArgumentNullException>(() => unmanagedResource.UnassignFromPool(null));
            Assert.ThrowsException<ArgumentException>(() => unmanagedResource.UnassignFromPool(Guid.Empty));
        }

        [TestMethod]
        public void UnassignFromPool_NotExistingId()
        {
            var prefix = Guid.NewGuid().ToString();

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };

            var poolId = Guid.NewGuid();
            unmanagedResource.UnassignFromPool(poolId);

            Assert.IsFalse(unmanagedResource.AssignedResourcePoolIds.Contains(poolId));
        }

        [TestMethod]
        public void CreateWithNotExistingIdThrowsException()
        {
            var prefix = Guid.NewGuid().ToString();

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };

            var poolId = Guid.NewGuid();
            unmanagedResource.AssignToPool(poolId);

            try
            {
                objectCreator.CreateResource(unmanagedResource);
            }
            catch (MediaOpsException ex)
            {
                StringAssert.Equals(ex.Message, $"Resource Pool with ID '{poolId}' not found.");

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void UpdateWithNotExistingIdThrowsException()
        {
            var prefix = Guid.NewGuid().ToString();

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };
            objectCreator.CreateResource(unmanagedResource);

            var resource = testContext.Api.Resources.Read(unmanagedResource.Id);

            var poolId = Guid.NewGuid();
            resource.AssignToPool(poolId);

            try
            {
                testContext.Api.Resources.Update(resource);
            }
            catch (MediaOpsException ex)
            {
                StringAssert.Equals(ex.Message, $"Resource Pool with ID '{poolId}' not found.");

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }
    }
}
