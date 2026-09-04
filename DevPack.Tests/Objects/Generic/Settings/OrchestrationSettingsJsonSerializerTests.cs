namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API.Tests.Objects.Generic.Settings
{
	using System;
	using System.Linq;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class OrchestrationSettingsJsonSerializerTests
	{
		[TestMethod]
		public void SerializeAndDeserialize_WithSettingsAndScriptDetails_RoundTripsAllValues()
		{
			var settings = new WorkflowOrchestrationSettings()
				.AddCapability(new CapabilitySetting(Guid.NewGuid()) { Value = "Capability", Reference = new ResourceNameReference("node-a") })
				.AddCapacity(new NumberCapacitySetting(Guid.NewGuid()) { Value = 10m, Reference = new CapacityParameterReference(Guid.NewGuid(), "node-b") })
				.AddCapacity(new RangeCapacitySetting(Guid.NewGuid()) { MinValue = 1m, MaxValue = 2m })
				.AddConfiguration(new TextConfigurationSetting(Guid.NewGuid()) { Value = "Text" })
				.AddConfiguration(new NumberConfigurationSetting(Guid.NewGuid()) { Value = 3m })
				.AddConfiguration(new DiscreteTextConfigurationSetting(Guid.NewGuid()) { Value = new TextDiscreet("value", "Display") })
				.AddConfiguration(new DiscreteNumberConfigurationSetting(Guid.NewGuid()) { Value = new NumberDiscreet(4m, "Four"), Reference = new JobPropertyReference(Guid.NewGuid(), "node-c") });

			var executionDetails = new ScriptExecutionDetails("Script")
				.AddScriptElement(new ScriptElementSetting("Dummy") { Reference = new ResourcePropertyReference(Guid.NewGuid(), "node-d") })
				.AddScriptParameter(new ScriptParameterSetting("Parameter") { Value = "Input" })
				.AddCapability(new CapabilitySetting(Guid.NewGuid()) { Reference = new CapabilityParameterReference(Guid.NewGuid(), "node-e") })
				.AddCapacity(new NumberCapacitySetting(Guid.NewGuid()) { Value = 5m })
				.AddConfiguration(new TextConfigurationSetting(Guid.NewGuid()) { Reference = new ConfigurationParameterReference(Guid.NewGuid(), "node-f") });

			settings.AddOrchestrationEvent(new OrchestrationEvent
			{
				EventType = OrchestrationEventType.PrerollStart,
				Metadata = "{\"source\":\"test\"}",
				ExecutionDetails = executionDetails,
			});

			var result = OrchestrationSettingsJsonSerializer.Deserialize(OrchestrationSettingsJsonSerializer.Serialize(settings));

			Assert.IsNotNull(result);
			Assert.AreEqual(1, result.Capabilities.Count);
			Assert.AreEqual(2, result.Capacities.Count);
			Assert.AreEqual(4, result.Configurations.Count);
			Assert.AreEqual(1, result.OrchestrationEvents.Count);
			Assert.IsInstanceOfType(result.Capacities.Single(x => x is NumberCapacitySetting), typeof(NumberCapacitySetting));
			Assert.IsInstanceOfType(result.Capacities.Single(x => x is RangeCapacitySetting), typeof(RangeCapacitySetting));
			Assert.IsInstanceOfType(result.Configurations.Single(x => x is DiscreteTextConfigurationSetting), typeof(DiscreteTextConfigurationSetting));
			Assert.IsInstanceOfType(result.Configurations.Single(x => x is DiscreteNumberConfigurationSetting), typeof(DiscreteNumberConfigurationSetting));

			var resultEvent = result.OrchestrationEvents.Single();
			Assert.AreEqual(OrchestrationEventType.PrerollStart, resultEvent.EventType);
			Assert.AreEqual("{\"source\":\"test\"}", resultEvent.Metadata);
			Assert.AreEqual("Script", resultEvent.ExecutionDetails.ScriptName);
			Assert.IsInstanceOfType(resultEvent.ExecutionDetails.ScriptElements.Single().Reference, typeof(ResourcePropertyReference));
			Assert.IsInstanceOfType(resultEvent.ExecutionDetails.Capabilities.Single().Reference, typeof(CapabilityParameterReference));
			Assert.IsInstanceOfType(resultEvent.ExecutionDetails.Configurations.Single().Reference, typeof(ConfigurationParameterReference));
		}

		[TestMethod]
		public void Deserialize_WithEmptyJson_ReturnsNull()
		{
			Assert.IsNull(OrchestrationSettingsJsonSerializer.Deserialize(null));
			Assert.IsNull(OrchestrationSettingsJsonSerializer.Deserialize("  "));
		}

	}
}
