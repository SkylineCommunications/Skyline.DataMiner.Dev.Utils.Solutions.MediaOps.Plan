namespace RT_MediaOps.Plan.RST.Capabilities
{
    using System.Collections.Generic;
    using System.Linq;

    using RT_MediaOps.Plan.RegressionTests;

    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

    [TestClass]
    [TestCategory("IntegrationTest")]
    [DoNotParallelize]
    public sealed class BasicTests : IDisposable
    {
        private readonly TestObjectCreator objectCreator;

        public BasicTests()
        {
            objectCreator = new TestObjectCreator(TestContext);
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

            objectCreator.CreateCapability(capability);

            var createdCapability = TestContext.Api.Capabilities.Read(capability.ID);
            Assert.IsNotNull(createdCapability);
            Assert.AreEqual(name, createdCapability.Name);
            Assert.IsTrue(createdCapability.IsMandatory);
            CollectionAssert.AreEquivalent(new List<string> { "Value 1", "Value 2", "Value 3" }, createdCapability.Discretes.ToList());
            Assert.IsFalse(createdCapability.IsTimeDependent);

            TestContext.Api.Capabilities.Delete(capability.ID);
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

            objectCreator.CreateCapability(capability);

            var mainApiCapability = TestContext.Api.Capabilities.Read(capability.ID);
            Assert.IsNotNull(mainApiCapability);
            Assert.IsFalse(TestContext.Api.Capabilities.Read().Any(x => x.Name.Equals(linkedName))); // Linked capabilities should not be accessible from API

            var mainCoreCapability = TestContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.Name.Equal(name)).SingleOrDefault();
            Assert.IsNotNull(mainCoreCapability);

            var linkedCoreCapability = TestContext.ProfileHelper.ProfileParameters.Read(Skyline.DataMiner.Net.Profiles.ParameterExposers.Name.Equal(linkedName)).SingleOrDefault();
            Assert.IsNotNull(linkedCoreCapability);

            Assert.IsTrue(TimeDependentCapabilityLink.TryDeserialize(mainCoreCapability.Remarks, out var linkedResult));
            Assert.IsTrue(linkedResult.IsTimeDependent);
            Assert.AreEqual(linkedCoreCapability.ID, linkedResult.LinkedParameterId);

            TestContext.Api.Capabilities.Delete(capability.ID);

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
            objectCreator.CreateCapability(capability);

            capability = TestContext.Api.Capabilities.Read(capability.ID);
            capability.IsTimeDependent = true;

            try
            {
                TestContext.Api.Capabilities.Update(capability);
            }
            catch (MediaOpsException ex)
            {
                StringAssert.Contains(ex.Message, "Changing the time dependency of a capability is not allowed.");

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var capabilityConfigurationError = ex.TraceData.ErrorData.OfType<CapabilityError>().SingleOrDefault();
                Assert.IsNotNull(capabilityConfigurationError);

                var capabilityConfigurationInvalidTimeDependencyError = capabilityConfigurationError as CapabilityInvalidTimeDependencyError;
                Assert.IsNotNull(capabilityConfigurationInvalidTimeDependencyError);
                Assert.AreEqual(capability.ID, capabilityConfigurationInvalidTimeDependencyError.Id);
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
            objectCreator.CreateCapability(capability);

            capability = TestContext.Api.Capabilities.Read(capability.ID);
            capability.IsTimeDependent = false;

            try
            {
                TestContext.Api.Capabilities.Update(capability);
            }
            catch (MediaOpsException ex)
            {
                StringAssert.Contains(ex.Message, "Changing the time dependency of a capability is not allowed.");

                Assert.AreEqual(1, ex.TraceData.ErrorData.Count);
                var capabilityConfigurationError = ex.TraceData.ErrorData.OfType<CapabilityError>().SingleOrDefault();
                Assert.IsNotNull(capabilityConfigurationError);

                var capabilityConfigurationInvalidTimeDependencyError = capabilityConfigurationError as CapabilityInvalidTimeDependencyError;
                Assert.IsNotNull(capabilityConfigurationInvalidTimeDependencyError);
                Assert.AreEqual(capability.ID, capabilityConfigurationInvalidTimeDependencyError.Id);
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

            objectCreator.CreateCapability(capability);

            var apiCapability = TestContext.Api.Capabilities.Read(capability.ID);

            Assert.AreEqual(1, apiCapability.Discretes.Count());
            Assert.AreEqual(discreteValue, apiCapability.Discretes.Single());
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

        [TestMethod]
        public void ReadWithEmptyListReturnsEmptyList()
        {
            var capabilities = TestContext.Api.Capabilities.Read(new List<Guid>());
            Assert.IsNotNull(capabilities);
            Assert.AreEqual(0, capabilities.Count());
        }

        [TestMethod]
        public void CreateWithNullNameThrowsException()
        {
            var capability = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
            {
                Name = null,
            };

            try
            {
                objectCreator.CreateCapability(capability);
            }
            catch (MediaOpsException ex)
            {
                var invalidNameError = ex.TraceData.ErrorData.OfType<CapabilityInvalidNameError>().SingleOrDefault();
                Assert.IsNotNull(invalidNameError);
                Assert.AreEqual("Name cannot be empty.", invalidNameError.ErrorMessage);
                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }

        [TestMethod]
        public void CreateWithEmptyNameThrowsException()
        {
            var capability = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability()
            {
                Name = string.Empty,
            };

            try
            {
                objectCreator.CreateCapability(capability);
            }
            catch (MediaOpsException ex)
            {
                var invalidNameError = ex.TraceData.ErrorData.OfType<CapabilityInvalidNameError>().SingleOrDefault();
                Assert.IsNotNull(invalidNameError);
                Assert.AreEqual("Name cannot be empty.", invalidNameError.ErrorMessage);
                return;
            }

            Assert.Fail("Expected exception was not thrown.");
        }
    }
}
