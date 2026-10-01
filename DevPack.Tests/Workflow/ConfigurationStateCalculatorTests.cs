namespace RT_MediaOps.Plan.Workflow
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Extensions;
	using Skyline.DataMiner.Solutions.MediaOps.Live.Orchestration.Script.Inputs;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Simulation;

	[TestClass]
	public sealed class ConfigurationStateCalculatorTests
	{
		private const string DynamicScriptName = "Dynamic Orchestration Script";

		[TestMethod]
		public void HasMissingMandatoryCapabilityValues_MandatoryCapabilityWithoutValue_ReturnsTrue()
		{
			var (planApi, liveApi) = CreateApis();
			var mandatoryCapability = planApi.Capabilities.Create(new Capability
			{
				Name = $"{Guid.NewGuid()}_MandatoryCapability",
				IsMandatory = true,
			}.SetDiscretes(["A"]));

			var settings = new WorkflowOrchestrationSettings();
			settings.AddCapability(new CapabilitySetting(mandatoryCapability));

			var calculator = ConfigurationStateCalculator.ForSettings(planApi, liveApi, settings);

			Assert.IsTrue(calculator.HasMissingMandatoryCapabilityValues(settings));
			Assert.IsTrue(calculator.HasMissingMandatoryValues(settings));
		}

		[TestMethod]
		public void HasMissingMandatoryCapabilityValues_NullSettings_ThrowsArgumentNullException()
		{
			var (planApi, liveApi) = CreateApis();
			var calculator = ConfigurationStateCalculator.ForSettings(planApi, liveApi, new WorkflowOrchestrationSettings());

			Assert.ThrowsException<ArgumentNullException>(() => calculator.HasMissingMandatoryCapabilityValues(null));
		}

		[TestMethod]
		public void HasMissingMandatoryCapabilityValues_MandatoryCapabilityWithReference_ReturnsFalse()
		{
			var (planApi, liveApi) = CreateApis();
			var mandatoryCapability = planApi.Capabilities.Create(new Capability
			{
				Name = $"{Guid.NewGuid()}_MandatoryCapability",
				IsMandatory = true,
			}.SetDiscretes(["A"]));

			var settings = new WorkflowOrchestrationSettings();
			settings.AddCapability(new CapabilitySetting(mandatoryCapability)
			{
				Reference = new ResourcePropertyReference(Guid.NewGuid()),
			});

			var calculator = ConfigurationStateCalculator.ForSettings(planApi, liveApi, settings);

			Assert.IsFalse(calculator.HasMissingMandatoryCapabilityValues(settings));
		}

		[TestMethod]
		public void HasMissingMandatoryCapacityValues_MandatoryCapacityWithoutValue_ReturnsTrue()
		{
			var (planApi, liveApi) = CreateApis();
			var mandatoryCapacity = (NumberCapacity)planApi.Capacities.Create(new NumberCapacity
			{
				Name = $"{Guid.NewGuid()}_MandatoryCapacity",
				IsMandatory = true,
			});

			var settings = new WorkflowOrchestrationSettings();
			settings.AddCapacity(new NumberCapacitySetting(mandatoryCapacity));

			var calculator = ConfigurationStateCalculator.ForSettings(planApi, liveApi, settings);

			Assert.IsTrue(calculator.HasMissingMandatoryCapacityValues(settings));
			Assert.IsTrue(calculator.HasMissingMandatoryValues(settings));
		}

		[TestMethod]
		public void HasMissingMandatoryCapacityValues_NullSettings_ThrowsArgumentNullException()
		{
			var (planApi, liveApi) = CreateApis();
			var calculator = ConfigurationStateCalculator.ForSettings(planApi, liveApi, new WorkflowOrchestrationSettings());

			Assert.ThrowsException<ArgumentNullException>(() => calculator.HasMissingMandatoryCapacityValues(null));
		}

		[TestMethod]
		public void HasMissingMandatoryCapacityValues_MandatoryCapacityWithValue_ReturnsFalse()
		{
			var (planApi, liveApi) = CreateApis();
			var mandatoryCapacity = (NumberCapacity)planApi.Capacities.Create(new NumberCapacity
			{
				Name = $"{Guid.NewGuid()}_MandatoryCapacity",
				IsMandatory = true,
			});

			var settings = new WorkflowOrchestrationSettings();
			settings.AddCapacity(new NumberCapacitySetting(mandatoryCapacity)
			{
				Value = 42,
			});

			var calculator = ConfigurationStateCalculator.ForSettings(planApi, liveApi, settings);

			Assert.IsFalse(calculator.HasMissingMandatoryCapacityValues(settings));
		}

		[TestMethod]
		public void ConfigurationStateCalculator_HasMissingMandatoryValues_DynamicScriptWithoutRequiredInput_ReturnsTrue()
		{
			var settings = CreateDynamicScriptSettings(new Dictionary<string, OrchestrationInputValue>
			{
				["Number of destinations"] = 2,
				["Destination 1/Endpoint"] = "ENC-A",
			});

			var (planApi, liveApi) = CreateApisWithDynamicScript();
			var calculator = ConfigurationStateCalculator.ForSettings(planApi, liveApi, settings);

			Assert.IsTrue(calculator.HasMissingMandatoryValues(settings));
		}

		[TestMethod]
		public void ConfigurationStateCalculator_HasMissingMandatoryValues_DynamicScriptWithAllRequiredInputs_ReturnsFalse()
		{
			var settings = CreateDynamicScriptSettings(new Dictionary<string, OrchestrationInputValue>
			{
				["Number of destinations"] = 2,
				["Destination 1/Endpoint"] = "ENC-A",
				["Destination 2/Endpoint"] = "ENC-B",
			});

			var (planApi, liveApi) = CreateApisWithDynamicScript();
			var calculator = ConfigurationStateCalculator.ForSettings(planApi, liveApi, settings);

			Assert.IsFalse(calculator.HasMissingMandatoryValues(settings));
		}

		[TestMethod]
		public void ConfigurationStateCalculator_HasMissingMandatoryValues_DynamicScriptWithReferencedRequiredInput_ReturnsFalse()
		{
			var settings = CreateDynamicScriptSettings(new Dictionary<string, OrchestrationInputValue>
			{
				["Number of destinations"] = 2,
				["Destination 1/Endpoint"] = "ENC-A",
			});

			settings.OrchestrationEvents.Single().ExecutionDetails.AddDynamicInput(new DynamicInputSetting("Destination 2/Endpoint") { Reference = new JobNameReference() });

			var (planApi, liveApi) = CreateApisWithDynamicScript();
			var calculator = ConfigurationStateCalculator.ForSettings(planApi, liveApi, settings);

			Assert.IsFalse(calculator.HasMissingMandatoryValues(settings));
		}

		private static WorkflowOrchestrationSettings CreateDynamicScriptSettings(Dictionary<string, OrchestrationInputValue> inputValues)
		{
			var executionDetails = new ScriptExecutionDetails(DynamicScriptName)
				.SetDynamicInputs(inputValues.Select(x => new DynamicInputSetting(x.Key) { Value = x.Value }));

			var settings = new WorkflowOrchestrationSettings();
			settings.SetOrchestrationEvents(new List<OrchestrationEvent>
			{
				new OrchestrationEvent
				{
					EventType = OrchestrationEventType.PrerollStart,
					ExecutionDetails = executionDetails,
				},
			});

			return settings;
		}

		private static (IMediaOpsPlanApi PlanApi, IMediaOpsLiveApi LiveApi) CreateApisWithDynamicScript()
		{
			var dms = MediaOpsPlanSimulation.Create(installMediaOpsLive: true);
			dms.AddDynamicOrchestrationScript(DynamicScriptName, providedValues =>
			{
				var builder = new OrchestrationInputBuilder()
					.AddNumber("Number of destinations", field =>
					{
						field.DefaultValue = 1;
						field.TriggersReevaluation = true;
					});

				for (var index = 1; index <= providedValues.GetInt32("Number of destinations", 1, 8); index++)
				{
					builder.AddGroup($"Destination {index}", destination => destination.AddText("Endpoint", field => field.IsRequired = true));
				}

				return builder.Build();
			});

			var connection = dms.CreateConnection();

			return (connection.GetMediaOpsPlanApi(), connection.GetMediaOpsLiveApi());
		}

		private static (IMediaOpsPlanApi PlanApi, IMediaOpsLiveApi LiveApi) CreateApis()
		{
			var dms = MediaOpsPlanSimulation.Create();
			var connection = dms.CreateConnection();

			return (connection.GetMediaOpsPlanApi(), connection.GetMediaOpsLiveApi());
		}
	}
}
