namespace RT_MediaOps.Plan.Generic.DataReferences
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	public sealed class OrchestrationSettingsReferencesTests
	{
		[TestMethod]
		public void OrchestrationSettings_GetReferences_ReturnsReferencesOfSettingsAndEvents()
		{
			var capabilityReference = new CapabilityParameterReference(Guid.NewGuid());
			var parameterReference = new ResourceNameReference();
			var eventCapabilityReference = new CapacityParameterReference(Guid.NewGuid(), "node-1");

			var settings = new WorkflowOrchestrationSettings()
				.AddCapability(new CapabilitySetting(Guid.NewGuid()) { Reference = capabilityReference })
				.AddCapability(new CapabilitySetting(Guid.NewGuid()) { Value = "HD" })
				.AddOrchestrationEvent(new OrchestrationEvent
				{
					EventType = OrchestrationEventType.PrerollStart,
					ExecutionDetails = new ScriptExecutionDetails("Script")
						.AddScriptParameter(new ScriptParameterSetting("Parameter") { Reference = parameterReference })
						.AddCapability(new CapabilitySetting(Guid.NewGuid()) { Reference = eventCapabilityReference }),
				})
				.AddOrchestrationEvent(new OrchestrationEvent { EventType = OrchestrationEventType.PostrollStop });

			var references = settings.GetReferences();

			Assert.AreEqual(3, references.Count);
			Assert.IsTrue(references.Contains(capabilityReference));
			Assert.IsTrue(references.Contains(parameterReference));
			Assert.IsTrue(references.Contains(eventCapabilityReference));
		}

		[TestMethod]
		public void OrchestrationSettings_GetReferences_ReturnsSameInstancesAsSettings()
		{
			var settings = new WorkflowOrchestrationSettings()
				.AddCapability(new CapabilitySetting(Guid.NewGuid()) { Reference = new CapabilityParameterReference(Guid.NewGuid()) });

			settings.GetReferences().Single().NodeId = "node-1";

			Assert.AreEqual("node-1", settings.Capabilities.Single().Reference.NodeId);
		}
	}
}
