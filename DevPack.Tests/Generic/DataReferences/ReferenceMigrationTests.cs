namespace RT_MediaOps.Plan.Generic.DataReferences
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	/// <summary>
	/// Tests that <see cref="DataReference"/> instances configured on a workflow (or recurring job) are migrated
	/// correctly onto the job that is built from it, and keep resolving against that job.
	/// </summary>
	[TestClass]
	public sealed class ReferenceMigrationTests
	{
		private static Workflow CompleteWorkflow(ReferenceTestContext context, Workflow workflow)
		{
			return context.Api.Workflows.Complete(context.Api.Workflows.Create(workflow));
		}

		private static string ResolveString(ReferenceResolver resolver, DataReference reference)
		{
			var resolved = resolver.ResolveValue(reference);

			Assert.IsTrue(resolved.IsResolved, $"Expected '{reference}' to resolve.");

			return ((StringResolvedValue)resolved).Value;
		}

		/// <summary>
		/// A workflow links a setting to the name of the workflow / job. After building a job from that workflow the
		/// migrated reference must resolve against the name of the job.
		/// </summary>
		[TestMethod]
		public void ReferenceMigrationTests_JobNameReference_MigratedFromWorkflowToJob_ResolvesAgainstTheJobName()
		{
			var context = ReferenceTestContext.Create();
			var capability = context.CreateCapability();
			var pool = context.CreatePool();
			var resource = context.CreateResource(pool);

			var workflow = new Workflow { Name = context.Name("Workflow") };
			var workflowNode = new WorkflowResourceNode(pool, resource);
			workflow.NodeGraph.Add(workflowNode);

			// One link at workflow level and one scoped to the node.
			workflow.OrchestrationSettings.AddCapability(new CapabilitySetting(capability) { Reference = new JobNameReference() });
			workflowNode.OrchestrationSettings.AddCapability(new CapabilitySetting(capability) { Reference = new JobNameReference(workflowNode.Id) });

			workflow = CompleteWorkflow(context, workflow);
			var workflowNodeId = workflow.NodeGraph.Nodes.Single().Id;

			var job = Job.FromWorkflow(context.Api, workflow.Id);
			job.Name = context.Name("Job");

			var jobNode = job.NodeGraph.Nodes.Single();

			// The workflow-level link targets the job itself, so it stays without a node id.
			var jobLevelReference = job.OrchestrationSettings.Capabilities.Single().Reference;
			Assert.IsInstanceOfType(jobLevelReference, typeof(JobNameReference));
			Assert.IsNull(jobLevelReference.NodeId);

			// The node-scoped link must have been retargeted at the cloned job node.
			var nodeLevelReference = jobNode.OrchestrationSettings.Capabilities.Single().Reference;
			Assert.IsInstanceOfType(nodeLevelReference, typeof(JobNameReference));
			Assert.AreNotEqual(workflowNodeId, nodeLevelReference.NodeId, "The reference still points at the workflow node.");
			Assert.AreEqual(jobNode.Id, nodeLevelReference.NodeId);

			var resolver = new JobReferenceResolver(context.Api, job);

			Assert.AreEqual(job.Name, ResolveString(resolver, jobLevelReference));
			Assert.AreEqual(job.Name, ResolveString(resolver, nodeLevelReference));

			// Resolving the capability follows the migrated link and ends up on the job name.
			Assert.AreEqual(job.Name, ResolveString(resolver, new CapabilityParameterReference(capability.Id)));
			Assert.AreEqual(job.Name, ResolveString(resolver, new CapabilityParameterReference(capability.Id, jobNode.Id)));
		}

		[TestMethod]
		public void ReferenceMigrationTests_JobNameReference_MigratedFromWorkflowToJob_SurvivesPersistence()
		{
			var context = ReferenceTestContext.Create();
			var capability = context.CreateCapability();
			var pool = context.CreatePool();
			var resource = context.CreateResource(pool);

			var workflow = new Workflow { Name = context.Name("Workflow") };
			var workflowNode = new WorkflowResourceNode(pool, resource);
			workflow.NodeGraph.Add(workflowNode);

			workflow.OrchestrationSettings.AddCapability(new CapabilitySetting(capability) { Reference = new JobNameReference() });
			workflowNode.OrchestrationSettings.AddCapability(new CapabilitySetting(capability) { Reference = new JobNameReference(workflowNode.Id) });

			workflow = CompleteWorkflow(context, workflow);

			var start = ReferenceTestContext.ScheduleStart;

			var job = Job.FromWorkflow(context.Api, workflow.Id);
			job.Name = context.Name("Job");
			job.Start = start;
			job.End = start.AddHours(1);
			job.PreRollStart = start;
			job.PostRollEnd = start.AddHours(1);

			var storedJob = context.Api.Jobs.Read(context.Api.Jobs.Create(job).Id);
			var storedNode = storedJob.NodeGraph.Nodes.Single();

			var jobLevelReference = storedJob.OrchestrationSettings.Capabilities.Single().Reference;
			Assert.IsInstanceOfType(jobLevelReference, typeof(JobNameReference));
			Assert.IsNull(jobLevelReference.NodeId);

			var nodeLevelReference = storedNode.OrchestrationSettings.Capabilities.Single().Reference;
			Assert.IsInstanceOfType(nodeLevelReference, typeof(JobNameReference));
			Assert.AreEqual(storedNode.Id, nodeLevelReference.NodeId);

			var resolver = new JobReferenceResolver(context.Api, storedJob);

			Assert.AreEqual(storedJob.Name, ResolveString(resolver, jobLevelReference));
			Assert.AreEqual(storedJob.Name, ResolveString(resolver, nodeLevelReference));
			Assert.AreEqual(storedJob.Name, ResolveString(resolver, new CapabilityParameterReference(capability.Id)));
		}

		[TestMethod]
		public void ReferenceMigrationTests_NodeScopedReference_MigratedFromWorkflowToJob_ResolvesAgainstTheClonedNode()
		{
			var context = ReferenceTestContext.Create();
			var capability = context.CreateCapability();
			var resourceProperty = context.CreateResourceProperty();
			var pool = context.CreatePool();
			var resource = context.CreateResource(pool, new ResourcePropertySettings(resourceProperty.Id) { Value = "Property value" });

			var workflow = new Workflow { Name = context.Name("Workflow") };
			var referencedNode = new WorkflowResourceNode(pool, resource);
			var referencingNode = new WorkflowResourcePoolNode(pool);
			workflow.NodeGraph.Add(referencedNode).Add(referencingNode);

			referencingNode.OrchestrationSettings.AddCapability(new CapabilitySetting(capability)
			{
				Reference = new ResourcePropertyReference(resourceProperty.Id, referencedNode.Id),
			});

			workflow = CompleteWorkflow(context, workflow);

			var job = Job.FromWorkflow(context.Api, workflow.Id);
			job.Name = context.Name("Job");

			var jobResourceNode = job.NodeGraph.Nodes.OfType<JobResourceNode>().Single();
			var jobPoolNode = job.NodeGraph.Nodes.OfType<JobResourcePoolNode>().Single();

			var reference = jobPoolNode.OrchestrationSettings.Capabilities.Single().Reference;
			Assert.AreEqual(jobResourceNode.Id, reference.NodeId);

			var resolver = new JobReferenceResolver(context.Api, job);

			Assert.AreEqual("Property value", ResolveString(resolver, reference));
			Assert.AreEqual("Property value", ResolveString(resolver, new CapabilityParameterReference(capability.Id, jobPoolNode.Id)));
		}

		[TestMethod]
		public void ReferenceMigrationTests_ReferenceToANodeOutsideTheGraph_IsLeftUntouched()
		{
			var context = ReferenceTestContext.Create();
			var capability = context.CreateCapability();
			var pool = context.CreatePool();
			var resource = context.CreateResource(pool);

			var workflow = new Workflow { Name = context.Name("Workflow") };
			workflow.NodeGraph.Add(new WorkflowResourceNode(pool, resource));
			workflow.OrchestrationSettings.AddCapability(new CapabilitySetting(capability)
			{
				Reference = new ResourceNameReference("external-node"),
			});

			workflow = CompleteWorkflow(context, workflow);

			var job = Job.FromWorkflow(context.Api, workflow.Id);

			Assert.AreEqual("external-node", job.OrchestrationSettings.Capabilities.Single().Reference.NodeId);
		}

		[TestMethod]
		public void ReferenceMigrationTests_MigratedReferences_AreIndependentFromTheSourceWorkflow()
		{
			var context = ReferenceTestContext.Create();
			var capability = context.CreateCapability();
			var pool = context.CreatePool();
			var resource = context.CreateResource(pool);

			var workflow = new Workflow { Name = context.Name("Workflow") };
			var workflowNode = new WorkflowResourceNode(pool, resource);
			workflow.NodeGraph.Add(workflowNode);
			workflow.OrchestrationSettings.AddCapability(new CapabilitySetting(capability)
			{
				Reference = new ResourceNameReference(workflowNode.Id),
			});

			workflow = CompleteWorkflow(context, workflow);
			var workflowNodeId = workflow.NodeGraph.Nodes.Single().Id;

			var job = Job.FromWorkflow(context.Api, workflow.Id);
			job.OrchestrationSettings.Capabilities.Single().Reference = new JobNameReference();

			Assert.AreEqual(workflowNodeId, workflow.OrchestrationSettings.Capabilities.Single().Reference.NodeId);
			Assert.IsInstanceOfType(workflow.OrchestrationSettings.Capabilities.Single().Reference, typeof(ResourceNameReference));
		}

		[TestMethod]
		public void ReferenceMigrationTests_ScriptExecutionReferences_AreMigratedOntoTheJob()
		{
			var context = ReferenceTestContext.Create();
			var pool = context.CreatePool();
			var resource = context.CreateResource(pool);

			var workflow = new Workflow { Name = context.Name("Workflow") };
			var workflowNode = new WorkflowResourceNode(pool, resource);
			workflow.NodeGraph.Add(workflowNode);

			workflow.OrchestrationSettings.AddOrchestrationEvent(new OrchestrationEvent
			{
				EventType = OrchestrationEventType.PrerollStart,
				ExecutionDetails = new ScriptExecutionDetails("SomeScript")
					.AddScriptParameter(new ScriptParameterSetting("Target") { Reference = new ResourceNameReference(workflowNode.Id) }),
			});

			workflow = CompleteWorkflow(context, workflow);

			var job = Job.FromWorkflow(context.Api, workflow.Id);
			job.Name = context.Name("Job");

			var jobNode = job.NodeGraph.Nodes.Single();
			var parameter = job.OrchestrationSettings.OrchestrationEvents.Single().ExecutionDetails.ScriptParameters.Single();

			Assert.AreEqual(jobNode.Id, parameter.Reference.NodeId);
			Assert.AreEqual(resource.Name, ResolveString(new JobReferenceResolver(context.Api, job), parameter.Reference));
		}

		[TestMethod]
		public void ReferenceMigrationTests_ReferencesOfARecurringJob_AreMigratedOntoTheGeneratedJob()
		{
			var context = ReferenceTestContext.Create();
			var capability = context.CreateCapability();
			var pool = context.CreatePool();
			var resource = context.CreateResource(pool);

			var recurringJob = new RecurringJob { Name = context.Name("RecurringJob") };
			var recurringNode = new RecurringJobResourceNode(pool.Id, resource.Id);
			recurringJob.NodeGraph.Add(recurringNode);
			recurringJob.OrchestrationSettings.AddCapability(new CapabilitySetting(capability)
			{
				Reference = new ResourceNameReference(recurringNode.Id),
			});

			var job = Job.FromRecurringJob(recurringJob, DateTimeOffset.UtcNow.AddHours(1));

			var jobNode = job.NodeGraph.Nodes.Single();
			var reference = job.OrchestrationSettings.Capabilities.Single().Reference;

			Assert.AreNotEqual(recurringNode.Id, reference.NodeId);
			Assert.AreEqual(jobNode.Id, reference.NodeId);
			Assert.AreEqual(resource.Name, ResolveString(new JobReferenceResolver(context.Api, job), reference));
		}
	}
}
