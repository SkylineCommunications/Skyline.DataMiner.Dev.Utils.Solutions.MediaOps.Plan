namespace RT_MediaOps.Plan.RST.ResourcePools
{
    using System;
    using System.Linq;
    using System.Xml.Linq;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using RT_MediaOps.Plan.RegressionTests;

    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    using Storage = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage;

    [TestClass]
    [TestCategory("IntegrationTest")]
    [DoNotParallelize]
    public sealed class DomTests : IDisposable
    {
        private readonly ResourceStudioObjectCreator objectCreator;

        public DomTests()
        {
            objectCreator = new ResourceStudioObjectCreator(TestContext.Api, TestContext.CategoriesApi);
        }

        private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

        public void Dispose()
        {
            objectCreator.Dispose();
        }

        [TestMethod]
        public void GeneralDataInDraftState()
        {
            var prefix = Guid.NewGuid().ToString();

            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool",
            };

            objectCreator.CreateResourcePool(resourcePool);

            var domResourcePool = TestContext.ResourceStudioDomHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(resourcePool.Id)).SingleOrDefault();
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

            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool",
            };

            objectCreator.CreateResourcePool(resourcePool);
            TestContext.Api.ResourcePools.MoveTo(resourcePool.Id, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePoolState.Complete);

            var domResourcePool = TestContext.ResourceStudioDomHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(resourcePool.Id)).SingleOrDefault();
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

            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool",
            };

            objectCreator.CreateResourcePool(resourcePool);
            TestContext.Api.ResourcePools.MoveTo(resourcePool.Id, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePoolState.Complete);
            TestContext.Api.ResourcePools.MoveTo(resourcePool.Id, Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePoolState.Deprecated);

            var domResourcePool = TestContext.ResourceStudioDomHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(resourcePool.Id)).SingleOrDefault();
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

            var fdResourcePoolIds = domResourcePoolInternalProperties.FieldValues.SingleOrDefault(f => f.FieldDescriptorID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolInternalProperties.Resource_Pool_Id.Id);
            Assert.IsNotNull(fdResourcePoolIds);
            Assert.IsTrue(Guid.TryParse(Convert.ToString(fdResourcePoolIds.Value.Value), out var coreResourcePoolId));
            Assert.IsTrue(coreResourcePoolId != Guid.Empty);
        }

        [TestMethod]
        public void LinkedPoolData()
        {
            var prefix = Guid.NewGuid().ToString();

            var resourcePool1 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool1",
            };
            var resourcePool2 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool2",
            };
            var resourcePool3 = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool3",
            };

            objectCreator.CreateResourcePools(new[] { resourcePool1, resourcePool2 });

            resourcePool3.AddLinkedResourcePool(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.LinkedResourcePool(resourcePool1.Id) { SelectionType = Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceSelectionType.Automatic });
            resourcePool3.AddLinkedResourcePool(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.LinkedResourcePool(resourcePool2.Id) { SelectionType = Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceSelectionType.Manual });
            objectCreator.CreateResourcePool(resourcePool3);

            var domResourcePool = TestContext.ResourceStudioDomHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(resourcePool3.Id)).SingleOrDefault();
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
                { resourcePool1.Id, (int)Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Enums.Resourceselectiontype.Automatic },
                { resourcePool2.Id, (int)Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Enums.Resourceselectiontype.Manual },
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

        [TestMethod]
        public void ExternallyManagedData()
        {
            var prefix = Guid.NewGuid().ToString();

            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool()
            {
                Name = $"{prefix}_ResourcePool",
            };

            objectCreator.CreateResourcePool(resourcePool);

            var domResourcePool = TestContext.ResourceStudioDomHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(resourcePool.Id)).SingleOrDefault();
            Assert.IsNotNull(domResourcePool);
            Assert.IsFalse(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ExternalMetadata.Id.Id));

            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);
            resourcePool.IsExternallyManaged = true;
            TestContext.Api.ResourcePools.Update(resourcePool);

            domResourcePool = TestContext.ResourceStudioDomHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(resourcePool.Id)).SingleOrDefault();
            Assert.IsNotNull(domResourcePool);
            Assert.IsTrue(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ExternalMetadata.Id.Id));

            var domExternalMetadata = domResourcePool.Sections.Single(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ExternalMetadata.Id.Id);
            Assert.IsNotNull(domExternalMetadata);

            var fdExternallyManaged = domExternalMetadata.FieldValues.SingleOrDefault(f => f.FieldDescriptorID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ExternalMetadata.ExternallyManaged.Id);
            Assert.IsNotNull(fdExternallyManaged);
            Assert.AreEqual(true, Convert.ToBoolean(fdExternallyManaged.Value.Value));

            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);
            resourcePool.IsExternallyManaged = false;
            TestContext.Api.ResourcePools.Update(resourcePool);

            domResourcePool = TestContext.ResourceStudioDomHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(resourcePool.Id)).SingleOrDefault();
            Assert.IsNotNull(domResourcePool);
            Assert.IsFalse(domResourcePool.Sections.Exists(s => s.SectionDefinitionID.Id == Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ExternalMetadata.Id.Id));
        }
    }
}
