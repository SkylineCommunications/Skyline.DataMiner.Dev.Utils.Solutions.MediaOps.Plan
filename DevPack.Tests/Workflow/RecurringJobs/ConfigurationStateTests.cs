namespace RT_MediaOps.Plan.Workflow.RecurringJobs
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	[TestCategory("IntegrationTest")]
	[DoNotParallelize]
	public sealed class ConfigurationStateTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public ConfigurationStateTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void RecurringJobWithoutOrchestrationSettings_HasNoParametersDefined()
		{
			var prefix = Guid.NewGuid();

			var recurringJob = NewValidRecurringJob($"{prefix}_RecurringJob");
			recurringJob.NodeGraph.Add(new RecurringJobResourcePoolNode(CreateResourcePool(prefix)));

			var createdRecurringJob = objectCreator.CreateRecurringJob(recurringJob);

			AssertConfigurationStates(createdRecurringJob, ConfigurationState.NoParametersDefined, ConfigurationState.NoParametersDefined);
		}

		[TestMethod]
		public void RecurringJobWithAllValuesProvided_HasAllValuesProvided()
		{
			var prefix = Guid.NewGuid();

			var capability = CreateCapability(prefix, isMandatory: false);

			var recurringJob = NewValidRecurringJob($"{prefix}_RecurringJob");
			recurringJob.OrchestrationSettings.AddCapability(new CapabilitySetting(capability) { Value = "Belgium" });

			var node = new RecurringJobResourcePoolNode(CreateResourcePool(prefix));
			node.OrchestrationSettings.AddCapability(new CapabilitySetting(capability) { Value = "USA" });
			recurringJob.NodeGraph.Add(node);

			var createdRecurringJob = objectCreator.CreateRecurringJob(recurringJob);

			AssertConfigurationStates(createdRecurringJob, ConfigurationState.AllValuesProvided, ConfigurationState.AllValuesProvided);
		}

		[TestMethod]
		public void RecurringJobWithMandatoryValueMissing_HasMandatoryValuesMissing()
		{
			var prefix = Guid.NewGuid();

			var mandatoryCapability = CreateCapability(prefix, isMandatory: true);

			var recurringJob = NewValidRecurringJob($"{prefix}_RecurringJob");
			recurringJob.OrchestrationSettings.AddCapability(new CapabilitySetting(mandatoryCapability));

			var node = new RecurringJobResourcePoolNode(CreateResourcePool(prefix));
			node.OrchestrationSettings.AddCapability(new CapabilitySetting(mandatoryCapability));
			recurringJob.NodeGraph.Add(node);

			var createdRecurringJob = objectCreator.CreateRecurringJob(recurringJob);

			AssertConfigurationStates(createdRecurringJob, ConfigurationState.MandatoryValuesMissing, ConfigurationState.MandatoryValuesMissing);
		}

		[TestMethod]
		public void RecurringJobWithNonMandatoryValueMissing_HasNonMandatoryValuesMissing()
		{
			var prefix = Guid.NewGuid();

			var capability = CreateCapability(prefix, isMandatory: false);

			var recurringJob = NewValidRecurringJob($"{prefix}_RecurringJob");
			recurringJob.OrchestrationSettings.AddCapability(new CapabilitySetting(capability));

			var node = new RecurringJobResourcePoolNode(CreateResourcePool(prefix));
			node.OrchestrationSettings.AddCapability(new CapabilitySetting(capability));
			recurringJob.NodeGraph.Add(node);

			var createdRecurringJob = objectCreator.CreateRecurringJob(recurringJob);

			AssertConfigurationStates(createdRecurringJob, ConfigurationState.NonMandatoryValuesMissing, ConfigurationState.NonMandatoryValuesMissing);
		}

		[TestMethod]
		public void RecurringJobNodeState_IsCalculatedIndependentlyOfTheRecurringJobState()
		{
			var prefix = Guid.NewGuid();

			var capability = CreateCapability(prefix, isMandatory: false);
			var mandatoryCapability = CreateCapability(prefix, isMandatory: true);
			var resourcePool = CreateResourcePool(prefix);

			var recurringJob = NewValidRecurringJob($"{prefix}_RecurringJob");
			recurringJob.OrchestrationSettings.AddCapability(new CapabilitySetting(capability) { Value = "Belgium" });

			var nodeWithoutSettings = new RecurringJobResourcePoolNode(resourcePool);

			var nodeWithMissingMandatoryValue = new RecurringJobResourcePoolNode(resourcePool);
			nodeWithMissingMandatoryValue.OrchestrationSettings.AddCapability(new CapabilitySetting(mandatoryCapability));

			recurringJob.NodeGraph.Add(nodeWithoutSettings).Add(nodeWithMissingMandatoryValue);

			var createdRecurringJob = objectCreator.CreateRecurringJob(recurringJob);

			var readRecurringJob = TestContext.Api.RecurringJobs.Read(createdRecurringJob.Id);

			foreach (var recurringJobToVerify in new[] { createdRecurringJob, readRecurringJob })
			{
				Assert.AreEqual(ConfigurationState.AllValuesProvided, recurringJobToVerify.ConfigurationState);

				var nodesById = recurringJobToVerify.NodeGraph.Nodes.ToDictionary(x => x.Id);
				Assert.AreEqual(ConfigurationState.NoParametersDefined, nodesById[nodeWithoutSettings.Id].ConfigurationState);
				Assert.AreEqual(ConfigurationState.MandatoryValuesMissing, nodesById[nodeWithMissingMandatoryValue.Id].ConfigurationState);
			}
		}

		[TestMethod]
		public void CreateRecurringJobsInBulk_CalculatesTheStatePerRecurringJob()
		{
			var prefix = Guid.NewGuid();

			var capability = CreateCapability(prefix, isMandatory: false);
			var mandatoryCapability = CreateCapability(prefix, isMandatory: true);
			var resourcePool = CreateResourcePool(prefix);

			var recurringJobWithAllValues = NewValidRecurringJob($"{prefix}_RecurringJob_1");
			recurringJobWithAllValues.OrchestrationSettings.AddCapability(new CapabilitySetting(capability) { Value = "Belgium" });

			var nodeWithAllValues = new RecurringJobResourcePoolNode(resourcePool);
			nodeWithAllValues.OrchestrationSettings.AddCapability(new CapabilitySetting(capability) { Value = "USA" });
			recurringJobWithAllValues.NodeGraph.Add(nodeWithAllValues);

			var recurringJobWithMissingValues = NewValidRecurringJob($"{prefix}_RecurringJob_2");
			recurringJobWithMissingValues.OrchestrationSettings.AddCapability(new CapabilitySetting(mandatoryCapability));

			var nodeWithMissingValues = new RecurringJobResourcePoolNode(resourcePool);
			nodeWithMissingValues.OrchestrationSettings.AddCapability(new CapabilitySetting(mandatoryCapability));
			recurringJobWithMissingValues.NodeGraph.Add(nodeWithMissingValues);

			var createdRecurringJobs = objectCreator.CreateRecurringJobs([recurringJobWithAllValues, recurringJobWithMissingValues]);

			AssertConfigurationStates(
				createdRecurringJobs.Single(x => x.Id == recurringJobWithAllValues.Id),
				ConfigurationState.AllValuesProvided,
				ConfigurationState.AllValuesProvided);

			AssertConfigurationStates(
				createdRecurringJobs.Single(x => x.Id == recurringJobWithMissingValues.Id),
				ConfigurationState.MandatoryValuesMissing,
				ConfigurationState.MandatoryValuesMissing);
		}

		/// <summary>
		/// Asserts the configuration state of the specified recurring job and of every one of its nodes, both on the
		/// given instance and on the stored instance. All nodes of the recurring job are expected to have the same state.
		/// </summary>
		private static void AssertConfigurationStates(RecurringJob recurringJob, ConfigurationState expectedRecurringJobState, ConfigurationState expectedNodeState)
		{
			var readRecurringJob = TestContext.Api.RecurringJobs.Read(recurringJob.Id);

			foreach (var recurringJobToVerify in new[] { recurringJob, readRecurringJob })
			{
				Assert.AreEqual(expectedRecurringJobState, recurringJobToVerify.ConfigurationState);

				foreach (var node in recurringJobToVerify.NodeGraph.Nodes)
				{
					Assert.AreEqual(expectedNodeState, node.ConfigurationState);
				}
			}
		}

		private static RecurringJob NewValidRecurringJob(string name)
		{
			var recurringJob = new RecurringJob
			{
				Name = name,
				Start = DateTime.UtcNow.AddHours(1),
				Duration = TimeSpan.FromHours(1),
			};

			recurringJob.Pattern.RepeatType = RepeatType.Daily;
			recurringJob.Pattern.RepeatEvery = 1;
			recurringJob.Pattern.EndDate = DateTime.UtcNow.AddDays(10);

			return recurringJob;
		}

		private Capability CreateCapability(Guid prefix, bool isMandatory)
		{
			var capability = new Capability
			{
				Name = $"{prefix}_{(isMandatory ? "Mandatory" : "Optional")}Capability",
				IsMandatory = isMandatory,
			}
			.SetDiscretes(["Belgium", "USA"]);

			return objectCreator.CreateCapability(capability);
		}

		private ResourcePool CreateResourcePool(Guid prefix)
		{
			return TestContext.Api.ResourcePools.Complete(objectCreator.CreateResourcePool(new ResourcePool
			{
				Name = $"{prefix}_ResourcePool",
			}));
		}
	}
}
