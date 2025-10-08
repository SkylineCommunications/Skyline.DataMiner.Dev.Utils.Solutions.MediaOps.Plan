namespace RT_MediaOps.Plan.RST.ResourcePools
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;

    using Alphaleonis.Win32.Filesystem;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using RT_MediaOps.Plan.RegressionTests;

    using Skyline.DataMiner.MediaOps.Plan.Exceptions;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public sealed class BasicTests : IDisposable
    {
        private readonly IntegrationTestContext testContext;
        private readonly HashSet<Guid> createdPoolIds = new HashSet<Guid>();

        public BasicTests()
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
        public void HappyPathCrud()
        {
            // Create pool and validate result
            var poolId = Guid.NewGuid();
            var name = $"{poolId}_ResourcePool";

            var resourcePool = new Skyline.DataMiner.MediaOps.Plan.API.ResourcePool(poolId)
            {
                Name = name,
            };

            var returnedId = CreateResourcePool(resourcePool);
            Assert.AreEqual(poolId, returnedId);

            var returnedResourcePool = testContext.Api.ResourcePools.Read(poolId);
            Assert.IsNotNull(returnedResourcePool);
            Assert.AreEqual(name, returnedResourcePool.Name);

            // Set pool to complete and validate result
            testContext.Api.ResourcePools.MoveTo(returnedResourcePool, Skyline.DataMiner.MediaOps.Plan.API.ResourcePoolState.Complete);
            returnedResourcePool = testContext.Api.ResourcePools.Read(poolId);
            Assert.IsNotNull(returnedResourcePool);
            Assert.AreEqual(Skyline.DataMiner.MediaOps.Plan.API.ResourcePoolState.Complete, returnedResourcePool.State);

            // Update pool and validate result
            var updatedName = name + "_updated";
            returnedResourcePool.Name = updatedName;
            testContext.Api.ResourcePools.Update(returnedResourcePool);
            returnedResourcePool = testContext.Api.ResourcePools.Read(poolId);
            Assert.IsNotNull(returnedResourcePool);
            Assert.AreEqual(updatedName, returnedResourcePool.Name);

            // Deprecate pool
            testContext.Api.ResourcePools.MoveTo(returnedResourcePool, Skyline.DataMiner.MediaOps.Plan.API.ResourcePoolState.Deprecated);
            returnedResourcePool = testContext.Api.ResourcePools.Read(poolId);
            Assert.IsNotNull(returnedResourcePool);
            Assert.AreEqual(Skyline.DataMiner.MediaOps.Plan.API.ResourcePoolState.Deprecated, returnedResourcePool.State);

            // Delete pool and validate it is gone
            testContext.Api.ResourcePools.Delete(returnedResourcePool);
            returnedResourcePool = testContext.Api.ResourcePools.Read(poolId);
            Assert.IsNull(returnedResourcePool);
        }

        [TestMethod]
        public void CreateWithExistingIdThrowsException()
        {
            var poolId = Guid.NewGuid();

            var resourcePool1 = new Skyline.DataMiner.MediaOps.Plan.API.ResourcePool(poolId)
            {
                Name = $"{poolId}_ResourcePool_1",
            };

            var resourcePool2 = new Skyline.DataMiner.MediaOps.Plan.API.ResourcePool(poolId)
            {
                Name = $"{poolId}_ResourcePool_2",
            };

            CreateResourcePool(resourcePool1);
            try
            {
                CreateResourcePool(resourcePool2);
            }
            catch (MediaOpsException ex)
            {
                StringAssert.Contains(ex.Message, "ID is already in use.");

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var resourcePoolConfigurationError = ex.TraceData.ErrorData.OfType<ResourcePoolConfigurationError>().SingleOrDefault();
                Assert.IsNotNull(resourcePoolConfigurationError);

                Assert.AreEqual(ResourcePoolConfigurationError.Reason.IdInUse, resourcePoolConfigurationError.ErrorReason);
                Assert.AreEqual("ID is already in use.", resourcePoolConfigurationError.ErrorMessage);

                return;
            }

            Assert.Fail("Exception not thrown");
        }

        [TestMethod]
        public void CreateWithSameIdInBulkThrowsException()
        {
            var poolId = Guid.NewGuid();

            var resourcePool1 = new Skyline.DataMiner.MediaOps.Plan.API.ResourcePool(poolId)
            {
                Name = $"{poolId}_ResourcePool_1",
            };

            var resourcePool2 = new Skyline.DataMiner.MediaOps.Plan.API.ResourcePool(poolId)
            {
                Name = $"{poolId}_ResourcePool_2",
            };

            try
            {
                CreateResourcePools(new[] {resourcePool1, resourcePool2});
            }
            catch (MediaOpsBulkException<Guid> ex)
            {
                if (!ex.Result.TraceDataPerItem.TryGetValue(poolId, out var traceData))
                {
                    Assert.Fail("No trace data found for the failed ID");
                }

                Assert.AreEqual(2, traceData.ErrorData.Count);
                var resourcePoolConfigurationErrors = traceData.ErrorData.OfType<ResourcePoolConfigurationError>();
                Assert.AreEqual(2, resourcePoolConfigurationErrors.Count());

                var errorMessages = new List<string>
                {
                   $"Resource pool '{resourcePool1.Name}' has a duplicate ID.",
                   $"Resource pool '{resourcePool2.Name}' has a duplicate ID."
                };

                foreach (var error in resourcePoolConfigurationErrors)
                {
                    Assert.AreEqual(ResourcePoolConfigurationError.Reason.DuplicateId, error.ErrorReason);
                    Assert.IsTrue(errorMessages.Contains(error.ErrorMessage));

                    errorMessages.Remove(error.ErrorMessage);
                }

                return;
            }

            Assert.Fail("Exception not thrown");
        }

        [TestMethod]
        public void CreateWithExistingNameThrowsException()
        {
            var prefix = Guid.NewGuid().ToString();

            var resourcePool1 = new Skyline.DataMiner.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool",
            };

            var resourcePool2 = new Skyline.DataMiner.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool",
            };

            CreateResourcePool(resourcePool1);
            try
            {
                CreateResourcePool(resourcePool2);
            }
            catch (MediaOpsException ex)
            {
                StringAssert.Contains(ex.Message, "Name is already in use.");

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var resourcePoolConfigurationError = ex.TraceData.ErrorData.OfType<ResourcePoolConfigurationError>().SingleOrDefault();
                Assert.IsNotNull(resourcePoolConfigurationError);

                Assert.AreEqual(ResourcePoolConfigurationError.Reason.NameExists, resourcePoolConfigurationError.ErrorReason);
                Assert.AreEqual("Name is already in use.", resourcePoolConfigurationError.ErrorMessage);

                return;
            }

            Assert.Fail("Exception not thrown");
        }

        [TestMethod]
        public void CreateWithSameNameInBulkThrowsException()
        {
            var prefix = Guid.NewGuid().ToString();

            var resourcePool1 = new Skyline.DataMiner.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool",
            };

            var resourcePool2 = new Skyline.DataMiner.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool",
            };

            try
            {
                CreateResourcePools(new[] { resourcePool1, resourcePool2 });
            }
            catch (MediaOpsBulkException<Guid> ex)
            {
                Assert.AreEqual(2, ex.Result.TraceDataPerItem.Count);

                foreach (var traceData in ex.Result.TraceDataPerItem.Values)
                {
                    Assert.AreEqual(1, traceData.ErrorData.Count);
                    var resourcePoolConfigurationError = traceData.ErrorData.OfType<ResourcePoolConfigurationError>().SingleOrDefault();
                    Assert.IsNotNull(resourcePoolConfigurationError);

                    Assert.AreEqual(ResourcePoolConfigurationError.Reason.DuplicateName, resourcePoolConfigurationError.ErrorReason);
                    Assert.AreEqual($"Resource pool '{resourcePool1.Name}' has a duplicate name.", resourcePoolConfigurationError.ErrorMessage);
                }

                return;
            }

            Assert.Fail("Exception not thrown");
        }

        [TestMethod]
        public void UpdateToSameNameThrowsException()
        {
            var prefix = Guid.NewGuid().ToString();

            var resourcePool1 = new Skyline.DataMiner.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool_1",
            };

            var resourcePool2 = new Skyline.DataMiner.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool_2",
            };

            var id1 = CreateResourcePool(resourcePool1);
            var id2 = CreateResourcePool(resourcePool2);

            var toUpdate = testContext.Api.ResourcePools.Read(id2);
            toUpdate.Name = resourcePool1.Name;

            try
            {
                testContext.Api.ResourcePools.Update(toUpdate);
            }
            catch (MediaOpsException ex)
            {
                StringAssert.Contains(ex.Message, "Name is already in use.");

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var resourcePoolConfigurationError = ex.TraceData.ErrorData.OfType<ResourcePoolConfigurationError>().SingleOrDefault();
                Assert.IsNotNull(resourcePoolConfigurationError);

                Assert.AreEqual(ResourcePoolConfigurationError.Reason.NameExists, resourcePoolConfigurationError.ErrorReason);
                Assert.AreEqual("Name is already in use.", resourcePoolConfigurationError.ErrorMessage);

                return;
            }

            Assert.Fail("Exception not thrown");
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
