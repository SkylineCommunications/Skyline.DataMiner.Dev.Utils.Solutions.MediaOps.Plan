namespace RT_MediaOps.Plan.RST.Capabilities
{
    using System.Collections.Generic;
    using System.Linq;
    using RT_MediaOps.Plan.RegressionTests;
    using Skyline.DataMiner.MediaOps.Plan.API;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public sealed class CapabilityTests
    {
        private readonly IntegrationTestContext testContext;

        public CapabilityTests()
        {
            testContext = new IntegrationTestContext();
        }

        [TestMethod]
        public void CreateCapability()
        {
            string name = $"Capability_{Guid.NewGuid()}";
            var capability = new Capability
            {
                Name = name,
                IsMandatory = true,
                IsTimeDependent = false,
            };

            capability.SetDiscretes(new[] { "Value 1", "Value 2", "Value 3" });

            var capabilityId = testContext.Api.Capabilities.Create(capability);

            var createdCapability = testContext.Api.Capabilities.Read(capabilityId);
            Assert.IsNotNull(createdCapability);
            Assert.AreEqual(name, createdCapability.Name);
            Assert.IsTrue(createdCapability.IsMandatory);
            CollectionAssert.AreEquivalent(new List<string> { "Value 1", "Value 2", "Value 3" }, createdCapability.Discretes.ToList());
            Assert.IsFalse(createdCapability.IsTimeDependent);

            testContext.Api.Capabilities.Delete(capabilityId);
        }
    }
}
