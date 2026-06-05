namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	[TestClass]
	[TestCategory("IntegrationTest")]
	[DoNotParallelize]
	public sealed class FromWorkflowTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public FromWorkflowTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void FromWorkflow_NullApi_Throws()
		{
			Assert.ThrowsException<ArgumentNullException>(() => Job.FromWorkflow(null, Guid.NewGuid()));
			Assert.ThrowsException<ArgumentNullException>(() => Job.FromWorkflow(null, new Workflow { Name = "X" }));
		}

		[TestMethod]
		public void FromWorkflow_NullWorkflow_Throws()
		{
			Assert.ThrowsException<ArgumentNullException>(() => Job.FromWorkflow(TestContext.Api, null));
		}

		[TestMethod]
		public void FromWorkflow_EmptyWorkflowId_Throws()
		{
			Assert.ThrowsException<ArgumentException>(() => Job.FromWorkflow(TestContext.Api, Guid.Empty));
		}

		[TestMethod]
		public void FromWorkflow_UnknownWorkflowId_ThrowsMediaOpsException()
		{
			var missingId = Guid.NewGuid();

			try
			{
				Job.FromWorkflow(TestContext.Api, missingId);
				Assert.Fail("Expected MediaOpsException was not thrown.");
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<WorkflowNotFoundError>().SingleOrDefault();
				Assert.IsNotNull(error);
				Assert.AreEqual(missingId, error.Id);
			}
		}

		[TestMethod]
		public void FromWorkflow_HappyPath_CopiesScalarFields()
		{
			var prefix = Guid.NewGuid();

			var workflow = new Workflow
			{
				Name = $"{prefix}_Workflow",
				Priority = WorkflowPriority.High,
				PreRoll = TimeSpan.FromSeconds(7),
				PostRoll = TimeSpan.FromSeconds(11),
			};
			workflow = objectCreator.CreateWorkflow(workflow);
			workflow = TestContext.Api.Workflows.Complete(workflow);

			var job = Job.FromWorkflow(TestContext.Api, workflow.Id);

			Assert.IsNotNull(job);
			Assert.AreEqual(JobPriority.High, job.Priority);

			// FromWorkflow no longer carries over the workflow's pre-roll/post-roll. It is up to the
			// consumer to apply the workflow's configuration onto the job timings if they want.
			Assert.AreEqual(default(DateTimeOffset), job.PreRollStart);
			Assert.AreEqual(default(DateTimeOffset), job.PostRollEnd);
		}

		[TestMethod]
		public void FromWorkflow_AcceptsWorkflowOverload()
		{
			var prefix = Guid.NewGuid();

			var workflow = objectCreator.CreateWorkflow(new Workflow
			{
				Name = $"{prefix}_Workflow",
				Priority = WorkflowPriority.Normal,
			});
			workflow = TestContext.Api.Workflows.Complete(workflow);

			var job = Job.FromWorkflow(TestContext.Api, workflow);

			Assert.IsNotNull(job);
			Assert.AreEqual(JobPriority.Normal, job.Priority);
		}

		[TestMethod]
		public void FromWorkflow_ClonesNodeGraph_WithFreshNodeAndConnectionIds()
		{
			var prefix = Guid.NewGuid();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(pool);
			resource = objectCreator.CreateResource(resource);
			resource = TestContext.Api.Resources.Complete(resource);

			var workflow = new Workflow { Name = $"{prefix}_Workflow" };
			var resourceNode = new WorkflowResourceNode(pool, resource) { Alias = "RN", IconImage = "icon-1" };
			var poolNode = new WorkflowResourcePoolNode(pool) { Alias = "PN", IconImage = "icon-2" };

			workflow.NodeGraph
				.Add(resourceNode)
				.Add(poolNode)
				.Connect(resourceNode, poolNode);

			workflow = objectCreator.CreateWorkflow(workflow);
			workflow = TestContext.Api.Workflows.Complete(workflow);

			var job = Job.FromWorkflow(TestContext.Api, workflow.Id);

			// Same shape.
			Assert.AreEqual(2, job.NodeGraph.Nodes.Count);
			Assert.AreEqual(1, job.NodeGraph.Connections.Count);

			// Same node types.
			Assert.AreEqual(1, job.NodeGraph.Nodes.OfType<JobResourceNode>().Count());
			Assert.AreEqual(1, job.NodeGraph.Nodes.OfType<JobResourcePoolNode>().Count());

			var jobResourceNode = job.NodeGraph.Nodes.OfType<JobResourceNode>().Single();
			var jobPoolNode = job.NodeGraph.Nodes.OfType<JobResourcePoolNode>().Single();

			// Resource/pool ids preserved.
			Assert.AreEqual(pool.Id, jobResourceNode.ResourcePoolId);
			Assert.AreEqual(resource.Id, jobResourceNode.ResourceId);
			Assert.AreEqual(pool.Id, jobPoolNode.ResourcePoolId);

			// Alias and icon preserved.
			Assert.AreEqual("RN", jobResourceNode.Alias);
			Assert.AreEqual("icon-1", jobResourceNode.IconImage);
			Assert.AreEqual("PN", jobPoolNode.Alias);
			Assert.AreEqual("icon-2", jobPoolNode.IconImage);

			// Node ids are regenerated.
			var workflowNodeIds = workflow.NodeGraph.Nodes.Select(n => n.Id).ToHashSet();
			foreach (var jobNode in job.NodeGraph.Nodes)
			{
				Assert.IsFalse(workflowNodeIds.Contains(jobNode.Id), $"Job node id {jobNode.Id} was not regenerated.");
			}

			// Connection ids are regenerated.
			var workflowConnectionIds = workflow.NodeGraph.Connections.Select(c => c.Id).ToHashSet();
			foreach (var connection in job.NodeGraph.Connections)
			{
				Assert.IsFalse(workflowConnectionIds.Contains(connection.Id), $"Job connection id {connection.Id} was not regenerated.");
			}

			// Connections reference the new job nodes (not workflow nodes).
			var jobConnection = job.NodeGraph.Connections.Single();
			Assert.AreSame(jobResourceNode, jobConnection.From);
			Assert.AreSame(jobPoolNode, jobConnection.To);
		}

		[TestMethod]
		public void FromWorkflow_CopiesJobLevelOrchestrationSettings()
		{
			var prefix = Guid.NewGuid();

			var capability = new Capability { Name = $"{prefix}_Capability" }.SetDiscretes(["Value 1", "Value 2"]);
			objectCreator.CreateCapability(capability);

			var numberCapacity = new NumberCapacity { Name = $"{prefix}_NumberCapacity" };
			objectCreator.CreateCapacities([numberCapacity]);

			var textConfiguration = new TextConfiguration { Name = $"{prefix}_TextConfiguration" };
			objectCreator.CreateConfigurations([textConfiguration]);

			var workflow = new Workflow { Name = $"{prefix}_Workflow" };
			workflow.OrchestrationSettings
				.AddCapability(new CapabilitySetting(capability))
				.AddCapacity(new NumberCapacitySetting(numberCapacity))
				.AddConfiguration(new TextConfigurationSetting(textConfiguration));

			workflow = objectCreator.CreateWorkflow(workflow);
			workflow = TestContext.Api.Workflows.Complete(workflow);

			var job = Job.FromWorkflow(TestContext.Api, workflow.Id);

			Assert.AreEqual(1, job.OrchestrationSettings.Capabilities.Count);
			Assert.AreEqual(capability.Id, job.OrchestrationSettings.Capabilities.Single().Id);

			Assert.AreEqual(1, job.OrchestrationSettings.Capacities.Count);
			Assert.AreEqual(numberCapacity.Id, job.OrchestrationSettings.Capacities.Single().Id);

			Assert.AreEqual(1, job.OrchestrationSettings.Configurations.Count);
			Assert.AreEqual(textConfiguration.Id, job.OrchestrationSettings.Configurations.Single().Id);
		}

		[TestMethod]
		public void FromWorkflow_CopiesNodeLevelOrchestrationSettings()
		{
			var prefix = Guid.NewGuid();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(pool);
			resource = objectCreator.CreateResource(resource);
			resource = TestContext.Api.Resources.Complete(resource);

			var capability = new Capability { Name = $"{prefix}_Capability" }.SetDiscretes(["Value 1", "Value 2"]);
			objectCreator.CreateCapability(capability);

			var workflow = new Workflow { Name = $"{prefix}_Workflow" };
			var resourceNode = new WorkflowResourceNode(pool, resource);
			resourceNode.OrchestrationSettings.AddCapability(new CapabilitySetting(capability));
			workflow.NodeGraph.Add(resourceNode);

			workflow = objectCreator.CreateWorkflow(workflow);
			workflow = TestContext.Api.Workflows.Complete(workflow);

			var job = Job.FromWorkflow(TestContext.Api, workflow.Id);

			var jobNode = job.NodeGraph.Nodes.OfType<JobResourceNode>().Single();
			Assert.AreEqual(1, jobNode.OrchestrationSettings.Capabilities.Count);
			Assert.AreEqual(capability.Id, jobNode.OrchestrationSettings.Capabilities.Single().Id);
		}

		[TestMethod]
		public void FromWorkflow_CopiesCustomPropertiesOnWorkflowAndNodes()
		{
			var prefix = Guid.NewGuid();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(pool);
			resource = objectCreator.CreateResource(resource);
			resource = TestContext.Api.Resources.Complete(resource);

			var workflow = new Workflow { Name = $"{prefix}_Workflow" };
			workflow.AddCustomProperty(new CustomPropertySetting("Tag1") { Value = "Value1" });

			var resourceNode = new WorkflowResourceNode(pool, resource);
			resourceNode.AddCustomProperty(new CustomPropertySetting("Tag2") { Value = "Value2" });
			workflow.NodeGraph.Add(resourceNode);

			workflow = objectCreator.CreateWorkflow(workflow);
			workflow = TestContext.Api.Workflows.Complete(workflow);

			var job = Job.FromWorkflow(TestContext.Api, workflow.Id);

			Assert.AreEqual(1, job.CustomPropertySettings.Count);
			var jobProperty = job.CustomPropertySettings.Single();
			Assert.AreEqual("Tag1", jobProperty.Name);
			Assert.AreEqual("Value1", jobProperty.Value);

			var jobNode = job.NodeGraph.Nodes.OfType<JobResourceNode>().Single();
			Assert.AreEqual(1, jobNode.CustomPropertySettings.Count);
			var jobNodeProperty = jobNode.CustomPropertySettings.Single();
			Assert.AreEqual("Tag2", jobNodeProperty.Name);
			Assert.AreEqual("Value2", jobNodeProperty.Value);
		}

		[TestMethod]
		public void FromWorkflow_CopiedProperties_AreIndependentInstances()
		{
			var prefix = Guid.NewGuid();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(pool);
			resource = objectCreator.CreateResource(resource);
			resource = TestContext.Api.Resources.Complete(resource);

			var workflow = new Workflow { Name = $"{prefix}_Workflow" };
			workflow.AddCustomProperty(new CustomPropertySetting("Tag1") { Value = "Value1" });

			var resourceNode = new WorkflowResourceNode(pool, resource);
			resourceNode.AddCustomProperty(new CustomPropertySetting("Tag2") { Value = "Value2" });
			workflow.NodeGraph.Add(resourceNode);

			workflow = objectCreator.CreateWorkflow(workflow);
			workflow = TestContext.Api.Workflows.Complete(workflow);

			var job = Job.FromWorkflow(TestContext.Api, workflow.Id);

			// The copied settings must not be the same instances as the workflow's settings.
			var workflowProperty = workflow.CustomPropertySettings.Single();
			var jobProperty = job.CustomPropertySettings.Single();
			Assert.AreNotSame(workflowProperty, jobProperty);

			var workflowNodeProperty = workflow.NodeGraph.Nodes.Single().CustomPropertySettings.Single();
			var jobNodeProperty = job.NodeGraph.Nodes.Single().CustomPropertySettings.Single();
			Assert.AreNotSame(workflowNodeProperty, jobNodeProperty);

			// Mutating the job copies must not affect the source workflow's settings.
			jobProperty.Value = "Changed";
			jobNodeProperty.Value = "Changed";

			Assert.AreEqual("Value1", workflow.CustomPropertySettings.Single().Value);
			Assert.AreEqual("Value2", workflow.NodeGraph.Nodes.Single().CustomPropertySettings.Single().Value);
		}

		[TestMethod]
		public void FromWorkflow_DoesNotMutateSourceWorkflow()
		{
			var prefix = Guid.NewGuid();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(pool);
			resource = objectCreator.CreateResource(resource);
			resource = TestContext.Api.Resources.Complete(resource);

			var capability = new Capability { Name = $"{prefix}_Capability" }.SetDiscretes(["Value 1", "Value 2"]);
			objectCreator.CreateCapability(capability);

			var workflow = new Workflow { Name = $"{prefix}_Workflow" };
			workflow.OrchestrationSettings.AddCapability(new CapabilitySetting(capability));

			var resourceNode = new WorkflowResourceNode(pool, resource);
			workflow.NodeGraph.Add(resourceNode);

			workflow = objectCreator.CreateWorkflow(workflow);
			workflow = TestContext.Api.Workflows.Complete(workflow);

			// Re-read so we have a fresh in-memory copy that we can compare against after FromWorkflow.
			var workflowBefore = TestContext.Api.Workflows.Read(workflow.Id);

			Job.FromWorkflow(TestContext.Api, workflow.Id);

			var workflowAfter = TestContext.Api.Workflows.Read(workflow.Id);

			Assert.AreEqual(workflowBefore, workflowAfter);
			Assert.AreEqual(workflowBefore.NodeGraph.Nodes.Count, workflowAfter.NodeGraph.Nodes.Count);
			Assert.AreEqual(workflowBefore.OrchestrationSettings.Capabilities.Count, workflowAfter.OrchestrationSettings.Capabilities.Count);
		}

		[TestMethod]
		public void FromWorkflow_TwoJobsFromSameWorkflow_HaveDistinctNodeAndConnectionIds()
		{
			var prefix = Guid.NewGuid();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(pool);
			resource = objectCreator.CreateResource(resource);
			resource = TestContext.Api.Resources.Complete(resource);

			var workflow = new Workflow { Name = $"{prefix}_Workflow" };
			var resourceNode = new WorkflowResourceNode(pool, resource);
			var poolNode = new WorkflowResourcePoolNode(pool);
			workflow.NodeGraph.Add(resourceNode).Add(poolNode).Connect(resourceNode, poolNode);

			workflow = objectCreator.CreateWorkflow(workflow);
			workflow = TestContext.Api.Workflows.Complete(workflow);

			var jobA = Job.FromWorkflow(TestContext.Api, workflow.Id);
			var jobB = Job.FromWorkflow(TestContext.Api, workflow.Id);

			var nodeIdsA = jobA.NodeGraph.Nodes.Select(n => n.Id).ToHashSet();
			var nodeIdsB = jobB.NodeGraph.Nodes.Select(n => n.Id).ToHashSet();
			Assert.AreEqual(0, nodeIdsA.Intersect(nodeIdsB).Count(), "Node ids must be unique across separate FromWorkflow calls.");

			var connectionIdsA = jobA.NodeGraph.Connections.Select(c => c.Id).ToHashSet();
			var connectionIdsB = jobB.NodeGraph.Connections.Select(c => c.Id).ToHashSet();
			Assert.AreEqual(0, connectionIdsA.Intersect(connectionIdsB).Count(), "Connection ids must be unique across separate FromWorkflow calls.");
		}

		[TestMethod]
		public void FromWorkflow_WorkflowInDraftState_Fails()
		{
			var prefix = Guid.NewGuid();

			// Create the workflow but intentionally leave it in Draft state.
			var workflow = objectCreator.CreateWorkflow(new Workflow { Name = $"{prefix}_Workflow" });
			Assert.AreEqual(WorkflowState.Draft, workflow.State);

			try
			{
				Job.FromWorkflow(TestContext.Api, workflow.Id);
				Assert.Fail("Expected MediaOpsException was not thrown.");
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<WorkflowInvalidStateError>().SingleOrDefault();
				Assert.IsNotNull(error);
				Assert.AreEqual(workflow.Id, error.Id);
				Assert.AreEqual("Not allowed to build a job from a workflow that is not in Complete state.", error.ErrorMessage);
			}
		}
	}
}
