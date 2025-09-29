namespace RT_MediaOps.Plan.RST.ResourcePools
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using RT_MediaOps.Plan.RegressionTests;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public sealed class BasicTests : IDisposable
    {
        private readonly IntegrationTestContext testContext;

        public BasicTests()
        {
            testContext = new IntegrationTestContext();
        }

        public void Dispose()
        {
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

            var returnedId = testContext.Api.ResourcePools.Create(resourcePool);
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
    }
}
