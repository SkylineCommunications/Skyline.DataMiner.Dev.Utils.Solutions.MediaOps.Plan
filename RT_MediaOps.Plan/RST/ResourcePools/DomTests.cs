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

            var domResourcePool = testContext.ResourceStudioDomHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(poolId)).SingleOrDefault();
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

            var domResourcePool = testContext.ResourceStudioDomHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(poolId)).SingleOrDefault();
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

            var fdDomResourcePoolIds = domResourcePoolInternalProperties.FieldValues.SingleOrDefault(f => f.FieldDescriptorID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolInternalProperties.Resource_Pool_Id.Id);
            Assert.IsNotNull(fdDomResourcePoolIds);
            Assert.IsTrue(Guid.TryParse(Convert.ToString(fdDomResourcePoolIds.Value.Value), out var coreResourcePoolId));
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

            var domResourcePool = testContext.ResourceStudioDomHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(poolId)).SingleOrDefault();
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

        [TestMethod]
        public void LinkedPoolData()
        {
            var prefix = Guid.NewGuid().ToString();

            var resourcePool1 = new Skyline.DataMiner.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool1",
            };
            var resourcePool2 = new Skyline.DataMiner.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool2",
            };
            var resourcePool3 = new Skyline.DataMiner.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool3",
            };

            var poolIds = objectCreator.CreateResourcePools(new[] { resourcePool1, resourcePool2 }).ToArray();

            resourcePool3.AddLinkedResourcePool(new Skyline.DataMiner.MediaOps.Plan.API.LinkedResourcePool(poolIds[0]) { SelectionType = Skyline.DataMiner.MediaOps.Plan.API.ResourceSelectionType.Automatic });
            resourcePool3.AddLinkedResourcePool(new Skyline.DataMiner.MediaOps.Plan.API.LinkedResourcePool(poolIds[1]) { SelectionType = Skyline.DataMiner.MediaOps.Plan.API.ResourceSelectionType.Manual });
            var poolId3 = objectCreator.CreateResourcePool(resourcePool3);

            var domResourcePool = testContext.ResourceStudioDomHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(poolId3)).SingleOrDefault();
            Assert.IsNotNull(domResourcePool);

            Assert.IsTrue(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolInfo.Id.Id));
            Assert.IsTrue(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolInternalProperties.Id.Id));
            Assert.IsFalse(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolCost.Id.Id));
            Assert.IsFalse(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolCapabilities.Id.Id));
            Assert.IsTrue(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolLinks.Id.Id));
            Assert.IsFalse(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ExternalMetadata.Id.Id));
            Assert.IsFalse(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolOther.Id.Id));
            Assert.IsFalse(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ConfigurationInfo.Id.Id));
            Assert.IsFalse(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.Errors.Id.Id));

            var expectedSelectionData = new Dictionary<Guid, int>
            {
                { poolIds[0], (int)Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Enums.Resourceselectiontype.Automatic },
                { poolIds[1], (int)Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Enums.Resourceselectiontype.Manual },
            };
            foreach (var resourcePoolLink in domResourcePool.Sections.Where(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolLinks.Id.Id))
            {
                var fdLinkedPoolId = resourcePoolLink.FieldValues.SingleOrDefault(f => f.FieldDescriptorID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolLinks.LinkedResourcePool.Id);
                Assert.IsNotNull(fdLinkedPoolId);
                Assert.IsTrue(Guid.TryParse(Convert.ToString(fdLinkedPoolId.Value.Value), out var linkedPoolId));
                Assert.IsTrue(linkedPoolId != Guid.Empty);

                Assert.IsTrue(expectedSelectionData.TryGetValue(linkedPoolId, out var expectedSelectionType));

                var fdSelectionType = resourcePoolLink.FieldValues.SingleOrDefault(f => f.FieldDescriptorID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolLinks.ResourceSelectionType.Id);
                Assert.IsNotNull(fdSelectionType);
                Assert.AreEqual(expectedSelectionType, Convert.ToInt32(fdSelectionType.Value.Value));
            }
        }
    }
}
