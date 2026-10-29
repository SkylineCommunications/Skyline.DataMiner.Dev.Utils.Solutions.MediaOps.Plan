namespace RT_MediaOps.Plan.Workflow.FlowRules
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.Extensions;
	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	[TestClass]
	[TestCategory("IntegrationTest")]
	[DoNotParallelize]
	public sealed class FlowRuleEnforcementTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public FlowRuleEnforcementTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void Job_WithForbiddenConnection_IsRejected()
		{
			var (ird, downstream) = CreatePoolsWithForbiddenConnection();

			var job = NewValidJob();
			var irdNode = new JobResourcePoolNode(ird) { Alias = "IRD" };
			var downstreamNode = new JobResourcePoolNode(downstream) { Alias = "Downstream" };
			job.NodeGraph.Add(irdNode).Add(downstreamNode).Connect(irdNode, downstreamNode);

			var exception = Assert.ThrowsException<MediaOpsException>(() => objectCreator.CreateJob(job));

			var error = exception.TraceData.ErrorData.OfType<JobNodeGraphForbiddenConnectionError>().Single();
			Assert.AreEqual(job.Id, error.Id);
			Assert.AreEqual(job.NodeGraph.Connections.Single().Id, error.ConnectionId);
			Assert.AreEqual(irdNode.Id, error.FromNodeId);
			Assert.AreEqual(downstreamNode.Id, error.ToNodeId);
			Assert.AreEqual(FlowRuleObjectType.ResourcePool, error.FromObjectType);
			Assert.AreEqual(ird.Id, error.FromObjectId);
			Assert.AreEqual(FlowRuleObjectType.ResourcePool, error.ToObjectType);
			Assert.AreEqual(downstream.Id, error.ToObjectId);
			StringAssert.Contains(error.ErrorMessage, "node 'IRD'");
			StringAssert.Contains(error.ErrorMessage, "node 'Downstream'");
			StringAssert.Contains(error.ErrorMessage, "No IRD before this pool.");
		}

		[TestMethod]
		public void Job_WithForbiddenConnectionInOtherDirection_IsAccepted()
		{
			var (ird, downstream) = CreatePoolsWithForbiddenConnection();

			var job = NewValidJob();
			var irdNode = new JobResourcePoolNode(ird);
			var downstreamNode = new JobResourcePoolNode(downstream);
			job.NodeGraph.Add(irdNode).Add(downstreamNode).Connect(downstreamNode, irdNode);

			Assert.IsNotNull(objectCreator.CreateJob(job));
		}

		[TestMethod]
		public void Job_WithWarningConnection_IsAccepted()
		{
			var prefix = Guid.NewGuid();
			var source = CreateCompletedPool($"{prefix}_Source");
			var destination = CreateCompletedPool($"{prefix}_Destination", rules => rules.SetDefault(FlowRuleDirection.Before, FlowRuleOutcome.Warning, "Unusual."));

			var job = NewValidJob();
			var sourceNode = new JobResourcePoolNode(source);
			var destinationNode = new JobResourcePoolNode(destination);
			job.NodeGraph.Add(sourceNode).Add(destinationNode).Connect(sourceNode, destinationNode);

			var created = objectCreator.CreateJob(job);

			Assert.AreEqual(1, created.NodeGraph.Connections.Count);
			Assert.AreEqual(FlowRuleOutcome.Warning, TestContext.Api.FlowRules.Evaluate(created.NodeGraph).Single().Outcome);
		}

		[TestMethod]
		public void Job_AddingForbiddenConnectionOnUpdate_IsRejected()
		{
			var (ird, downstream) = CreatePoolsWithForbiddenConnection();

			var job = NewValidJob();
			job.NodeGraph.Add(new JobResourcePoolNode(ird)).Add(new JobResourcePoolNode(downstream));
			job = objectCreator.CreateJob(job);

			var irdNode = job.NodeGraph.Nodes.OfType<JobResourcePoolNode>().Single(x => x.ResourcePoolId == ird.Id);
			var downstreamNode = job.NodeGraph.Nodes.OfType<JobResourcePoolNode>().Single(x => x.ResourcePoolId == downstream.Id);
			job.NodeGraph.Connect(irdNode, downstreamNode);

			var exception = Assert.ThrowsException<MediaOpsException>(() => TestContext.Api.Jobs.Update(job));
			Assert.AreEqual(1, exception.TraceData.ErrorData.OfType<JobNodeGraphForbiddenConnectionError>().Count());
		}

		[TestMethod]
		public void Job_ForbiddenAfterRuleChange_StaysEditableAndIsFlaggedByValidation()
		{
			var prefix = Guid.NewGuid();
			var ird = CreateCompletedPool($"{prefix}_IRD");
			var downstream = CreateCompletedPool($"{prefix}_Downstream");

			var job = NewValidJob();
			var irdNode = new JobResourcePoolNode(ird);
			var downstreamNode = new JobResourcePoolNode(downstream);
			job.NodeGraph.Add(irdNode).Add(downstreamNode).Connect(irdNode, downstreamNode);
			job = objectCreator.CreateJob(job);

			// The rule is added after the job was saved.
			downstream = TestContext.Api.ResourcePools.Read(downstream.Id);
			downstream.FlowRules.SetOverride(FlowRuleDirection.Before, ird, FlowRuleOutcome.Forbidden, "No IRD before this pool.");
			TestContext.Api.ResourcePools.Update(downstream);

			// The existing connection does not block unrelated updates.
			job = TestContext.Api.Jobs.Read(job.Id);
			job.Description = "Updated after the rule change.";
			job = TestContext.Api.Jobs.Update(job);

			// The validation flags the job and the error can be stored.
			var result = TestContext.Api.Jobs.Validate([job]).Single();
			Assert.IsTrue(result.HasError(FlowRuleViolationJobValidationError.ErrorCode));
			StringAssert.Contains(result.Errors.Single(x => x.Code == FlowRuleViolationJobValidationError.ErrorCode).Message, "No IRD before this pool.");
			Assert.IsTrue(result.SyncToJob());
			job = TestContext.Api.Jobs.Update(job);
			Assert.IsTrue(TestContext.Api.Jobs.Read(job.Id).Errors.Any(x => x.Code == FlowRuleViolationJobValidationError.ErrorCode));

			// Resolving the connection clears the error in the same save.
			job.NodeGraph.Disconnect(job.NodeGraph.Connections.Single());
			job = TestContext.Api.Jobs.Update(job);

			Assert.IsFalse(job.Errors.Any(x => x.Code == FlowRuleViolationJobValidationError.ErrorCode));
			Assert.IsFalse(TestContext.Api.Jobs.Read(job.Id).Errors.Any(x => x.Code == FlowRuleViolationJobValidationError.ErrorCode));
		}

		[TestMethod]
		public void Job_SwappingToAForbiddenPool_IsRejected()
		{
			var prefix = Guid.NewGuid();
			var (ird, downstream) = CreatePoolsWithForbiddenConnection();
			var other = CreateCompletedPool($"{prefix}_Other");

			var job = NewValidJob();
			var otherNode = new JobResourcePoolNode(other);
			var downstreamNode = new JobResourcePoolNode(downstream);
			job.NodeGraph.Add(otherNode).Add(downstreamNode).Connect(otherNode, downstreamNode);
			job = objectCreator.CreateJob(job);

			var storedOtherNode = job.NodeGraph.Nodes.OfType<JobResourcePoolNode>().Single(x => x.ResourcePoolId == other.Id);
			job.NodeGraph.Swap(storedOtherNode, new JobResourcePoolNode(ird));

			var exception = Assert.ThrowsException<MediaOpsException>(() => TestContext.Api.Jobs.Update(job));
			Assert.AreEqual(1, exception.TraceData.ErrorData.OfType<JobNodeGraphForbiddenConnectionError>().Count());
		}

		[TestMethod]
		public void Job_ConnectionBetweenNodesOfSamePool_UsesSelfTargetingOverride()
		{
			var prefix = Guid.NewGuid();
			var ird = CreateCompletedPool($"{prefix}_IRD");
			ird.FlowRules.SetOverride(FlowRuleDirection.After, ird, FlowRuleOutcome.Forbidden);
			ird = TestContext.Api.ResourcePools.Update(ird);

			var job = NewValidJob();
			var first = new JobResourcePoolNode(ird);
			var second = new JobResourcePoolNode(ird);
			job.NodeGraph.Add(first).Add(second).Connect(first, second);

			var exception = Assert.ThrowsException<MediaOpsException>(() => objectCreator.CreateJob(job));
			Assert.AreEqual(1, exception.TraceData.ErrorData.OfType<JobNodeGraphForbiddenConnectionError>().Count());
		}

		[TestMethod]
		public void Workflow_WithForbiddenConnection_IsRejected()
		{
			var (ird, downstream) = CreatePoolsWithForbiddenConnection();

			var workflow = new Workflow { Name = $"{Guid.NewGuid()}_Workflow" };
			var irdNode = new WorkflowResourcePoolNode(ird);
			var downstreamNode = new WorkflowResourcePoolNode(downstream);
			workflow.NodeGraph.Add(irdNode).Add(downstreamNode).Connect(irdNode, downstreamNode);

			var exception = Assert.ThrowsException<MediaOpsException>(() => objectCreator.CreateWorkflow(workflow));

			var error = exception.TraceData.ErrorData.OfType<WorkflowNodeGraphForbiddenConnectionError>().Single();
			Assert.AreEqual(irdNode.Id, error.FromNodeId);
			Assert.AreEqual(downstreamNode.Id, error.ToNodeId);
		}

		[TestMethod]
		public void RecurringJob_WithForbiddenConnection_IsRejected()
		{
			var (ird, downstream) = CreatePoolsWithForbiddenConnection();

			var recurringJob = new RecurringJob
			{
				Name = $"{Guid.NewGuid()}_RecurringJob",
				Start = DateTime.UtcNow.AddHours(1),
				Duration = TimeSpan.FromHours(1),
			};
			recurringJob.Pattern.RepeatType = RepeatType.Daily;
			recurringJob.Pattern.RepeatEvery = 1;
			recurringJob.Pattern.EndDate = DateTime.UtcNow.AddDays(10);

			var irdNode = new RecurringJobResourcePoolNode(ird);
			var downstreamNode = new RecurringJobResourcePoolNode(downstream);
			recurringJob.NodeGraph.Add(irdNode).Add(downstreamNode).Connect(irdNode, downstreamNode);

			var exception = Assert.ThrowsException<MediaOpsException>(() => objectCreator.CreateRecurringJob(recurringJob));

			Assert.AreEqual(1, exception.TraceData.ErrorData.OfType<RecurringJobNodeGraphForbiddenConnectionError>().Count());
		}

		private (ResourcePool Ird, ResourcePool Downstream) CreatePoolsWithForbiddenConnection()
		{
			var prefix = Guid.NewGuid();
			var ird = CreateCompletedPool($"{prefix}_IRD");
			var downstream = CreateCompletedPool(
				$"{prefix}_Downstream",
				rules => rules
					.SetDefault(FlowRuleDirection.Before, FlowRuleOutcome.Allowed)
					.SetOverride(FlowRuleDirection.Before, ird, FlowRuleOutcome.Forbidden, "No IRD before this pool."));

			return (ird, downstream);
		}

		private ResourcePool CreateCompletedPool(string name, Action<Skyline.DataMiner.Solutions.MediaOps.Plan.API.FlowRules> configureRules = null)
		{
			var pool = new ResourcePool { Name = name };
			configureRules?.Invoke(pool.FlowRules);

			return TestContext.Api.ResourcePools.Complete(objectCreator.CreateResourcePool(pool));
		}

		private static Job NewValidJob()
		{
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			return new Job
			{
				Name = $"{Guid.NewGuid()}_Job",
				Start = currentTime.AddHours(1),
				End = currentTime.AddHours(2),
				PreRollStart = currentTime.AddHours(1),
				PostRollEnd = currentTime.AddHours(2),
			};
		}
	}
}
