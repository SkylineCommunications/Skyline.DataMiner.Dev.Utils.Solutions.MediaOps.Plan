namespace RT_MediaOps.Plan.RST.ResourcePools
{
    using System;
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
        public void ParameterSettingsWithNoValues()
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
        public void CreatePoolWithDuplicateParameterSettingsThrowsException()
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

        [TestMethod]
        public void CreatePoolWithSingleOrchestrationEvents()
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

            Assert.IsNull(resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.ConfigurationSettings.First().OriginalSection);
            Assert.IsTrue(resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.ConfigurationSettings.First().IsNew);
            Assert.IsFalse(resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.ConfigurationSettings.First().HasChanges);

            objectCreator.CreateResourcePool(resourcePool);
            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);

            Assert.IsNotNull(resourcePool.OrchestrationSettings);
            Assert.IsNotNull(resourcePool.OrchestrationSettings.OrchestrationEvents);
            Assert.AreEqual(1, resourcePool.OrchestrationSettings.OrchestrationEvents.Count);

            Assert.AreEqual(OrchestrationEventType.PrerollStart, resourcePool.OrchestrationSettings.OrchestrationEvents.First().EventType);
            Assert.IsNotNull(resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails);
            Assert.AreEqual("SomeScript", resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.ScriptName);

            Assert.IsNotNull(resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.CapabilitySettings);
            Assert.IsNotNull(resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.CapacitySettings);
            Assert.IsNotNull(resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.ConfigurationSettings);

            Assert.AreEqual(0, resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.CapabilitySettings.Count);
            Assert.AreEqual(0, resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.CapacitySettings.Count);
            Assert.AreEqual(1, resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.ConfigurationSettings.Count);

            Assert.IsNull(resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.ConfigurationSettings.First().OriginalSection); // No original section as this data isn't coming from DOM Section but from JSON
            Assert.IsFalse(resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.ConfigurationSettings.First().IsNew);
            Assert.IsFalse(resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.ConfigurationSettings.First().HasChanges);

            var textConfigurationSetting = resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.ConfigurationSettings.First() as TextConfigurationSetting;
            Assert.IsNotNull(textConfigurationSetting);
            Assert.AreEqual(textConfiguration.Id, textConfigurationSetting.Id);
            Assert.AreEqual("HelloWorld", textConfigurationSetting.Value);
        }

        [TestMethod]
        public void UpdatePoolWithSingleOrchestrationEvents()
        {
            var prefix = Guid.NewGuid();
            var resourcePool = new ResourcePool
            {
                Name = $"{prefix}_ResourcePool",
            };

            objectCreator.CreateResourcePool(resourcePool);
            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);

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
            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);

            Assert.IsNotNull(resourcePool.OrchestrationSettings);
            Assert.IsNotNull(resourcePool.OrchestrationSettings.OrchestrationEvents);
            Assert.AreEqual(1, resourcePool.OrchestrationSettings.OrchestrationEvents.Count);

            Assert.AreEqual(OrchestrationEventType.PrerollStart, resourcePool.OrchestrationSettings.OrchestrationEvents.First().EventType);
            Assert.IsNotNull(resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails);
            Assert.AreEqual("SomeScript", resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.ScriptName);

            Assert.IsNotNull(resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.CapabilitySettings);
            Assert.IsNotNull(resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.CapacitySettings);
            Assert.IsNotNull(resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.ConfigurationSettings);

            Assert.AreEqual(0, resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.CapabilitySettings.Count);
            Assert.AreEqual(0, resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.CapacitySettings.Count);
            Assert.AreEqual(1, resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.ConfigurationSettings.Count);

            Assert.IsNull(resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.ConfigurationSettings.First().OriginalSection); // No original section as this data isn't coming from DOM Section but from JSON
            Assert.IsFalse(resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.ConfigurationSettings.First().IsNew);
            Assert.IsFalse(resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.ConfigurationSettings.First().HasChanges);

            var textConfigurationSetting = resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.ConfigurationSettings.First() as TextConfigurationSetting;
            Assert.IsNotNull(textConfigurationSetting);
            Assert.AreEqual(textConfiguration.Id, textConfigurationSetting.Id);
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
            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);

            Assert.IsNotNull(resourcePool.OrchestrationSettings);
            Assert.IsNotNull(resourcePool.OrchestrationSettings.OrchestrationEvents);
            Assert.AreEqual(2, resourcePool.OrchestrationSettings.OrchestrationEvents.Count);

            var prerollStartEvent = resourcePool.OrchestrationSettings.OrchestrationEvents.FirstOrDefault(e => e.EventType == OrchestrationEventType.PrerollStart);

            Assert.IsNotNull(prerollStartEvent);
            Assert.IsNotNull(prerollStartEvent.ExecutionDetails);
            Assert.AreEqual("PrerollStartScript", prerollStartEvent.ExecutionDetails.ScriptName);

            Assert.AreEqual(0, prerollStartEvent.ExecutionDetails.CapabilitySettings.Count);
            Assert.AreEqual(0, prerollStartEvent.ExecutionDetails.CapacitySettings.Count);
            Assert.AreEqual(1, prerollStartEvent.ExecutionDetails.ConfigurationSettings.Count);

            var prerollStartTextConfigurationSetting = prerollStartEvent.ExecutionDetails.ConfigurationSettings.First() as TextConfigurationSetting;
            Assert.IsNotNull(prerollStartTextConfigurationSetting);
            Assert.AreEqual(textConfiguration.Id, prerollStartTextConfigurationSetting.Id);
            Assert.AreEqual("HelloWorld", prerollStartTextConfigurationSetting.Value);

            var postrollStartEvent = resourcePool.OrchestrationSettings.OrchestrationEvents.FirstOrDefault(e => e.EventType == OrchestrationEventType.PostrollStart);

            Assert.IsNotNull(postrollStartEvent);
            Assert.IsNotNull(postrollStartEvent.ExecutionDetails);
            Assert.AreEqual("PostrollStartScript", postrollStartEvent.ExecutionDetails.ScriptName);

            Assert.AreEqual(0, postrollStartEvent.ExecutionDetails.CapabilitySettings.Count);
            Assert.AreEqual(0, postrollStartEvent.ExecutionDetails.CapacitySettings.Count);
            Assert.AreEqual(1, postrollStartEvent.ExecutionDetails.ConfigurationSettings.Count);

            var postrollDiscreteConfigurationSetting = postrollStartEvent.ExecutionDetails.ConfigurationSettings.First() as DiscreteNumberConfigurationSetting;
            Assert.IsNotNull(postrollDiscreteConfigurationSetting);
            Assert.AreEqual(discreteNumberConfiguration.Id, postrollDiscreteConfigurationSetting.Id);
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
            resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);

            Assert.IsNotNull(resourcePool.OrchestrationSettings);
            Assert.IsNotNull(resourcePool.OrchestrationSettings.OrchestrationEvents);
            Assert.AreEqual(2, resourcePool.OrchestrationSettings.OrchestrationEvents.Count);

            prerollStartEvent = resourcePool.OrchestrationSettings.OrchestrationEvents.FirstOrDefault(e => e.EventType == OrchestrationEventType.PrerollStart);
            Assert.IsNull(prerollStartEvent);

            var prerollStopEvent = resourcePool.OrchestrationSettings.OrchestrationEvents.FirstOrDefault(e => e.EventType == OrchestrationEventType.PrerollStop);

            Assert.IsNotNull(prerollStopEvent);
            Assert.IsNotNull(prerollStopEvent.ExecutionDetails);
            Assert.AreEqual("PrerollStopScript", prerollStopEvent.ExecutionDetails.ScriptName);

            Assert.AreEqual(0, prerollStopEvent.ExecutionDetails.CapabilitySettings.Count);
            Assert.AreEqual(0, prerollStopEvent.ExecutionDetails.CapacitySettings.Count);
            Assert.AreEqual(1, prerollStopEvent.ExecutionDetails.ConfigurationSettings.Count);

            var prerollStopTextConfigurationSetting = prerollStopEvent.ExecutionDetails.ConfigurationSettings.First() as TextConfigurationSetting;
            Assert.IsNotNull(prerollStopTextConfigurationSetting);
            Assert.AreEqual(textConfiguration.Id, prerollStopTextConfigurationSetting.Id);
            Assert.AreEqual("HelloWorld", prerollStopTextConfigurationSetting.Value);

            var updatedPostrollStartEvent = resourcePool.OrchestrationSettings.OrchestrationEvents.FirstOrDefault(e => e.EventType == OrchestrationEventType.PostrollStart);

            Assert.IsNotNull(updatedPostrollStartEvent);
            Assert.IsNotNull(updatedPostrollStartEvent.ExecutionDetails);
            Assert.AreEqual("UpdatedPostrollScript", updatedPostrollStartEvent.ExecutionDetails.ScriptName);

            Assert.AreEqual(0, updatedPostrollStartEvent.ExecutionDetails.CapabilitySettings.Count);
            Assert.AreEqual(0, updatedPostrollStartEvent.ExecutionDetails.CapacitySettings.Count);
            Assert.AreEqual(1, updatedPostrollStartEvent.ExecutionDetails.ConfigurationSettings.Count);

            var discreteConfigurationSetting = updatedPostrollStartEvent.ExecutionDetails.ConfigurationSettings.First() as DiscreteNumberConfigurationSetting;
            Assert.IsNotNull(discreteConfigurationSetting);
            Assert.AreEqual(discreteNumberConfiguration.Id, discreteConfigurationSetting.Id);
            Assert.AreEqual(new NumberDiscreet(2, "Two"), discreteConfigurationSetting.Value);
        }
    }
}