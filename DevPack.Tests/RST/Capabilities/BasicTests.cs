namespace RT_MediaOps.Plan.RST.Capabilities
{
    using System.Collections.Generic;
    using System.Linq;

    using RT_MediaOps.Plan.RegressionTests;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

    [TestClass]
    [TestCategory("IntegrationTest")]
    [DoNotParallelize]
    public sealed class BasicTests : IDisposable
    {
        private readonly ResourceStudioObjectCreator objectCreator;

        public BasicTests()
        {
            objectCreator = new ResourceStudioObjectCreator(TestContext.Api);
        }

        private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

        public void Dispose()
        {
            objectCreator.Dispose();
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
            };

            capability.SetDiscretes(new[] { "Value 1", "Value 2", "Value 3" });

            var capabilityId = TestContext.Api.Capabilities.Create(capability);

            var createdCapability = TestContext.Api.Capabilities.Read(capabilityId);
            Assert.IsNotNull(createdCapability);
            Assert.AreEqual(name, createdCapability.Name);
            Assert.IsTrue(createdCapability.IsMandatory);
            CollectionAssert.AreEquivalent(new List<string> { "Value 1", "Value 2", "Value 3" }, createdCapability.Discretes.ToList());
            Assert.IsFalse(createdCapability.IsTimeDependent);

            TestContext.Api.Capabilities.Delete(capabilityId);
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
            };

            capability.SetDiscretes(new[] { "Value 1", "Value 2", "Value 3" });

            var capabilityId = TestContext.Api.Capabilities.Create(capability);

            var mainApiCapability = TestContext.Api.Capabilities.Read(capabilityId);
            Assert.IsNotNull(mainApiCapability);
            Assert.IsFalse(TestContext.Api.Capabilities.Read().Any(x => x.Name.Equals(linkedName))); // Linked capabilities should not be accessible from API

            var mainCoreCapability = TestContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.Name.Equal(name)).SingleOrDefault();
            Assert.IsNotNull(mainCoreCapability);

            var linkedCoreCapability = TestContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.Name.Equal(linkedName)).SingleOrDefault();
            Assert.IsNotNull(linkedCoreCapability);

            Assert.IsTrue(TimeDependentCapabilityLink.TryDeserialize(mainCoreCapability.Remarks, out var linkedResult));
            Assert.IsTrue(linkedResult.IsTimeDependent);
            Assert.AreEqual(linkedCoreCapability.ID, linkedResult.LinkedParameterId);

            TestContext.Api.Capabilities.Delete(capabilityId);

            // Verify whether both parameters were removed
            mainCoreCapability = TestContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.Name.Equal(name)).SingleOrDefault();
            linkedCoreCapability = TestContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.Name.Equal(linkedName)).SingleOrDefault();

            Assert.IsNull(mainCoreCapability);
            Assert.IsNull(linkedCoreCapability);
        }

        [TestMethod]
        public void ChangeRegularCapabilityToTimeDependentThrowsException()
        {
            var prefix = Guid.NewGuid().ToString();

            var capability = new Capability
            {
                Name = $"{prefix}_Capability",
                IsTimeDependent = false,
            };
            capability.SetDiscretes(new[] { "Value 1", "Value 2", "Value 3" });
            var capabilityId = TestContext.Api.Capabilities.Create(capability);

            capability = TestContext.Api.Capabilities.Read(capabilityId);
            capability.IsTimeDependent = true;

            try
            {
                TestContext.Api.Capabilities.Update(capability);
            }
            catch (MediaOpsException ex)
            {
                StringAssert.Contains(ex.Message, "Changing the time dependency of a capability is not allowed.");

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var capabilityConfigurationError = ex.TraceData.ErrorData.OfType<CapabilityConfigurationError>().SingleOrDefault();
                Assert.IsNotNull(capabilityConfigurationError);

                Assert.AreEqual(CapabilityConfigurationError.Reason.InvalidTimeDependency, capabilityConfigurationError.ErrorReason);
                Assert.AreEqual("Changing the time dependency of a capability is not allowed.", capabilityConfigurationError.ErrorMessage);

                return;
            }

            Assert.Fail("Expected exception was not thrown when changing a regular capability to time-dependent.");
        }

        [TestMethod]
        public void ChangeTimeDependentToRegularCapabilityThrowsException()
        {
            var prefix = Guid.NewGuid().ToString();
            var capability = new Capability
            {
                Name = $"{prefix}_Capability",
                IsTimeDependent = true,
            };
            capability.SetDiscretes(new[] { "Value 1", "Value 2", "Value 3" });
            var capabilityId = TestContext.Api.Capabilities.Create(capability);

            capability = TestContext.Api.Capabilities.Read(capabilityId);
            capability.IsTimeDependent = false;

            try
            {
                TestContext.Api.Capabilities.Update(capability);
            }
            catch (MediaOpsException ex)
            {
                StringAssert.Contains(ex.Message, "Changing the time dependency of a capability is not allowed.");

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var capabilityConfigurationError = ex.TraceData.ErrorData.OfType<CapabilityConfigurationError>().SingleOrDefault();
                Assert.IsNotNull(capabilityConfigurationError);

                Assert.AreEqual(CapabilityConfigurationError.Reason.InvalidTimeDependency, capabilityConfigurationError.ErrorReason);
                Assert.AreEqual("Changing the time dependency of a capability is not allowed.", capabilityConfigurationError.ErrorMessage);

                return;
            }

            Assert.Fail("Expected exception was not thrown when changing a time-dependent capability to regular.");
        }

        [TestMethod]
        public void DuplicateDiscretes()
        {
            string name = $"Capability_{Guid.NewGuid()}";
            string linkedName = $"{name} - Time Dependent";
            var capability = new Capability
            {
                Name = name,
                IsMandatory = true,
                IsTimeDependent = true,
            };

            string discreteValue = "Value 1";
            capability.SetDiscretes(Enumerable.Repeat(discreteValue, 10));

            var capabilityId = TestContext.Api.Capabilities.Create(capability);

            var apiCapability = TestContext.Api.Capabilities.Read(capabilityId);

            Assert.AreEqual(1, apiCapability.Discretes.Count());
            Assert.AreEqual(discreteValue, apiCapability.Discretes.Single());
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
            };

            capability1.SetDiscretes(discretes);

            var capability2 = new Capability
            {
                Name = $"Capability2_{Guid.NewGuid()}",
                IsMandatory = true,
                IsTimeDependent = false,
            };

            capability2.SetDiscretes(discretes);

            var currentCapabilities = TestContext.Api.Capabilities.Read();
            var currentCapabilityCount = currentCapabilities.Count();
            var mandatoryCapabilities = currentCapabilities.Count(x => x.IsMandatory);
            var optionalCapabilities = currentCapabilities.Count(x => !x.IsMandatory);
            var timeDependentCapabilities = currentCapabilities.Count(x => x.IsTimeDependent);

            TestContext.Api.Capabilities.CreateOrUpdate(new[] { capability1, capability2 });

            Assert.AreEqual(currentCapabilityCount + 2, TestContext.Api.Capabilities.Query().Count());
            Assert.AreEqual(optionalCapabilities, TestContext.Api.Capabilities.Query().Count(x => !x.IsMandatory));
            Assert.AreEqual(mandatoryCapabilities + 2, TestContext.Api.Capabilities.Query().Count(x => x.IsMandatory));
            Assert.AreEqual(mandatoryCapabilities + 2, TestContext.Api.Capabilities.Query().Count(x => x.IsMandatory == true));
            Assert.AreEqual(mandatoryCapabilities + 2, TestContext.Api.Capabilities.Query().Count(x => x.IsMandatory.Equals(true)));
            Assert.AreEqual(timeDependentCapabilities + 1, TestContext.Api.Capabilities.Query().Count(x => x.IsTimeDependent == true));
            Assert.AreEqual(1, TestContext.Api.Capabilities.Query().Count(x => x.Name == capability1.Name));
            Assert.AreEqual(2, TestContext.Api.Capabilities.Query().Count(x => x.Discretes.Contains(discretes[1])));

            TestContext.Api.Capabilities.Delete(new[] { capability1, capability2 });
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
            };

            capability1.SetDiscretes(discretes);

            capability1.SetDiscretes(discretes);

            var capability2 = new Capability
            {
                Name = $"Capability2_{Guid.NewGuid()}",
                IsMandatory = true,
                IsTimeDependent = false,
            };

            capability2.SetDiscretes(discretes);

            TestContext.Api.Capabilities.CreateOrUpdate(new[] { capability1, capability2 });

            Assert.AreEqual(2, TestContext.Api.Capabilities.Query().Where(x => x.Name.Contains(capability1.Name) || x.Name.Contains(capability2.Name)).Count());
            Assert.AreEqual(2, TestContext.Api.Capabilities.Query().Where(x => x.Name == capability1.Name || x.Name == capability2.Name).Count());
            Assert.AreEqual(2, TestContext.Api.Capabilities.Query().Where(x => x.Name.Equals(capability1.Name) || x.Name.Equals(capability2.Name)).Count());
            Assert.AreEqual(2, TestContext.Api.Capabilities.Query().Where(x => String.Equals(x.Name, capability1.Name) || String.Equals(capability2.Name, x.Name)).Count());
            Assert.AreEqual(2, TestContext.Api.Capabilities.Query().Where(x => x.Name.StartsWith(capability1.Name) || x.Name.StartsWith(capability2.Name)).Count());
            Assert.AreEqual(2, TestContext.Api.Capabilities.Query().Where(x => x.Name.EndsWith(capability1.Name) || x.Name.EndsWith(capability2.Name)).Count());

            Assert.AreEqual(0, TestContext.Api.Capabilities.Query().Where(x => (x.Name.Equals(capability1.Name) || x.Name.Equals(capability2.Name)) && !x.IsMandatory).Count());
            Assert.AreEqual(2, TestContext.Api.Capabilities.Query().Where(x => (x.Name.Equals(capability1.Name) || x.Name.Equals(capability2.Name)) && x.IsMandatory).Count());
            Assert.AreEqual(2, TestContext.Api.Capabilities.Query().Where(x => (x.Name.Equals(capability1.Name) || x.Name.Equals(capability2.Name)) && x.IsMandatory == true).Count());
            Assert.AreEqual(2, TestContext.Api.Capabilities.Query().Where(x => (x.Name.Equals(capability1.Name) || x.Name.Equals(capability2.Name)) && x.IsMandatory.Equals(true)).Count());
            Assert.AreEqual(1, TestContext.Api.Capabilities.Query().Where(x => (x.Name.Equals(capability1.Name) || x.Name.Equals(capability2.Name)) && x.IsTimeDependent).Count());
            Assert.AreEqual(1, TestContext.Api.Capabilities.Query().Where(x => x.Name.Equals(capability1.Name)).Count());
            Assert.AreEqual(2, TestContext.Api.Capabilities.Query().Where(x => x.Discretes.Contains(discretes[1])).Count());
        }

        [TestMethod]
        public void ReadAllPaged()
        {
            foreach (var page in TestContext.Api.Capabilities.ReadPaged())
            {
                foreach (var capability in page)
                {
                    Assert.IsNotNull(capability);
                }
            }
        }
    }
}
