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
			Assert.AreEqual(capability.Id, orchestrationSettings.Capabilities.Single().Id);

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
			Assert.IsTrue(orchestrationSettings.Capacities.Any(c => c.Id == numberCapacity.Id));
			Assert.IsTrue(orchestrationSettings.Capacities.Any(c => c.Id == rangeCapacity.Id));

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
			Assert.IsTrue(configurationIds.Contains(textConfiguration.Id));
			Assert.IsTrue(configurationIds.Contains(numberConfiguration.Id));
			Assert.IsTrue(configurationIds.Contains(discreteTextConfiguration.Id));
			Assert.IsTrue(configurationIds.Contains(discreteNumberConfiguration.Id));

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
			Assert.AreEqual(capability.Id, orchestrationSettings.Capabilities.Single().Id);

			objectCreator.CreateResourcePool(resourcePool);
			resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);

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
			Assert.IsTrue(orchestrationSettings.Capacities.Any(c => c.Id == numberCapacity.Id));
			Assert.IsTrue(orchestrationSettings.Capacities.Any(c => c.Id == rangeCapacity.Id));

			objectCreator.CreateResourcePool(resourcePool);
			resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);

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
			Assert.IsTrue(configurationIds.Contains(textConfiguration.Id));
			Assert.IsTrue(configurationIds.Contains(numberConfiguration.Id));
			Assert.IsTrue(configurationIds.Contains(discreteTextConfiguration.Id));
			Assert.IsTrue(configurationIds.Contains(discreteNumberConfiguration.Id));

			objectCreator.CreateResourcePool(resourcePool);
			resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);

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

			// Discrete number configuration contains two discrete values, only one is added as configuration references the existing object
			Assert.IsTrue(configurationIds.Contains(discreteNumberConfiguration.Id));
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
			resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);

			resourcePool.OrchestrationSettings
				.AddCapability(new CapabilitySetting(capability))
				.AddCapacity(new NumberCapacitySetting(numberCapacity))
				.AddCapacity(new RangeCapacitySetting(rangeCapacity))
				.AddConfiguration(new TextConfigurationSetting(textConfiguration))
				.AddConfiguration(new NumberConfigurationSetting(numberConfiguration))
				.AddConfiguration(new DiscreteTextConfigurationSetting(discreteTextConfiguration))
				.AddConfiguration(new DiscreteNumberConfigurationSetting(discreteNumberConfiguration));

			TestContext.Api.ResourcePools.Update(resourcePool);
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
			resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);

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
			resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);

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
			Assert.IsTrue(capabilityIds.Contains(capability.Id));
			Assert.IsNotNull(orchestrationEvent.ExecutionDetails.Capabilities.First());
			Assert.IsNull(orchestrationEvent.ExecutionDetails.Capabilities.First().Value);

			var capacityIds = orchestrationEvent.ExecutionDetails.Capacities.Select(c => c.Id).ToList();
			Assert.IsTrue(capacityIds.Contains(numberCapacity.Id));
			Assert.IsTrue(capacityIds.Contains(rangeCapacity.Id));

			var numberCapacitySetting = orchestrationEvent.ExecutionDetails.Capacities.First(c => c.Id == numberCapacity.Id) as NumberCapacitySetting;
			Assert.IsNotNull(numberCapacitySetting);
			Assert.IsNull(numberCapacitySetting.Value);

			var rangeCapacitySetting = orchestrationEvent.ExecutionDetails.Capacities.First(c => c.Id == rangeCapacity.Id) as RangeCapacitySetting;
			Assert.IsNotNull(rangeCapacitySetting);
			Assert.IsNull(rangeCapacitySetting.MinValue);
			Assert.IsNull(rangeCapacitySetting.MaxValue);

			var configurationIds = orchestrationEvent.ExecutionDetails.Configurations.Select(c => c.Id).ToList();
			Assert.IsTrue(configurationIds.Contains(textConfiguration.Id));
			Assert.IsTrue(configurationIds.Contains(numberConfiguration.Id));
			Assert.IsTrue(configurationIds.Contains(discreteTextConfiguration.Id));
			Assert.IsTrue(configurationIds.Contains(discreteNumberConfiguration.Id));

			var textConfigurationSetting = orchestrationEvent.ExecutionDetails.Configurations.First(c => c.Id == textConfiguration.Id) as TextConfigurationSetting;
			Assert.IsNotNull(textConfigurationSetting);
			Assert.IsNull(textConfigurationSetting.Value);

			var numberConfigurationSetting = orchestrationEvent.ExecutionDetails.Configurations.First(c => c.Id == numberConfiguration.Id) as NumberConfigurationSetting;
			Assert.IsNotNull(numberConfigurationSetting);
			Assert.IsNull(numberConfigurationSetting.Value);

			var discreteTextConfigurationSetting = orchestrationEvent.ExecutionDetails.Configurations.First(c => c.Id == discreteTextConfiguration.Id) as DiscreteTextConfigurationSetting;
			Assert.IsNotNull(discreteTextConfigurationSetting);
			Assert.IsNull(discreteTextConfigurationSetting.Value);

			var discreteNumberConfigurationSetting = orchestrationEvent.ExecutionDetails.Configurations.First(c => c.Id == discreteNumberConfiguration.Id) as DiscreteNumberConfigurationSetting;
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
					ExecutionDetails = new ScriptExecutionDetails("SomeScript").AddConfiguration(new TextConfigurationSetting(textConfiguration) { Value = "HelloWorld" }),
				},
			});

			Assert.IsNull(resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.First().OriginalSection);
			Assert.IsTrue(resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.First().IsNew);
			Assert.IsFalse(resourcePool.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.First().HasChanges);

			objectCreator.CreateResourcePool(resourcePool);
			resourcePool = TestContext.Api.ResourcePools.Read(resourcePool.Id);

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
			Assert.AreEqual(textConfiguration.Id, textConfigurationSetting.Id);
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
					ExecutionDetails = new ScriptExecutionDetails("SomeScript").AddConfiguration(new TextConfigurationSetting(textConfiguration) { Value = "HelloWorld" }),
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
					ExecutionDetails = new ScriptExecutionDetails("PrerollStartScript").AddConfiguration(new TextConfigurationSetting(textConfiguration) { Value = "HelloWorld" }),
				},
				new OrchestrationEvent
				{
					EventType = OrchestrationEventType.PostrollStart,
					ExecutionDetails = new ScriptExecutionDetails("PostrollStartScript").AddConfiguration(new DiscreteNumberConfigurationSetting(discreteNumberConfiguration) { Value = new NumberDiscreet(3, "Three") }),
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

			Assert.AreEqual(0, prerollStartEvent.ExecutionDetails.Capabilities.Count);
			Assert.AreEqual(0, prerollStartEvent.ExecutionDetails.Capacities.Count);
			Assert.AreEqual(1, prerollStartEvent.ExecutionDetails.Configurations.Count);

			var prerollStartTextConfigurationSetting = prerollStartEvent.ExecutionDetails.Configurations.First() as TextConfigurationSetting;
			Assert.IsNotNull(prerollStartTextConfigurationSetting);
			Assert.AreEqual(textConfiguration.Id, prerollStartTextConfigurationSetting.Id);
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
				ExecutionDetails = new ScriptExecutionDetails("PrerollStopScript").AddConfiguration(new TextConfigurationSetting(textConfiguration) { Value = "HelloWorld" }),
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

			Assert.AreEqual(0, prerollStopEvent.ExecutionDetails.Capabilities.Count);
			Assert.AreEqual(0, prerollStopEvent.ExecutionDetails.Capacities.Count);
			Assert.AreEqual(1, prerollStopEvent.ExecutionDetails.Configurations.Count);

			var prerollStopTextConfigurationSetting = prerollStopEvent.ExecutionDetails.Configurations.First() as TextConfigurationSetting;
			Assert.IsNotNull(prerollStopTextConfigurationSetting);
			Assert.AreEqual(textConfiguration.Id, prerollStopTextConfigurationSetting.Id);
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
			Assert.AreEqual(discreteNumberConfiguration.Id, discreteConfigurationSetting.Id);
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
			resourcePool1 = TestContext.Api.ResourcePools.Read(resourcePool1.Id);

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

			resourcePool2 = TestContext.Api.ResourcePools.Read(resourcePool2.Id);
			Assert.IsNotNull(resourcePool2);
		}

		[TestMethod]
		public void AssignWithMismatchedCapacityTypeThrowsException()
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
			resourcePool.OrchestrationSettings
				.AddCapacity(new NumberCapacitySetting(rangeCapacity.Id))
				.AddCapacity(new RangeCapacitySetting(numberCapacity.Id));

			MediaOpsException? expectedException = null;
			try
			{
				objectCreator.CreateResourcePool(resourcePool);
			}
			catch (MediaOpsException ex)
			{
				expectedException = ex;
			}

			Assert.IsNotNull(expectedException, "Expected exception was not thrown.");

			Assert.AreEqual(2, expectedException.TraceData.ErrorData.Count);
			var orchestrationSettingsError = expectedException.TraceData.ErrorData.OfType<OrchestrationSettingsError>();
			Assert.AreEqual(2, orchestrationSettingsError.Count());

			var orchestrationSettingsInvalidCapacitySettingsError = orchestrationSettingsError.OfType<OrchestrationSettingsInvalidCapacitySettingsError>().ToList();
			Assert.AreEqual(2, orchestrationSettingsInvalidCapacitySettingsError.Count);

			var rangeCapacitySettingError = orchestrationSettingsInvalidCapacitySettingsError.FirstOrDefault(e => e.CapacityId == numberCapacity.Id);
			Assert.IsNotNull(rangeCapacitySettingError);
			Assert.AreEqual($"A range capacity setting cannot be used with a capacity of type 'NumberCapacity'.", rangeCapacitySettingError.ErrorMessage);
			Assert.AreEqual(resourcePool.OrchestrationSettings.Id, rangeCapacitySettingError.Id);

			var numberCapacitySettingError = orchestrationSettingsInvalidCapacitySettingsError.FirstOrDefault(e => e.CapacityId == rangeCapacity.Id);
			Assert.IsNotNull(numberCapacitySettingError);
			Assert.AreEqual($"A number capacity setting cannot be used with a capacity of type 'RangeCapacity'.", numberCapacitySettingError.ErrorMessage);
			Assert.AreEqual(resourcePool.OrchestrationSettings.Id, numberCapacitySettingError.Id);
		}

		[TestMethod]
		public void AssignWithInvalidCapabilityValueThrowsException()
		{
			var prefix = Guid.NewGuid();

			var capability = new Capability
			{
				Name = $"{prefix}_Capability",
			}
			.SetDiscretes(["Valid1", "Valid2"]);
			objectCreator.CreateCapability(capability);

			var resourcePool = new ResourcePool
			{
				Name = $"{prefix}_ResourcePool",
			};
			resourcePool.OrchestrationSettings
				.AddCapability(new CapabilitySetting(capability) { Value = "InvalidValue" });

			MediaOpsException? expectedException = null;
			try
			{
				objectCreator.CreateResourcePool(resourcePool);
			}
			catch (MediaOpsException ex)
			{
				expectedException = ex;
			}

			Assert.IsNotNull(expectedException, "Expected exception was not thrown.");

			Assert.AreEqual(1, expectedException.TraceData.ErrorData.Count);
			var orchestrationSettingsErrors = expectedException.TraceData.ErrorData.OfType<OrchestrationSettingsError>().ToList();
			Assert.AreEqual(1, orchestrationSettingsErrors.Count);

			var invalidCapabilitySettingsError = orchestrationSettingsErrors.OfType<OrchestrationSettingsInvalidCapabilitySettingsError>().Single();
			Assert.AreEqual($"Discrete value 'InvalidValue' is not valid for capability '{capability.Name}'.", invalidCapabilitySettingsError.ErrorMessage);
			Assert.AreEqual(capability.Id, invalidCapabilitySettingsError.CapabilityId);
			Assert.AreEqual(resourcePool.OrchestrationSettings.Id, invalidCapabilitySettingsError.Id);
		}

		[TestMethod]
		public void AssignWithMismatchedConfigurationTypeThrowsException()
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

			// Each setting uses the ID of the wrong configuration type
			resourcePool.OrchestrationSettings
				.AddConfiguration(new NumberConfigurationSetting(textConfiguration.Id) { Value = 42 })
				.AddConfiguration(new TextConfigurationSetting(numberConfiguration.Id) { Value = "text" })
				.AddConfiguration(new DiscreteTextConfigurationSetting(discreteNumberConfiguration.Id) { Value = new TextDiscreet("A", "A") })
				.AddConfiguration(new DiscreteNumberConfigurationSetting(discreteTextConfiguration.Id) { Value = new NumberDiscreet(1, "One") });

			MediaOpsException? expectedException = null;
			try
			{
				objectCreator.CreateResourcePool(resourcePool);
			}
			catch (MediaOpsException ex)
			{
				expectedException = ex;
			}

			Assert.IsNotNull(expectedException, "Expected exception was not thrown.");

			Assert.AreEqual(4, expectedException.TraceData.ErrorData.Count);
			var orchestrationSettingsErrors = expectedException.TraceData.ErrorData.OfType<OrchestrationSettingsError>().ToList();
			Assert.AreEqual(4, orchestrationSettingsErrors.Count);

			var invalidConfigurationSettingsErrors = orchestrationSettingsErrors.OfType<OrchestrationSettingsInvalidConfigurationSettingsError>().ToList();
			Assert.AreEqual(4, invalidConfigurationSettingsErrors.Count);

			var numberOnTextError = invalidConfigurationSettingsErrors.SingleOrDefault(e => e.ConfigurationId == textConfiguration.Id);
			Assert.IsNotNull(numberOnTextError);
			Assert.AreEqual("A number configuration setting cannot be used with a configuration of type 'TextConfiguration'.", numberOnTextError.ErrorMessage);
			Assert.AreEqual(resourcePool.OrchestrationSettings.Id, numberOnTextError.Id);

			var textOnNumberError = invalidConfigurationSettingsErrors.SingleOrDefault(e => e.ConfigurationId == numberConfiguration.Id);
			Assert.IsNotNull(textOnNumberError);
			Assert.AreEqual("A text configuration setting cannot be used with a configuration of type 'NumberConfiguration'.", textOnNumberError.ErrorMessage);
			Assert.AreEqual(resourcePool.OrchestrationSettings.Id, textOnNumberError.Id);

			var discreteTextOnDiscreteNumberError = invalidConfigurationSettingsErrors.SingleOrDefault(e => e.ConfigurationId == discreteNumberConfiguration.Id);
			Assert.IsNotNull(discreteTextOnDiscreteNumberError);
			Assert.AreEqual("A discrete text configuration setting cannot be used with a configuration of type 'DiscreteNumberConfiguration'.", discreteTextOnDiscreteNumberError.ErrorMessage);
			Assert.AreEqual(resourcePool.OrchestrationSettings.Id, discreteTextOnDiscreteNumberError.Id);

			var discreteNumberOnDiscreteTextError = invalidConfigurationSettingsErrors.SingleOrDefault(e => e.ConfigurationId == discreteTextConfiguration.Id);
			Assert.IsNotNull(discreteNumberOnDiscreteTextError);
			Assert.AreEqual("A discrete number configuration setting cannot be used with a configuration of type 'DiscreteTextConfiguration'.", discreteNumberOnDiscreteTextError.ErrorMessage);
			Assert.AreEqual(resourcePool.OrchestrationSettings.Id, discreteNumberOnDiscreteTextError.Id);
		}

		[TestMethod]
		public void AssignWithInvalidDiscreteTextConfigurationValueThrowsException()
		{
			var prefix = Guid.NewGuid();

			var configuration = new DiscreteTextConfiguration
			{
				Name = $"{prefix}_DiscreteTextConfiguration",
			}
			.SetDiscretes([new TextDiscreet("1", "Value1"), new TextDiscreet("2", "Value2")]);
			objectCreator.CreateConfiguration(configuration);

			var invalidValue = new TextDiscreet("99", "InvalidValue");

			var resourcePool = new ResourcePool
			{
				Name = $"{prefix}_ResourcePool",
			};
			resourcePool.OrchestrationSettings
				.AddConfiguration(new DiscreteTextConfigurationSetting(configuration.Id) { Value = invalidValue });

			MediaOpsException? expectedException = null;
			try
			{
				objectCreator.CreateResourcePool(resourcePool);
			}
			catch (MediaOpsException ex)
			{
				expectedException = ex;
			}

			Assert.IsNotNull(expectedException, "Expected exception was not thrown.");

			Assert.AreEqual(1, expectedException.TraceData.ErrorData.Count);
			var invalidConfigurationSettingsError = expectedException.TraceData.ErrorData.OfType<OrchestrationSettingsInvalidConfigurationSettingsError>().Single();
			Assert.AreEqual($"Value '{invalidValue}' is not a valid discrete value for this configuration.", invalidConfigurationSettingsError.ErrorMessage);
			Assert.AreEqual(configuration.Id, invalidConfigurationSettingsError.ConfigurationId);
			Assert.AreEqual(resourcePool.OrchestrationSettings.Id, invalidConfigurationSettingsError.Id);
		}

		[TestMethod]
		public void AssignWithInvalidDiscreteNumberConfigurationValueThrowsException()
		{
			var prefix = Guid.NewGuid();

			var configuration = new DiscreteNumberConfiguration
			{
				Name = $"{prefix}_DiscreteNumberConfiguration",
			}
			.SetDiscretes([new NumberDiscreet(1, "One"), new NumberDiscreet(2, "Two")]);
			objectCreator.CreateConfiguration(configuration);

			var invalidValue = new NumberDiscreet(99, "NinetyNine");

			var resourcePool = new ResourcePool
			{
				Name = $"{prefix}_ResourcePool",
			};
			resourcePool.OrchestrationSettings
				.AddConfiguration(new DiscreteNumberConfigurationSetting(configuration.Id) { Value = invalidValue });

			MediaOpsException? expectedException = null;
			try
			{
				objectCreator.CreateResourcePool(resourcePool);
			}
			catch (MediaOpsException ex)
			{
				expectedException = ex;
			}

			Assert.IsNotNull(expectedException, "Expected exception was not thrown.");

			Assert.AreEqual(1, expectedException.TraceData.ErrorData.Count);
			var invalidConfigurationSettingsError = expectedException.TraceData.ErrorData.OfType<OrchestrationSettingsInvalidConfigurationSettingsError>().Single();
			Assert.AreEqual($"Value '{invalidValue}' is not a valid discrete value for this configuration.", invalidConfigurationSettingsError.ErrorMessage);
			Assert.AreEqual(configuration.Id, invalidConfigurationSettingsError.ConfigurationId);
			Assert.AreEqual(resourcePool.OrchestrationSettings.Id, invalidConfigurationSettingsError.Id);
		}

		[TestMethod]
		public void AssignWithNumberConfigurationValueOutOfRangeThrowsException()
		{
			var prefix = Guid.NewGuid();

			var configuration = new NumberConfiguration
			{
				Name = $"{prefix}_NumberConfiguration",
				RangeMin = 0,
				RangeMax = 100,
			};
			objectCreator.CreateConfiguration(configuration);

			var resourcePool = new ResourcePool
			{
				Name = $"{prefix}_ResourcePool",
			};
			resourcePool.OrchestrationSettings
				.AddConfiguration(new NumberConfigurationSetting(configuration.Id) { Value = 150 });

			MediaOpsException? expectedException = null;
			try
			{
				objectCreator.CreateResourcePool(resourcePool);
			}
			catch (MediaOpsException ex)
			{
				expectedException = ex;
			}

			Assert.IsNotNull(expectedException, "Expected exception was not thrown.");

			Assert.AreEqual(1, expectedException.TraceData.ErrorData.Count);
			var invalidConfigurationSettingsError = expectedException.TraceData.ErrorData.OfType<OrchestrationSettingsInvalidConfigurationSettingsError>().Single();
			Assert.AreEqual($"Value '150' must be lower than or equal to '100'.", invalidConfigurationSettingsError.ErrorMessage);
			Assert.AreEqual(configuration.Id, invalidConfigurationSettingsError.ConfigurationId);
			Assert.AreEqual(resourcePool.OrchestrationSettings.Id, invalidConfigurationSettingsError.Id);
		}
	}
}