namespace RT_MediaOps.Plan.Generic.DataReferences
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	/// <summary>
	/// Tests how <see cref="ReferenceResolver.ResolveValue"/> follows a setting that links to another setting,
	/// and how it protects itself against cycles.
	/// </summary>
	[TestClass]
	public sealed class ReferenceResolverChainTests
	{
		[TestMethod]
		public void ReferenceResolverChainTests_AcrossMultipleSettingsOfTheSameType_ResolvesTheFinalValue()
		{
			var context = ReferenceTestContext.Create();

			// Capability A links to capability B, capability B links to capability C and capability C holds the value.
			var capabilityAId = Guid.NewGuid();
			var capabilityBId = Guid.NewGuid();
			var capabilityCId = Guid.NewGuid();

			var job = new Job { Name = "Chain job" };
			job.OrchestrationSettings
				.AddCapability(new CapabilitySetting(capabilityAId) { Reference = new CapabilityParameterReference(capabilityBId) })
				.AddCapability(new CapabilitySetting(capabilityBId) { Reference = new CapabilityParameterReference(capabilityCId) })
				.AddCapability(new CapabilitySetting(capabilityCId) { Value = "final-value" });

			var resolved = new JobReferenceResolver(context.Api, job).ResolveValue(new CapabilityParameterReference(capabilityAId));

			Assert.IsTrue(resolved.IsResolved);
			Assert.AreEqual("final-value", ((StringResolvedValue)resolved).Value);
		}

		[TestMethod]
		public void ReferenceResolverChainTests_AcrossDifferentReferenceTypes_ResolvesTheFinalValue()
		{
			var context = ReferenceTestContext.Create();
			var capability = context.CreateCapability();
			var configuration = context.CreateTextConfiguration();
			var pool = context.CreatePool();
			var resource = context.CreateResource(pool);

			var job = new Job { Name = "Chain job" };
			var node = new JobResourceNode(pool, resource);
			job.NodeGraph.Add(node);

			// Capability -> configuration -> resource name of the node.
			job.OrchestrationSettings
				.AddCapability(new CapabilitySetting(capability) { Reference = new ConfigurationParameterReference(configuration.Id) })
				.AddConfiguration(new TextConfigurationSetting(configuration) { Reference = new ResourceNameReference(node.Id) });

			var resolved = new JobReferenceResolver(context.Api, job).ResolveValue(new CapabilityParameterReference(capability.Id));

			Assert.IsTrue(resolved.IsResolved);
			Assert.AreEqual(resource.Name, ((StringResolvedValue)resolved).Value);
		}

		[TestMethod]
		public void ReferenceResolverChainTests_FromNodeLevelToJobLevel_ResolvesTheFinalValue()
		{
			var context = ReferenceTestContext.Create();
			var capability = context.CreateCapability();
			var pool = context.CreatePool();

			var job = new Job { Name = "Chain job" };
			var node = new JobResourcePoolNode(pool);
			job.NodeGraph.Add(node);

			// The node links to the job-level setting, which is where the value lives.
			node.OrchestrationSettings.AddCapability(new CapabilitySetting(capability) { Reference = new JobNameReference() });
			job.OrchestrationSettings.AddCapability(new CapabilitySetting(capability) { Value = "job-level" });

			var resolver = new JobReferenceResolver(context.Api, job);

			Assert.AreEqual(job.Name, ((StringResolvedValue)resolver.ResolveValue(new CapabilityParameterReference(capability.Id, node.Id))).Value);
			Assert.AreEqual("job-level", ((StringResolvedValue)resolver.ResolveValue(new CapabilityParameterReference(capability.Id))).Value);
		}

		[TestMethod]
		public void ReferenceResolverChainTests_EndingInAnUnknownSetting_ReportsTheDeepestUnresolvedReference()
		{
			var context = ReferenceTestContext.Create();

			var capabilityAId = Guid.NewGuid();
			var missingReference = new CapabilityParameterReference(Guid.NewGuid());

			var job = new Job { Name = "Chain job" };
			job.OrchestrationSettings.AddCapability(new CapabilitySetting(capabilityAId) { Reference = missingReference });

			var resolved = new JobReferenceResolver(context.Api, job).ResolveValue(new CapabilityParameterReference(capabilityAId));

			Assert.IsFalse(resolved.IsResolved);
			Assert.AreEqual(missingReference, resolved.UnresolvedReference);
		}

		[TestMethod]
		public void ReferenceResolverChainTests_SelfReference_Throws()
		{
			var context = ReferenceTestContext.Create();

			var capabilityId = Guid.NewGuid();

			var job = new Job { Name = "Cyclic job" };
			job.OrchestrationSettings.AddCapability(new CapabilitySetting(capabilityId) { Reference = new CapabilityParameterReference(capabilityId) });

			var resolver = new JobReferenceResolver(context.Api, job);
			var reference = new CapabilityParameterReference(capabilityId);

			var exception = Assert.ThrowsException<CircularReferenceException>(() => resolver.ResolveValue(reference));
			Assert.AreEqual(reference, exception.Reference);
		}

		[TestMethod]
		public void ReferenceResolverChainTests_TwoSettingsReferencingEachOther_Throws()
		{
			var context = ReferenceTestContext.Create();

			var capabilityAId = Guid.NewGuid();
			var capabilityBId = Guid.NewGuid();

			var job = new Job { Name = "Cyclic job" };
			job.OrchestrationSettings
				.AddCapability(new CapabilitySetting(capabilityAId) { Reference = new CapabilityParameterReference(capabilityBId) })
				.AddCapability(new CapabilitySetting(capabilityBId) { Reference = new CapabilityParameterReference(capabilityAId) });

			var resolver = new JobReferenceResolver(context.Api, job);

			Assert.ThrowsException<CircularReferenceException>(() => resolver.ResolveValue(new CapabilityParameterReference(capabilityAId)));
		}

		[TestMethod]
		public void ReferenceResolverChainTests_CycleAcrossNodes_Throws()
		{
			var context = ReferenceTestContext.Create();
			var capability = context.CreateCapability();
			var pool = context.CreatePool();

			var job = new Job { Name = "Cyclic job" };
			var firstNode = new JobResourcePoolNode(pool);
			var secondNode = new JobResourcePoolNode(pool);
			job.NodeGraph.Add(firstNode).Add(secondNode);

			firstNode.OrchestrationSettings.AddCapability(new CapabilitySetting(capability) { Reference = new CapabilityParameterReference(capability.Id, secondNode.Id) });
			secondNode.OrchestrationSettings.AddCapability(new CapabilitySetting(capability) { Reference = new CapabilityParameterReference(capability.Id, firstNode.Id) });

			var resolver = new JobReferenceResolver(context.Api, job);

			Assert.ThrowsException<CircularReferenceException>(() => resolver.ResolveValue(new CapabilityParameterReference(capability.Id, firstNode.Id)));
		}

		/// <summary>The owning node travels along the chain, so a linked resource reference stays on that node.</summary>
		[TestMethod]
		public void ReferenceResolverChainTests_OwningNode_IsCarriedAlongTheChain()
		{
			var context = ReferenceTestContext.Create();
			var capability = context.CreateCapability();
			var pool = context.CreatePool();
			var firstResource = context.CreateResource(pool, "FirstResource");
			var secondResource = context.CreateResource(pool, "SecondResource");

			var job = new Job { Name = "Chain job" };
			var firstNode = new JobResourceNode(pool, firstResource);
			var secondNode = new JobResourceNode(pool, secondResource);
			job.NodeGraph.Add(firstNode).Add(secondNode);

			// Both nodes carry the same configured reference; each must resolve against its own resource.
			firstNode.OrchestrationSettings.AddCapability(new CapabilitySetting(capability) { Reference = new ResourceNameReference() });
			secondNode.OrchestrationSettings.AddCapability(new CapabilitySetting(capability) { Reference = new ResourceNameReference() });

			var resolver = new JobReferenceResolver(context.Api, job);

			Assert.AreEqual(firstResource.Name, ((StringResolvedValue)resolver.ResolveValue(new CapabilityParameterReference(capability.Id, firstNode.Id))).Value);
			Assert.AreEqual(secondResource.Name, ((StringResolvedValue)resolver.ResolveValue(new CapabilityParameterReference(capability.Id, secondNode.Id))).Value);
		}

		[TestMethod]
		public void ReferenceResolverChainTests_CanResolve_CircularChain_ReturnsFalseInsteadOfThrowing()
		{
			var context = ReferenceTestContext.Create();

			var capabilityAId = Guid.NewGuid();
			var capabilityBId = Guid.NewGuid();

			var job = new Job { Name = "Cyclic job" };
			job.OrchestrationSettings
				.AddCapability(new CapabilitySetting(capabilityAId) { Reference = new CapabilityParameterReference(capabilityBId) })
				.AddCapability(new CapabilitySetting(capabilityBId) { Reference = new CapabilityParameterReference(capabilityAId) });

			var resolver = new JobReferenceResolver(context.Api, job);

			Assert.IsFalse(resolver.CanResolve(new CapabilityParameterReference(capabilityAId)));
		}
	}
}
