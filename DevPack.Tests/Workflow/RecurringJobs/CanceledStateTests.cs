namespace RT_MediaOps.Plan.Workflow.RecurringJobs
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	using RecurringJobBehavior = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.SlcWorkflowIds.Behaviors.Recurringjob_Behavior;

	[TestClass]
	[TestCategory("IntegrationTest")]
	[DoNotParallelize]
	public sealed class CanceledStateTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public CanceledStateTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void Cancel_ActiveRecurringJob_TransitionsToCancelled()
		{
			var recurringJob = objectCreator.CreateRecurringJob(NewValidRecurringJob($"{Guid.NewGuid()}_RecurringJob"));
			Assert.AreEqual(RecurringJobState.Active, recurringJob.State, "Expected the recurring job to be Active before cancellation.");

			var cancelled = TestContext.Api.RecurringJobs.Cancel(recurringJob);

			Assert.IsNotNull(cancelled);
			Assert.AreEqual(RecurringJobState.Cancelled, cancelled.State, "Expected the recurring job state to be Cancelled.");

			var reread = TestContext.Api.RecurringJobs.Read(recurringJob.Id);
			Assert.AreEqual(RecurringJobState.Cancelled, reread.State);

			var planApi = (MediaOpsPlanApi)TestContext.Api;
			var instance = planApi.DomHelpers.SlcWorkflowHelper.GetRecurringJobs(new[] { recurringJob.Id }).Single();
			Assert.AreEqual(RecurringJobBehavior.StatusesEnum.Cancelled, instance.Status, "Expected the backend DOM instance status to be Cancelled.");
		}

		[TestMethod]
		public void Cancel_ById_TransitionsToCancelled()
		{
			var recurringJob = objectCreator.CreateRecurringJob(NewValidRecurringJob($"{Guid.NewGuid()}_RecurringJob"));

			var cancelled = TestContext.Api.RecurringJobs.Cancel(recurringJob.Id);

			Assert.IsNotNull(cancelled);
			Assert.AreEqual(RecurringJobState.Cancelled, cancelled.State);
		}

		[TestMethod]
		public void Cancel_UnknownRecurringJob_ReturnsNull()
		{
			var result = TestContext.Api.RecurringJobs.Cancel(Guid.NewGuid());

			Assert.IsNull(result, "Expected null when cancelling a recurring job that does not exist.");
		}

		[TestMethod]
		public void Cancel_AlreadyCancelledRecurringJob_ThrowsInvalidStateError()
		{
			var recurringJob = objectCreator.CreateRecurringJob(NewValidRecurringJob($"{Guid.NewGuid()}_RecurringJob"));
			TestContext.Api.RecurringJobs.Cancel(recurringJob);

			var exception = Assert.ThrowsException<MediaOpsException>(() => TestContext.Api.RecurringJobs.Cancel(recurringJob.Id));
			Assert.IsTrue(
				exception.TraceData.ErrorData.OfType<RecurringJobInvalidStateError>().Any(),
				"Expected a RecurringJobInvalidStateError when cancelling a recurring job that is not Active.");
		}

		[TestMethod]
		public void Cancel_CompletedRecurringJob_ThrowsInvalidStateError()
		{
			var recurringJob = objectCreator.CreateRecurringJob(NewValidRecurringJob($"{Guid.NewGuid()}_RecurringJob"));
			objectCreator.CompleteRecurringJob(TestContext.Api.RecurringJobs.Read(recurringJob.Id));

			var exception = Assert.ThrowsException<MediaOpsException>(() => TestContext.Api.RecurringJobs.Cancel(recurringJob.Id));
			Assert.IsTrue(
				exception.TraceData.ErrorData.OfType<RecurringJobInvalidStateError>().Any(),
				"Expected a RecurringJobInvalidStateError when cancelling a completed recurring job.");
		}

		private static RecurringJob NewValidRecurringJob(string name)
		{
			var recurringJob = new RecurringJob { Name = name, Start = DateTime.UtcNow.AddHours(1) };
			recurringJob.Pattern.RepeatType = RepeatType.Daily;
			recurringJob.Pattern.RepeatEvery = 1;
			return recurringJob;
		}
	}
}
