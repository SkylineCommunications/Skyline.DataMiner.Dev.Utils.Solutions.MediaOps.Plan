namespace RT_MediaOps.Plan.Workflow
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Extensions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Simulation;

	[TestClass]
	public sealed class ConfigurationStateCalculatorTests
	{
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

		private static (IMediaOpsPlanApi PlanApi, IMediaOpsLiveApi LiveApi) CreateApis()
		{
			var dms = MediaOpsPlanSimulation.Create();
			var connection = dms.CreateConnection();

			return (connection.GetMediaOpsPlanApi(), connection.GetMediaOpsLiveApi());
		}
	}
}
