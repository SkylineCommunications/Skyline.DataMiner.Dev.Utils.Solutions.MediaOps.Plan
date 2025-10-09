namespace RT_MediaOps.Plan.RST.ResourcePools
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using RT_MediaOps.Plan.RegressionTests;

    using Skyline.DataMiner.MediaOps.Plan.Exceptions;

    using SLDataGateway.API.Collections.Linq;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public sealed class LinkedPoolTests : IDisposable
    {
        private readonly IntegrationTestContext testContext;
        private readonly HashSet<Guid> createdPoolIds = new HashSet<Guid>();

        public LinkedPoolTests()
        {
            testContext = new IntegrationTestContext();
        }

        public void Dispose()
        {
            try
            {
                testContext.Api.ResourcePools.Delete(createdPoolIds.ToArray());
            }
            catch
            {
                // Ignore cleanup errors
            }

            testContext.Dispose();
        }

        [TestMethod]
        public void HappyPath()
        {
            var prefix = Guid.NewGuid().ToString();

            var resourcePool1 = new Skyline.DataMiner.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool1",
            };
            var resourcePool2 = new Skyline.DataMiner.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool2",
            };
            var resourcePool3 = new Skyline.DataMiner.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool3",
            };

            var poolIds = CreateResourcePools(new[] { resourcePool1, resourcePool2 }).ToArray();

            // Create pool with link
            resourcePool3.AddLinkedResourcePool(new Skyline.DataMiner.MediaOps.Plan.API.LinkedResourcePool(poolIds[0]));
            var poolId3 = CreateResourcePool(resourcePool3);

            resourcePool3 = testContext.Api.ResourcePools.Read(poolId3);
            Assert.AreEqual(1, resourcePool3.LinkedResourcePools.Count);

            Assert.AreEqual(Skyline.DataMiner.MediaOps.Plan.API.ResourceSelectionType.Automatic, resourcePool3.LinkedResourcePools.First().SelectionType);
            Assert.AreEqual(poolIds[0], resourcePool3.LinkedResourcePools.First().LinkedResourcePoolId);

            // Update pool with new link
            resourcePool3.AddLinkedResourcePool(new Skyline.DataMiner.MediaOps.Plan.API.LinkedResourcePool(poolIds[1]));
            testContext.Api.ResourcePools.Update(resourcePool3);

            resourcePool3 = testContext.Api.ResourcePools.Read(poolId3);
            Assert.AreEqual(2, resourcePool3.LinkedResourcePools.Count);

            var linkedPoolIds = poolIds.ToList();
            foreach (var link in resourcePool3.LinkedResourcePools)
            {
                Assert.AreEqual(Skyline.DataMiner.MediaOps.Plan.API.ResourceSelectionType.Automatic, link.SelectionType);
                Assert.IsTrue(linkedPoolIds.Contains(link.LinkedResourcePoolId));

                linkedPoolIds.Remove(link.LinkedResourcePoolId);
            }

            // Remove link
            var linkToRemove = resourcePool3.LinkedResourcePools.First(x => x.LinkedResourcePoolId == poolIds[0]);
            resourcePool3.RemoveLinkedResourcePool(linkToRemove);
            testContext.Api.ResourcePools.Update(resourcePool3);

            resourcePool3 = testContext.Api.ResourcePools.Read(poolId3);
            Assert.AreEqual(1, resourcePool3.LinkedResourcePools.Count);

            Assert.AreEqual(Skyline.DataMiner.MediaOps.Plan.API.ResourceSelectionType.Automatic, resourcePool3.LinkedResourcePools.First().SelectionType);
            Assert.AreEqual(poolIds[1], resourcePool3.LinkedResourcePools.First().LinkedResourcePoolId);
        }

        [TestMethod]
        public void CreateWithNotExistingLinkThrowsException()
        {
            var prefix = Guid.NewGuid().ToString();

            var resourcePool = new Skyline.DataMiner.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool",
            };

            var invalidPoolId = Guid.NewGuid();
            resourcePool.AddLinkedResourcePool(new Skyline.DataMiner.MediaOps.Plan.API.LinkedResourcePool(invalidPoolId));

            try
            {
                CreateResourcePool(resourcePool);
            }
            catch (MediaOpsException ex)
            {
                var errorMessage = $"Linked resource pool with ID '{invalidPoolId}' does not exist.";
                Assert.AreEqual(errorMessage, ex.Message);

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var resourcePoolConfigurationError = ex.TraceData.ErrorData.OfType<ResourcePoolConfigurationError>().SingleOrDefault();
                Assert.IsNotNull(resourcePoolConfigurationError);

                Assert.AreEqual(ResourcePoolConfigurationError.Reason.InvalidPoolLink, resourcePoolConfigurationError.ErrorReason);
                Assert.AreEqual(errorMessage, resourcePoolConfigurationError.ErrorMessage);

                return;
            }

            Assert.Fail("Exception not thrown");
        }

        [TestMethod]
        public void UpdateWithNotExistingLinkThrowsException()
        {
            var prefix = Guid.NewGuid().ToString();

            var resourcePool = new Skyline.DataMiner.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool",
            };

            var poolId = CreateResourcePool(resourcePool);
            resourcePool = testContext.Api.ResourcePools.Read(poolId);

            var invalidPoolId = Guid.NewGuid();
            resourcePool.AddLinkedResourcePool(new Skyline.DataMiner.MediaOps.Plan.API.LinkedResourcePool(invalidPoolId));

            try
            {
                testContext.Api.ResourcePools.Update(resourcePool);
            }
            catch (MediaOpsException ex)
            {
                var errorMessage = $"Linked resource pool with ID '{invalidPoolId}' does not exist.";
                Assert.AreEqual(errorMessage, ex.Message);

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var resourcePoolConfigurationError = ex.TraceData.ErrorData.OfType<ResourcePoolConfigurationError>().SingleOrDefault();
                Assert.IsNotNull(resourcePoolConfigurationError);

                Assert.AreEqual(ResourcePoolConfigurationError.Reason.InvalidPoolLink, resourcePoolConfigurationError.ErrorReason);
                Assert.AreEqual(errorMessage, resourcePoolConfigurationError.ErrorMessage);

                return;
            }

            Assert.Fail("Exception not thrown");
        }

        [TestMethod]
        public void DeletePoolUsedAsLink()
        {
            var prefix = Guid.NewGuid().ToString();

            var resourcePool1 = new Skyline.DataMiner.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool1",
            };
            var resourcePool2 = new Skyline.DataMiner.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool2",
            };
            var resourcePool3 = new Skyline.DataMiner.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool3",
            };

            var poolIds = CreateResourcePools(new[] { resourcePool1, resourcePool2 }).ToArray();

            // Create pool with 2 links
            resourcePool3.AddLinkedResourcePool(new Skyline.DataMiner.MediaOps.Plan.API.LinkedResourcePool(poolIds[0]));
            resourcePool3.AddLinkedResourcePool(new Skyline.DataMiner.MediaOps.Plan.API.LinkedResourcePool(poolIds[1]));
            var poolId3 = CreateResourcePool(resourcePool3);

            resourcePool3 = testContext.Api.ResourcePools.Read(poolId3);
            Assert.AreEqual(2, resourcePool3.LinkedResourcePools.Count);

            // Delete linked pool
            testContext.Api.ResourcePools.Delete(poolIds[0]);
            resourcePool3 = testContext.Api.ResourcePools.Read(poolId3);
            Assert.AreEqual(1, resourcePool3.LinkedResourcePools.Count);

            Assert.AreEqual(Skyline.DataMiner.MediaOps.Plan.API.ResourceSelectionType.Automatic, resourcePool3.LinkedResourcePools.First().SelectionType);
            Assert.AreEqual(poolIds[1], resourcePool3.LinkedResourcePools.First().LinkedResourcePoolId);
        }

        private Guid CreateResourcePool(Skyline.DataMiner.MediaOps.Plan.API.ResourcePool resourcePool)
        {
            var poolId = testContext.Api.ResourcePools.Create(resourcePool);
            createdPoolIds.Add(poolId);

            return poolId;
        }

        private IEnumerable<Guid> CreateResourcePools(IEnumerable<Skyline.DataMiner.MediaOps.Plan.API.ResourcePool> resourcePools)
        {
            var poolIds = testContext.Api.ResourcePools.Create(resourcePools);
            foreach (var id in poolIds)
            {
                createdPoolIds.Add(id);
            }
            return poolIds;
        }
    }
}
