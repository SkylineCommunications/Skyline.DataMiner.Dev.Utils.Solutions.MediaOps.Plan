namespace RT_MediaOps.Plan.Workflow.Filtering
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	using Workflow = Skyline.DataMiner.Solutions.MediaOps.Plan.API.Workflow;

	[TestClass]
	[TestCategory("IntegrationTest")]
	public sealed class WorkflowFilteringTests
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
		/// Gets the expected objects mapped to the applied filter.
		/// </summary>
		private Tuple<Workflow[], FilterElement<Workflow>>[] WorkflowFilterTestCases => new[]
		{
			new Tuple<Workflow[], FilterElement<Workflow>>(Setup.Workflows, WorkflowFilter),
			new Tuple<Workflow[], FilterElement<Workflow>>([Setup.DraftWorkflow1!, Setup.DraftWorkflow2!], WorkflowFilter.AND(WorkflowExposers.Name.Contains("Workflow_Draft"))),

			new Tuple<Workflow[], FilterElement<Workflow>>([Setup.DraftWorkflow1!], WorkflowFilter.AND(WorkflowExposers.Description.Equal("First draft workflow"))),
			new Tuple<Workflow[], FilterElement<Workflow>>([Setup.DraftWorkflow1!, Setup.DraftWorkflow2!], WorkflowFilter.AND(WorkflowExposers.Description.Contains("draft workflow"))),
			new Tuple<Workflow[], FilterElement<Workflow>>([], WorkflowFilter.AND(WorkflowExposers.Description.Contains("Unknown description"))),

			new Tuple<Workflow[], FilterElement<Workflow>>([Setup.CompleteWorkflow3!], WorkflowFilter.AND(WorkflowExposers.Notes.Equal("Notes of the third workflow"))),
			new Tuple<Workflow[], FilterElement<Workflow>>(Setup.Workflows, WorkflowFilter.AND(WorkflowExposers.Notes.Contains("Notes of the"))),
			new Tuple<Workflow[], FilterElement<Workflow>>([], WorkflowFilter.AND(WorkflowExposers.Notes.Contains("Unknown notes"))),

			new Tuple<Workflow[], FilterElement<Workflow>>([Setup.DraftWorkflow1!, Setup.CompleteWorkflow3!], WorkflowFilter.AND(WorkflowExposers.IsFavorite.Equal(true))),
			new Tuple<Workflow[], FilterElement<Workflow>>([Setup.DraftWorkflow2!], WorkflowFilter.AND(WorkflowExposers.IsFavorite.Equal(false))),

			new Tuple<Workflow[], FilterElement<Workflow>>([Setup.DraftWorkflow1!], WorkflowFilter.AND(WorkflowExposers.Priority.Equal(WorkflowPriority.High))),
			new Tuple<Workflow[], FilterElement<Workflow>>([Setup.DraftWorkflow2!], WorkflowFilter.AND(WorkflowExposers.Priority.Equal(WorkflowPriority.Normal))),
			new Tuple<Workflow[], FilterElement<Workflow>>([Setup.CompleteWorkflow3!], WorkflowFilter.AND(WorkflowExposers.Priority.Equal(WorkflowPriority.Low))),
			new Tuple<Workflow[], FilterElement<Workflow>>([Setup.DraftWorkflow2!, Setup.CompleteWorkflow3!], WorkflowFilter.AND(WorkflowExposers.Priority.NotEqual(WorkflowPriority.High))),

			new Tuple<Workflow[], FilterElement<Workflow>>([Setup.DraftWorkflow1!, Setup.CompleteWorkflow3!], WorkflowFilter.AND(WorkflowExposers.PreRoll.Equal(TimeSpan.FromSeconds(30)))),
			new Tuple<Workflow[], FilterElement<Workflow>>([Setup.DraftWorkflow2!], WorkflowFilter.AND(WorkflowExposers.PreRoll.Equal(TimeSpan.FromSeconds(45)))),
			new Tuple<Workflow[], FilterElement<Workflow>>([], WorkflowFilter.AND(WorkflowExposers.PreRoll.Equal(TimeSpan.FromSeconds(1)))),

			new Tuple<Workflow[], FilterElement<Workflow>>([Setup.DraftWorkflow1!], WorkflowFilter.AND(WorkflowExposers.PostRoll.Equal(TimeSpan.FromSeconds(60)))),
			new Tuple<Workflow[], FilterElement<Workflow>>([Setup.CompleteWorkflow3!], WorkflowFilter.AND(WorkflowExposers.PostRoll.Equal(TimeSpan.FromSeconds(120)))),
			new Tuple<Workflow[], FilterElement<Workflow>>([], WorkflowFilter.AND(WorkflowExposers.PostRoll.Equal(TimeSpan.FromSeconds(1)))),

			new Tuple<Workflow[], FilterElement<Workflow>>([Setup.DraftWorkflow1!, Setup.DraftWorkflow2!], WorkflowFilter.AND(WorkflowExposers.State.Equal(WorkflowState.Draft))),
			new Tuple<Workflow[], FilterElement<Workflow>>([Setup.CompleteWorkflow3!], WorkflowFilter.AND(WorkflowExposers.State.Equal(WorkflowState.Complete))),
		};

		[TestMethod]
		public void ReadWorkflowsWithFilter()
		{
			foreach (var (expectedObjects, filter) in WorkflowFilterTestCases)
			{
				var expectedObjectIds = expectedObjects.Select(x => x.Id).OrderBy(x => x).ToList();
				var actualObjectIds = TestContext.Api.Workflows.Read(filter).Select(x => x.Id).OrderBy(x => x).ToList();

				Assert.IsTrue(expectedObjectIds.SequenceEqual(actualObjectIds), filter.ToString());
			}
		}

		[TestMethod]
		public void CountWorkflowsWithFilter()
		{
			foreach (var (expectedObjects, filter) in WorkflowFilterTestCases)
			{
				Assert.AreEqual(expectedObjects.Length, TestContext.Api.Workflows.Count(filter), filter.ToString());
			}
		}

		[TestMethod]
		public void ReadWorkflowsPagedWithFilter_DefaultPageSize()
		{
			foreach (var (expectedObjects, filter) in WorkflowFilterTestCases)
			{
				var expectedObjectIds = expectedObjects.Select(x => x.Id).OrderBy(x => x).ToList();
				var pages = TestContext.Api.Workflows.ReadPaged(filter).ToList();
				var actualObjectIds = pages.SelectMany(x => x).Select(x => x.Id).OrderBy(x => x).ToList();

				Assert.AreEqual(1, pages.Count, filter.ToString());
				Assert.IsTrue(expectedObjectIds.SequenceEqual(actualObjectIds), filter.ToString());
			}
		}

		[TestMethod]
		public void ReadWorkflowsPagedWithFilter_CustomPageSize()
		{
			foreach (var (expectedObjects, filter) in WorkflowFilterTestCases)
			{
				var expectedObjectIds = expectedObjects.Select(x => x.Id).OrderBy(x => x).ToList();
				var pages = TestContext.Api.Workflows.ReadPaged(filter, 2).ToList();
				var actualObjectIds = pages.SelectMany(x => x).Select(x => x.Id).OrderBy(x => x).ToList();

				Assert.IsTrue(expectedObjectIds.SequenceEqual(actualObjectIds), filter.ToString());

				foreach (var page in pages)
				{
					Assert.IsTrue(page.Count() <= 2, filter.ToString());
				}
			}
		}
	}
}
