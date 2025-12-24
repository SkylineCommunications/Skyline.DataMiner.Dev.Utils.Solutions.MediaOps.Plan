namespace RT_MediaOps.Plan.RST.Configurations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using RT_MediaOps.Plan.RegressionTests;

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
        public void ReadWithEmptyListReturnsEmptyList()
        {
            var configurations = TestContext.Api.Configurations.Read(new List<Guid>());
            Assert.IsNotNull(configurations);
            Assert.AreEqual(0, configurations.Count());
        }
    }
}
