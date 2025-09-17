namespace RT_MediaOps.Plan.RST.Resources
{
    using System;

    using RT_MediaOps.Plan.RegressionTests;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public sealed class CreateResourceTests : IDisposable
    {
        private readonly IntegrationTestContext testContext;

        public CreateResourceTests()
        {
            testContext = new IntegrationTestContext();
        }

        [TestMethod]
        public void CreateUnmanagedResource()
        {
            string name = TestHelper.GetRandomName("UnmanagedResource_");
            var id = Guid.NewGuid();
            var unmanagedResource = new Skyline.DataMiner.MediaOps.Plan.API.UnmanagedResource(id)
            {
                Name = name,
                Concurrency = 5,
                IsFavorite = true,
            };
            var returnedId = testContext.Api.Resources.Create(unmanagedResource);
            Assert.AreEqual(id, returnedId);
            var returnedResource = testContext.Api.Resources.Read(returnedId);
            Assert.AreEqual(name, returnedResource.Name);
            Assert.AreEqual(5, returnedResource.Concurrency);
            Assert.IsTrue(returnedResource.IsFavorite);
            Assert.AreEqual(Skyline.DataMiner.MediaOps.Plan.API.ResourceState.Draft, returnedResource.State);
        }

        public void Dispose()
        {
            testContext.Dispose();
        }
    }
}
