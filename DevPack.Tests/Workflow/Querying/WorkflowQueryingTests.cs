namespace RT_MediaOps.Plan.Workflow.Querying
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.Querying;
	using RT_MediaOps.Plan.RegressionTests;
	using RT_MediaOps.Plan.Workflow.Filtering;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.SDM;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	using SLDataGateway.API.Querying;
	using SLDataGateway.API.Types.Querying;

	[TestClass]
	[TestCategory("IntegrationTest")]
	public sealed class WorkflowQueryingTests
	{
		private static TestObjectCreator? objectCreator;
		private static WorkflowFilteringSetup? setup;

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		private static WorkflowFilteringSetup Setup => setup ?? throw new InvalidOperationException("Test setup was not initialized.");

		[ClassInitialize]
		public static void ClassInitialize(TestContext context)
		{
			objectCreator = new TestObjectCreator(TestContext);
			setup = new WorkflowFilteringSetup(objectCreator, TestContext);
		}

		[ClassCleanup(ClassCleanupBehavior.EndOfClass)]
		public static void ClassCleanup()
		{
			objectCreator?.Dispose();
			objectCreator = null;
			setup = null;
		}

		private FilterElement<Workflow> WorkflowFilter => new ORFilterElement<Workflow>(Setup.Workflows.Select(x => WorkflowExposers.Id.Equal(x.Id)).ToArray());

		/// <summary>
		/// Gets the expected objects, in the expected order, mapped to the applied query.
		/// </summary>
		private Tuple<Workflow[], IQuery<Workflow>>[] WorkflowQueryTestCases => new[]
		{
			new Tuple<Workflow[], IQuery<Workflow>>(
				[Setup.CompleteWorkflow3!, Setup.DraftWorkflow1!, Setup.DraftWorkflow2!],
				WorkflowFilter.ToQuery().OrderBy(WorkflowExposers.Name)),
			new Tuple<Workflow[], IQuery<Workflow>>(
				[Setup.DraftWorkflow2!, Setup.DraftWorkflow1!, Setup.CompleteWorkflow3!],
				WorkflowFilter.ToQuery().OrderByDescending(WorkflowExposers.Name)),

			new Tuple<Workflow[], IQuery<Workflow>>(
				[Setup.DraftWorkflow1!, Setup.DraftWorkflow2!, Setup.CompleteWorkflow3!],
				WorkflowFilter.ToQuery().OrderBy(WorkflowExposers.PostRoll)),
			new Tuple<Workflow[], IQuery<Workflow>>(
				[Setup.CompleteWorkflow3!, Setup.DraftWorkflow2!, Setup.DraftWorkflow1!],
				WorkflowFilter.ToQuery().OrderByDescending(WorkflowExposers.PostRoll)),

			new Tuple<Workflow[], IQuery<Workflow>>(
				[Setup.DraftWorkflow2!, Setup.DraftWorkflow1!],
				WorkflowFilter.AND(WorkflowExposers.Name.Contains("Workflow_Draft")).ToQuery().OrderByDescending(WorkflowExposers.Name)),
			new Tuple<Workflow[], IQuery<Workflow>>(
				[],
				WorkflowFilter.AND(WorkflowExposers.Name.Contains("Unknown")).ToQuery().OrderBy(WorkflowExposers.Name)),
		};

		/// <summary>
		/// Gets the expected objects, in the expected order, mapped to the applied query with a limit.
		/// </summary>
		private Tuple<Workflow[], IQuery<Workflow>>[] WorkflowLimitedQueryTestCases => new[]
		{
			new Tuple<Workflow[], IQuery<Workflow>>(
				[Setup.CompleteWorkflow3!],
				WorkflowFilter.ToQuery().OrderBy(WorkflowExposers.Name).Limit(1)),
			new Tuple<Workflow[], IQuery<Workflow>>(
				[Setup.CompleteWorkflow3!, Setup.DraftWorkflow1!],
				WorkflowFilter.ToQuery().OrderBy(WorkflowExposers.Name).Limit(2)),
			new Tuple<Workflow[], IQuery<Workflow>>(
				[Setup.CompleteWorkflow3!, Setup.DraftWorkflow1!, Setup.DraftWorkflow2!],
				WorkflowFilter.ToQuery().OrderBy(WorkflowExposers.Name).Limit(10)),
		};

		[TestMethod]
		public void ReadWorkflowsWithQuery()
		{
			foreach (var (expectedObjects, query) in WorkflowQueryTestCases)
			{
				QueryAssert.Read(TestContext.Api.Workflows, expectedObjects, query);
			}
		}

		[TestMethod]
		public void ReadPartialWorkflowsWithQuery_ReturnsSameWorkflowsInSameOrder()
		{
			foreach (var (expectedObjects, query) in WorkflowQueryTestCases.Concat(WorkflowLimitedQueryTestCases))
			{
				var partialWorkflows = TestContext.Api.Workflows.ReadPartial(query.WithFields(WorkflowExposers.Name)).ToList();

				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), partialWorkflows.Select(x => x.Id).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Name).ToList(), partialWorkflows.Select(x => x.GetValue(WorkflowExposers.Name)).ToList(), $"Query: {query}");
			}
		}

		[TestMethod]
		public void ReadPartialPagedWorkflowsWithQuery_DefaultPageSize_ReturnsSameWorkflowsInOnePage()
		{
			foreach (var (expectedObjects, query) in WorkflowQueryTestCases.Concat(WorkflowLimitedQueryTestCases))
			{
				var pages = TestContext.Api.Workflows.ReadPartialPaged(query.WithFields(WorkflowExposers.Name)).ToList();

				Assert.AreEqual(1, pages.Count, $"Query: {query}");
				Assert.IsFalse(pages[0].HasNextPage, $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), pages[0].Select(x => x.Id).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Name).ToList(), pages[0].Select(x => x.GetValue(WorkflowExposers.Name)).ToList(), $"Query: {query}");
			}
		}

		[TestMethod]
		public void ReadPartialPagedWorkflowsWithQuery_CustomPageSize_SplitsWorkflowsOverPages()
		{
			var repository = (WorkflowsRepository)TestContext.Api.Workflows;

			foreach (var (expectedObjects, query) in WorkflowQueryTestCases.Concat(WorkflowLimitedQueryTestCases))
			{
				var pages = repository.ReadPartialPaged(query.WithFields(WorkflowExposers.Name), 2).ToList();

				CollectionAssert.AreEqual(expectedObjects.Select(x => x.Id).ToList(), pages.SelectMany(x => x).Select(x => x.Id).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(Enumerable.Range(0, pages.Count).ToList(), pages.Select(x => x.PageNumber).ToList(), $"Query: {query}");
				CollectionAssert.AreEqual(pages.Select((_, i) => i < pages.Count - 1).ToList(), pages.Select(x => x.HasNextPage).ToList(), $"Query: {query}");
				Assert.IsTrue(pages.All(x => x.Count <= 2), $"Query: {query}");
			}

			Assert.IsTrue(WorkflowQueryTestCases.Any(x => x.Item1.Length > 2), "No query returns more than one page.");
		}

		[TestMethod]
		public void ReadPartialPagedWorkflowsWithFilter_ReturnsAllWorkflows()
		{
			var pages = TestContext.Api.Workflows.ReadPartialPaged(WorkflowFilter.WithFields(WorkflowExposers.Name, WorkflowExposers.Resources)).ToList();
			var workflowsById = TestContext.Api.Workflows.Read(WorkflowFilter).ToDictionary(x => x.Id);

			Assert.AreEqual(1, pages.Count);
			CollectionAssert.AreEquivalent(workflowsById.Keys.ToList(), pages[0].Select(x => x.Id).ToList());

			foreach (var partialWorkflow in pages[0])
			{
				var workflow = workflowsById[partialWorkflow.Id];
				Assert.AreEqual(workflow.Name, partialWorkflow.GetValue(WorkflowExposers.Name));
				CollectionAssert.AreEqual(workflow.NodeGraph.Nodes.OfType<IResourceNode>().Select(x => x.ResourceId).ToList(), partialWorkflow.GetValues(WorkflowExposers.Resources).ToList(), workflow.Name);
			}
		}

		[TestMethod]
		public void ReadPartialWorkflowsWithFilter_ReturnsSameValuesAsFullRead()
		{
			var partialFilter = WorkflowFilter.WithFields(
				WorkflowExposers.Id,
				WorkflowExposers.Name,
				WorkflowExposers.Description,
				WorkflowExposers.Priority,
				WorkflowExposers.IsFavorite,
				WorkflowExposers.PreRoll,
				WorkflowExposers.PostRoll,
				WorkflowExposers.Notes,
				WorkflowExposers.JobTypeCategoryId,
				WorkflowExposers.State,
				WorkflowExposers.Resources,
				WorkflowExposers.ResourcePools);

			var partialWorkflowsById = TestContext.Api.Workflows.ReadPartial(partialFilter).ToDictionary(x => x.Id);
			var workflows = TestContext.Api.Workflows.Read(WorkflowFilter).ToList();

			Assert.AreEqual(workflows.Count, partialWorkflowsById.Count);

			foreach (var workflow in workflows)
			{
				var partialWorkflow = partialWorkflowsById[workflow.Id];

				Assert.AreEqual(workflow.Id, partialWorkflow.GetValue(WorkflowExposers.Id));
				Assert.AreEqual(workflow.Name, partialWorkflow.GetValue(WorkflowExposers.Name));
				Assert.AreEqual(workflow.Description, partialWorkflow.GetValue(WorkflowExposers.Description));
				Assert.AreEqual(workflow.Priority, partialWorkflow.GetValue(WorkflowExposers.Priority));
				Assert.AreEqual(workflow.IsFavorite, partialWorkflow.GetValue(WorkflowExposers.IsFavorite));
				Assert.AreEqual(workflow.PreRoll, partialWorkflow.GetValue(WorkflowExposers.PreRoll));
				Assert.AreEqual(workflow.PostRoll, partialWorkflow.GetValue(WorkflowExposers.PostRoll));
				Assert.AreEqual(workflow.Notes, partialWorkflow.GetValue(WorkflowExposers.Notes));
				Assert.AreEqual(workflow.JobTypeCategoryId, partialWorkflow.GetValue(WorkflowExposers.JobTypeCategoryId));
				Assert.AreEqual(workflow.State, partialWorkflow.GetValue(WorkflowExposers.State));
				CollectionAssert.AreEqual(workflow.NodeGraph.Nodes.OfType<IResourceNode>().Select(x => x.ResourceId).ToList(), partialWorkflow.GetValues(WorkflowExposers.Resources).ToList(), workflow.Name);
				CollectionAssert.AreEqual(workflow.NodeGraph.Nodes.Select(GetResourcePoolId).Where(x => x != Guid.Empty).ToList(), partialWorkflow.GetValues(WorkflowExposers.ResourcePools).ToList(), workflow.Name);
			}

			// The setup must cover the multi-section fields, otherwise the comparison above proves little.
			Assert.IsTrue(workflows.Any(x => x.NodeGraph.Nodes.OfType<IResourceNode>().Any() && x.NodeGraph.Nodes.OfType<IResourcePoolNode>().Any()), "No workflow with both node types.");
			Assert.IsTrue(workflows.Any(x => !x.NodeGraph.Nodes.Any()), "No workflow without nodes.");
		}

		[TestMethod]
		public void ReadPartialWorkflows_UnknownField_ThrowsBeforeReading()
		{
			var unknownField = new Exposer<Workflow, string>(x => x.Name, "Unknown");

			Assert.ThrowsException<NotSupportedException>(() => TestContext.Api.Workflows.ReadPartial(WorkflowFilter.WithFields(unknownField)));
		}

		private static Guid GetResourcePoolId(WorkflowNode node)
		{
			switch (node)
			{
				case IResourceNode resourceNode:
					return resourceNode.ResourcePoolId;
				case IResourcePoolNode resourcePoolNode:
					return resourcePoolNode.ResourcePoolId;
				default:
					return Guid.Empty;
			}
		}

		[TestMethod]
		public void CountWorkflowsWithQuery()
		{
			foreach (var (expectedObjects, query) in WorkflowQueryTestCases)
			{
				QueryAssert.Count(TestContext.Api.Workflows, expectedObjects, query);
			}
		}

		[TestMethod]
		public void ReadWorkflowsPagedWithQuery_DefaultPageSize()
		{
			foreach (var (expectedObjects, query) in WorkflowQueryTestCases)
			{
				QueryAssert.ReadPaged(TestContext.Api.Workflows, expectedObjects, query);
			}
		}

		[TestMethod]
		public void ReadWorkflowsPagedWithQuery_CustomPageSize()
		{
			foreach (var (expectedObjects, query) in WorkflowQueryTestCases)
			{
				QueryAssert.ReadPaged(TestContext.Api.Workflows, expectedObjects, query, 2);
			}
		}

		[TestMethod]
		public void ReadWorkflowsWithLimitedQuery()
		{
			foreach (var (expectedObjects, query) in WorkflowLimitedQueryTestCases)
			{
				QueryAssert.Read(TestContext.Api.Workflows, expectedObjects, query);
			}
		}
	}
}
