namespace RT_MediaOps.Plan.RST.Resources
{
    using System;
    using System.Data;
    using System.Xml.Linq;

    using RT_MediaOps.Plan.RegressionTests;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public sealed class UnmanagedResourcesTests : IDisposable
    {
        private readonly IntegrationTestContext testContext;

        public UnmanagedResourcesTests()
        {
            testContext = new IntegrationTestContext();
        }

        [TestMethod]
        public void UnmanagedResourceCrud()
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
            ValidateUnmanagedResource(id, name, 5, true, Skyline.DataMiner.MediaOps.Plan.API.ResourceState.Draft, returnedResource);
            testContext.Api.Resources.MoveTo(id, Skyline.DataMiner.MediaOps.Plan.API.ResourceState.Complete);
            returnedResource = testContext.Api.Resources.Read(returnedId);
            ValidateUnmanagedResource(id, name, 5, true, Skyline.DataMiner.MediaOps.Plan.API.ResourceState.Complete, returnedResource);
            returnedResource.Concurrency = 10;
            returnedResource.IsFavorite = false;
            returnedResource.Name = name + "_updated";
            testContext.Api.Resources.Update(returnedResource);
            ValidateUnmanagedResource(id, name + "_updated", 10, false, Skyline.DataMiner.MediaOps.Plan.API.ResourceState.Complete, returnedResource);
            testContext.Api.Resources.Delete(returnedResource);
        }

        public void Dispose()
        {
            testContext.Dispose();
        }

        private static void ValidateUnmanagedResource(
            Guid expectedId,
            string expectedName,
            int expectedConcurrency,
            bool expectedIsFavorite,
            Skyline.DataMiner.MediaOps.Plan.API.ResourceState expectedState,
            Skyline.DataMiner.MediaOps.Plan.API.Resource resource)
        {
            Assert.AreEqual(expectedId, resource.Id);
            Assert.AreEqual(expectedName, resource.Name);
            Assert.AreEqual(expectedConcurrency, resource.Concurrency);
            Assert.AreEqual(expectedIsFavorite, resource.IsFavorite);
            Assert.AreEqual(expectedState, resource.State);
        }
    }
}
