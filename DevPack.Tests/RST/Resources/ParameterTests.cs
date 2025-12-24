namespace RT_MediaOps.Plan.RST.Resources
{
    using System;
    using RT_MediaOps.Plan.RegressionTests;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public sealed class ParameterTests : IDisposable
    {
        public ParameterTests()
        {
        }

        private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

        [TestMethod]
        public void ValidateConfigurationParameters()
        {
            // Define capacity and capabilities on some resources in a pool that can be used later for filtering(different capacities and capabilities) with one mandatory parameter(don't define them yet on pool level).
            // Create a job with a node containing that pool
            // Define configuration and see if correct filtering is applied in the pick/ swap resource panel. Evaluate as well that all profile parameters are available in the drop downs(capabilities, capacities and config).Mandatory field is not there by default
            // Define which parameters apply on pool level
            // Evaluate that the configuration is dropdown is limited and that the mandatory field is there and can't be removed.

            var prefix = Guid.NewGuid();

            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool
            {
                Name = $"{prefix}_Resource Pool 1",
            };
            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource(Guid.NewGuid())
            {
                Name = $"{prefix}_Resource 1",
            };
            var capacity = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity
            {
                Name = $"{prefix}_Mandatory Capacity 1",
                IsMandatory = true,
            };

            TestContext.Api.ResourcePools.Create(resourcePool);
            TestContext.Api.Resources.Create(unmanagedResource);

            //testContext.Api.ResourcePools.AssignResource(resourcePoolId, resource1Id);

            TestContext.Api.Capacities.Create(capacity); ;

            TestContext.Api.ResourcePools.Delete(resourcePool.Id);
            TestContext.Api.Resources.Delete(unmanagedResource.Id);
            TestContext.Api.Capacities.Delete(capacity.Id);

            Assert.IsNull(TestContext.Api.ResourcePools.Read(resourcePool.Id));
            Assert.IsNull(TestContext.Api.Resources.Read(unmanagedResource.Id));
            Assert.IsNull(TestContext.Api.Capacities.Read(capacity.Id));
        }

        public void Dispose()
        {
        }
    }
}
