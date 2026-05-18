namespace RT_MediaOps.Plan.Workflow.Jobs
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

			var job = new Job
			{
				Name = $"{prefix}_Job",
			};

			var orchestrationSettings = job.OrchestrationSettings;

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

			var job = new Job
			{
				Name = $"{prefix}_Job",
			};

			var orchestrationSettings = job.OrchestrationSettings;

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

			var job = new Job
			{
				Name = $"{prefix}_Job",
			};

			var orchestrationSettings = job.OrchestrationSettings;

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

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = DateTime.Now,
				End = DateTime.Now.AddMinutes(5),
			};

			var orchestrationSettings = job.OrchestrationSettings;

			// Add
			orchestrationSettings.AddCapability(new CapabilitySetting(capability));
			Assert.AreEqual(1, orchestrationSettings.Capabilities.Count);
			Assert.AreEqual(capability.Id, orchestrationSettings.Capabilities.Single().Id);

			job = objectCreator.CreateJob(job);

			job.OrchestrationSettings.RemoveCapability(orchestrationSettings.Capabilities.Single());
			Assert.AreEqual(0, job.OrchestrationSettings.Capabilities.Count);
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

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = DateTime.Now,
				End = DateTime.Now.AddMinutes(5),
			};

			var orchestrationSettings = job.OrchestrationSettings;

			// Add
			orchestrationSettings
				.AddCapacity(new NumberCapacitySetting(numberCapacity))
				.AddCapacity(new RangeCapacitySetting(rangeCapacity));

			Assert.AreEqual(2, orchestrationSettings.Capacities.Count);
			Assert.IsTrue(orchestrationSettings.Capacities.Any(c => c.Id == numberCapacity.Id));
			Assert.IsTrue(orchestrationSettings.Capacities.Any(c => c.Id == rangeCapacity.Id));

			job = objectCreator.CreateJob(job);

			foreach (var capacity in orchestrationSettings.Capacities.ToList())
			{
				job.OrchestrationSettings.RemoveCapacity(capacity);
			}

			Assert.AreEqual(0, job.OrchestrationSettings.Capacities.Count);
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

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = DateTime.Now,
				End = DateTime.Now.AddMinutes(5),
			};

			var orchestrationSettings = job.OrchestrationSettings;

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

			job = objectCreator.CreateJob(job);

			foreach (var configuration in orchestrationSettings.Configurations.ToList())
			{
				job.OrchestrationSettings.RemoveConfiguration(configuration);
			}

			Assert.AreEqual(0, job.OrchestrationSettings.Configurations.Count);
		}

		[TestMethod]
		public void ParameterSettingsWithNoValues_CreateJob()
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

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = DateTime.Now,
				End = DateTime.Now.AddMinutes(5),
			};
			job.OrchestrationSettings
				.AddCapability(new CapabilitySetting(capability))
				.AddCapacity(new NumberCapacitySetting(numberCapacity))
				.AddCapacity(new RangeCapacitySetting(rangeCapacity))
				.AddConfiguration(new TextConfigurationSetting(textConfiguration))
				.AddConfiguration(new NumberConfigurationSetting(numberConfiguration))
				.AddConfiguration(new DiscreteTextConfigurationSetting(discreteTextConfiguration))
				.AddConfiguration(new DiscreteNumberConfigurationSetting(discreteNumberConfiguration));

			job = objectCreator.CreateJob(job);
			Assert.IsNotNull(job);
			Assert.IsNotNull(job.OrchestrationSettings);
			Assert.AreEqual(1, job.OrchestrationSettings.Capabilities.Count);
			Assert.AreEqual(2, job.OrchestrationSettings.Capacities.Count);
			Assert.AreEqual(4, job.OrchestrationSettings.Configurations.Count);

			var capabilityIds = job.OrchestrationSettings.Capabilities.Select(c => c.Id).ToList();
			Assert.IsTrue(capabilityIds.Contains(capability.Id));

			var capacityIds = job.OrchestrationSettings.Capacities.Select(c => c.Id).ToList();
			Assert.IsTrue(capacityIds.Contains(numberCapacity.Id));
			Assert.IsTrue(capacityIds.Contains(rangeCapacity.Id));

			var configurationIds = job.OrchestrationSettings.Configurations.Select(c => c.Id).ToList();
			Assert.IsTrue(configurationIds.Contains(textConfiguration.Id));
			Assert.IsTrue(configurationIds.Contains(numberConfiguration.Id));
			Assert.IsTrue(configurationIds.Contains(discreteTextConfiguration.Id));

			// Discrete number configuration contains two discrete values, only one is added as configuration references the existing object
			Assert.IsTrue(configurationIds.Contains(discreteNumberConfiguration.Id));
		}

		[TestMethod]
		public void ParameterSettingsWithNoValues_UpdateJob()
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

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = DateTime.Now,
				End = DateTime.Now.AddMinutes(5),
			};

			job = objectCreator.CreateJob(job);

			job.OrchestrationSettings
				.AddCapability(new CapabilitySetting(capability))
				.AddCapacity(new NumberCapacitySetting(numberCapacity))
				.AddCapacity(new RangeCapacitySetting(rangeCapacity))
				.AddConfiguration(new TextConfigurationSetting(textConfiguration))
				.AddConfiguration(new NumberConfigurationSetting(numberConfiguration))
				.AddConfiguration(new DiscreteTextConfigurationSetting(discreteTextConfiguration))
				.AddConfiguration(new DiscreteNumberConfigurationSetting(discreteNumberConfiguration));

			job = TestContext.Api.Jobs.Update(job);

			Assert.IsNotNull(job);
			Assert.IsNotNull(job.OrchestrationSettings);
			Assert.AreEqual(1, job.OrchestrationSettings.Capabilities.Count);
			Assert.AreEqual(2, job.OrchestrationSettings.Capacities.Count);
			Assert.AreEqual(4, job.OrchestrationSettings.Configurations.Count);

			var capabilityIds = job.OrchestrationSettings.Capabilities.Select(c => c.Id).ToList();
			Assert.IsTrue(capabilityIds.Contains(capability.Id));

			var capacityIds = job.OrchestrationSettings.Capacities.Select(c => c.Id).ToList();
			Assert.IsTrue(capacityIds.Contains(numberCapacity.Id));
			Assert.IsTrue(capacityIds.Contains(rangeCapacity.Id));

			var configurationIds = job.OrchestrationSettings.Configurations.Select(c => c.Id).ToList();
			Assert.IsTrue(configurationIds.Contains(textConfiguration.Id));
			Assert.IsTrue(configurationIds.Contains(numberConfiguration.Id));
			Assert.IsTrue(configurationIds.Contains(discreteTextConfiguration.Id));
			Assert.IsTrue(configurationIds.Contains(discreteNumberConfiguration.Id));
		}

		[TestMethod]
		public void OrchestrationEvents_ParameterSettingsWithNoValues_UpdateJob()
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

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = DateTime.Now,
				End = DateTime.Now.AddMinutes(5),
			};

			job = objectCreator.CreateJob(job);

			job.OrchestrationSettings.AddOrchestrationEvent(new OrchestrationEvent
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

			job = TestContext.Api.Jobs.Update(job);

			Assert.IsNotNull(job);
			Assert.IsNotNull(job.OrchestrationSettings);
			Assert.IsNotNull(job.OrchestrationSettings.OrchestrationEvents);
			Assert.AreEqual(1, job.OrchestrationSettings.OrchestrationEvents.Count);

			var orchestrationEvent = job.OrchestrationSettings.OrchestrationEvents.First();
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
		public void DuplicateParameterSettingsThrowsException_CreateJob()
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

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = DateTime.Now,
				End = DateTime.Now.AddMinutes(5),
			};
			job.OrchestrationSettings
				.AddCapability(new CapabilitySetting(capability))
				.AddCapability(new CapabilitySetting(capability))
				.AddCapacity(new NumberCapacitySetting(numberCapacity))
				.AddCapacity(new NumberCapacitySetting(numberCapacity))
				.AddConfiguration(new TextConfigurationSetting(textConfiguration))
				.AddConfiguration(new TextConfigurationSetting(textConfiguration));

			try
			{
				job = objectCreator.CreateJob(job);
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
				Assert.AreEqual(job.OrchestrationSettings.Id, invalidCapacitySettingsError.Id);

				var invalidCapabilitySettingsError = orchestrationSettingsErrors.OfType<OrchestrationSettingsInvalidCapabilitySettingsError>().SingleOrDefault();
				Assert.IsNotNull(invalidCapabilitySettingsError);
				Assert.AreEqual($"Capability with ID '{capability.Id}' is defined 2 times. Duplicate capability settings are not allowed.", invalidCapabilitySettingsError.ErrorMessage);
				Assert.AreEqual(capability.Id, invalidCapabilitySettingsError.CapabilityId);
				Assert.AreEqual(job.OrchestrationSettings.Id, invalidCapabilitySettingsError.Id);

				var invalidConfigurationSettingsError = orchestrationSettingsErrors.OfType<OrchestrationSettingsInvalidConfigurationSettingsError>().SingleOrDefault();
				Assert.IsNotNull(invalidConfigurationSettingsError);
				Assert.AreEqual($"Configuration with ID '{textConfiguration.Id}' is defined 2 times. Duplicate configuration settings are not allowed.", invalidConfigurationSettingsError.ErrorMessage);
				Assert.AreEqual(textConfiguration.Id, invalidConfigurationSettingsError.ConfigurationId);
				Assert.AreEqual(job.OrchestrationSettings.Id, invalidConfigurationSettingsError.Id);
			}
		}

		[TestMethod]
		public void SingleOrchestrationEvents_CreateJob()
		{
			// Create Configuration
			var prefix = Guid.NewGuid();
			var textConfiguration = new TextConfiguration
			{
				Name = $"{prefix}_TextConfiguration",
			};

			objectCreator.CreateConfigurations([textConfiguration]);

			// Create new job with Orchestration Setting referencing the created configuration
			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = DateTime.Now,
				End = DateTime.Now.AddMinutes(5),
			};

			job.OrchestrationSettings.SetOrchestrationEvents(new List<OrchestrationEvent>
			{
				new OrchestrationEvent
				{
					EventType = OrchestrationEventType.PrerollStart,
					ExecutionDetails = new ScriptExecutionDetails("SomeScript").AddConfiguration(new TextConfigurationSetting(textConfiguration) { Value = "HelloWorld" }),
				},
			});

			Assert.IsNull(job.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.First().OriginalSection);
			Assert.IsTrue(job.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.First().IsNew);
			Assert.IsFalse(job.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.First().HasChanges);

			job = objectCreator.CreateJob(job);

			Assert.IsNotNull(job.OrchestrationSettings);
			Assert.IsNotNull(job.OrchestrationSettings.OrchestrationEvents);
			Assert.AreEqual(1, job.OrchestrationSettings.OrchestrationEvents.Count);

			Assert.AreEqual(OrchestrationEventType.PrerollStart, job.OrchestrationSettings.OrchestrationEvents.First().EventType);
			Assert.IsNotNull(job.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails);
			Assert.AreEqual("SomeScript", job.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.ScriptName);

			Assert.IsNotNull(job.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Capabilities);
			Assert.IsNotNull(job.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Capacities);
			Assert.IsNotNull(job.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations);

			Assert.AreEqual(0, job.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Capabilities.Count);
			Assert.AreEqual(0, job.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Capacities.Count);
			Assert.AreEqual(1, job.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.Count);
			Assert.IsNull(job.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.First().OriginalSection); // No original section as this data isn't coming from DOM Section but from JSON
			Assert.IsFalse(job.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.First().IsNew);
			Assert.IsFalse(job.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.First().HasChanges);

			var textConfigurationSetting = job.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.First() as TextConfigurationSetting;
			Assert.IsNotNull(textConfigurationSetting);
			Assert.AreEqual(textConfiguration.Id, textConfigurationSetting.Id);
			Assert.AreEqual("HelloWorld", textConfigurationSetting.Value);
		}

		[TestMethod]
		public void SingleOrchestrationEvents_UpdateJob()
		{
			var prefix = Guid.NewGuid();
			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = DateTime.Now,
				End = DateTime.Now.AddMinutes(5),
			};

			job = objectCreator.CreateJob(job);

			// Create Configuration
			var textConfiguration = new TextConfiguration
			{
				Name = $"{prefix}_TextConfiguration",
			};

			objectCreator.CreateConfigurations([textConfiguration]);

			job.OrchestrationSettings.SetOrchestrationEvents(new List<OrchestrationEvent>
			{
				new OrchestrationEvent
				{
					EventType = OrchestrationEventType.PrerollStart,
					ExecutionDetails = new ScriptExecutionDetails("SomeScript").AddConfiguration(new TextConfigurationSetting(textConfiguration) { Value = "HelloWorld" }),
				},
			});

			job = TestContext.Api.Jobs.Update(job);

			Assert.IsNotNull(job.OrchestrationSettings);
			Assert.IsNotNull(job.OrchestrationSettings.OrchestrationEvents);
			Assert.AreEqual(1, job.OrchestrationSettings.OrchestrationEvents.Count);

			Assert.AreEqual(OrchestrationEventType.PrerollStart, job.OrchestrationSettings.OrchestrationEvents.First().EventType);
			Assert.IsNotNull(job.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails);
			Assert.AreEqual("SomeScript", job.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.ScriptName);

			Assert.IsNotNull(job.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Capabilities);
			Assert.IsNotNull(job.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Capacities);
			Assert.IsNotNull(job.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations);

			Assert.AreEqual(0, job.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Capabilities.Count);
			Assert.AreEqual(0, job.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Capacities.Count);
			Assert.AreEqual(1, job.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.Count);

			Assert.IsNull(job.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.First().OriginalSection); // No original section as this data isn't coming from DOM Section but from JSON
			Assert.IsFalse(job.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.First().IsNew);
			Assert.IsFalse(job.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.First().HasChanges);

			var textConfigurationSetting = job.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.Configurations.First() as TextConfigurationSetting;
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

			// Create new job with Orchestration Setting referencing the created configuration
			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = DateTime.Now,
				End = DateTime.Now.AddMinutes(5),
			};

			job.OrchestrationSettings.SetOrchestrationEvents(new List<OrchestrationEvent>
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

			job = objectCreator.CreateJob(job);

			Assert.IsNotNull(job.OrchestrationSettings);
			Assert.IsNotNull(job.OrchestrationSettings.OrchestrationEvents);
			Assert.AreEqual(2, job.OrchestrationSettings.OrchestrationEvents.Count);

			var prerollStartEvent = job.OrchestrationSettings.OrchestrationEvents.FirstOrDefault(e => e.EventType == OrchestrationEventType.PrerollStart);

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

			var postrollStartEvent = job.OrchestrationSettings.OrchestrationEvents.FirstOrDefault(e => e.EventType == OrchestrationEventType.PostrollStart);

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
			job.OrchestrationSettings.RemoveOrchestrationEvent(job.OrchestrationSettings.OrchestrationEvents.First(x => x.EventType == OrchestrationEventType.PrerollStart));
			Assert.AreEqual(1, job.OrchestrationSettings.OrchestrationEvents.Count);

			// Update PostrollStart Event
			job.OrchestrationSettings.OrchestrationEvents.First(x => x.EventType == OrchestrationEventType.PostrollStart)
				.ExecutionDetails = new ScriptExecutionDetails("UpdatedPostrollScript")
				.AddConfiguration(new DiscreteNumberConfigurationSetting(discreteNumberConfiguration) { Value = new NumberDiscreet(2, "Two") });

			// Add PrerollStop Event
			job.OrchestrationSettings.AddOrchestrationEvent(new OrchestrationEvent
			{
				EventType = OrchestrationEventType.PrerollStop,
				ExecutionDetails = new ScriptExecutionDetails("PrerollStopScript").AddConfiguration(new TextConfigurationSetting(textConfiguration) { Value = "HelloWorld" }),
			});

			job = TestContext.Api.Jobs.Update(job);

			Assert.IsNotNull(job.OrchestrationSettings);
			Assert.IsNotNull(job.OrchestrationSettings.OrchestrationEvents);
			Assert.AreEqual(2, job.OrchestrationSettings.OrchestrationEvents.Count);

			prerollStartEvent = job.OrchestrationSettings.OrchestrationEvents.FirstOrDefault(e => e.EventType == OrchestrationEventType.PrerollStart);
			Assert.IsNull(prerollStartEvent);

			var prerollStopEvent = job.OrchestrationSettings.OrchestrationEvents.FirstOrDefault(e => e.EventType == OrchestrationEventType.PrerollStop);

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

			var updatedPostrollStartEvent = job.OrchestrationSettings.OrchestrationEvents.FirstOrDefault(e => e.EventType == OrchestrationEventType.PostrollStart);

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
		public void AssignParameterFromExistingJobToNewJob()
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

			var job1 = new Job
			{
				Name = $"{prefix}_Job 1",
				Start = DateTime.Now,
				End = DateTime.Now.AddMinutes(5),
			};

			job1.OrchestrationSettings
				.SetCapabilities([new CapabilitySetting(capability)])
				.SetCapacities([new NumberCapacitySetting(numberCapacity), new RangeCapacitySetting(rangeCapacity)])
				.SetConfigurations([
					new TextConfigurationSetting(textConfiguration),
					new NumberConfigurationSetting(numberConfiguration),
					new DiscreteTextConfigurationSetting(discreteTextConfiguration),
					new DiscreteNumberConfigurationSetting(discreteNumberConfiguration)
				]);

			job1 = objectCreator.CreateJob(job1);

			var job2 = new Job
			{
				Name = $"{prefix}_Job 2",
				Start = DateTime.Now,
				End = DateTime.Now.AddMinutes(5),
			};

			foreach (var capacitySetting in job1.OrchestrationSettings.Capacities)
			{
				job2.OrchestrationSettings.AddCapacity(capacitySetting);
			}

			foreach (var capabilitySetting in job1.OrchestrationSettings.Capabilities)
			{
				job2.OrchestrationSettings.AddCapability(capabilitySetting);
			}

			foreach (var configurationSetting in job1.OrchestrationSettings.Configurations)
			{
				job2.OrchestrationSettings.AddConfiguration(configurationSetting);
			}

			job2 = objectCreator.CreateJob(job2);
			Assert.IsNotNull(job2);
		}
	}
}
