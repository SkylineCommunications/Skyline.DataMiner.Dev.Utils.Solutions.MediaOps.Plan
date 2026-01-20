namespace RT_MediaOps.Plan.RST.ResourcePools
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using RT_MediaOps.Plan.RegressionTests;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.Utils.Categories.API.Objects;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public sealed class BasicTests : IDisposable
    {
        private readonly TestObjectCreator objectCreator;

        public BasicTests()
        {
            objectCreator = new TestObjectCreator(TestContext);
        }

        private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

        public void Dispose()
        {
            objectCreator.Dispose();
        }

        [TestMethod]
        public void HappyPathCrud()
        {
            // Create pool and validate result
            var poolId = Guid.NewGuid();
            var name = $"{poolId}_ResourcePool";

            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool(poolId)
            {
                Name = name,
            };

            objectCreator.CreateResourcePool(resourcePool);

            var returnedResourcePool = TestContext.Api.ResourcePools.Read(poolId);
            Assert.IsNotNull(returnedResourcePool);
            Assert.AreEqual(name, returnedResourcePool.Name);

            // Set pool to complete and validate result
            TestContext.Api.ResourcePools.MoveTo(returnedResourcePool, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePoolState.Complete);
            returnedResourcePool = TestContext.Api.ResourcePools.Read(poolId);
            Assert.IsNotNull(returnedResourcePool);
            Assert.AreEqual(Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePoolState.Complete, returnedResourcePool.State);

            // Update pool and validate result
            var updatedName = name + "_updated";
            returnedResourcePool.Name = updatedName;
            TestContext.Api.ResourcePools.Update(returnedResourcePool);
            returnedResourcePool = TestContext.Api.ResourcePools.Read(poolId);
            Assert.IsNotNull(returnedResourcePool);
            Assert.AreEqual(updatedName, returnedResourcePool.Name);

            // Deprecate pool
            TestContext.Api.ResourcePools.MoveTo(returnedResourcePool, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePoolState.Deprecated);
            returnedResourcePool = TestContext.Api.ResourcePools.Read(poolId);
            Assert.IsNotNull(returnedResourcePool);
            Assert.AreEqual(Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePoolState.Deprecated, returnedResourcePool.State);

            // Delete pool and validate it is gone
            TestContext.Api.ResourcePools.Delete(returnedResourcePool);
            returnedResourcePool = TestContext.Api.ResourcePools.Read(poolId);
            Assert.IsNull(returnedResourcePool);
        }

        [TestMethod]
        public void CreateWithExistingIdThrowsException()
        {
            var poolId = Guid.NewGuid();

            var resourcePool1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool(poolId)
            {
                Name = $"{poolId}_ResourcePool_1",
            };

            var resourcePool2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool(poolId)
            {
                Name = $"{poolId}_ResourcePool_2",
            };

            objectCreator.CreateResourcePool(resourcePool1);
            try
            {
                objectCreator.CreateResourcePool(resourcePool2);
            }
            catch (MediaOpsException ex)
            {
                StringAssert.Contains(ex.Message, "ID is already in use.");

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var resourcePoolConfigurationError = ex.TraceData.ErrorData.OfType<ResourcePoolError>().SingleOrDefault();
                Assert.IsNotNull(resourcePoolConfigurationError);

                var resourcePoolConfigurationIdInUseError = resourcePoolConfigurationError as ResourcePoolIdInUseError;
                Assert.IsNotNull(resourcePoolConfigurationIdInUseError);
                Assert.AreEqual(poolId, resourcePoolConfigurationIdInUseError.Id);
                Assert.AreEqual("ID is already in use.", resourcePoolConfigurationError.ErrorMessage);

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void CreateWithSameIdInBulkThrowsException()
        {
            var poolId = Guid.NewGuid();

            var resourcePool1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool(poolId)
            {
                Name = $"{poolId}_ResourcePool_1",
            };

            var resourcePool2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool(poolId)
            {
                Name = $"{poolId}_ResourcePool_2",
            };

            try
            {
                objectCreator.CreateResourcePools(new[] { resourcePool1, resourcePool2 });
            }
            catch (MediaOpsBulkException<Guid> ex)
            {
                if (!ex.Result.TraceDataPerItem.TryGetValue(poolId, out var traceData))
                {
                    Assert.Fail("No trace data found for the failed ID");
                }

                Assert.AreEqual(2, traceData.ErrorData.Count);
                var resourcePoolConfigurationErrors = traceData.ErrorData.OfType<ResourcePoolError>();
                Assert.AreEqual(2, resourcePoolConfigurationErrors.Count());

                var errorMessages = new List<string>
                {
                   $"Resource pool '{resourcePool1.Name}' has a duplicate ID.",
                   $"Resource pool '{resourcePool2.Name}' has a duplicate ID."
                };

                foreach (var error in resourcePoolConfigurationErrors)
                {
                    var resourcePoolConfigurationDuplicateIdError = error as ResourcePoolDuplicateIdError;
                    Assert.IsNotNull(resourcePoolConfigurationDuplicateIdError);
                    Assert.AreEqual(poolId, resourcePoolConfigurationDuplicateIdError.Id);
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

            var resourcePool1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool",
            };

            var resourcePool2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool",
            };

            objectCreator.CreateResourcePool(resourcePool1);
            try
            {
                objectCreator.CreateResourcePool(resourcePool2);
            }
            catch (MediaOpsException ex)
            {
                StringAssert.Contains(ex.Message, "Name is already in use.");

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var resourcePoolConfigurationError = ex.TraceData.ErrorData.OfType<ResourcePoolError>().SingleOrDefault();
                Assert.IsNotNull(resourcePoolConfigurationError);

                var resourcePoolConfigurationNameExistsError = resourcePoolConfigurationError as ResourcePoolNameExistsError;
                Assert.IsNotNull(resourcePoolConfigurationNameExistsError);
                Assert.AreEqual(resourcePool2.Id, resourcePoolConfigurationNameExistsError.Id);
                Assert.AreEqual(resourcePool2.Name, resourcePoolConfigurationNameExistsError.Name);
                Assert.AreEqual("Name is already in use.", resourcePoolConfigurationError.ErrorMessage);

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void CreateWithSameNameInBulkThrowsException()
        {
            var prefix = Guid.NewGuid().ToString();

            var resourcePool1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool",
            };

            var resourcePool2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool",
            };

            try
            {
                objectCreator.CreateResourcePools(new[] { resourcePool1, resourcePool2 });
            }
            catch (MediaOpsBulkException<Guid> ex)
            {
                Assert.AreEqual(2, ex.Result.TraceDataPerItem.Count);

                foreach (var traceData in ex.Result.TraceDataPerItem.Values)
                {
                    Assert.AreEqual(1, traceData.ErrorData.Count);
                    var resourcePoolConfigurationError = traceData.ErrorData.OfType<ResourcePoolError>().SingleOrDefault();
                    Assert.IsNotNull(resourcePoolConfigurationError);

                    var resourcePoolConfigurationDuplicateNameError = resourcePoolConfigurationError as ResourcePoolDuplicateNameError;
                    Assert.IsNotNull(resourcePoolConfigurationDuplicateNameError);
                    Assert.AreEqual(resourcePool1.Name, resourcePoolConfigurationDuplicateNameError.Name);
                    Assert.AreEqual($"Resource pool '{resourcePool1.Name}' has a duplicate name.", resourcePoolConfigurationError.ErrorMessage);
                }

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void UpdateToSameNameThrowsException()
        {
            var prefix = Guid.NewGuid().ToString();

            var resourcePool1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool_1",
            };

            var resourcePool2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool_2",
            };

            objectCreator.CreateResourcePool(resourcePool1);
            objectCreator.CreateResourcePool(resourcePool2);

            var toUpdate = TestContext.Api.ResourcePools.Read(resourcePool2.Id);
            toUpdate.Name = resourcePool1.Name;

            try
            {
                TestContext.Api.ResourcePools.Update(toUpdate);
            }
            catch (MediaOpsException ex)
            {
                StringAssert.Contains(ex.Message, "Name is already in use.");

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var resourcePoolConfigurationError = ex.TraceData.ErrorData.OfType<ResourcePoolError>().SingleOrDefault();
                Assert.IsNotNull(resourcePoolConfigurationError);

                var resourcePoolConfigurationNameExistsError = resourcePoolConfigurationError as ResourcePoolNameExistsError;
                Assert.IsNotNull(resourcePoolConfigurationNameExistsError);
                Assert.AreEqual(toUpdate.Id, resourcePoolConfigurationNameExistsError.Id);
                Assert.AreEqual(toUpdate.Name, resourcePoolConfigurationNameExistsError.Name);
                Assert.AreEqual("Name is already in use.", resourcePoolConfigurationError.ErrorMessage);

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void ReadWithEmptyListReturnsEmptyList()
        {
            var resourcePools = TestContext.Api.ResourcePools.Read(new List<Guid>());
            Assert.IsNotNull(resourcePools);
            Assert.AreEqual(0, resourcePools.Count());
        }

        [TestMethod]
        public void CreatePoolWithCategory()
        {
            var resourcePoolsScope = TestContext.CategoriesApi.Scopes.Read("Resource Pools") ?? throw new InvalidOperationException("Category Scope 'Resource Pools' is not available");

            var category = objectCreator.CreateCategory(new Category
            {
                Name = $"ResourcePoolCategory_{Guid.NewGuid()}",
                Scope = resourcePoolsScope,
            });

            var resourcePool1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"ResourcePool_1_{Guid.NewGuid()}",
                CategoryId = category.ID.ToString(),
            };

            objectCreator.CreateResourcePool(resourcePool1);

            var readPool = TestContext.Api.ResourcePools.Read(resourcePool1.Id);
            Assert.AreEqual(resourcePool1.CategoryId, readPool.CategoryId);
        }

        [TestMethod]
        public void CreatePoolWithNonExistingCategoryThrowsException()
        {
            var resourcePool1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"ResourcePool_1_{Guid.NewGuid()}",
                CategoryId = Guid.NewGuid().ToString(),
            };

            try
            {
                objectCreator.CreateResourcePool(resourcePool1);
            }
            catch (MediaOpsException ex)
            {
                StringAssert.Contains(ex.Message, $"Category with ID '{resourcePool1.CategoryId}' could not found in Scope 'Resource Pools'.");

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var resourcePoolConfigurationError = ex.TraceData.ErrorData.OfType<ResourcePoolError>().SingleOrDefault();
                Assert.IsNotNull(resourcePoolConfigurationError);

                var resourcePoolConfigurationNameExistsError = resourcePoolConfigurationError as ResourcePoolCategoryNotFoundError;
                Assert.IsNotNull(resourcePoolConfigurationNameExistsError);
                Assert.AreEqual(resourcePool1.Id, resourcePoolConfigurationNameExistsError.Id);
                Assert.AreEqual(resourcePool1.CategoryId, resourcePoolConfigurationNameExistsError.CategoryId);
                Assert.AreEqual($"Category with ID '{resourcePool1.CategoryId}' could not found in Scope 'Resource Pools'.", resourcePoolConfigurationError.ErrorMessage);

                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void ResourcePoolWithEmptyNameThrowsExceptionOnCreate()
        {
            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = string.Empty,
            };

            try
            {
                objectCreator.CreateResourcePool(resourcePool);
            }
            catch (MediaOpsException ex)
            {
                StringAssert.Contains(ex.Message, "Name cannot be empty.");
                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var resourcePoolConfigurationError = ex.TraceData.ErrorData.OfType<ResourcePoolError>().SingleOrDefault();
                Assert.IsNotNull(resourcePoolConfigurationError);

                var resourcePoolConfigurationNameEmptyError = resourcePoolConfigurationError as ResourcePoolInvalidNameError;
                Assert.IsNotNull(resourcePoolConfigurationNameEmptyError);
                Assert.AreEqual(resourcePool.Id, resourcePoolConfigurationNameEmptyError.Id);
                Assert.AreEqual("Name cannot be empty.", resourcePoolConfigurationError.ErrorMessage);
                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void QueryResourcePoolsBasedOnStateDoesNotThrowException()
        {
            var resourcePools = TestContext.Api.ResourcePools.Read(ResourcePoolExposers.State.Equal((int)ResourcePoolState.Complete)).ToList();
            Assert.IsNotNull(resourcePools);
        }
    }
}
