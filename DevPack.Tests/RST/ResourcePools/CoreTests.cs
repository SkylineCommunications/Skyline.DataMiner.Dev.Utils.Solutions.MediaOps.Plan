namespace RT_MediaOps.Plan.RST.ResourcePools
{
    using System;
    using System.Linq;

    using RT_MediaOps.Plan.RegressionTests;

    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    using Storage = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public sealed class CoreTests : IDisposable
    {
        private readonly TestObjectCreator objectCreator;

        public CoreTests()
        {
            objectCreator = new TestObjectCreator(TestContext);
        }

        private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

        public void Dispose()
        {
            objectCreator.Dispose();
        }

        [TestMethod]
        public void GeneralDataInCompletedState()
        {
            var prefix = Guid.NewGuid().ToString();

            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool",
            };

            objectCreator.CreateResourcePool(resourcePool);
            TestContext.Api.ResourcePools.Complete(resourcePool.Id);

            var domResourcePool = TestContext.ResourceStudioDomHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(resourcePool.Id)).SingleOrDefault();
            Assert.IsNotNull(domResourcePool);

            var domResourcePoolInternalProperties = domResourcePool.Sections.Single(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolInternalProperties.Id.Id);
            var fdDomResourcePoolIds = domResourcePoolInternalProperties.FieldValues.SingleOrDefault(f => f.FieldDescriptorID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolInternalProperties.Resource_Pool_Id.Id);
            var coreResourcePoolId = Guid.Parse(Convert.ToString(fdDomResourcePoolIds.Value.Value));

            var coreResourcePool = TestContext.ResourceManagerHelper.GetResourcePool(coreResourcePoolId);
            Assert.IsNotNull(coreResourcePool);
            Assert.AreEqual(resourcePool.Name, coreResourcePool.Name);
        }

        [TestMethod]
        public void GeneralDataInDeprecatedState()
        {
            var prefix = Guid.NewGuid().ToString();

            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool",
            };

            objectCreator.CreateResourcePool(resourcePool);
            TestContext.Api.ResourcePools.Complete(resourcePool.Id);
            TestContext.Api.ResourcePools.Deprecate(resourcePool.Id);

            var domResourcePool = TestContext.ResourceStudioDomHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(resourcePool.Id)).SingleOrDefault();
            Assert.IsNotNull(domResourcePool);

            var domResourcePoolInternalProperties = domResourcePool.Sections.Single(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolInternalProperties.Id.Id);
            var fdDomResourcePoolIds = domResourcePoolInternalProperties.FieldValues.SingleOrDefault(f => f.FieldDescriptorID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolInternalProperties.Resource_Pool_Id.Id);
            var coreResourcePoolId = Guid.Parse(Convert.ToString(fdDomResourcePoolIds.Value.Value));

            var coreResourcePool = TestContext.ResourceManagerHelper.GetResourcePool(coreResourcePoolId);
            Assert.IsNotNull(coreResourcePool);
            Assert.AreEqual(resourcePool.Name, coreResourcePool.Name);
        }
    }
}
