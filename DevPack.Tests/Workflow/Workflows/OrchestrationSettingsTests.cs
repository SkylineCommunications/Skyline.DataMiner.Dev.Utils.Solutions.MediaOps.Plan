namespace RT_MediaOps.Plan.Workflow.Workflows
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using RT_MediaOps.Plan.RegressionTests;
	using RT_MediaOps.Plan.RST;

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
		public void WorkflowOrchestrationSettings_AddAndRemoveCapability_NoPersistence()
		{
			var prefix = Guid.NewGuid();

			var capability = new Capability
			{
				Name = $"{prefix}_Capability",
			}
			.SetDiscretes(["Value 1", "Value 2"]);
			objectCreator.CreateCapability(capability);

			var workflow = new Workflow
			{
				Name = $"{prefix}_Workflow",
			};

			var orchestrationSettings = workflow.OrchestrationSettings;

			// Add
			orchestrationSettings.AddCapability(new CapabilitySetting(capability));
			Assert.AreEqual(1, orchestrationSettings.Capabilities.Count);
			Assert.AreEqual(capability.Id, orchestrationSettings.Capabilities.Single().Id);

			orchestrationSettings.RemoveCapability(orchestrationSettings.Capabilities.Single());
			Assert.AreEqual(0, orchestrationSettings.Capabilities.Count);
		}

		[TestMethod]
		public void WorkflowOrchestrationSettings_AddAndRemoveCapacities_NoPersistence()
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

			var workflow = new Workflow
			{
				Name = $"{prefix}_Workflow",
			};

			var orchestrationSettings = workflow.OrchestrationSettings;

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
		public void WorkflowOrchestrationSettings_AddAndRemoveConfigurations_NoPersistence()
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

			var workflow = new Workflow
			{
				Name = $"{prefix}_Workflow",
			};

			var orchestrationSettings = workflow.OrchestrationSettings;

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
		public void WorkflowOrchestrationSettings_AddAndRemoveCapability_WithPersistence()
		{
			var prefix = Guid.NewGuid();

			var capability = new Capability
			{
				Name = $"{prefix}_Capability",
			}
			.SetDiscretes(["Value 1", "Value 2"]);
			objectCreator.CreateCapability(capability);

			var workflow = new Workflow
			{
				Name = $"{prefix}_Workflow",
			};

			var orchestrationSettings = workflow.OrchestrationSettings;

			// Add
			orchestrationSettings.AddCapability(new CapabilitySetting(capability));
			Assert.AreEqual(1, orchestrationSettings.Capabilities.Count);
			Assert.AreEqual(capability.Id, orchestrationSettings.Capabilities.Single().Id);

			workflow = objectCreator.CreateWorkflow(workflow);

			workflow.OrchestrationSettings.RemoveCapability(orchestrationSettings.Capabilities.Single());
			Assert.AreEqual(0, workflow.OrchestrationSettings.Capabilities.Count);
		}

		[TestMethod]
		public void WorkflowOrchestrationSettings_AddAndRemoveCapacities_WithPersistence()
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

			var workflow = new Workflow
			{
				Name = $"{prefix}_Workflow",
			};

			var orchestrationSettings = workflow.OrchestrationSettings;

			// Add
			orchestrationSettings
				.AddCapacity(new NumberCapacitySetting(numberCapacity))
				.AddCapacity(new RangeCapacitySetting(rangeCapacity));

			Assert.AreEqual(2, orchestrationSettings.Capacities.Count);
			Assert.IsTrue(orchestrationSettings.Capacities.Any(c => c.Id == numberCapacity.Id));
			Assert.IsTrue(orchestrationSettings.Capacities.Any(c => c.Id == rangeCapacity.Id));

			workflow = objectCreator.CreateWorkflow(workflow);

			foreach (var capacity in orchestrationSettings.Capacities.ToList())
			{
				workflow.OrchestrationSettings.RemoveCapacity(capacity);
			}

			Assert.AreEqual(0, workflow.OrchestrationSettings.Capacities.Count);
		}

		[TestMethod]
		public void WorkflowOrchestrationSettings_AddAndRemoveConfigurations_WithPersistence()
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

			var workflow = new Workflow
			{
				Name = $"{prefix}_Workflow",
			};

			var orchestrationSettings = workflow.OrchestrationSettings;

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

			workflow = objectCreator.CreateWorkflow(workflow);

			foreach (var configuration in orchestrationSettings.Configurations.ToList())
			{
				workflow.OrchestrationSettings.RemoveConfiguration(configuration);
			}

			Assert.AreEqual(0, workflow.OrchestrationSettings.Configurations.Count);
		}

		[TestMethod]
		public void ParameterSettingsWithNoValues_CreateWorkflow()
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

			var workflow = new Workflow
			{
				Name = $"{prefix}_Workflow",
			};
			workflow.OrchestrationSettings
				.AddCapability(new CapabilitySetting(capability))
				.AddCapacity(new NumberCapacitySetting(numberCapacity))
				.AddCapacity(new RangeCapacitySetting(rangeCapacity))
				.AddConfiguration(new TextConfigurationSetting(textConfiguration))
				.AddConfiguration(new NumberConfigurationSetting(numberConfiguration))
				.AddConfiguration(new DiscreteTextConfigurationSetting(discreteTextConfiguration))
				.AddConfiguration(new DiscreteNumberConfigurationSetting(discreteNumberConfiguration));

			workflow = objectCreator.CreateWorkflow(workflow);
			Assert.IsNotNull(workflow);
			Assert.IsNotNull(workflow.OrchestrationSettings);
			Assert.AreEqual(1, workflow.OrchestrationSettings.Capabilities.Count);
			Assert.AreEqual(2, workflow.OrchestrationSettings.Capacities.Count);
			Assert.AreEqual(4, workflow.OrchestrationSettings.Configurations.Count);

			var capabilityIds = workflow.OrchestrationSettings.Capabilities.Select(c => c.Id).ToList();
			Assert.IsTrue(capabilityIds.Contains(capability.Id));

			var capacityIds = workflow.OrchestrationSettings.Capacities.Select(c => c.Id).ToList();
			Assert.IsTrue(capacityIds.Contains(numberCapacity.Id));
			Assert.IsTrue(capacityIds.Contains(rangeCapacity.Id));

			var configurationIds = workflow.OrchestrationSettings.Configurations.Select(c => c.Id).ToList();
			Assert.IsTrue(configurationIds.Contains(textConfiguration.Id));
			Assert.IsTrue(configurationIds.Contains(numberConfiguration.Id));
			Assert.IsTrue(configurationIds.Contains(discreteTextConfiguration.Id));
			Assert.IsTrue(configurationIds.Contains(discreteNumberConfiguration.Id));
		}

		[TestMethod]
		public void ParameterSettingsWithNoValues_UpdateWorkflow()
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

			var workflow = new Workflow
			{
				Name = $"{prefix}_Workflow",
			};

			workflow = objectCreator.CreateWorkflow(workflow);

			workflow.OrchestrationSettings
				.AddCapability(new CapabilitySetting(capability))
				.AddCapacity(new NumberCapacitySetting(numberCapacity))
				.AddCapacity(new RangeCapacitySetting(rangeCapacity))
				.AddConfiguration(new TextConfigurationSetting(textConfiguration))
				.AddConfiguration(new NumberConfigurationSetting(numberConfiguration))
				.AddConfiguration(new DiscreteTextConfigurationSetting(discreteTextConfiguration))
				.AddConfiguration(new DiscreteNumberConfigurationSetting(discreteNumberConfiguration));

			workflow = TestContext.Api.Workflows.Update(workflow);

			Assert.IsNotNull(workflow);
			Assert.IsNotNull(workflow.OrchestrationSettings);
			Assert.AreEqual(1, workflow.OrchestrationSettings.Capabilities.Count);
			Assert.AreEqual(2, workflow.OrchestrationSettings.Capacities.Count);
			Assert.AreEqual(4, workflow.OrchestrationSettings.Configurations.Count);

			var capabilityIds = workflow.OrchestrationSettings.Capabilities.Select(c => c.Id).ToList();
			Assert.IsTrue(capabilityIds.Contains(capability.Id));

			var capacityIds = workflow.OrchestrationSettings.Capacities.Select(c => c.Id).ToList();
			Assert.IsTrue(capacityIds.Contains(numberCapacity.Id));
			Assert.IsTrue(capacityIds.Contains(rangeCapacity.Id));

			var configurationIds = workflow.OrchestrationSettings.Configurations.Select(c => c.Id).ToList();
			Assert.IsTrue(configurationIds.Contains(textConfiguration.Id));
			Assert.IsTrue(configurationIds.Contains(numberConfiguration.Id));
			Assert.IsTrue(configurationIds.Contains(discreteTextConfiguration.Id));
			Assert.IsTrue(configurationIds.Contains(discreteNumberConfiguration.Id));
		}

		[TestMethod]
		public void OrchestrationEvents_ParameterSettingsWithNoValues_UpdateWorkflow()
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

			var workflow = new Workflow
			{
				Name = $"{prefix}_Workflow",
			};

			workflow = objectCreator.CreateWorkflow(workflow);

			workflow.OrchestrationSettings.AddOrchestrationEvent(new OrchestrationEvent
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

			workflow = TestContext.Api.Workflows.Update(workflow);

			Assert.IsNotNull(workflow);
			Assert.IsNotNull(workflow.OrchestrationSettings);
			Assert.IsNotNull(workflow.OrchestrationSettings.OrchestrationEvents);
			Assert.AreEqual(1, workflow.OrchestrationSettings.OrchestrationEvents.Count);

			var orchestrationEvent = workflow.OrchestrationSettings.OrchestrationEvents.First();
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
		public void DuplicateParameterSettingsThrowsException_CreateWorkflow()
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

			var workflow = new Workflow
			{
				Name = $"{prefix}_Workflow",
			};
			workflow.OrchestrationSettings
				.AddCapability(new CapabilitySetting(capability))
				.AddCapability(new CapabilitySetting(capability))
				.AddCapacity(new NumberCapacitySetting(numberCapacity))
				.AddCapacity(new NumberCapacitySetting(numberCapacity))
				.AddConfiguration(new TextConfigurationSetting(textConfiguration))
				.AddConfiguration(new TextConfigurationSetting(textConfiguration));

			try
			{
				workflow = objectCreator.CreateWorkflow(workflow);
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
				Assert.AreEqual(workflow.OrchestrationSettings.Id, invalidCapacitySettingsError.Id);

				var invalidCapabilitySettingsError = orchestrationSettingsErrors.OfType<OrchestrationSettingsInvalidCapabilitySettingsError>().SingleOrDefault();
				Assert.IsNotNull(invalidCapabilitySettingsError);
				Assert.AreEqual($"Capability with ID '{capability.Id}' is defined 2 times. Duplicate capability settings are not allowed.", invalidCapabilitySettingsError.ErrorMessage);
				Assert.AreEqual(capability.Id, invalidCapabilitySettingsError.CapabilityId);
				Assert.AreEqual(workflow.OrchestrationSettings.Id, invalidCapabilitySettingsError.Id);

				var invalidConfigurationSettingsError = orchestrationSettingsErrors.OfType<OrchestrationSettingsInvalidConfigurationSettingsError>().SingleOrDefault();
				Assert.IsNotNull(invalidConfigurationSettingsError);
				Assert.AreEqual($"Configuration with ID '{textConfiguration.Id}' is defined 2 times. Duplicate configuration settings are not allowed.", invalidConfigurationSettingsError.ErrorMessage);
				Assert.AreEqual(textConfiguration.Id, invalidConfigurationSettingsError.ConfigurationId);
				Assert.AreEqual(workflow.OrchestrationSettings.Id, invalidConfigurationSettingsError.Id);
			}
		}

		[TestMethod]
		public void SingleOrchestrationEvents_CreateWorkflow()
		{
			// Create Configuration
			var prefix = Guid.NewGuid();
			var textConfiguration = new TextConfiguration
			{
				Name = $"{prefix}_TextConfiguration",
			};

			objectCreator.CreateConfigurations([textConfiguration]);

			// Create new workflow with Orchestration Setting referencing the created configuration
			var workflow = new Workflow
			{
				Name = $"{prefix}_Workflow",
			};

			workflow.OrchestrationSettings.SetOrchestrationEvents(new List<OrchestrationEvent>
			{
				new OrchestrationEvent
				{
					EventType = OrchestrationEventType.PrerollStart,
					ExecutionDetails = new ScriptExecutionDetails("SomeScript").AddConfiguration(new TextConfigurationSetting(textConfiguration) { Value = "HelloWorld" }),
				},
			});

			Assert.IsNull(workflow.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.First().OriginalSection);
			Assert.IsTrue(workflow.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.First().IsNew);
			Assert.IsFalse(workflow.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.First().HasChanges);

			workflow = objectCreator.CreateWorkflow(workflow);

			Assert.IsNotNull(workflow.OrchestrationSettings);
			Assert.IsNotNull(workflow.OrchestrationSettings.OrchestrationEvents);
			Assert.AreEqual(1, workflow.OrchestrationSettings.OrchestrationEvents.Count);

			Assert.AreEqual(OrchestrationEventType.PrerollStart, workflow.OrchestrationSettings.OrchestrationEvents.First().EventType);
			Assert.IsNotNull(workflow.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails);
			Assert.AreEqual("SomeScript", workflow.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.ScriptName);

			Assert.IsNotNull(workflow.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Capabilities);
			Assert.IsNotNull(workflow.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Capacities);
			Assert.IsNotNull(workflow.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations);

			Assert.AreEqual(0, workflow.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Capabilities.Count);
			Assert.AreEqual(0, workflow.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Capacities.Count);
			Assert.AreEqual(1, workflow.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.Count);
			Assert.IsNull(workflow.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.First().OriginalSection);
			Assert.IsFalse(workflow.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.First().IsNew);
			Assert.IsFalse(workflow.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.First().HasChanges);

			var textConfigurationSetting = workflow.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.First() as TextConfigurationSetting;
			Assert.IsNotNull(textConfigurationSetting);
			Assert.AreEqual(textConfiguration.Id, textConfigurationSetting.Id);
			Assert.AreEqual("HelloWorld", textConfigurationSetting.Value);
		}

		[TestMethod]
		public void SingleOrchestrationEvents_UpdateWorkflow()
		{
			var prefix = Guid.NewGuid();

			var workflow = new Workflow
			{
				Name = $"{prefix}_Workflow",
			};

			workflow = objectCreator.CreateWorkflow(workflow);

			// Create Configuration
			var textConfiguration = new TextConfiguration
			{
				Name = $"{prefix}_TextConfiguration",
			};

			objectCreator.CreateConfigurations([textConfiguration]);

			workflow.OrchestrationSettings.SetOrchestrationEvents(new List<OrchestrationEvent>
			{
				new OrchestrationEvent
				{
					EventType = OrchestrationEventType.PrerollStart,
					ExecutionDetails = new ScriptExecutionDetails("SomeScript").AddConfiguration(new TextConfigurationSetting(textConfiguration) { Value = "HelloWorld" }),
				},
			});

			workflow = TestContext.Api.Workflows.Update(workflow);

			Assert.IsNotNull(workflow.OrchestrationSettings);
			Assert.IsNotNull(workflow.OrchestrationSettings.OrchestrationEvents);
			Assert.AreEqual(1, workflow.OrchestrationSettings.OrchestrationEvents.Count);

			Assert.AreEqual(OrchestrationEventType.PrerollStart, workflow.OrchestrationSettings.OrchestrationEvents.First().EventType);
			Assert.IsNotNull(workflow.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails);
			Assert.AreEqual("SomeScript", workflow.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.ScriptName);

			Assert.IsNotNull(workflow.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Capabilities);
			Assert.IsNotNull(workflow.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Capacities);
			Assert.IsNotNull(workflow.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations);

			Assert.AreEqual(0, workflow.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Capabilities.Count);
			Assert.AreEqual(0, workflow.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Capacities.Count);
			Assert.AreEqual(1, workflow.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.Count);

			Assert.IsNull(workflow.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.First().OriginalSection);
			Assert.IsFalse(workflow.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.First().IsNew);
			Assert.IsFalse(workflow.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.First().HasChanges);

			var textConfigurationSetting = workflow.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.First() as TextConfigurationSetting;
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

			// Create new workflow with Orchestration Setting referencing the created configuration
			var workflow = new Workflow
			{
				Name = $"{prefix}_Workflow",
			};

			workflow.OrchestrationSettings.SetOrchestrationEvents(new List<OrchestrationEvent>
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

			workflow = objectCreator.CreateWorkflow(workflow);

			Assert.IsNotNull(workflow.OrchestrationSettings);
			Assert.IsNotNull(workflow.OrchestrationSettings.OrchestrationEvents);
			Assert.AreEqual(2, workflow.OrchestrationSettings.OrchestrationEvents.Count);

			var prerollStartEvent = workflow.OrchestrationSettings.OrchestrationEvents.FirstOrDefault(e => e.EventType == OrchestrationEventType.PrerollStart);

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

			var postrollStartEvent = workflow.OrchestrationSettings.OrchestrationEvents.FirstOrDefault(e => e.EventType == OrchestrationEventType.PostrollStart);

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
			workflow.OrchestrationSettings.RemoveOrchestrationEvent(workflow.OrchestrationSettings.OrchestrationEvents.First(x => x.EventType == OrchestrationEventType.PrerollStart));
			Assert.AreEqual(1, workflow.OrchestrationSettings.OrchestrationEvents.Count);

			// Update PostrollStart Event
			workflow.OrchestrationSettings.OrchestrationEvents.First(x => x.EventType == OrchestrationEventType.PostrollStart)
				.ExecutionDetails = new ScriptExecutionDetails("UpdatedPostrollScript")
				.AddConfiguration(new DiscreteNumberConfigurationSetting(discreteNumberConfiguration) { Value = new NumberDiscreet(2, "Two") });

			// Add PrerollStop Event
			workflow.OrchestrationSettings.AddOrchestrationEvent(new OrchestrationEvent
			{
				EventType = OrchestrationEventType.PrerollStop,
				ExecutionDetails = new ScriptExecutionDetails("PrerollStopScript").AddConfiguration(new TextConfigurationSetting(textConfiguration) { Value = "HelloWorld" }),
			});

			workflow = TestContext.Api.Workflows.Update(workflow);

			Assert.IsNotNull(workflow.OrchestrationSettings);
			Assert.IsNotNull(workflow.OrchestrationSettings.OrchestrationEvents);
			Assert.AreEqual(2, workflow.OrchestrationSettings.OrchestrationEvents.Count);

			prerollStartEvent = workflow.OrchestrationSettings.OrchestrationEvents.FirstOrDefault(e => e.EventType == OrchestrationEventType.PrerollStart);
			Assert.IsNull(prerollStartEvent);

			var prerollStopEvent = workflow.OrchestrationSettings.OrchestrationEvents.FirstOrDefault(e => e.EventType == OrchestrationEventType.PrerollStop);

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

			var updatedPostrollStartEvent = workflow.OrchestrationSettings.OrchestrationEvents.FirstOrDefault(e => e.EventType == OrchestrationEventType.PostrollStart);

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
		public void AssignParameterFromExistingWorkflowToNewWorkflow()
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

			var workflow1 = new Workflow
			{
				Name = $"{prefix}_Workflow 1",
			};

			workflow1.OrchestrationSettings
				.SetCapabilities([new CapabilitySetting(capability)])
				.SetCapacities([new NumberCapacitySetting(numberCapacity), new RangeCapacitySetting(rangeCapacity)])
				.SetConfigurations([
					new TextConfigurationSetting(textConfiguration),
					new NumberConfigurationSetting(numberConfiguration),
					new DiscreteTextConfigurationSetting(discreteTextConfiguration),
					new DiscreteNumberConfigurationSetting(discreteNumberConfiguration)
				]);

			workflow1 = objectCreator.CreateWorkflow(workflow1);

			var workflow2 = new Workflow
			{
				Name = $"{prefix}_Workflow 2",
			};

			foreach (var capacitySetting in workflow1.OrchestrationSettings.Capacities)
			{
				workflow2.OrchestrationSettings.AddCapacity(capacitySetting);
			}

			foreach (var capabilitySetting in workflow1.OrchestrationSettings.Capabilities)
			{
				workflow2.OrchestrationSettings.AddCapability(capabilitySetting);
			}

			foreach (var configurationSetting in workflow1.OrchestrationSettings.Configurations)
			{
				workflow2.OrchestrationSettings.AddConfiguration(configurationSetting);
			}

			workflow2 = objectCreator.CreateWorkflow(workflow2);
			Assert.IsNotNull(workflow2);
		}
	}
}
