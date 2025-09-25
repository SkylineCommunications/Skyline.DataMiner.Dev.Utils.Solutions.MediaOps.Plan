namespace RT_MediaOps.Plan.RST.Resources
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using RT_MediaOps.Plan.RegressionTests;

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
            var capacityId = testContext.Api.Capacities.Create(new Skyline.DataMiner.MediaOps.Plan.API.Capacity());

        }

        public void Dispose()
        {
            testContext.Dispose();
        }
    }
}
