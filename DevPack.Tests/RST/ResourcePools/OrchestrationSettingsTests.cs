namespace RT_MediaOps.Plan.RST.ResourcePools
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using RT_MediaOps.Plan.RegressionTests;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
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
        public void ResourceStudioOrchestrationSettings_AddAndRemoveCapability_NoPersistence()
        {
            var prefix = Guid.NewGuid();

            var capability = new Capability
            {
                Name = $"{prefix}_Capability",
            }
            .SetDiscretes(["Value 1", "Value 2"]);
            objectCreator.CreateCapability(capability);

            var resourcePool = new ResourcePool
            {
                Name = $"{prefix}_ResourcePool",
            };

            var orchestrationSettings = resourcePool.OrchestrationSettings;

            // Add
            orchestrationSettings.AddCapability(new CapabilitySetting(capability));
            Assert.AreEqual(1, orchestrationSettings.Capabilities.Count);
            Assert.AreEqual(capability.ID, orchestrationSettings.Capabilities.Single().Id);

            orchestrationSettings.RemoveCapability(orchestrationSettings.Capabilities.Single());
            Assert.AreEqual(0, orchestrationSettings.Capabilities.Count);
        }

        [TestMethod]
        public void ResourceStudioOrchestrationSettings_AddAndRemoveCapacities_NoPersistence()
        {
            var prefix = Guid.NewGuid();

            var numberCapacity = new NumberCapacity
            {
                Name = $"{prefix}_NumberCapacity",
            };
            var rangeCapacity = new RangeCapacity
            {
                Name = $"{prefix}_RangeCapacity",
            };
            objectCreator.CreateCapacities([numberCapacity, rangeCapacity]);

            var resourcePool = new ResourcePool
            {
                Name = $"{prefix}_ResourcePool",
            };

            var orchestrationSettings = resourcePool.OrchestrationSettings;

            // Add
            orchestrationSettings
                .AddCapacity(new NumberCapacitySetting(numberCapacity))
                .AddCapacity(new RangeCapacitySetting(rangeCapacity));

            Assert.AreEqual(2, orchestrationSettings.Capacities.Count);
            Assert.IsTrue(orchestrationSettings.Capacities.Any(c => c.Id == numberCapacity.ID));
            Assert.IsTrue(orchestrationSettings.Capacities.Any(c => c.Id == rangeCapacity.ID));

            foreach (var capacity in orchestrationSettings.Capacities.ToList())
            {
                orchestrationSettings.RemoveCapacity(capacity);
            }

            Assert.AreEqual(0, orchestrationSettings.Capacities.Count);
        }

        [TestMethod]
        public void ResourceStudioOrchestrationSettings_AddAndRemoveConfigurations_NoPersistence()
        {
            var prefix = Guid.NewGuid();

            var textConfiguration = new TextConfiguration
            {
                Name = $"{prefix}_TextConfiguration",
            };
            var numberConfiguration = new NumberConfiguration
            {
                Name = $"{prefix}_NumberConfiguration",
            };
            var discreteTextConfiguration = new DiscreteTextConfiguration
            {
                Name = $"{prefix}_DiscreteTextConfiguration",
            }
            .AddDiscrete(new TextDiscreet("A", "A"));
            var discreteNumberConfiguration = new DiscreteNumberConfiguration
            {
                Name = $"{prefix}_DiscreteNumberConfiguration",
            }
            .AddDiscrete(new NumberDiscreet(1, "One"));
            objectCreator.CreateConfigurations([textConfiguration, numberConfiguration, discreteTextConfiguration, discreteNumberConfiguration]);

            var resourcePool = new ResourcePool
            {
                Name = $"{prefix}_ResourcePool",
            };

            var orchestrationSettings = resourcePool.OrchestrationSettings;

            // Add
            orchestrationSettings
                .AddConfiguration(new TextConfigurationSetting(textConfiguration))
                .AddConfiguration(new NumberConfigurationSetting(numberConfiguration))
                .AddConfiguration(new DiscreteTextConfigurationSetting(discreteTextConfiguration))
                .AddConfiguration(new DiscreteNumberConfigurationSetting(discreteNumberConfiguration));

            Assert.AreEqual(4, orchestrationSettings.Configurations.Count);
            var configurationIds = orchestrationSettings.Configurations.Select(c => c.Id).ToList();
            Assert.IsTrue(configurationIds.Contains(textConfiguration.ID));
            Assert.IsTrue(configurationIds.Contains(numberConfiguration.ID));
            Assert.IsTrue(configurationIds.Contains(discreteTextConfiguration.ID));
            Assert.IsTrue(configurationIds.Contains(discreteNumberConfiguration.ID));

            foreach (var configuration in orchestrationSettings.Configurations.ToList())
            {
                orchestrationSettings.RemoveConfiguration(configuration);
            }

            Assert.AreEqual(0, orchestrationSettings.Configurations.Count);
        }

        [TestMethod]
        public void ResourceStudioOrchestrationSettings_AddAndRemoveCapability_WithPersistence()
        {
            var prefix = Guid.NewGuid();

            var capability = new Capability
            {
                Name = $"{prefix}_Capability",
            }
            .SetDiscretes(["Value 1", "Value 2"]);
            objectCreator.CreateCapability(capability);

            var resourcePool = new ResourcePool
            {
                Name = $"{prefix}_ResourcePool",
            };

            var orchestrationSettings = resourcePool.OrchestrationSettings;

            // Add
            orchestrationSettings.AddCapability(new CapabilitySetting(capability));
            Assert.AreEqual(1, orchestrationSettings.Capabilities.Count);
            Assert.AreEqual(capability.ID, orchestrationSettings.Capabilities.Single().Id);

            objectCreator.CreateResourcePool(resourcePool);
            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.ID);

            resourcePool.OrchestrationSettings.RemoveCapability(orchestrationSettings.Capabilities.Single());
            Assert.AreEqual(0, resourcePool.OrchestrationSettings.Capabilities.Count);
        }

        [TestMethod]
        public void ResourceStudioOrchestrationSettings_AddAndRemoveCapacities_WithPersistence()
        {
            var prefix = Guid.NewGuid();

            var numberCapacity = new NumberCapacity
            {
                Name = $"{prefix}_NumberCapacity",
            };
            var rangeCapacity = new RangeCapacity
            {
                Name = $"{prefix}_RangeCapacity",
            };
            objectCreator.CreateCapacities([numberCapacity, rangeCapacity]);

            var resourcePool = new ResourcePool
            {
                Name = $"{prefix}_ResourcePool",
            };

            var orchestrationSettings = resourcePool.OrchestrationSettings;

            // Add
            orchestrationSettings
                .AddCapacity(new NumberCapacitySetting(numberCapacity))
                .AddCapacity(new RangeCapacitySetting(rangeCapacity));

            Assert.AreEqual(2, orchestrationSettings.Capacities.Count);
            Assert.IsTrue(orchestrationSettings.Capacities.Any(c => c.Id == numberCapacity.ID));
            Assert.IsTrue(orchestrationSettings.Capacities.Any(c => c.Id == rangeCapacity.ID));

            objectCreator.CreateResourcePool(resourcePool);
            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.ID);

            foreach (var capacity in orchestrationSettings.Capacities.ToList())
            {
                resourcePool.OrchestrationSettings.RemoveCapacity(capacity);
            }

            Assert.AreEqual(0, resourcePool.OrchestrationSettings.Capacities.Count);
        }

        [TestMethod]
        public void ResourceStudioOrchestrationSettings_AddAndRemoveConfigurations_WithPersistence()
        {
            var prefix = Guid.NewGuid();

            var textConfiguration = new TextConfiguration
            {
                Name = $"{prefix}_TextConfiguration",
            };
            var numberConfiguration = new NumberConfiguration
            {
                Name = $"{prefix}_NumberConfiguration",
            };
            var discreteTextConfiguration = new DiscreteTextConfiguration
            {
                Name = $"{prefix}_DiscreteTextConfiguration",
            }
            .AddDiscrete(new TextDiscreet("A", "A"));
            var discreteNumberConfiguration = new DiscreteNumberConfiguration
            {
                Name = $"{prefix}_DiscreteNumberConfiguration",
            }
            .AddDiscrete(new NumberDiscreet(1, "One"));
            objectCreator.CreateConfigurations([textConfiguration, numberConfiguration, discreteTextConfiguration, discreteNumberConfiguration]);

            var resourcePool = new ResourcePool
            {
                Name = $"{prefix}_ResourcePool",
            };

            var orchestrationSettings = resourcePool.OrchestrationSettings;

            // Add
            orchestrationSettings
                .AddConfiguration(new TextConfigurationSetting(textConfiguration))
                .AddConfiguration(new NumberConfigurationSetting(numberConfiguration))
                .AddConfiguration(new DiscreteTextConfigurationSetting(discreteTextConfiguration))
                .AddConfiguration(new DiscreteNumberConfigurationSetting(discreteNumberConfiguration));

            Assert.AreEqual(4, orchestrationSettings.Configurations.Count);
            var configurationIds = orchestrationSettings.Configurations.Select(c => c.Id).ToList();
            Assert.IsTrue(configurationIds.Contains(textConfiguration.ID));
            Assert.IsTrue(configurationIds.Contains(numberConfiguration.ID));
            Assert.IsTrue(configurationIds.Contains(discreteTextConfiguration.ID));
            Assert.IsTrue(configurationIds.Contains(discreteNumberConfiguration.ID));

            objectCreator.CreateResourcePool(resourcePool);
            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.ID);

            foreach (var configuration in orchestrationSettings.Configurations.ToList())
            {
                resourcePool.OrchestrationSettings.RemoveConfiguration(configuration);
            }

            Assert.AreEqual(0, resourcePool.OrchestrationSettings.Configurations.Count);
        }

        [TestMethod]
        public void ParameterSettingsWithNoValues_CreatePool()
        {
            var prefix = Guid.NewGuid();

            var capability = new Capability
            {
                Name = $"{prefix}_Capability",
            }
            .SetDiscretes(["Value 1", "Value 2"]);
            objectCreator.CreateCapability(capability);

            var numberCapacity = new NumberCapacity
            {
                Name = $"{prefix}_NumberCapacity",
            };
            var rangeCapacity = new RangeCapacity
            {
                Name = $"{prefix}_RangeCapacity",
            };
            objectCreator.CreateCapacities([numberCapacity, rangeCapacity]);

            var textConfiguration = new TextConfiguration
            {
                Name = $"{prefix}_TextConfiguration",
            };
            var numberConfiguration = new NumberConfiguration
            {
                Name = $"{prefix}_NumberConfiguration",
            };
            var discreteTextConfiguration = new DiscreteTextConfiguration
            {
                Name = $"{prefix}_DiscreteTextConfiguration",
            }
            .AddDiscrete(new TextDiscreet("A", "A"));
            var discreteNumberConfiguration = new DiscreteNumberConfiguration
            {
                Name = $"{prefix}_DiscreteNumberConfiguration",
            }
            .AddDiscrete(new NumberDiscreet(1, "A"));
            objectCreator.CreateConfigurations([textConfiguration, numberConfiguration, discreteTextConfiguration, discreteNumberConfiguration]);

            var resourcePool = new ResourcePool
            {
                Name = $"{prefix}_ResourcePool",
            };
            resourcePool.OrchestrationSettings
                .AddCapability(new CapabilitySetting(capability))
                .AddCapacity(new NumberCapacitySetting(numberCapacity))
                .AddCapacity(new RangeCapacitySetting(rangeCapacity))
                .AddConfiguration(new TextConfigurationSetting(textConfiguration))
                .AddConfiguration(new NumberConfigurationSetting(numberConfiguration))
                .AddConfiguration(new DiscreteTextConfigurationSetting(discreteTextConfiguration))
                .AddConfiguration(new DiscreteNumberConfigurationSetting(discreteNumberConfiguration));
            objectCreator.CreateResourcePool(resourcePool);

            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.ID);
            Assert.IsNotNull(resourcePool);
            Assert.IsNotNull(resourcePool.OrchestrationSettings);
            Assert.AreEqual(1, resourcePool.OrchestrationSettings.Capabilities.Count);
            Assert.AreEqual(2, resourcePool.OrchestrationSettings.Capacities.Count);
            Assert.AreEqual(4, resourcePool.OrchestrationSettings.Configurations.Count);

            var capabilityIds = resourcePool.OrchestrationSettings.Capabilities.Select(c => c.Id).ToList();
            Assert.IsTrue(capabilityIds.Contains(capability.ID));

            var capacityIds = resourcePool.OrchestrationSettings.Capacities.Select(c => c.Id).ToList();
            Assert.IsTrue(capacityIds.Contains(numberCapacity.ID));
            Assert.IsTrue(capacityIds.Contains(rangeCapacity.ID));

            var configurationIds = resourcePool.OrchestrationSettings.Configurations.Select(c => c.Id).ToList();
            Assert.IsTrue(configurationIds.Contains(textConfiguration.ID));
            Assert.IsTrue(configurationIds.Contains(numberConfiguration.ID));
            Assert.IsTrue(configurationIds.Contains(discreteTextConfiguration.ID));

            // Discrete number configuration contains two discrete values, only one is added as configuration references the existing object
            Assert.IsTrue(configurationIds.Contains(discreteNumberConfiguration.ID));
        }

        [TestMethod]
        public void ParameterSettingsWithNoValues_UpdatePool()
        {
            var prefix = Guid.NewGuid();

            var capability = new Capability
            {
                Name = $"{prefix}_Capability",
            }
            .SetDiscretes(["Value 1", "Value 2"]);
            objectCreator.CreateCapability(capability);

            var numberCapacity = new NumberCapacity
            {
                Name = $"{prefix}_NumberCapacity",
            };
            var rangeCapacity = new RangeCapacity
            {
                Name = $"{prefix}_RangeCapacity",
            };
            objectCreator.CreateCapacities([numberCapacity, rangeCapacity]);

            var textConfiguration = new TextConfiguration
            {
                Name = $"{prefix}_TextConfiguration",
            };
            var numberConfiguration = new NumberConfiguration
            {
                Name = $"{prefix}_NumberConfiguration",
            };
            var discreteTextConfiguration = new DiscreteTextConfiguration
            {
                Name = $"{prefix}_DiscreteTextConfiguration",
            }
            .AddDiscrete(new TextDiscreet("A", "A"));
            var discreteNumberConfiguration = new DiscreteNumberConfiguration
            {
                Name = $"{prefix}_DiscreteNumberConfiguration",
            }
            .AddDiscrete(new NumberDiscreet(1, "A"));
            objectCreator.CreateConfigurations([textConfiguration, numberConfiguration, discreteTextConfiguration, discreteNumberConfiguration]);

            var resourcePool = new ResourcePool
            {
                Name = $"{prefix}_ResourcePool",
            };

            objectCreator.CreateResourcePool(resourcePool);
            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.ID);

            resourcePool.OrchestrationSettings
                .AddCapability(new CapabilitySetting(capability))
                .AddCapacity(new NumberCapacitySetting(numberCapacity))
                .AddCapacity(new RangeCapacitySetting(rangeCapacity))
                .AddConfiguration(new TextConfigurationSetting(textConfiguration))
                .AddConfiguration(new NumberConfigurationSetting(numberConfiguration))
                .AddConfiguration(new DiscreteTextConfigurationSetting(discreteTextConfiguration))
                .AddConfiguration(new DiscreteNumberConfigurationSetting(discreteNumberConfiguration));

            TestContext.Api.ResourcePools.Update(resourcePool);
            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.ID);

            Assert.IsNotNull(resourcePool);
            Assert.IsNotNull(resourcePool.OrchestrationSettings);
            Assert.AreEqual(1, resourcePool.OrchestrationSettings.Capabilities.Count);
            Assert.AreEqual(2, resourcePool.OrchestrationSettings.Capacities.Count);
            Assert.AreEqual(4, resourcePool.OrchestrationSettings.Configurations.Count);

            var capabilityIds = resourcePool.OrchestrationSettings.Capabilities.Select(c => c.Id).ToList();
            Assert.IsTrue(capabilityIds.Contains(capability.ID));

            var capacityIds = resourcePool.OrchestrationSettings.Capacities.Select(c => c.Id).ToList();
            Assert.IsTrue(capacityIds.Contains(numberCapacity.ID));
            Assert.IsTrue(capacityIds.Contains(rangeCapacity.ID));

            var configurationIds = resourcePool.OrchestrationSettings.Configurations.Select(c => c.Id).ToList();
            Assert.IsTrue(configurationIds.Contains(textConfiguration.ID));
            Assert.IsTrue(configurationIds.Contains(numberConfiguration.ID));
            Assert.IsTrue(configurationIds.Contains(discreteTextConfiguration.ID));
            Assert.IsTrue(configurationIds.Contains(discreteNumberConfiguration.ID));
        }

        [TestMethod]
        public void OrchestrationEvents_ParameterSettingsWithNoValues_UpdatePool()
        {
            var prefix = Guid.NewGuid();

            var capability = new Capability
            {
                Name = $"{prefix}_Capability",
            }
            .SetDiscretes(["Value 1", "Value 2"]);
            objectCreator.CreateCapability(capability);

            var numberCapacity = new NumberCapacity
            {
                Name = $"{prefix}_NumberCapacity",
            };
            var rangeCapacity = new RangeCapacity
            {
                Name = $"{prefix}_RangeCapacity",
            };
            objectCreator.CreateCapacities([numberCapacity, rangeCapacity]);

            var textConfiguration = new TextConfiguration
            {
                Name = $"{prefix}_TextConfiguration",
            };
            var numberConfiguration = new NumberConfiguration
            {
                Name = $"{prefix}_NumberConfiguration",
            };
            var discreteTextConfiguration = new DiscreteTextConfiguration
            {
                Name = $"{prefix}_DiscreteTextConfiguration",
            }
            .AddDiscrete(new TextDiscreet("A", "A"));
            var discreteNumberConfiguration = new DiscreteNumberConfiguration
            {
                Name = $"{prefix}_DiscreteNumberConfiguration",
            }
            .AddDiscrete(new NumberDiscreet(1, "A"));
            objectCreator.CreateConfigurations([textConfiguration, numberConfiguration, discreteTextConfiguration, discreteNumberConfiguration]);

            var resourcePool = new ResourcePool
            {
                Name = $"{prefix}_ResourcePool",
            };

            objectCreator.CreateResourcePool(resourcePool);
            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.ID);

            resourcePool.OrchestrationSettings.AddOrchestrationEvent(new OrchestrationEvent
            {
                EventType = OrchestrationEventType.PrerollStart,
                ExecutionDetails = new ScriptExecutionDetails("InitialScript")
                .SetCapabilities([new CapabilitySetting(capability)])
                .SetCapacities([new NumberCapacitySetting(numberCapacity), new RangeCapacitySetting(rangeCapacity)])
                .SetConfigurations([
                    new TextConfigurationSetting(textConfiguration),
                    new NumberConfigurationSetting(numberConfiguration),
                    new DiscreteTextConfigurationSetting(discreteTextConfiguration),
                    new DiscreteNumberConfigurationSetting(discreteNumberConfiguration)
                ]),
            });

            TestContext.Api.ResourcePools.Update(resourcePool);
            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.ID);

            Assert.IsNotNull(resourcePool);
            Assert.IsNotNull(resourcePool.OrchestrationSettings);
            Assert.IsNotNull(resourcePool.OrchestrationSettings.OrchestrationEvents);
            Assert.AreEqual(1, resourcePool.OrchestrationSettings.OrchestrationEvents.Count);

            var orchestrationEvent = resourcePool.OrchestrationSettings.OrchestrationEvents.First();
            Assert.AreEqual(OrchestrationEventType.PrerollStart, orchestrationEvent.EventType);
            Assert.AreEqual(1, orchestrationEvent.ExecutionDetails.Capabilities.Count);
            Assert.AreEqual(2, orchestrationEvent.ExecutionDetails.Capacities.Count);
            Assert.AreEqual(4, orchestrationEvent.ExecutionDetails.Configurations.Count);

            var capabilityIds = orchestrationEvent.ExecutionDetails.Capabilities.Select(c => c.Id).ToList();
            Assert.IsTrue(capabilityIds.Contains(capability.ID));
            Assert.IsNotNull(orchestrationEvent.ExecutionDetails.Capabilities.First());
            Assert.IsNull(orchestrationEvent.ExecutionDetails.Capabilities.First().Value);

            var capacityIds = orchestrationEvent.ExecutionDetails.Capacities.Select(c => c.Id).ToList();
            Assert.IsTrue(capacityIds.Contains(numberCapacity.ID));
            Assert.IsTrue(capacityIds.Contains(rangeCapacity.ID));

            var numberCapacitySetting = orchestrationEvent.ExecutionDetails.Capacities.First(c => c.Id == numberCapacity.ID) as NumberCapacitySetting;
            Assert.IsNotNull(numberCapacitySetting);
            Assert.IsNull(numberCapacitySetting.Value);

            var rangeCapacitySetting = orchestrationEvent.ExecutionDetails.Capacities.First(c => c.Id == rangeCapacity.ID) as RangeCapacitySetting;
            Assert.IsNotNull(rangeCapacitySetting);
            Assert.IsNull(rangeCapacitySetting.MinValue);
            Assert.IsNull(rangeCapacitySetting.MaxValue);

            var configurationIds = orchestrationEvent.ExecutionDetails.Configurations.Select(c => c.Id).ToList();
            Assert.IsTrue(configurationIds.Contains(textConfiguration.ID));
            Assert.IsTrue(configurationIds.Contains(numberConfiguration.ID));
            Assert.IsTrue(configurationIds.Contains(discreteTextConfiguration.ID));
            Assert.IsTrue(configurationIds.Contains(discreteNumberConfiguration.ID));

            var textConfigurationSetting = orchestrationEvent.ExecutionDetails.Configurations.First(c => c.Id == textConfiguration.ID) as TextConfigurationSetting;
            Assert.IsNotNull(textConfigurationSetting);
            Assert.IsNull(textConfigurationSetting.Value);

            var numberConfigurationSetting = orchestrationEvent.ExecutionDetails.Configurations.First(c => c.Id == numberConfiguration.ID) as NumberConfigurationSetting;
            Assert.IsNotNull(numberConfigurationSetting);
            Assert.IsNull(numberConfigurationSetting.Value);

            var discreteTextConfigurationSetting = orchestrationEvent.ExecutionDetails.Configurations.First(c => c.Id == discreteTextConfiguration.ID) as DiscreteTextConfigurationSetting;
            Assert.IsNotNull(discreteTextConfigurationSetting);
            Assert.IsNull(discreteTextConfigurationSetting.Value);

            var discreteNumberConfigurationSetting = orchestrationEvent.ExecutionDetails.Configurations.First(c => c.Id == discreteNumberConfiguration.ID) as DiscreteNumberConfigurationSetting;
            Assert.IsNotNull(discreteNumberConfigurationSetting);
            Assert.IsNull(discreteNumberConfigurationSetting.Value);
        }

        [TestMethod]
        public void DuplicateParameterSettingsThrowsException_CreatePool()
        {
            var prefix = Guid.NewGuid();

            var capability = new Capability
            {
                Name = $"{prefix}_Capability",
            }
            .SetDiscretes(["Value 1", "Value 2"]);
            objectCreator.CreateCapability(capability);

            var numberCapacity = new NumberCapacity
            {
                Name = $"{prefix}_NumberCapacity",
            };
            objectCreator.CreateCapacity(numberCapacity);

            var textConfiguration = new TextConfiguration
            {
                Name = $"{prefix}_TextConfiguration",
            };
            objectCreator.CreateConfiguration(textConfiguration);

            var resourcePool = new ResourcePool
            {
                Name = $"{prefix}_ResourcePool",
            };
            resourcePool.OrchestrationSettings
                .AddCapability(new CapabilitySetting(capability))
                .AddCapability(new CapabilitySetting(capability))
                .AddCapacity(new NumberCapacitySetting(numberCapacity))
                .AddCapacity(new NumberCapacitySetting(numberCapacity))
                .AddConfiguration(new TextConfigurationSetting(textConfiguration))
                .AddConfiguration(new TextConfigurationSetting(textConfiguration));

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
                Assert.AreEqual($"Capacity with ID '{numberCapacity.ID}' is defined 2 times. Duplicate capacity settings are not allowed.", invalidCapacitySettingsError.ErrorMessage);
                Assert.AreEqual(numberCapacity.ID, invalidCapacitySettingsError.CapacityId);
                Assert.AreEqual(resourcePool.OrchestrationSettings.ID, invalidCapacitySettingsError.Id);

                var invalidCapabilitySettingsError = orchestrationSettingsErrors.OfType<OrchestrationSettingsInvalidCapabilitySettingsError>().SingleOrDefault();
                Assert.IsNotNull(invalidCapabilitySettingsError);
                Assert.AreEqual($"Capability with ID '{capability.ID}' is defined 2 times. Duplicate capability settings are not allowed.", invalidCapabilitySettingsError.ErrorMessage);
                Assert.AreEqual(capability.ID, invalidCapabilitySettingsError.CapabilityId);
                Assert.AreEqual(resourcePool.OrchestrationSettings.ID, invalidCapabilitySettingsError.Id);

                var invalidConfigurationSettingsError = orchestrationSettingsErrors.OfType<OrchestrationSettingsInvalidConfigurationSettingsError>().SingleOrDefault();
                Assert.IsNotNull(invalidConfigurationSettingsError);
                Assert.AreEqual($"Configuration with ID '{textConfiguration.ID}' is defined 2 times. Duplicate configuration settings are not allowed.", invalidConfigurationSettingsError.ErrorMessage);
                Assert.AreEqual(textConfiguration.ID, invalidConfigurationSettingsError.ConfigurationId);
                Assert.AreEqual(resourcePool.OrchestrationSettings.ID, invalidConfigurationSettingsError.Id);
            }
        }

        [TestMethod]
        public void SingleOrchestrationEvents_CreatePool()
        {
            // Create Configuration
            var prefix = Guid.NewGuid();
            var textConfiguration = new TextConfiguration
            {
                Name = $"{prefix}_TextConfiguration",
            };

            objectCreator.CreateConfigurations([textConfiguration]);

            // Create new pool with Orchestration Setting referencing the created configuration
            var resourcePool = new ResourcePool
            {
                Name = $"{prefix}_ResourcePool",
            };

            resourcePool.OrchestrationSettings.SetOrchestrationEvents(new List<OrchestrationEvent>
            {
                new OrchestrationEvent
                {
                    EventType = OrchestrationEventType.PrerollStart,
                    ExecutionDetails = new ScriptExecutionDetails("SomeScript").AddConfiguration(new TextConfigurationSetting(textConfiguration) { Value = "HelloWorld" })
                },
            });

            Assert.IsNull(resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.First().OriginalSection);
            Assert.IsTrue(resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.First().IsNew);
            Assert.IsFalse(resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.First().HasChanges);

            objectCreator.CreateResourcePool(resourcePool);
            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.ID);

            Assert.IsNotNull(resourcePool.OrchestrationSettings);
            Assert.IsNotNull(resourcePool.OrchestrationSettings.OrchestrationEvents);
            Assert.AreEqual(1, resourcePool.OrchestrationSettings.OrchestrationEvents.Count);

            Assert.AreEqual(OrchestrationEventType.PrerollStart, resourcePool.OrchestrationSettings.OrchestrationEvents.First().EventType);
            Assert.IsNotNull(resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails);
            Assert.AreEqual("SomeScript", resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.ScriptName);

            Assert.IsNotNull(resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Capabilities);
            Assert.IsNotNull(resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Capacities);
            Assert.IsNotNull(resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations);

            Assert.AreEqual(0, resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Capabilities.Count);
            Assert.AreEqual(0, resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Capacities.Count);
            Assert.AreEqual(1, resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.Count);

            Assert.IsNull(resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.First().OriginalSection); // No original section as this data isn't coming from DOM Section but from JSON
            Assert.IsFalse(resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.First().IsNew);
            Assert.IsFalse(resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.First().HasChanges);

            var textConfigurationSetting = resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.First() as TextConfigurationSetting;
            Assert.IsNotNull(textConfigurationSetting);
            Assert.AreEqual(textConfiguration.ID, textConfigurationSetting.Id);
            Assert.AreEqual("HelloWorld", textConfigurationSetting.Value);
        }

        [TestMethod]
        public void SingleOrchestrationEvents_UpdatePool()
        {
            var prefix = Guid.NewGuid();
            var resourcePool = new ResourcePool
            {
                Name = $"{prefix}_ResourcePool",
            };

            objectCreator.CreateResourcePool(resourcePool);
            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.ID);

            // Create Configuration
            var textConfiguration = new TextConfiguration
            {
                Name = $"{prefix}_TextConfiguration",
            };

            objectCreator.CreateConfigurations([textConfiguration]);

            resourcePool.OrchestrationSettings.SetOrchestrationEvents(new List<OrchestrationEvent>
            {
                new OrchestrationEvent
                {
                    EventType = OrchestrationEventType.PrerollStart,
                    ExecutionDetails = new ScriptExecutionDetails("SomeScript").AddConfiguration(new TextConfigurationSetting(textConfiguration) { Value = "HelloWorld" })
                },
            });

            TestContext.Api.ResourcePools.Update(resourcePool);
            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.ID);

            Assert.IsNotNull(resourcePool.OrchestrationSettings);
            Assert.IsNotNull(resourcePool.OrchestrationSettings.OrchestrationEvents);
            Assert.AreEqual(1, resourcePool.OrchestrationSettings.OrchestrationEvents.Count);

            Assert.AreEqual(OrchestrationEventType.PrerollStart, resourcePool.OrchestrationSettings.OrchestrationEvents.First().EventType);
            Assert.IsNotNull(resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails);
            Assert.AreEqual("SomeScript", resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.ScriptName);

            Assert.IsNotNull(resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Capabilities);
            Assert.IsNotNull(resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Capacities);
            Assert.IsNotNull(resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations);

            Assert.AreEqual(0, resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Capabilities.Count);
            Assert.AreEqual(0, resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Capacities.Count);
            Assert.AreEqual(1, resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.Count);

            Assert.IsNull(resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.First().OriginalSection); // No original section as this data isn't coming from DOM Section but from JSON
            Assert.IsFalse(resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.First().IsNew);
            Assert.IsFalse(resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.First().HasChanges);

            var textConfigurationSetting = resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.First() as TextConfigurationSetting;
            Assert.IsNotNull(textConfigurationSetting);
            Assert.AreEqual(textConfiguration.ID, textConfigurationSetting.Id);
            Assert.AreEqual("HelloWorld", textConfigurationSetting.Value);
        }

        [TestMethod]
        public void UpdateOrchestrationEvents()
        {
            var prefix = Guid.NewGuid();

            var textConfiguration = new TextConfiguration
            {
                Name = $"{prefix}_TextConfiguration",
            };

            var discreteNumberConfiguration = new DiscreteNumberConfiguration
            {
                Name = $"{prefix}_DiscreteNumberConfiguration",
            }.SetDiscretes([new NumberDiscreet(1, "One"), new NumberDiscreet(2, "Two"), new NumberDiscreet(3, "Three")]);

            objectCreator.CreateConfigurations([textConfiguration, discreteNumberConfiguration]);

            // Create new pool with Orchestration Setting referencing the created configuration
            var resourcePool = new ResourcePool
            {
                Name = $"{prefix}_ResourcePool",
            };

            resourcePool.OrchestrationSettings.SetOrchestrationEvents(new List<OrchestrationEvent>
            {
                new OrchestrationEvent
                {
                    EventType = OrchestrationEventType.PrerollStart,
                    ExecutionDetails = new ScriptExecutionDetails("PrerollStartScript").AddConfiguration(new TextConfigurationSetting(textConfiguration) { Value = "HelloWorld" })
                },
                new OrchestrationEvent
                {
                    EventType = OrchestrationEventType.PostrollStart,
                    ExecutionDetails = new ScriptExecutionDetails("PostrollStartScript").AddConfiguration(new DiscreteNumberConfigurationSetting(discreteNumberConfiguration) { Value = new NumberDiscreet(3, "Three") })
                },
            });

            objectCreator.CreateResourcePool(resourcePool);
            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.ID);

            Assert.IsNotNull(resourcePool.OrchestrationSettings);
            Assert.IsNotNull(resourcePool.OrchestrationSettings.OrchestrationEvents);
            Assert.AreEqual(2, resourcePool.OrchestrationSettings.OrchestrationEvents.Count);

            var prerollStartEvent = resourcePool.OrchestrationSettings.OrchestrationEvents.FirstOrDefault(e => e.EventType == OrchestrationEventType.PrerollStart);

            Assert.IsNotNull(prerollStartEvent);
            Assert.IsNotNull(prerollStartEvent.ExecutionDetails);
            Assert.AreEqual("PrerollStartScript", prerollStartEvent.ExecutionDetails.ScriptName);

            Assert.AreEqual(0, prerollStartEvent.ExecutionDetails.Capabilities.Count);
            Assert.AreEqual(0, prerollStartEvent.ExecutionDetails.Capacities.Count);
            Assert.AreEqual(1, prerollStartEvent.ExecutionDetails.Configurations.Count);

            var prerollStartTextConfigurationSetting = prerollStartEvent.ExecutionDetails.Configurations.First() as TextConfigurationSetting;
            Assert.IsNotNull(prerollStartTextConfigurationSetting);
            Assert.AreEqual(textConfiguration.ID, prerollStartTextConfigurationSetting.Id);
            Assert.AreEqual("HelloWorld", prerollStartTextConfigurationSetting.Value);

            var postrollStartEvent = resourcePool.OrchestrationSettings.OrchestrationEvents.FirstOrDefault(e => e.EventType == OrchestrationEventType.PostrollStart);

            Assert.IsNotNull(postrollStartEvent);
            Assert.IsNotNull(postrollStartEvent.ExecutionDetails);
            Assert.AreEqual("PostrollStartScript", postrollStartEvent.ExecutionDetails.ScriptName);

            Assert.AreEqual(0, postrollStartEvent.ExecutionDetails.Capabilities.Count);
            Assert.AreEqual(0, postrollStartEvent.ExecutionDetails.Capacities.Count);
            Assert.AreEqual(1, postrollStartEvent.ExecutionDetails.Configurations.Count);

            var postrollDiscreteConfigurationSetting = postrollStartEvent.ExecutionDetails.Configurations.First() as DiscreteNumberConfigurationSetting;
            Assert.IsNotNull(postrollDiscreteConfigurationSetting);
            Assert.AreEqual(discreteNumberConfiguration.ID, postrollDiscreteConfigurationSetting.Id);
            Assert.AreEqual(new NumberDiscreet(3, "Three"), postrollDiscreteConfigurationSetting.Value);

            // Remove PrerollStart Event
            resourcePool.OrchestrationSettings.RemoveOrchestrationEvent(resourcePool.OrchestrationSettings.OrchestrationEvents.First(x => x.EventType == OrchestrationEventType.PrerollStart));
            Assert.AreEqual(1, resourcePool.OrchestrationSettings.OrchestrationEvents.Count);

            // Update PostrollStart Event
            resourcePool.OrchestrationSettings.OrchestrationEvents.First(x => x.EventType == OrchestrationEventType.PostrollStart)
                .ExecutionDetails = new ScriptExecutionDetails("UpdatedPostrollScript")
                .AddConfiguration(new DiscreteNumberConfigurationSetting(discreteNumberConfiguration) { Value = new NumberDiscreet(2, "Two") });

            // Add PrerollStop Event
            resourcePool.OrchestrationSettings.AddOrchestrationEvent(new OrchestrationEvent
            {
                EventType = OrchestrationEventType.PrerollStop,
                ExecutionDetails = new ScriptExecutionDetails("PrerollStopScript").AddConfiguration(new TextConfigurationSetting(textConfiguration) { Value = "HelloWorld" })
            });

            TestContext.Api.ResourcePools.Update(resourcePool);
            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.ID);

            Assert.IsNotNull(resourcePool.OrchestrationSettings);
            Assert.IsNotNull(resourcePool.OrchestrationSettings.OrchestrationEvents);
            Assert.AreEqual(2, resourcePool.OrchestrationSettings.OrchestrationEvents.Count);

            prerollStartEvent = resourcePool.OrchestrationSettings.OrchestrationEvents.FirstOrDefault(e => e.EventType == OrchestrationEventType.PrerollStart);
            Assert.IsNull(prerollStartEvent);

            var prerollStopEvent = resourcePool.OrchestrationSettings.OrchestrationEvents.FirstOrDefault(e => e.EventType == OrchestrationEventType.PrerollStop);

            Assert.IsNotNull(prerollStopEvent);
            Assert.IsNotNull(prerollStopEvent.ExecutionDetails);
            Assert.AreEqual("PrerollStopScript", prerollStopEvent.ExecutionDetails.ScriptName);

            Assert.AreEqual(0, prerollStopEvent.ExecutionDetails.Capabilities.Count);
            Assert.AreEqual(0, prerollStopEvent.ExecutionDetails.Capacities.Count);
            Assert.AreEqual(1, prerollStopEvent.ExecutionDetails.Configurations.Count);

            var prerollStopTextConfigurationSetting = prerollStopEvent.ExecutionDetails.Configurations.First() as TextConfigurationSetting;
            Assert.IsNotNull(prerollStopTextConfigurationSetting);
            Assert.AreEqual(textConfiguration.ID, prerollStopTextConfigurationSetting.Id);
            Assert.AreEqual("HelloWorld", prerollStopTextConfigurationSetting.Value);

            var updatedPostrollStartEvent = resourcePool.OrchestrationSettings.OrchestrationEvents.FirstOrDefault(e => e.EventType == OrchestrationEventType.PostrollStart);

            Assert.IsNotNull(updatedPostrollStartEvent);
            Assert.IsNotNull(updatedPostrollStartEvent.ExecutionDetails);
            Assert.AreEqual("UpdatedPostrollScript", updatedPostrollStartEvent.ExecutionDetails.ScriptName);

            Assert.AreEqual(0, updatedPostrollStartEvent.ExecutionDetails.Capabilities.Count);
            Assert.AreEqual(0, updatedPostrollStartEvent.ExecutionDetails.Capacities.Count);
            Assert.AreEqual(1, updatedPostrollStartEvent.ExecutionDetails.Configurations.Count);

            var discreteConfigurationSetting = updatedPostrollStartEvent.ExecutionDetails.Configurations.First() as DiscreteNumberConfigurationSetting;
            Assert.IsNotNull(discreteConfigurationSetting);
            Assert.AreEqual(discreteNumberConfiguration.ID, discreteConfigurationSetting.Id);
            Assert.AreEqual(new NumberDiscreet(2, "Two"), discreteConfigurationSetting.Value);
        }

        [TestMethod]
        public void AssignParameterFromExistingResourcePoolToNewResourcePool()
        {
            var prefix = Guid.NewGuid();

            var capability = new Capability
            {
                Name = $"{prefix}_Capability",
            }
            .SetDiscretes(["Value 1", "Value 2"]);
            objectCreator.CreateCapability(capability);

            var numberCapacity = new NumberCapacity
            {
                Name = $"{prefix}_NumberCapacity",
            };
            var rangeCapacity = new RangeCapacity
            {
                Name = $"{prefix}_RangeCapacity",
            };
            objectCreator.CreateCapacities([numberCapacity, rangeCapacity]);

            var textConfiguration = new TextConfiguration
            {
                Name = $"{prefix}_TextConfiguration",
            };
            var numberConfiguration = new NumberConfiguration
            {
                Name = $"{prefix}_NumberConfiguration",
            };
            var discreteTextConfiguration = new DiscreteTextConfiguration
            {
                Name = $"{prefix}_DiscreteTextConfiguration",
            }
            .AddDiscrete(new TextDiscreet("A", "A"));
            var discreteNumberConfiguration = new DiscreteNumberConfiguration
            {
                Name = $"{prefix}_DiscreteNumberConfiguration",
            }
            .AddDiscrete(new NumberDiscreet(1, "A"));
            objectCreator.CreateConfigurations([textConfiguration, numberConfiguration, discreteTextConfiguration, discreteNumberConfiguration]);

            var resourcePool1 = new ResourcePool
            {
                Name = $"{prefix}_ResourcePool1",
            };

            resourcePool1.OrchestrationSettings
                .SetCapabilities([new CapabilitySetting(capability)])
                .SetCapacities([new NumberCapacitySetting(numberCapacity), new RangeCapacitySetting(rangeCapacity)])
                .SetConfigurations([
                    new TextConfigurationSetting(textConfiguration),
                    new NumberConfigurationSetting(numberConfiguration),
                    new DiscreteTextConfigurationSetting(discreteTextConfiguration),
                    new DiscreteNumberConfigurationSetting(discreteNumberConfiguration)
                ]);

            objectCreator.CreateResourcePool(resourcePool1);
            resourcePool1 = TestContext.Api.ResourcePools.Read(resourcePool1.ID);

            var resourcePool2 = new ResourcePool
            {
                Name = $"{prefix}_ResourcePool2",
            };

            foreach (var capacitySetting in resourcePool1.OrchestrationSettings.Capacities)
            {
                resourcePool2.OrchestrationSettings.AddCapacity(capacitySetting);
            }

            foreach (var capabilitySetting in resourcePool1.OrchestrationSettings.Capabilities)
            {
                resourcePool2.OrchestrationSettings.AddCapability(capabilitySetting);
            }

            foreach (var configurationSetting in resourcePool1.OrchestrationSettings.Configurations)
            {
                resourcePool2.OrchestrationSettings.AddConfiguration(configurationSetting);
            }

            objectCreator.CreateResourcePool(resourcePool2);

            resourcePool2 = TestContext.Api.ResourcePools.Read(resourcePool2.ID);
            Assert.IsNotNull(resourcePool2);
        }
    }
}