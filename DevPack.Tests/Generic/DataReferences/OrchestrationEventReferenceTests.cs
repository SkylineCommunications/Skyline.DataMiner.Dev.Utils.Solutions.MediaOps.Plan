namespace RT_MediaOps.Plan.Generic.DataReferences
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	/// <summary>
	/// Tests the references configured on the orchestration events of a job.
	/// </summary>
	/// <remarks>
	/// A resource reference only has a meaning within a node, so one that does not target a node of its own falls back
	/// to the node that owns it. At job level there is no such node, so it stays unresolved.
	/// </remarks>
	[TestClass]
	public sealed class OrchestrationEventReferenceTests
	{
		private static Job CreateConfirmedJob(ReferenceTestContext context, out JobResourceNode node)
		{
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

			job = context.Api.Jobs.Confirm(context.Api.Jobs.SaveAsTentative(context.Api.Jobs.Create(job)));
			node = job.NodeGraph.Nodes.OfType<JobResourceNode>().Single();

			return job;
		}

		/// <summary>Mirrors the reported setup: a pre-roll and a post-roll script whose 'Element' input is linked.</summary>
		private static void AddElementLinkedEvents(JobNode node, DataReference reference)
		{
			node.OrchestrationSettings.AddOrchestrationEvent(new OrchestrationEvent
			{
				EventType = OrchestrationEventType.PrerollStart,
				ExecutionDetails = new ScriptExecutionDetails("SpectrumAnalyzerConfigureOS")
					.AddScriptParameter(new ScriptParameterSetting("Element") { Reference = reference })
					.AddScriptParameter(new ScriptParameterSetting("Bandwidth Size") { Value = "6" })
					.AddScriptParameter(new ScriptParameterSetting("Center Frequency") { Value = "14318.0000" }),
			});

			node.OrchestrationSettings.AddOrchestrationEvent(new OrchestrationEvent
			{
				EventType = OrchestrationEventType.PostrollStart,
				ExecutionDetails = new ScriptExecutionDetails("SpectrumAnalyzerDecommissionOS")
					.AddScriptParameter(new ScriptParameterSetting("Element") { Reference = reference }),
			});
		}

		private static DataReference GetElementReference(JobNode node)
		{
			return node.OrchestrationSettings.OrchestrationEvents.First().ExecutionDetails.ScriptParameters
				.Single(parameter => parameter.Name == "Element").Reference;
		}

		[TestMethod]
		public void OrchestrationEventReferenceTests_ResourceReferenceWithoutNodeId_ResolvesAgainstTheOwningNode()
		{
			var context = ReferenceTestContext.Create();
			var pool = context.CreatePool();
			var resource = context.CreateResource(pool);

			var job = new Job { Name = context.Name("Job") };
			var node = new JobResourceNode(pool, resource);
			job.NodeGraph.Add(node);

			AddElementLinkedEvents(node, new ResourceLinkedObjectIdReference());

			var resolver = new JobReferenceResolver(context.Api, job);
			var reference = GetElementReference(node);

			Assert.IsTrue(resolver.CanResolve(reference, node.Id));

			// Without a current node the reference targets the job, which has no resource of its own.
			Assert.IsFalse(resolver.CanResolve(reference));
		}

		[TestMethod]
		public void OrchestrationEventReferenceTests_ResourceReferenceWithNodeId_ResolvesAgainstThatNode()
		{
			var context = ReferenceTestContext.Create();
			var pool = context.CreatePool();
			var resource = context.CreateResource(pool);

			var job = new Job { Name = context.Name("Job") };
			var node = new JobResourceNode(pool, resource);
			job.NodeGraph.Add(node);

			AddElementLinkedEvents(node, new ResourceLinkedObjectIdReference(node.Id));

			Assert.IsTrue(new JobReferenceResolver(context.Api, job).CanResolve(GetElementReference(node)));
		}

		[TestMethod]
		public void OrchestrationEventReferenceTests_ResourceReferenceOnAnotherNode_ResolvesAgainstThatNode()
		{
			var context = ReferenceTestContext.Create();
			var pool = context.CreatePool();
			var resource = context.CreateResource(pool);

			var job = new Job { Name = context.Name("Job") };
			var resourceNode = new JobResourceNode(pool, resource);
			var poolNode = new JobResourcePoolNode(pool);
			job.NodeGraph.Add(resourceNode).Add(poolNode);

			AddElementLinkedEvents(poolNode, new ResourceNameReference(resourceNode.Id));

			var resolved = new JobReferenceResolver(context.Api, job).ResolveValue(GetElementReference(poolNode));

			Assert.AreEqual(resource.Name, ((StringResolvedValue)resolved).Value);
		}

		/// <summary>The reported scenario: a node-level script parameter linked without an explicit node id.</summary>
		[TestMethod]
		public void OrchestrationEventReferenceTests_ResourceReferenceWithoutNodeId_IsAcceptedOnAConfirmedJob()
		{
			var context = ReferenceTestContext.Create();
			var job = CreateConfirmedJob(context, out var node);

			AddElementLinkedEvents(node, new ResourceLinkedObjectIdReference());

			var updatedJob = context.Api.Jobs.Update(job);

			Assert.AreEqual(JobState.Confirmed, updatedJob.State);
		}

		/// <summary>The fallback picks the owning node, but the value still has to exist on that node's resource.</summary>
		[TestMethod]
		public void OrchestrationEventReferenceTests_ReferenceThatTheOwningNodeCannotProvide_IsReportedOnAConfirmedJob()
		{
			var context = ReferenceTestContext.Create();
			var resourceProperty = context.CreateResourceProperty();
			var job = CreateConfirmedJob(context, out var node);

			AddElementLinkedEvents(node, new ResourcePropertyReference(resourceProperty.Id));

			var exception = Assert.ThrowsException<MediaOpsException>(() => context.Api.Jobs.Update(job));

			var errors = exception.TraceData.ErrorData.OfType<OrchestrationSettingsUnresolvedReferenceError>().ToList();
			Assert.AreNotEqual(0, errors.Count, "Expected the unresolved orchestration event reference to be reported.");
		}

		[TestMethod]
		public void OrchestrationEventReferenceTests_ResolvableEventReference_IsAcceptedOnAConfirmedJob()
		{
			var context = ReferenceTestContext.Create();
			var job = CreateConfirmedJob(context, out var node);

			AddElementLinkedEvents(node, new ResourceLinkedObjectIdReference(node.Id));

			var updatedJob = context.Api.Jobs.Update(job);

			Assert.AreEqual(JobState.Confirmed, updatedJob.State);
		}

		[TestMethod]
		public void OrchestrationEventReferenceTests_UnresolvedEventReference_DoesNotBlockADraftJob()
		{
			var context = ReferenceTestContext.Create();
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

			var node = new JobResourceNode(pool, resource);
			job.NodeGraph.Add(node);

			AddElementLinkedEvents(node, new ResourceLinkedObjectIdReference());

			var createdJob = context.Api.Jobs.Create(job);

			Assert.AreEqual(JobState.Draft, createdJob.State);
		}
	}
}
