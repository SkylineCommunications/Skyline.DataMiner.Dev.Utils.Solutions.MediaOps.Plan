namespace RT_MediaOps.Plan.RST.ResourcePools
{
    using System;
    using System.Linq;

    using RT_MediaOps.Plan.RegressionTests;

    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    using Storage = Skyline.DataMiner.MediaOps.Plan.Storage;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public sealed class CoreTests : IDisposable
    {
        private readonly IntegrationTestContext testContext;
        private readonly ResourceStudioObjectCreator objectCreator;

        public CoreTests()
        {
            testContext = new IntegrationTestContext();
            objectCreator = new ResourceStudioObjectCreator(testContext.Api);
        }

        public void Dispose()
        {
            objectCreator.Dispose();
            testContext.Dispose();
        }

        [TestMethod]
        public void GeneralDataInCompletedState()
        {
            var prefix = Guid.NewGuid().ToString();

            var resourcePool = new Skyline.DataMiner.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool",
            };

            var poolId = objectCreator.CreateResourcePool(resourcePool);
            testContext.Api.ResourcePools.MoveTo(poolId, Skyline.DataMiner.MediaOps.Plan.API.ResourcePoolState.Complete);

            var domResourcePool = testContext.DomHelpers.SlcResourceStudioHelper.DomHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(poolId)).SingleOrDefault();
            Assert.IsNotNull(domResourcePool);

            var domResourcePoolInternalProperties = domResourcePool.Sections.Single(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolInternalProperties.Id.Id);
            var fdDomResourcePoolIds = domResourcePoolInternalProperties.FieldValues.SingleOrDefault(f => f.FieldDescriptorID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolInternalProperties.Resource_Pool_Id.Id);
            var coreResourcePoolId = Guid.Parse(Convert.ToString(fdDomResourcePoolIds.Value.Value));

            var coreResourcePool = testContext.CoreHelpers.ResourceManagerHelper.GetResourcePool(coreResourcePoolId);
            Assert.IsNotNull(coreResourcePool);
            Assert.AreEqual(resourcePool.Name, coreResourcePool.Name);
        }

        [TestMethod]
        public void GeneralDataInDeprecatedState()
        {
            var prefix = Guid.NewGuid().ToString();

            var resourcePool = new Skyline.DataMiner.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool",
            };

            var poolId = objectCreator.CreateResourcePool(resourcePool);
            testContext.Api.ResourcePools.MoveTo(poolId, Skyline.DataMiner.MediaOps.Plan.API.ResourcePoolState.Complete);
            testContext.Api.ResourcePools.MoveTo(poolId, Skyline.DataMiner.MediaOps.Plan.API.ResourcePoolState.Deprecated);

            var domResourcePool = testContext.DomHelpers.SlcResourceStudioHelper.DomHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(poolId)).SingleOrDefault();
            Assert.IsNotNull(domResourcePool);

            var domResourcePoolInternalProperties = domResourcePool.Sections.Single(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolInternalProperties.Id.Id);
            var fdDomResourcePoolIds = domResourcePoolInternalProperties.FieldValues.SingleOrDefault(f => f.FieldDescriptorID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolInternalProperties.Resource_Pool_Id.Id);
            var coreResourcePoolId = Guid.Parse(Convert.ToString(fdDomResourcePoolIds.Value.Value));

            var coreResourcePool = testContext.CoreHelpers.ResourceManagerHelper.GetResourcePool(coreResourcePoolId);
            Assert.IsNotNull(coreResourcePool);
            Assert.AreEqual(resourcePool.Name, coreResourcePool.Name);
        }
    }
}
