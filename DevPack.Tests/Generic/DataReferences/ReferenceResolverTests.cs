namespace RT_MediaOps.Plan.Generic.DataReferences
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	/// <summary>
	/// Tests how <see cref="JobReferenceResolver"/> and <see cref="WorkflowReferenceResolver"/> turn a
	/// <see cref="DataReference"/> into an actual value. Chains and cycles are covered by
	/// <see cref="ReferenceResolverChainTests"/>.
	/// </summary>
	[TestClass]
	public sealed class ReferenceResolverTests
	{
		private static string ResolveString(ReferenceResolver resolver, DataReference reference)
		{
			var resolved = resolver.ResolveValue(reference);

			Assert.IsTrue(resolved.IsResolved, $"Expected '{reference}' to resolve.");
			Assert.IsInstanceOfType(resolved, typeof(StringResolvedValue), $"Expected '{reference}' to resolve to a string.");

			return ((StringResolvedValue)resolved).Value;
		}

		private static string ResolveString(ReferenceResolver resolver, DataReference reference, string currentNodeId)
		{
			var resolved = resolver.ResolveValue(reference, currentNodeId);

			Assert.IsTrue(resolved.IsResolved, $"Expected '{reference}' to resolve on node '{currentNodeId}'.");

			return ((StringResolvedValue)resolved).Value;
		}

		private static decimal ResolveDecimal(ReferenceResolver resolver, DataReference reference)
		{
			var resolved = resolver.ResolveValue(reference);

			Assert.IsTrue(resolved.IsResolved, $"Expected '{reference}' to resolve.");
			Assert.IsInstanceOfType(resolved, typeof(DecimalResolvedValue), $"Expected '{reference}' to resolve to a number.");

			return ((DecimalResolvedValue)resolved).Value;
		}

		private static void AssertUnresolved(ReferenceResolver resolver, DataReference reference)
		{
			var resolved = resolver.ResolveValue(reference);

			Assert.IsFalse(resolved.IsResolved, $"Expected '{reference}' to stay unresolved.");
			Assert.AreEqual(reference, resolved.UnresolvedReference);
			Assert.IsFalse(resolver.CanResolve(reference));
		}

		#region Constructor guards

		[TestMethod]
		public void ReferenceResolverTests_Constructors_NullArguments_Throw()
		{
			var context = ReferenceTestContext.Create();

			Assert.ThrowsException<ArgumentNullException>(() => new ReferenceResolver(null));
			Assert.ThrowsException<ArgumentNullException>(() => new JobReferenceResolver(null, new Job { Name = "Job" }));
			Assert.ThrowsException<ArgumentNullException>(() => new JobReferenceResolver(context.Api, null));
			Assert.ThrowsException<ArgumentNullException>(() => new WorkflowReferenceResolver(null, new Workflow { Name = "Workflow" }));
			Assert.ThrowsException<ArgumentNullException>(() => new WorkflowReferenceResolver(context.Api, null));
		}

		[TestMethod]
		public void ReferenceResolverTests_NullReference_Throws()
		{
			var context = ReferenceTestContext.Create();
			var resolver = new JobReferenceResolver(context.Api, new Job { Name = "Job" });

			Assert.ThrowsException<ArgumentNullException>(() => resolver.ResolveValue(null));
			Assert.ThrowsException<ArgumentNullException>(() => resolver.CanResolve(null));
			Assert.ThrowsException<ArgumentNullException>(() => resolver.GetDisplayLabel(null));
		}

		#endregion

		#region Orchestration setting values

		[TestMethod]
		public void ReferenceResolverTests_CapabilityReference_ResolvesJobLevelAndNodeLevelValues()
		{
			var context = ReferenceTestContext.Create();
			var capability = context.CreateCapability();
			var pool = context.CreatePool();

			var job = new Job { Name = context.Name("Job") };
			job.OrchestrationSettings.AddCapability(new CapabilitySetting(capability) { Value = "Value 1" });

			var node = new JobResourcePoolNode(pool);
			node.OrchestrationSettings.AddCapability(new CapabilitySetting(capability) { Value = "Value 2" });
			job.NodeGraph.Add(node);

			var resolver = new JobReferenceResolver(context.Api, job);

			Assert.AreEqual("Value 1", ResolveString(resolver, new CapabilityParameterReference(capability.Id)));
			Assert.AreEqual("Value 2", ResolveString(resolver, new CapabilityParameterReference(capability.Id, node.Id)));
		}

		[TestMethod]
		public void ReferenceResolverTests_CapacityReferences_ResolveNumberValueAndRangeMaximum()
		{
			var context = ReferenceTestContext.Create();
			var numberCapacity = context.CreateNumberCapacity();
			var rangeCapacity = context.CreateRangeCapacity();

			var job = new Job { Name = context.Name("Job") };
			job.OrchestrationSettings
				.AddCapacity(new NumberCapacitySetting(numberCapacity) { Value = 20 })
				.AddCapacity(new RangeCapacitySetting(rangeCapacity) { MinValue = 1, MaxValue = 9 });

			var resolver = new JobReferenceResolver(context.Api, job);

			Assert.AreEqual(20m, ResolveDecimal(resolver, new CapacityParameterReference(numberCapacity.Id)));
			Assert.AreEqual(9m, ResolveDecimal(resolver, new CapacityParameterReference(rangeCapacity.Id)));
		}

		[TestMethod]
		public void ReferenceResolverTests_ConfigurationReferences_ResolveEveryConfigurationFlavor()
		{
			var context = ReferenceTestContext.Create();
			var textConfiguration = context.CreateTextConfiguration();
			var numberConfiguration = context.CreateNumberConfiguration();
			var discreteTextConfiguration = context.CreateDiscreteTextConfiguration();
			var discreteNumberConfiguration = context.CreateDiscreteNumberConfiguration();

			var job = new Job { Name = context.Name("Job") };
			job.OrchestrationSettings
				.AddConfiguration(new TextConfigurationSetting(textConfiguration) { Value = "Text" })
				.AddConfiguration(new NumberConfigurationSetting(numberConfiguration) { Value = 3 })
				.AddConfiguration(new DiscreteTextConfigurationSetting(discreteTextConfiguration) { Value = discreteTextConfiguration.Discretes.First() })
				.AddConfiguration(new DiscreteNumberConfigurationSetting(discreteNumberConfiguration) { Value = discreteNumberConfiguration.Discretes.First() });

			var resolver = new JobReferenceResolver(context.Api, job);

			Assert.AreEqual("Text", ResolveString(resolver, new ConfigurationParameterReference(textConfiguration.Id)));
			Assert.AreEqual(3m, ResolveDecimal(resolver, new ConfigurationParameterReference(numberConfiguration.Id)));
			Assert.AreEqual("A", ResolveString(resolver, new ConfigurationParameterReference(discreteTextConfiguration.Id)));
			Assert.AreEqual(7m, ResolveDecimal(resolver, new ConfigurationParameterReference(discreteNumberConfiguration.Id)));
		}

		[TestMethod]
		public void ReferenceResolverTests_SettingWithoutValueOrReference_StaysUnresolved()
		{
			var context = ReferenceTestContext.Create();
			var capability = context.CreateCapability();

			var job = new Job { Name = context.Name("Job") };
			job.OrchestrationSettings.AddCapability(new CapabilitySetting(capability));

			AssertUnresolved(new JobReferenceResolver(context.Api, job), new CapabilityParameterReference(capability.Id));
		}

		[TestMethod]
		public void ReferenceResolverTests_SettingThatIsNotConfigured_StaysUnresolved()
		{
			var context = ReferenceTestContext.Create();

			var job = new Job { Name = context.Name("Job") };

			AssertUnresolved(new JobReferenceResolver(context.Api, job), new CapabilityParameterReference(Guid.NewGuid()));
		}

		#endregion

		#region Resource references

		[TestMethod]
		public void ReferenceResolverTests_ResourceReferences_ScopedToANode_ResolveAgainstThatNodesResource()
		{
			var context = ReferenceTestContext.Create();
			var resourceProperty = context.CreateResourceProperty();
			var pool = context.CreatePool();
			var resource = context.CreateResource(pool, new ResourcePropertySettings(resourceProperty.Id) { Value = "Property value" });

			var job = new Job { Name = context.Name("Job") };
			var node = new JobResourceNode(pool, resource);
			job.NodeGraph.Add(node);

			var resolver = new JobReferenceResolver(context.Api, job);

			Assert.AreEqual(resource.Name, ResolveString(resolver, new ResourceNameReference(node.Id)));
			Assert.AreEqual("Property value", ResolveString(resolver, new ResourcePropertyReference(resourceProperty.Id, node.Id)));

			// An unmanaged resource is not linked to an element or service, so its linked object id is empty.
			Assert.AreEqual(String.Empty, ResolveString(resolver, new ResourceLinkedObjectIdReference(node.Id)));
		}

		[TestMethod]
		public void ReferenceResolverTests_ResourceReferences_WithoutNodeId_StayUnresolved()
		{
			var context = ReferenceTestContext.Create();
			var pool = context.CreatePool();
			var resource = context.CreateResource(pool);

			var job = new Job { Name = context.Name("Job") };
			job.NodeGraph.Add(new JobResourceNode(pool, resource));

			var resolver = new JobReferenceResolver(context.Api, job);

			AssertUnresolved(resolver, new ResourceNameReference());
			AssertUnresolved(resolver, new ResourceLinkedObjectIdReference());
			AssertUnresolved(resolver, new ResourcePropertyReference(Guid.NewGuid()));
		}

		[TestMethod]
		public void ReferenceResolverTests_ResourceReferences_PointingAtAnUnknownNode_StayUnresolved()
		{
			var context = ReferenceTestContext.Create();
			var pool = context.CreatePool();
			var resource = context.CreateResource(pool);

			var job = new Job { Name = context.Name("Job") };
			job.NodeGraph.Add(new JobResourceNode(pool, resource));

			AssertUnresolved(new JobReferenceResolver(context.Api, job), new ResourceNameReference("does-not-exist"));
		}

		[TestMethod]
		public void ReferenceResolverTests_ResourceReferences_PointingAtAPoolNode_StayUnresolved()
		{
			var context = ReferenceTestContext.Create();
			var pool = context.CreatePool();

			var job = new Job { Name = context.Name("Job") };
			var poolNode = new JobResourcePoolNode(pool);
			job.NodeGraph.Add(poolNode);

			AssertUnresolved(new JobReferenceResolver(context.Api, job), new ResourceNameReference(poolNode.Id));
		}

		/// <summary>A resource reference has no meaning outside a node, so it falls back to the node that owns it.</summary>
		[TestMethod]
		public void ReferenceResolverTests_ResourceReferences_WithoutNodeId_ResolveAgainstTheOwningNode()
		{
			var context = ReferenceTestContext.Create();
			var resourceProperty = context.CreateResourceProperty();
			var pool = context.CreatePool();
			var resource = context.CreateResource(pool, new ResourcePropertySettings(resourceProperty.Id) { Value = "Property value" });

			var job = new Job { Name = context.Name("Job") };
			var node = new JobResourceNode(pool, resource);
			job.NodeGraph.Add(node);

			var resolver = new JobReferenceResolver(context.Api, job);

			Assert.AreEqual(resource.Name, ResolveString(resolver, new ResourceNameReference(), node.Id));
			Assert.AreEqual("Property value", ResolveString(resolver, new ResourcePropertyReference(resourceProperty.Id), node.Id));
			Assert.AreEqual(String.Empty, ResolveString(resolver, new ResourceLinkedObjectIdReference(), node.Id));
		}

		/// <summary>
		/// The fallback must not apply to the parameter references: without a node they deliberately target the
		/// job level settings, also when they are configured on a node.
		/// </summary>
		[TestMethod]
		public void ReferenceResolverTests_ParameterReferences_WithoutNodeId_KeepTargetingTheJobLevel()
		{
			var context = ReferenceTestContext.Create();
			var capability = context.CreateCapability();
			var pool = context.CreatePool();

			var job = new Job { Name = context.Name("Job") };
			job.OrchestrationSettings.AddCapability(new CapabilitySetting(capability) { Value = "job-level" });

			var node = new JobResourcePoolNode(pool);
			node.OrchestrationSettings.AddCapability(new CapabilitySetting(capability) { Value = "node-level" });
			job.NodeGraph.Add(node);

			var resolver = new JobReferenceResolver(context.Api, job);

			Assert.AreEqual("job-level", ResolveString(resolver, new CapabilityParameterReference(capability.Id), node.Id));
		}

		[TestMethod]
		public void ReferenceResolverTests_CurrentNode_OnlyAppliesToResourceReferencesWithoutANode()
		{
			var context = ReferenceTestContext.Create();
			var pool = context.CreatePool();
			var resource = context.CreateResource(pool);

			var job = new Job { Name = context.Name("Job") };
			var firstNode = new JobResourceNode(pool, resource);
			var poolNode = new JobResourcePoolNode(pool);
			job.NodeGraph.Add(firstNode).Add(poolNode);

			var resolver = new JobReferenceResolver(context.Api, job);

			// A reference that names its own node keeps targeting that node.
			Assert.AreEqual(resource.Name, ResolveString(resolver, new ResourceNameReference(firstNode.Id), poolNode.Id));

			// Without a node it targets the current node, which is a pool node and has no resource.
			Assert.IsFalse(resolver.CanResolve(new ResourceNameReference(), poolNode.Id));
		}

		[TestMethod]
		public void ReferenceResolverTests_ResourcePropertyReference_PropertyNotSetOnTheResource_StaysUnresolved()
		{
			var context = ReferenceTestContext.Create();
			var resourceProperty = context.CreateResourceProperty();
			var pool = context.CreatePool();
			var resource = context.CreateResource(pool);

			var job = new Job { Name = context.Name("Job") };
			var node = new JobResourceNode(pool, resource);
			job.NodeGraph.Add(node);

			AssertUnresolved(new JobReferenceResolver(context.Api, job), new ResourcePropertyReference(resourceProperty.Id, node.Id));
		}

		#endregion

		#region Job references

		[TestMethod]
		public void ReferenceResolverTests_JobNameReference_ResolvesToTheJobName()
		{
			var context = ReferenceTestContext.Create();
			var pool = context.CreatePool();

			var job = new Job { Name = context.Name("Job") };
			var node = new JobResourcePoolNode(pool);
			job.NodeGraph.Add(node);

			var resolver = new JobReferenceResolver(context.Api, job);

			Assert.AreEqual(job.Name, ResolveString(resolver, new JobNameReference()));
			Assert.AreEqual(job.Name, ResolveString(resolver, new JobNameReference(node.Id)));
		}

		[TestMethod]
		public void ReferenceResolverTests_JobPropertyReference_ResolvesToThePersistedPropertyValue()
		{
			var context = ReferenceTestContext.Create();
			var jobProperty = context.CreateJobProperty();
			var pool = context.CreatePool();
			var resource = context.CreateResource(pool);

			var start = ReferenceTestContext.ScheduleStart;
			var job = new Job
			{
				Name = context.Name("Job"),
				Start = start,
				End = start.AddHours(1),
				PreRollStart = start,
				PostRollEnd = start.AddHours(1),
			};
			job.NodeGraph.Add(new JobResourceNode(pool, resource));
			job.AddProperty(new StringPropertySetting(jobProperty) { Value = "Property value" });

			job = context.Api.Jobs.Create(job);

			var resolver = new JobReferenceResolver(context.Api, job);

			Assert.AreEqual("Property value", ResolveString(resolver, new JobPropertyReference(jobProperty.Id)));
		}

		[TestMethod]
		public void ReferenceResolverTests_JobPropertyReference_WithoutAPersistedValue_StaysUnresolved()
		{
			var context = ReferenceTestContext.Create();
			var jobProperty = context.CreateJobProperty();

			var job = new Job(Guid.NewGuid()) { Name = context.Name("Job") };

			AssertUnresolved(new JobReferenceResolver(context.Api, job), new JobPropertyReference(jobProperty.Id));
		}

		/// <summary>
		/// A workflow has no job yet, so the job-scoped references are only resolvable once a job exists.
		/// <see cref="ReferenceMigrationTests"/> covers that transition.
		/// </summary>
		[TestMethod]
		public void ReferenceResolverTests_JobScopedReferences_OnAWorkflow_StayUnresolved()
		{
			var context = ReferenceTestContext.Create();

			var workflow = new Workflow { Name = context.Name("Workflow") };
			var resolver = new WorkflowReferenceResolver(context.Api, workflow);

			AssertUnresolved(resolver, new JobNameReference());
			AssertUnresolved(resolver, new JobPropertyReference(Guid.NewGuid()));
		}

		#endregion

		#region Workflow resolver

		[TestMethod]
		public void ReferenceResolverTests_WorkflowResolver_ResolvesResourceAndOrchestrationReferences()
		{
			var context = ReferenceTestContext.Create();
			var capability = context.CreateCapability();
			var resourceProperty = context.CreateResourceProperty();
			var pool = context.CreatePool();
			var resource = context.CreateResource(pool, new ResourcePropertySettings(resourceProperty.Id) { Value = "Property value" });

			var workflow = new Workflow { Name = context.Name("Workflow") };
			workflow.OrchestrationSettings.AddCapability(new CapabilitySetting(capability) { Value = "Value 1" });

			var node = new WorkflowResourceNode(pool, resource);
			node.OrchestrationSettings.AddCapability(new CapabilitySetting(capability) { Value = "Value 2" });
			workflow.NodeGraph.Add(node);

			var resolver = new WorkflowReferenceResolver(context.Api, workflow);

			Assert.AreEqual("Value 1", ResolveString(resolver, new CapabilityParameterReference(capability.Id)));
			Assert.AreEqual("Value 2", ResolveString(resolver, new CapabilityParameterReference(capability.Id, node.Id)));
			Assert.AreEqual(resource.Name, ResolveString(resolver, new ResourceNameReference(node.Id)));
			Assert.AreEqual("Property value", ResolveString(resolver, new ResourcePropertyReference(resourceProperty.Id, node.Id)));
		}

		#endregion

		#region Display labels

		[TestMethod]
		public void ReferenceResolverTests_GetDisplayLabel_DescribesTheReferencedDefinition()
		{
			var context = ReferenceTestContext.Create();
			var capability = context.CreateCapability();
			var numberCapacity = context.CreateNumberCapacity();
			var textConfiguration = context.CreateTextConfiguration();
			var resourceProperty = context.CreateResourceProperty();
			var jobProperty = context.CreateJobProperty();

			var resolver = new JobReferenceResolver(context.Api, new Job { Name = context.Name("Job") });

			Assert.AreEqual("Resource Name", resolver.GetDisplayLabel(new ResourceNameReference()));
			Assert.AreEqual("Resource Linked Object ID", resolver.GetDisplayLabel(new ResourceLinkedObjectIdReference()));
			Assert.AreEqual("Job Name", resolver.GetDisplayLabel(new JobNameReference()));
			Assert.AreEqual($"Resource Property: {resourceProperty.Name}", resolver.GetDisplayLabel(new ResourcePropertyReference(resourceProperty.Id)));
			Assert.AreEqual($"Capability: {capability.Name}", resolver.GetDisplayLabel(new CapabilityParameterReference(capability.Id)));
			Assert.AreEqual($"Capacity: {numberCapacity.Name}", resolver.GetDisplayLabel(new CapacityParameterReference(numberCapacity.Id)));
			Assert.AreEqual($"Configuration: {textConfiguration.Name}", resolver.GetDisplayLabel(new ConfigurationParameterReference(textConfiguration.Id)));
			Assert.AreEqual($"Job Property: {jobProperty.Name}", resolver.GetDisplayLabel(new JobPropertyReference(jobProperty.Id)));
		}

		[TestMethod]
		public void ReferenceResolverTests_GetDisplayLabel_UnknownDefinition_FallsBackToTheTypeDescription()
		{
			var context = ReferenceTestContext.Create();
			var resolver = new JobReferenceResolver(context.Api, new Job { Name = context.Name("Job") });

			Assert.AreEqual("Capability", resolver.GetDisplayLabel(new CapabilityParameterReference(Guid.NewGuid())));
		}

		#endregion
	}
}
