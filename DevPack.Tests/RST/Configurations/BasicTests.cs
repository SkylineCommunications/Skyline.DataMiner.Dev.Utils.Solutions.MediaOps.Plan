namespace RT_MediaOps.Plan.RST.Configurations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using RT_MediaOps.Plan.RegressionTests;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

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

        [TestMethod]
        public void DeleteTextConfiguration_PartOfResourcePoolOrchestrationSettings_ThrowsException()
        {
            var prefix = Guid.NewGuid();
            var textConfiguration = new TextConfiguration
            {
                Name = $"{prefix}_TextConfiguration",
            };

            objectCreator.CreateConfigurations([textConfiguration]);

            var resourcePool = new ResourcePool
            {
                Name = $"{prefix}_ResourcePool",
            };

            resourcePool.OrchestrationSettings.SetConfigurations([new TextConfigurationSetting(textConfiguration)]);
            objectCreator.CreateResourcePool(resourcePool);

            try
            {
                TestContext.Api.Configurations.Delete(textConfiguration);
            }
            catch (MediaOpsException ex)
            {
                var errorMessage = $"Configuration '{textConfiguration.Name}' is in use by Resource Pools.";
                Assert.AreEqual(errorMessage, ex.Message);

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);

                var configurationInUseError = ex.TraceData.ErrorData.Single() as ConfigurationInUseByResourcePoolsError;
                Assert.IsNotNull(configurationInUseError);
                Assert.AreEqual(errorMessage, configurationInUseError.ErrorMessage);
                Assert.IsNotNull(configurationInUseError.ResourcePoolIds);
                Assert.AreEqual(1, configurationInUseError.ResourcePoolIds.Count());
                Assert.AreEqual(resourcePool.Id, configurationInUseError.ResourcePoolIds.Single());

                return;
            }

            Assert.Fail();
        }
    }
}
