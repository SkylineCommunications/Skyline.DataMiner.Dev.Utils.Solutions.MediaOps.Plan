namespace RT_MediaOps.Plan.RST.Capabilities
{
    using System.Collections.Generic;
    using System.Linq;
    using RT_MediaOps.Plan.RegressionTests;
    using Skyline.DataMiner.MediaOps.Plan.API;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    [TestClass]
    [TestCategory("IntegrationTest")]
    [DoNotParallelize]
    public sealed class CapabilityTests
    {
        private readonly IntegrationTestContext testContext;

        public CapabilityTests()
        {
            testContext = new IntegrationTestContext();
        }

        [TestMethod]
        public void BasicCrudActions()
        {
            string name = $"Capability_{Guid.NewGuid()}";
            var capability = new Capability
            {
                Name = name,
                IsMandatory = true,
                IsTimeDependent = false,
                Discretes = new[] { "Value 1", "Value 2", "Value 3" },
            };

            var capabilityId = testContext.Api.Capabilities.Create(capability);

            var createdCapability = testContext.Api.Capabilities.Read(capabilityId);
            Assert.IsNotNull(createdCapability);
            Assert.AreEqual(name, createdCapability.Name);
            Assert.IsTrue(createdCapability.IsMandatory);
            CollectionAssert.AreEquivalent(new List<string> { "Value 1", "Value 2", "Value 3" }, createdCapability.Discretes.ToList());
            Assert.IsFalse(createdCapability.IsTimeDependent);

            testContext.Api.Capabilities.Delete(capabilityId);
        }

        [TestMethod]
        public void TimeDependentCapability()
        {
            string name = $"Capability_{Guid.NewGuid()}";
            string linkedName = $"{name} - Time Dependent";
            var capability = new Capability
            {
                Name = name,
                IsMandatory = true,
                IsTimeDependent = true,
                Discretes = new[] { "Value 1", "Value 2", "Value 3" },
            };

            var capabilityId = testContext.Api.Capabilities.Create(capability);

            var mainApiCapability = testContext.Api.Capabilities.Read(capabilityId);
            Assert.IsNotNull(mainApiCapability);
            Assert.IsFalse(testContext.Api.Capabilities.ReadAll().Any(x => x.Name.Equals(linkedName))); // Linked capabilities should not be accessible from API

            var mainCoreCapability = testContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.Name.Equal(name)).SingleOrDefault();
            Assert.IsNotNull(mainCoreCapability);

            var linkedCoreCapability = testContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.Name.Equal(linkedName)).SingleOrDefault();
            Assert.IsNotNull(linkedCoreCapability);

            Assert.IsTrue(TimeDependentCapabilityLink.TryDeserialize(mainCoreCapability.Remarks, out var linkedResult));
            Assert.IsTrue(linkedResult.IsTimeDependent);
            Assert.AreEqual(linkedCoreCapability.ID, linkedResult.LinkedParameterId);

            testContext.Api.Capabilities.Delete(capabilityId);

            // Verify whether both parameters were removed
            mainCoreCapability = testContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.Name.Equal(name)).SingleOrDefault();
            linkedCoreCapability = testContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.Name.Equal(linkedName)).SingleOrDefault();

            Assert.IsNull(mainCoreCapability);
            Assert.IsNull(linkedCoreCapability);
        }

        [TestMethod]
        public void QueryCount()
        {
            var discretes = new string[]
            {
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
            };

            var capability1 = new Capability
            {
                Name = $"Capability1_{Guid.NewGuid()}",
                IsMandatory = true,
                IsTimeDependent = true,
                Discretes = discretes,
            };

            var capability2 = new Capability
            {
                Name = $"Capability2_{Guid.NewGuid()}",
                IsMandatory = true,
                IsTimeDependent = false,
                Discretes = discretes,
            };

            var currentCapabilities = testContext.Api.Capabilities.ReadAll();
            var currentCapabilityCount = currentCapabilities.Count();
            var mandatoryCapabilities = currentCapabilities.Count(x => x.IsMandatory);
            var optionalCapabilities = currentCapabilities.Count(x => !x.IsMandatory);
            var timeDependentCapabilities = currentCapabilities.Count(x => x.IsTimeDependent);

            testContext.Api.Capabilities.CreateOrUpdate(new[] { capability1, capability2 });

            Assert.AreEqual(currentCapabilityCount + 2, testContext.Api.Capabilities.Query().Count());
            Assert.AreEqual(optionalCapabilities, testContext.Api.Capabilities.Query().Count(x => !x.IsMandatory));
            Assert.AreEqual(mandatoryCapabilities + 2, testContext.Api.Capabilities.Query().Count(x => x.IsMandatory));
            Assert.AreEqual(mandatoryCapabilities + 2, testContext.Api.Capabilities.Query().Count(x => x.IsMandatory == true));
            Assert.AreEqual(mandatoryCapabilities + 2, testContext.Api.Capabilities.Query().Count(x => x.IsMandatory.Equals(true)));
            Assert.AreEqual(timeDependentCapabilities + 1, testContext.Api.Capabilities.Query().Count(x => x.IsTimeDependent == true));
            Assert.AreEqual(1, testContext.Api.Capabilities.Query().Count(x => x.Name == capability1.Name));
            Assert.AreEqual(2, testContext.Api.Capabilities.Query().Count(x => x.Discretes.Contains(discretes[1])));

            testContext.Api.Capabilities.Delete(new[] { capability1, capability2 });
        }

        [TestMethod]
        public void QueryWhere()
        {
            var discretes = new string[]
{
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
                Guid.NewGuid().ToString(),
};

            var capability1 = new Capability
            {
                Name = $"Capability1_{Guid.NewGuid()}",
                IsMandatory = true,
                IsTimeDependent = true,
                Discretes = discretes,
            };

            var capability2 = new Capability
            {
                Name = $"Capability2_{Guid.NewGuid()}",
                IsMandatory = true,
                IsTimeDependent = false,
                Discretes = discretes,
            };

            testContext.Api.Capabilities.CreateOrUpdate(new[] { capability1, capability2 });

            Assert.AreEqual(2, testContext.Api.Capabilities.Query().Where(x => x.Name.Contains(capability1.Name) || x.Name.Contains(capability2.Name)).Count());
            Assert.AreEqual(2, testContext.Api.Capabilities.Query().Where(x => x.Name == capability1.Name || x.Name == capability2.Name).Count());
            Assert.AreEqual(2, testContext.Api.Capabilities.Query().Where(x => x.Name.Equals(capability1.Name) || x.Name.Equals(capability2.Name)).Count());
            Assert.AreEqual(2, testContext.Api.Capabilities.Query().Where(x => String.Equals(x.Name, capability1.Name) || String.Equals(capability2.Name, x.Name)).Count());
            Assert.AreEqual(2, testContext.Api.Capabilities.Query().Where(x => x.Name.StartsWith(capability1.Name) || x.Name.StartsWith(capability2.Name)).Count());
            Assert.AreEqual(2, testContext.Api.Capabilities.Query().Where(x => x.Name.EndsWith(capability1.Name) || x.Name.EndsWith(capability2.Name)).Count());

            Assert.AreEqual(0, testContext.Api.Capabilities.Query().Where(x => (x.Name.Equals(capability1.Name) || x.Name.Equals(capability2.Name)) && !x.IsMandatory).Count());
            Assert.AreEqual(2, testContext.Api.Capabilities.Query().Where(x => (x.Name.Equals(capability1.Name) || x.Name.Equals(capability2.Name)) && x.IsMandatory).Count());
            Assert.AreEqual(2, testContext.Api.Capabilities.Query().Where(x => (x.Name.Equals(capability1.Name) || x.Name.Equals(capability2.Name)) && x.IsMandatory == true).Count());
            Assert.AreEqual(2, testContext.Api.Capabilities.Query().Where(x => (x.Name.Equals(capability1.Name) || x.Name.Equals(capability2.Name)) && x.IsMandatory.Equals(true)).Count());
            Assert.AreEqual(1, testContext.Api.Capabilities.Query().Where(x => (x.Name.Equals(capability1.Name) || x.Name.Equals(capability2.Name)) && x.IsTimeDependent).Count());
            Assert.AreEqual(1, testContext.Api.Capabilities.Query().Where(x => x.Name.Equals(capability1.Name)).Count());
            Assert.AreEqual(2, testContext.Api.Capabilities.Query().Where(x => x.Discretes.Contains(discretes[1])).Count());
        }

        [TestMethod]
        public void ReadAllPaged()
        {
            foreach (var page in testContext.Api.Capabilities.ReadAllPaged())
            {
                foreach (var capability in page)
                {
                    Assert.IsNotNull(capability);
                }
            }
        }
    }
}
