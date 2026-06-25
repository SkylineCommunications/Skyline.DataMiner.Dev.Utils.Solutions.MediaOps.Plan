namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Simulation;

	[TestClass]
	public sealed class ReferenceResolverChainTests
	{
		private static IMediaOpsPlanApi CreateApi()
		{
			var dms = MediaOpsPlanSimulation.Create();
			return dms.CreateConnection().GetMediaOpsPlanApi();
		}

		[TestMethod]
		public void ResolveValue_FollowsChainAcrossMultipleReferences()
		{
			// Capability A references capability B, capability B references capability C and capability C holds the value.
			var capabilityAId = Guid.NewGuid();
			var capabilityBId = Guid.NewGuid();
			var capabilityCId = Guid.NewGuid();

			var job = new Job { Name = "Chain job" };
			job.OrchestrationSettings.AddCapability(new CapabilitySetting(capabilityAId) { Reference = new CapabilityParameterReference(capabilityBId) });
			job.OrchestrationSettings.AddCapability(new CapabilitySetting(capabilityBId) { Reference = new CapabilityParameterReference(capabilityCId) });
			job.OrchestrationSettings.AddCapability(new CapabilitySetting(capabilityCId) { Value = "final-value" });

			var resolver = new JobReferenceResolver(CreateApi(), job);

			var resolved = resolver.ResolveValue(new CapabilityParameterReference(capabilityAId));

			Assert.IsTrue(resolved.IsResolved);
			Assert.AreEqual("final-value", (resolved as StringResolvedValue)?.Value);
		}

		[TestMethod]
		public void ResolveValue_CircularChain_Throws()
		{
			// Capability A references capability B and capability B references capability A.
			var capabilityAId = Guid.NewGuid();
			var capabilityBId = Guid.NewGuid();

			var job = new Job { Name = "Cyclic job" };
			job.OrchestrationSettings.AddCapability(new CapabilitySetting(capabilityAId) { Reference = new CapabilityParameterReference(capabilityBId) });
			job.OrchestrationSettings.AddCapability(new CapabilitySetting(capabilityBId) { Reference = new CapabilityParameterReference(capabilityAId) });

			var resolver = new JobReferenceResolver(CreateApi(), job);

			Assert.ThrowsException<CircularReferenceException>(() => resolver.ResolveValue(new CapabilityParameterReference(capabilityAId)));
		}
	}
}
