namespace RT_MediaOps.Plan.RST.Resources
{
    using System;
    using System.Linq;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using RT_MediaOps.Plan.RegressionTests;

    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public sealed class ResourcePoolAssignmentTests : IDisposable
    {
        private readonly TestObjectCreator objectCreator;

        public ResourcePoolAssignmentTests()
        {
            objectCreator = new TestObjectCreator(TestContext);
        }

        private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

        public void Dispose()
        {
            objectCreator.Dispose();
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

            objectCreator.CreateResourcePools(new[] { resourcePool1, resourcePool2, resourcePool3 });

            // Create resource with 2 pools assigned
            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };
            unmanagedResource.SetPools([resourcePool1, resourcePool2]);
            objectCreator.CreateResource(unmanagedResource);

            var resource = TestContext.Api.Resources.Read(unmanagedResource.ID);
            Assert.AreEqual(2, resource.ResourcePoolIds.Count);
            Assert.IsTrue(resource.ResourcePoolIds.Contains(resourcePool1.ID));
            Assert.IsTrue(resource.ResourcePoolIds.Contains(resourcePool2.ID));

            // Update resource with additional pool assignment
            resource.AssignToPool(resourcePool3);
            TestContext.Api.Resources.Update(resource);

            resource = TestContext.Api.Resources.Read(unmanagedResource.ID);
            Assert.AreEqual(3, resource.ResourcePoolIds.Count);
            Assert.IsTrue(resource.ResourcePoolIds.Contains(resourcePool1.ID));
            Assert.IsTrue(resource.ResourcePoolIds.Contains(resourcePool2.ID));
            Assert.IsTrue(resource.ResourcePoolIds.Contains(resourcePool3.ID));

            // Remove pool
            resource.UnassignFromPool(resourcePool2);
            TestContext.Api.Resources.Update(resource);

            resource = TestContext.Api.Resources.Read(unmanagedResource.ID);
            Assert.AreEqual(2, resource.ResourcePoolIds.Count);
            Assert.IsTrue(resource.ResourcePoolIds.Contains(resourcePool1.ID));
            Assert.IsTrue(resource.ResourcePoolIds.Contains(resourcePool3.ID));
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

            objectCreator.CreateResourcePools(new[] { resourcePool1, resourcePool2, resourcePool3 });
            TestContext.Api.ResourcePools.Complete(resourcePool1.ID);
            TestContext.Api.ResourcePools.Complete(resourcePool2.ID);
            TestContext.Api.ResourcePools.Complete(resourcePool3.ID);

            resourcePool1 = TestContext.Api.ResourcePools.Read(resourcePool1.ID);
            resourcePool2 = TestContext.Api.ResourcePools.Read(resourcePool2.ID);
            resourcePool3 = TestContext.Api.ResourcePools.Read(resourcePool3.ID);

            // Create resource with 2 pools assigned
            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
            };
            unmanagedResource.SetPools([resourcePool1, resourcePool2]);
            objectCreator.CreateResource(unmanagedResource);

            var resource = TestContext.Api.Resources.Read(unmanagedResource.ID);
            Assert.AreEqual(2, resource.ResourcePoolIds.Count);
            Assert.IsTrue(resource.ResourcePoolIds.Contains(resourcePool1.ID));
            Assert.IsTrue(resource.ResourcePoolIds.Contains(resourcePool2.ID));

            // Set resource to complete
            TestContext.Api.Resources.Complete(resource.ID);

            resource = TestContext.Api.Resources.Read(unmanagedResource.ID);
            var coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);
            Assert.AreEqual(2, coreResource.PoolGUIDs.Count);
            Assert.IsTrue(coreResource.PoolGUIDs.Contains(resourcePool1.CoreResourcePoolId));
            Assert.IsTrue(coreResource.PoolGUIDs.Contains(resourcePool2.CoreResourcePoolId));

            // Update resource with additional pool assignment
            resource.AssignToPool(resourcePool3);
            TestContext.Api.Resources.Update(resource);

            resource = TestContext.Api.Resources.Read(unmanagedResource.ID);
            Assert.AreEqual(3, resource.ResourcePoolIds.Count);
            Assert.IsTrue(resource.ResourcePoolIds.Contains(resourcePool1.ID));
            Assert.IsTrue(resource.ResourcePoolIds.Contains(resourcePool2.ID));
            Assert.IsTrue(resource.ResourcePoolIds.Contains(resourcePool3.ID));

            coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);
            Assert.AreEqual(3, coreResource.PoolGUIDs.Count);
            Assert.IsTrue(coreResource.PoolGUIDs.Contains(resourcePool1.CoreResourcePoolId));
            Assert.IsTrue(coreResource.PoolGUIDs.Contains(resourcePool2.CoreResourcePoolId));
            Assert.IsTrue(coreResource.PoolGUIDs.Contains(resourcePool3.CoreResourcePoolId));

            // Remove pool
            resource.UnassignFromPool(resourcePool2);
            TestContext.Api.Resources.Update(resource);

            resource = TestContext.Api.Resources.Read(unmanagedResource.ID);
            Assert.AreEqual(2, resource.ResourcePoolIds.Count);
            Assert.IsTrue(resource.ResourcePoolIds.Contains(resourcePool1.ID));
            Assert.IsTrue(resource.ResourcePoolIds.Contains(resourcePool3.ID));

            coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);
            Assert.AreEqual(2, coreResource.PoolGUIDs.Count);
            Assert.IsTrue(coreResource.PoolGUIDs.Contains(resourcePool1.CoreResourcePoolId));
            Assert.IsTrue(coreResource.PoolGUIDs.Contains(resourcePool3.CoreResourcePoolId));
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
            Assert.AreEqual(0, unmanagedResource.ResourcePoolIds.Count);

            unmanagedResource.SetPools(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool[] { });
            Assert.AreEqual(0, unmanagedResource.ResourcePoolIds.Count);
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

            Assert.IsFalse(unmanagedResource.ResourcePoolIds.Contains(poolId));
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

            var resource = TestContext.Api.Resources.Read(unmanagedResource.ID);

            var poolId = Guid.NewGuid();
            resource.AssignToPool(poolId);

            try
            {
                TestContext.Api.Resources.Update(resource);
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
