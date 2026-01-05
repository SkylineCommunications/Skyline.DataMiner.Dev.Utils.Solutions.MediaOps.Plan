namespace RT_MediaOps.Plan.RST.Capabilities
{
    using System;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

    [TestClass]
    public sealed class HashCodeTests
    {
        [TestMethod]
        public void Capability_TrackableObject_Name()
        {
            var capability = new Capability
            {
                Name = $"Capability_{Guid.NewGuid()}",
            };

            var initialHash = capability.GetHashCode();

            capability.Name += "_Updated";

            var updatedHash = capability.GetHashCode();

            Assert.AreNotEqual(initialHash, updatedHash, "Changing Name should affect the hash code for change tracking.");
        }

        [TestMethod]
        public void Capability_TrackableObject_IsMandatory()
        {
            var capability = new Capability
            {
                Name = $"Capability_{Guid.NewGuid()}",
                IsMandatory = false,
            };

            var initialHash = capability.GetHashCode();

            capability.IsMandatory = true;

            var updatedHash = capability.GetHashCode();

            Assert.AreNotEqual(initialHash, updatedHash, "Changing IsMandatory should affect the hash code for change tracking.");
        }

        [TestMethod]
        public void Capability_TrackableObject_IsTimeDependent()
        {
            var capability = new Capability
            {
                Name = $"Capability_{Guid.NewGuid()}",
                IsTimeDependent = false,
            };

            var initialHash = capability.GetHashCode();

            capability.IsTimeDependent = true;

            var updatedHash = capability.GetHashCode();

            Assert.AreNotEqual(initialHash, updatedHash, "Changing IsTimeDependent should affect the hash code for change tracking.");
        }

        [TestMethod]
        public void Capability_TrackableObject_Discretes()
        {
            var capability = new Capability
            {
                Name = $"Capability_{Guid.NewGuid()}",
            };

            capability.SetDiscretes(new[] { "Value 1", "Value 2" });

            var initialHash = capability.GetHashCode();

            capability.AddDiscrete("Value 3");

            var updatedHash = capability.GetHashCode();

            Assert.AreNotEqual(initialHash, updatedHash, "Changing Discretes should affect the hash code for change tracking.");
        }
    }
}
