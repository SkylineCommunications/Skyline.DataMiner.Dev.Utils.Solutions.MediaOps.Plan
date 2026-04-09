namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using RT_MediaOps.Plan.RegressionTests;
	using RT_MediaOps.Plan.RST;

	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

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
			Assert.AreEqual(1, job.OrchestrationSettings.Capabilities.Count);

			job.OrchestrationSettings.RemoveCapability(orchestrationSettings.Capabilities.Single());
			Assert.AreEqual(0, job.OrchestrationSettings.Capabilities.Count);
		}
	}
}
