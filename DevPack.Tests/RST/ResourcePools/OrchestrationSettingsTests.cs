namespace RT_MediaOps.Plan.RST.ResourcePools
{
    using System;
    using System.Linq;

    using RT_MediaOps.Plan.RegressionTests;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

    [TestClass]
    [TestCategory("IntegrationTest")]
    public sealed class OrchestrationSettingsTests : IDisposable
    {
        private readonly TestObjectCreator objectCreator;

        public OrchestrationSettingsTests()
        {
            objectCreator = new TestObjectCreator(TestContext);
        }

        private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

        public void Dispose()
        {
            objectCreator.Dispose();
        }

        [TestMethod]
        public void ProfileParametersWithNoValues()
        {
            var prefix = Guid.NewGuid();

            var capability = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability
            {
                Name = $"{prefix}_Capability",
            }
            .SetDiscretes(["Value 1", "Value 2"]);
            objectCreator.CreateCapability(capability);

            var numberCapacity = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity
            {
                Name = $"{prefix}_NumberCapacity",
            };
            var rangeCapacity = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.RangeCapacity
            {
                Name = $"{prefix}_RangeCapacity",
            };
            objectCreator.CreateCapacities([numberCapacity, rangeCapacity]);

            var textConfiguration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextConfiguration
            {
                Name = $"{prefix}_TextConfiguration",
            };
            var numberConfiguration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberConfiguration
            {
                Name = $"{prefix}_NumberConfiguration",
            };
            var discreteTextConfiguration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfiguration
            {
                Name = $"{prefix}_DiscreteTextConfiguration",
            }
            .AddDiscrete(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextDiscreet("A", "A"));
            var discreteNumberConfiguration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteNumberConfiguration
            {
                Name = $"{prefix}_DiscreteNumberConfiguration",
            }
            .AddDiscrete(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberDiscreet(1, "A"));
            objectCreator.CreateConfigurations([textConfiguration, numberConfiguration, discreteTextConfiguration, discreteNumberConfiguration]);

            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool
            {
                Name = $"{prefix}_ResourcePool",
            };
            resourcePool.OrchestrationSettings
                .AddCapability(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySetting(capability))
                .AddCapacity(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacitySetting(numberCapacity))
                .AddCapacity(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.RangeCapacitySetting(rangeCapacity))
                .AddConfiguration(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextConfigurationSetting(textConfiguration))
                .AddConfiguration(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberConfigurationSetting(numberConfiguration))
                .AddConfiguration(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteTextConfigurationSetting(discreteTextConfiguration))
                .AddConfiguration(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.DiscreteNumberConfigurationSetting(discreteNumberConfiguration));
            objectCreator.CreateResourcePool(resourcePool);

            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);
            Assert.IsNotNull(resourcePool);
            Assert.IsNotNull(resourcePool.OrchestrationSettings);
            Assert.AreEqual(1, resourcePool.OrchestrationSettings.Capabilities.Count);
            Assert.AreEqual(2, resourcePool.OrchestrationSettings.Capacities.Count);
            Assert.AreEqual(4, resourcePool.OrchestrationSettings.Configurations.Count);

            var capabilityIds = resourcePool.OrchestrationSettings.Capabilities.Select(c => c.Id).ToList();
            Assert.IsTrue(capabilityIds.Contains(capability.Id));

            var capacityIds = resourcePool.OrchestrationSettings.Capacities.Select(c => c.Id).ToList();
            Assert.IsTrue(capacityIds.Contains(numberCapacity.Id));
            Assert.IsTrue(capacityIds.Contains(rangeCapacity.Id));

            var configurationIds = resourcePool.OrchestrationSettings.Configurations.Select(c => c.Id).ToList();
            Assert.IsTrue(configurationIds.Contains(textConfiguration.Id));
            Assert.IsTrue(configurationIds.Contains(numberConfiguration.Id));
            Assert.IsTrue(configurationIds.Contains(discreteTextConfiguration.Id));
            Assert.IsTrue(configurationIds.Contains(discreteNumberConfiguration.Id));
        }

        [TestMethod]
        public void CreateWithDuplicateSettingsThrowsException()
        {
            var prefix = Guid.NewGuid();

            var capability = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.Capability
            {
                Name = $"{prefix}_Capability",
            }
            .SetDiscretes(["Value 1", "Value 2"]);
            objectCreator.CreateCapability(capability);

            var numberCapacity = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacity
            {
                Name = $"{prefix}_NumberCapacity",
            };
            objectCreator.CreateCapacity(numberCapacity);

            var textConfiguration = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextConfiguration
            {
                Name = $"{prefix}_TextConfiguration",
            };
            objectCreator.CreateConfiguration(textConfiguration);

            var resourcePool = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool
            {
                Name = $"{prefix}_ResourcePool",
            };
            resourcePool.OrchestrationSettings
                .AddCapability(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySetting(capability))
                .AddCapability(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.CapabilitySetting(capability))
                .AddCapacity(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacitySetting(numberCapacity))
                .AddCapacity(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.NumberCapacitySetting(numberCapacity))
                .AddConfiguration(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextConfigurationSetting(textConfiguration))
                .AddConfiguration(new Skyline.DataMiner.Solutions.MediaOps.Plan.API.TextConfigurationSetting(textConfiguration));

            try
            {
                objectCreator.CreateResourcePool(resourcePool);
            }
            catch (MediaOpsException ex)
            {
                Assert.AreEqual(3, ex.TraceData.ErrorData.Count);

                var orchestrationSettingsErrors = ex.TraceData.ErrorData.OfType<OrchestrationSettingsError>();
                Assert.AreEqual(3, orchestrationSettingsErrors.Count());

                var invalidCapacitySettingsError = orchestrationSettingsErrors.OfType<OrchestrationSettingsInvalidCapacitySettingsError>().SingleOrDefault();
                Assert.IsNotNull(invalidCapacitySettingsError);
                Assert.AreEqual($"Capacity with ID '{numberCapacity.Id}' is defined 2 times. Duplicate capacity settings are not allowed.", invalidCapacitySettingsError.ErrorMessage);
                Assert.AreEqual(numberCapacity.Id, invalidCapacitySettingsError.CapacityId);
                Assert.AreEqual(resourcePool.OrchestrationSettings.Id, invalidCapacitySettingsError.Id);

                var invalidCapabilitySettingsError = orchestrationSettingsErrors.OfType<OrchestrationSettingsInvalidCapabilitySettingsError>().SingleOrDefault();
                Assert.IsNotNull(invalidCapabilitySettingsError);
                Assert.AreEqual($"Capability with ID '{capability.Id}' is defined 2 times. Duplicate capability settings are not allowed.", invalidCapabilitySettingsError.ErrorMessage);
                Assert.AreEqual(capability.Id, invalidCapabilitySettingsError.CapabilityId);
                Assert.AreEqual(resourcePool.OrchestrationSettings.Id, invalidCapabilitySettingsError.Id);

                var invalidConfigurationSettingsError = orchestrationSettingsErrors.OfType<OrchestrationSettingsInvalidConfigurationSettingsError>().SingleOrDefault();
                Assert.IsNotNull(invalidConfigurationSettingsError);
                Assert.AreEqual($"Configuration with ID '{textConfiguration.Id}' is defined 2 times. Duplicate configuration settings are not allowed.", invalidConfigurationSettingsError.ErrorMessage);
                Assert.AreEqual(textConfiguration.Id, invalidConfigurationSettingsError.ConfigurationId);
                Assert.AreEqual(resourcePool.OrchestrationSettings.Id, invalidConfigurationSettingsError.Id);
            }
        }
    }
}