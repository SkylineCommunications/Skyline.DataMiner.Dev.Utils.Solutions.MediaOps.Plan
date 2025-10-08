namespace RT_MediaOps.Plan.RST.Resources
{
    using System;
    using System.Reflection.Emit;
    using System.Security.Policy;
    using DataMinerMessageBroker.API.Configuration;
    using RT_MediaOps.Plan.RegressionTests;
    using Skyline.DataMiner.Core.DataMinerSystem.Common.Selectors;
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Net.Jobs;
    using Skyline.DataMiner.Net.ServiceManager.Objects;
    using SLDataGateway.API.Querying;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public sealed class ParameterTests : IDisposable
    {
        private readonly IntegrationTestContext testContext;

        public ParameterTests()
        {
            testContext = new IntegrationTestContext();
        }

        [TestMethod]
        public void ValidateConfigurationParameters()
        {
            // Define capacity and capabilities on some resources in a pool that can be used later for filtering(different capacities and capabilities) with one mandatory parameter(don't define them yet on pool level).
            // Create a job with a node containing that pool
            // Define configuration and see if correct filtering is applied in the pick/ swap resource panel. Evaluate as well that all profile parameters are available in the drop downs(capabilities, capacities and config).Mandatory field is not there by default
            // Define which parameters apply on pool level
            // Evaluate that the configuration is dropdown is limited and that the mandatory field is there and can't be removed.

            var resourcePoolId = testContext.Api.ResourcePools.Create(new Skyline.DataMiner.MediaOps.Plan.API.ResourcePool
            {
                Name = "Resource Pool 1",
            });

            var resource1Id = testContext.Api.Resources.Create(new Skyline.DataMiner.MediaOps.Plan.API.UnmanagedResource(Guid.NewGuid())
            {
                Name = "Resource 1",
            });

            //testContext.Api.ResourcePools.AssignResource(resourcePoolId, resource1Id);

            var capacityId = testContext.Api.Capacities.Create(new Skyline.DataMiner.MediaOps.Plan.API.Capacity
            {
                Name = "Mandatory Capacity 1",
                IsMandatory = true,
            });

            testContext.Api.ResourcePools.Delete(resourcePoolId);
            testContext.Api.Resources.Delete(resource1Id);
            testContext.Api.Capacities.Delete(capacityId);

            Assert.IsNull(testContext.Api.ResourcePools.Read(resourcePoolId));
            Assert.IsNull(testContext.Api.Resources.Read(resource1Id));
            Assert.IsNull(testContext.Api.Capacities.Read(capacityId));
        }

        public void Dispose()
        {
            testContext.Dispose();
        }
    }
}
