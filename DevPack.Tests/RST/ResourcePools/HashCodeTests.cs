namespace RT_MediaOps.Plan.RST.ResourcePools
{
    using System;

    [TestClass]
    public sealed class HashCodeTests
    {
        [TestMethod]
        public void ResourcePool_TrackableObject_Name()
        {
            var poolId = Guid.NewGuid();

            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool(poolId)
            {
                Name = $"{poolId}_ResourcePool",
            };

            var initialHash = resourcePool.GetHashCode();

            resourcePool.Name += "_Updated";

            var updatedHash = resourcePool.GetHashCode();

            Assert.AreNotEqual(initialHash, updatedHash, "Changing Name should affect the hash code for change tracking.");
        }

        [TestMethod]
        public void ResourcePool_TrackableObject_CategoryId()
        {
            var poolId = Guid.NewGuid();

            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool(poolId)
            {
                Name = $"{poolId}_ResourcePool",
                CategoryId = Guid.NewGuid().ToString(),
            };

            var initialHash = resourcePool.GetHashCode();

            resourcePool.CategoryId = Guid.NewGuid().ToString();

            var updatedHash = resourcePool.GetHashCode();

            Assert.AreNotEqual(initialHash, updatedHash, "Changing CategoryId should affect the hash code for change tracking.");
        }

        [TestMethod]
        public void ResourcePool_TrackableObject_LinkedResourcePools_Add()
        {
            var poolId = Guid.NewGuid();

            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool(poolId)
            {
                Name = $"{poolId}_ResourcePool",
            };

            var linkedPool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool();
            var link = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.LinkedResourcePool(linkedPool);

            var initialHash = resourcePool.GetHashCode();

            resourcePool.AddLinkedResourcePool(link);

            var updatedHash = resourcePool.GetHashCode();

            Assert.AreNotEqual(initialHash, updatedHash, "Adding a LinkedResourcePool should affect the hash code for change tracking.");
        }

        [TestMethod]
        public void ResourcePool_TrackableObject_LinkedResourcePools_Edit()
        {
            var poolId = Guid.NewGuid();

            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool(poolId)
            {
                Name = $"{poolId}_ResourcePool",
            };

            var linkedPool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool();
            var link = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.LinkedResourcePool(linkedPool)
            {
                SelectionType = Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceSelectionType.Automatic,
            };

            resourcePool.AddLinkedResourcePool(link);

            var initialHash = resourcePool.GetHashCode();

            // Edit property of an existing linked pool
            link.SelectionType = Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourceSelectionType.Manual;

            var updatedHash = resourcePool.GetHashCode();

            Assert.AreNotEqual(initialHash, updatedHash, "Editing an existing LinkedResourcePool should affect the hash code for change tracking.");
        }

        [TestMethod]
        public void ResourcePool_TrackableObject_Capabilities_Add()
        {
            var poolId = Guid.NewGuid();

            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool(poolId)
            {
                Name = $"{poolId}_ResourcePool",
            };

            var capability = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability
            {
                Name = $"Capability_{Guid.NewGuid()}",
            };
            capability.SetDiscretes(new[] { "Value 1", "Value 2" });

            var capabilitySetting = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySettings(capability.Id);
            capabilitySetting.SetDiscretes(new[] { "Value 1" });

            var initialHash = resourcePool.GetHashCode();

            resourcePool.AddCapability(capabilitySetting);

            var updatedHash = resourcePool.GetHashCode();

            Assert.AreNotEqual(initialHash, updatedHash, "Adding a CapabilitySetting should affect the hash code for change tracking.");
        }

        [TestMethod]
        public void ResourcePool_TrackableObject_Capabilities_Edit()
        {
            var poolId = Guid.NewGuid();

            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool(poolId)
            {
                Name = $"{poolId}_ResourcePool",
            };

            var capability = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability
            {
                Name = $"Capability_{Guid.NewGuid()}",
            };
            capability.SetDiscretes(new[] { "Value 1", "Value 2" });

            var capabilitySetting = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySettings(capability.Id);
            capabilitySetting.SetDiscretes(new[] { "Value 1" });

            resourcePool.AddCapability(capabilitySetting);

            var initialHash = resourcePool.GetHashCode();

            // Edit the existing capability setting
            resourcePool.Capabilities.First(x => x.Id.Equals(capability.Id)).AddDiscrete("Value 2");

            var updatedHash = resourcePool.GetHashCode();

            Assert.AreNotEqual(initialHash, updatedHash, "Editing an existing CapabilitySetting should affect the hash code for change tracking.");
        }
    }
}
