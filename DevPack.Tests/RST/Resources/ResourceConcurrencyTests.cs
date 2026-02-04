namespace RT_MediaOps.Plan.RST.Resources
{
    using System;
    using System.Linq;

    using RT_MediaOps.Plan.RegressionTests;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public sealed class ResourceConcurrencyTests
    {
        private readonly TestObjectCreator objectCreator;

        public ResourceConcurrencyTests()
        {
            objectCreator = new TestObjectCreator(TestContext);
        }

        private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

        public void Dispose()
        {
            objectCreator.Dispose();
        }

        [TestMethod]
        public void CreateWithInvalidConcurrencyThrowsException()
        {
            var prefix = Guid.NewGuid();
            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
                Concurrency = 0, // Invalid concurrency
            };

            try
            {
                objectCreator.CreateResource(unmanagedResource);
            }
            catch (MediaOpsException ex)
            {
                var errorMessage = $"Concurrency must be greater than or equal to 1.";
                Assert.AreEqual(errorMessage, ex.Message);
                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);

                var resourceConfigurationError = ex.TraceData.ErrorData.OfType<ResourceInvalidConcurrencyError>().SingleOrDefault();
                Assert.IsNotNull(resourceConfigurationError);

                return;
            }

            Assert.Fail("Exception not thrown");
        }

        [TestMethod]
        public void UpdateWithInvalidConcurrencyThrowsException()
        {
            var prefix = Guid.NewGuid();

            var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
            {
                Name = $"{prefix}_Resource",
                Concurrency = 10,
            };

            objectCreator.CreateResource(unmanagedResource);

            var resource = TestContext.Api.Resources.Read(unmanagedResource.Id);
            resource.Concurrency = -10; // Invalid concurrency

            try
            {
                TestContext.Api.Resources.Update(resource);
            }
            catch (MediaOpsException ex)
            {
                var errorMessage = $"Concurrency must be greater than or equal to 1.";
                Assert.AreEqual(errorMessage, ex.Message);
                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);

                var resourceConfigurationError = ex.TraceData.ErrorData.OfType<ResourceInvalidConcurrencyError>().SingleOrDefault();
                Assert.IsNotNull(resourceConfigurationError);

                return;
            }

            Assert.Fail("Exception not thrown");
        }
    }
}
