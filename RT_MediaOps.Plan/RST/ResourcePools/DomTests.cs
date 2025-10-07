namespace RT_MediaOps.Plan.RST.ResourcePools
{
    using System;
    using System.Linq;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using RT_MediaOps.Plan.RegressionTests;

    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    using Storage = Skyline.DataMiner.MediaOps.Plan.Storage;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public sealed class DomTests : IDisposable
    {
        private readonly IntegrationTestContext testContext;
        private readonly ResourceStudioObjectCreator objectCreator;

        public DomTests()
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
        public void GeneralDataInDraftState()
        {
            var prefix = Guid.NewGuid().ToString();

            var resourcePool = new Skyline.DataMiner.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool",
            };

            var poolId = objectCreator.CreateResourcePool(resourcePool);

            var domResourcePool = testContext.DomHelpers.SlcResourceStudioHelper.DomHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(poolId)).SingleOrDefault();
            Assert.IsNotNull(domResourcePool);
            Assert.AreEqual(Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Definitions.Resourcepool.Id, domResourcePool.DomDefinitionId.Id);
            Assert.AreEqual(Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Behaviors.Resource_Behavior.Statuses.Draft, domResourcePool.StatusId);

            Assert.IsTrue(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolInfo.Id.Id));
            Assert.IsTrue(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolInternalProperties.Id.Id));
            Assert.IsFalse(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolCost.Id.Id));
            Assert.IsFalse(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolCapabilities.Id.Id));
            Assert.IsFalse(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolLinks.Id.Id));
            Assert.IsFalse(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ExternalMetadata.Id.Id));
            Assert.IsFalse(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolOther.Id.Id));
            Assert.IsFalse(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ConfigurationInfo.Id.Id));
            Assert.IsFalse(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.Errors.Id.Id));

            var domResourcePoolInfo = domResourcePool.Sections.Single(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolInfo.Id.Id);
            var domResourcePoolInternalProperties = domResourcePool.Sections.Single(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolInternalProperties.Id.Id);

            var fdName = domResourcePoolInfo.FieldValues.SingleOrDefault(f => f.FieldDescriptorID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolInfo.Name.Id);
            Assert.IsNotNull(fdName);
            Assert.AreEqual(resourcePool.Name, Convert.ToString(fdName.Value.Value));

            var fdDomain = domResourcePoolInfo.FieldValues.SingleOrDefault(f => f.FieldDescriptorID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolInfo.Domain.Id);
            Assert.IsNull(fdDomain);

            var domResourcePoolIds = domResourcePoolInternalProperties.FieldValues.SingleOrDefault(f => f.FieldDescriptorID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolInternalProperties.Resource_Pool_Id.Id);
            Assert.IsNull(domResourcePoolIds);
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
            Assert.AreEqual(Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Definitions.Resourcepool.Id, domResourcePool.DomDefinitionId.Id);
            Assert.AreEqual(Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Behaviors.Resource_Behavior.Statuses.Complete, domResourcePool.StatusId);

            Assert.IsTrue(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolInfo.Id.Id));
            Assert.IsTrue(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolInternalProperties.Id.Id));
            Assert.IsFalse(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolCost.Id.Id));
            Assert.IsFalse(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolCapabilities.Id.Id));
            Assert.IsFalse(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolLinks.Id.Id));
            Assert.IsFalse(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ExternalMetadata.Id.Id));
            Assert.IsFalse(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolOther.Id.Id));
            Assert.IsFalse(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ConfigurationInfo.Id.Id));
            Assert.IsFalse(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.Errors.Id.Id));

            var domResourcePoolInfo = domResourcePool.Sections.Single(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolInfo.Id.Id);
            var domResourcePoolInternalProperties = domResourcePool.Sections.Single(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolInternalProperties.Id.Id);

            var fdName = domResourcePoolInfo.FieldValues.SingleOrDefault(f => f.FieldDescriptorID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolInfo.Name.Id);
            Assert.IsNotNull(fdName);
            Assert.AreEqual(resourcePool.Name, Convert.ToString(fdName.Value.Value));

            var fdDomain = domResourcePoolInfo.FieldValues.SingleOrDefault(f => f.FieldDescriptorID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolInfo.Domain.Id);
            Assert.IsNull(fdDomain);

            var domResourcePoolIds = domResourcePoolInternalProperties.FieldValues.SingleOrDefault(f => f.FieldDescriptorID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolInternalProperties.Resource_Pool_Id.Id);
            Assert.IsNotNull(domResourcePoolIds);
            Assert.IsTrue(Guid.TryParse(Convert.ToString(domResourcePoolIds.Value.Value), out var coreResourcePoolId));
            Assert.IsTrue(coreResourcePoolId != Guid.Empty);
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
            Assert.AreEqual(Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Definitions.Resourcepool.Id, domResourcePool.DomDefinitionId.Id);
            Assert.AreEqual(Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Behaviors.Resource_Behavior.Statuses.Deprecated, domResourcePool.StatusId);

            Assert.IsTrue(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolInfo.Id.Id));
            Assert.IsTrue(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolInternalProperties.Id.Id));
            Assert.IsFalse(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolCost.Id.Id));
            Assert.IsFalse(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolCapabilities.Id.Id));
            Assert.IsFalse(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolLinks.Id.Id));
            Assert.IsFalse(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ExternalMetadata.Id.Id));
            Assert.IsFalse(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolOther.Id.Id));
            Assert.IsFalse(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ConfigurationInfo.Id.Id));
            Assert.IsFalse(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.Errors.Id.Id));

            var domResourcePoolInfo = domResourcePool.Sections.Single(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolInfo.Id.Id);
            var domResourcePoolInternalProperties = domResourcePool.Sections.Single(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolInternalProperties.Id.Id);

            var fdName = domResourcePoolInfo.FieldValues.SingleOrDefault(f => f.FieldDescriptorID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolInfo.Name.Id);
            Assert.IsNotNull(fdName);
            Assert.AreEqual(resourcePool.Name, Convert.ToString(fdName.Value.Value));

            var fdDomain = domResourcePoolInfo.FieldValues.SingleOrDefault(f => f.FieldDescriptorID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolInfo.Domain.Id);
            Assert.IsNull(fdDomain);

            var domResourcePoolIds = domResourcePoolInternalProperties.FieldValues.SingleOrDefault(f => f.FieldDescriptorID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolInternalProperties.Resource_Pool_Id.Id);
            Assert.IsNotNull(domResourcePoolIds);
            Assert.IsTrue(Guid.TryParse(Convert.ToString(domResourcePoolIds.Value.Value), out var coreResourcePoolId));
            Assert.IsTrue(coreResourcePoolId != Guid.Empty);
        }
    }
}
