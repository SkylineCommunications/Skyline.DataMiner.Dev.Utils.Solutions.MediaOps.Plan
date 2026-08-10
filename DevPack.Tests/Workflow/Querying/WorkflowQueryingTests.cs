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
