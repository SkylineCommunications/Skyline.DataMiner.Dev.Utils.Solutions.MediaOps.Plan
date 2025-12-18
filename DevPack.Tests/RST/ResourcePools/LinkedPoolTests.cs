namespace RT_MediaOps.Plan.RST.ResourcePools
{
    using System;
    using System.Linq;

    using RT_MediaOps.Plan.RegressionTests;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

    [TestClass]
    [TestCategory("IntegrationTest")]
    [DoNotParallelize]
    public sealed class LinkedPoolTests : IDisposable
    {
        private readonly ResourceStudioObjectCreator objectCreator;

        public LinkedPoolTests()
        {
            objectCreator = new ResourceStudioObjectCreator(TestContext.Api);
        }

        private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

        public void Dispose()
        {
            objectCreator.Dispose();
        }

        [TestMethod]
        public void HappyPath()
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

            objectCreator.CreateResourcePools(new[] { resourcePool1, resourcePool2 });

            // Create pool with link
            resourcePool3.AddLinkedResourcePool(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.LinkedResourcePool(resourcePool1.Id));
            objectCreator.CreateResourcePool(resourcePool3);

            resourcePool3 = TestContext.Api.ResourcePools.Read(resourcePool3.Id);
            Assert.AreEqual(1, resourcePool3.LinkedResourcePools.Count);

            Assert.AreEqual(Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceSelectionType.Automatic, resourcePool3.LinkedResourcePools.First().SelectionType);
            Assert.AreEqual(resourcePool1.Id, resourcePool3.LinkedResourcePools.First().LinkedResourcePoolId);

            // Update pool with new link
            resourcePool3.AddLinkedResourcePool(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.LinkedResourcePool(resourcePool2.Id));
            TestContext.Api.ResourcePools.Update(resourcePool3);

            resourcePool3 = TestContext.Api.ResourcePools.Read(resourcePool3.Id);
            Assert.AreEqual(2, resourcePool3.LinkedResourcePools.Count);

            var linkedPoolIds = new List<Guid> { resourcePool1.Id, resourcePool2.Id };
            foreach (var link in resourcePool3.LinkedResourcePools)
            {
                Assert.AreEqual(Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceSelectionType.Automatic, link.SelectionType);
                Assert.IsTrue(linkedPoolIds.Contains(link.LinkedResourcePoolId));

                linkedPoolIds.Remove(link.LinkedResourcePoolId);
            }

            // Remove link
            var linkToRemove = resourcePool3.LinkedResourcePools.First(x => x.LinkedResourcePoolId == resourcePool1.Id);
            resourcePool3.RemoveLinkedResourcePool(linkToRemove);
            TestContext.Api.ResourcePools.Update(resourcePool3);

            resourcePool3 = TestContext.Api.ResourcePools.Read(resourcePool3.Id);
            Assert.AreEqual(1, resourcePool3.LinkedResourcePools.Count);

            Assert.AreEqual(Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceSelectionType.Automatic, resourcePool3.LinkedResourcePools.First().SelectionType);
            Assert.AreEqual(resourcePool2.Id, resourcePool3.LinkedResourcePools.First().LinkedResourcePoolId);
        }

        [TestMethod]
        public void CreateWithNotExistingLinkThrowsException()
        {
            var prefix = Guid.NewGuid().ToString();

            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool",
            };

            var invalidPoolId = Guid.NewGuid();
            resourcePool.AddLinkedResourcePool(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.LinkedResourcePool(invalidPoolId));

            try
            {
                objectCreator.CreateResourcePool(resourcePool);
            }
            catch (MediaOpsException ex)
            {
                var errorMessage = $"Linked resource pool with ID '{invalidPoolId}' does not exist.";
                Assert.AreEqual(errorMessage, ex.Message);

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var resourcePoolConfigurationError = ex.TraceData.ErrorData.OfType<ResourcePoolError>().SingleOrDefault();
                Assert.IsNotNull(resourcePoolConfigurationError);

                var resourcePoolConfigurationInvalidPoolLinkError = resourcePoolConfigurationError as ResourcePoolNotFoundPoolLinkError;
                Assert.IsNotNull(resourcePoolConfigurationInvalidPoolLinkError);
                Assert.AreEqual(invalidPoolId, resourcePoolConfigurationInvalidPoolLinkError.LinkedResourcePoolId);
                Assert.AreEqual(errorMessage, resourcePoolConfigurationError.ErrorMessage);

                return;
            }

            Assert.Fail("Exception not thrown");
        }

        [TestMethod]
        public void UpdateWithNotExistingLinkThrowsException()
        {
            var prefix = Guid.NewGuid().ToString();

            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool",
            };

            objectCreator.CreateResourcePool(resourcePool);
            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);

            var invalidPoolId = Guid.NewGuid();
            resourcePool.AddLinkedResourcePool(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.LinkedResourcePool(invalidPoolId));

            try
            {
                TestContext.Api.ResourcePools.Update(resourcePool);
            }
            catch (MediaOpsException ex)
            {
                var errorMessage = $"Linked resource pool with ID '{invalidPoolId}' does not exist.";
                Assert.AreEqual(errorMessage, ex.Message);

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var resourcePoolConfigurationError = ex.TraceData.ErrorData.OfType<ResourcePoolError>().SingleOrDefault();
                Assert.IsNotNull(resourcePoolConfigurationError);

                var resourcePoolConfigurationInvalidPoolLinkError = resourcePoolConfigurationError as ResourcePoolNotFoundPoolLinkError;
                Assert.IsNotNull(resourcePoolConfigurationInvalidPoolLinkError);
                Assert.AreEqual(invalidPoolId, resourcePoolConfigurationInvalidPoolLinkError.LinkedResourcePoolId);
                Assert.AreEqual(errorMessage, resourcePoolConfigurationError.ErrorMessage);

                return;
            }

            Assert.Fail("Exception not thrown");
        }

        [TestMethod]
        public void DeletePoolUsedAsLink()
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

            objectCreator.CreateResourcePools(new[] { resourcePool1, resourcePool2 });

            // Create pool with 2 links
            resourcePool3.AddLinkedResourcePool(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.LinkedResourcePool(resourcePool1.Id));
            resourcePool3.AddLinkedResourcePool(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.LinkedResourcePool(resourcePool2.Id));
            objectCreator.CreateResourcePool(resourcePool3);

            resourcePool3 = TestContext.Api.ResourcePools.Read(resourcePool3.Id);
            Assert.AreEqual(2, resourcePool3.LinkedResourcePools.Count);

            // Delete linked pool
            TestContext.Api.ResourcePools.Delete(resourcePool1.Id);
            resourcePool3 = TestContext.Api.ResourcePools.Read(resourcePool3.Id);
            Assert.AreEqual(1, resourcePool3.LinkedResourcePools.Count);

            Assert.AreEqual(Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceSelectionType.Automatic, resourcePool3.LinkedResourcePools.First().SelectionType);
            Assert.AreEqual(resourcePool2.Id, resourcePool3.LinkedResourcePools.First().LinkedResourcePoolId);
        }

        [TestMethod]
        public void CreateWithDifferentLinkTypes()
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

            objectCreator.CreateResourcePools(new[] { resourcePool1, resourcePool2 });

            resourcePool3.AddLinkedResourcePool(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.LinkedResourcePool(resourcePool1.Id) { SelectionType = Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceSelectionType.Automatic });
            resourcePool3.AddLinkedResourcePool(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.LinkedResourcePool(resourcePool2.Id) { SelectionType = Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceSelectionType.Manual });
            objectCreator.CreateResourcePool(resourcePool3);

            resourcePool3 = TestContext.Api.ResourcePools.Read(resourcePool3.Id);
            Assert.AreEqual(2, resourcePool3.LinkedResourcePools.Count);

            var expectedPoolData = new Dictionary<Guid, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceSelectionType>
            {
                { resourcePool1.Id, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceSelectionType.Automatic },
                { resourcePool2.Id, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceSelectionType.Manual },
            };
            foreach (var link in resourcePool3.LinkedResourcePools)
            {
                Assert.IsTrue(expectedPoolData.TryGetValue(link.LinkedResourcePoolId, out var expectedSelectionType));
                Assert.AreEqual(expectedSelectionType, link.SelectionType);
            }
        }
    }
}
