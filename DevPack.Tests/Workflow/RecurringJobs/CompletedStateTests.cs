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
	public sealed class CompletedStateTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public CompletedStateTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void Complete_ActiveRecurringJob_TransitionsToCompleted()
		{
			var recurringJob = objectCreator.CreateRecurringJob(NewValidRecurringJob($"{Guid.NewGuid()}_RecurringJob"));
			Assert.AreEqual(RecurringJobState.Active, recurringJob.State, "Expected the recurring job to be Active before completion.");

			var completed = objectCreator.CompleteRecurringJob(TestContext.Api.RecurringJobs.Read(recurringJob.Id));

			Assert.IsNotNull(completed);
			Assert.AreEqual(RecurringJobState.Completed, completed.State, "Expected the recurring job state to be Completed.");

			var reread = TestContext.Api.RecurringJobs.Read(recurringJob.Id);
			Assert.AreEqual(RecurringJobState.Completed, reread.State);

			var planApi = (MediaOpsPlanApi)TestContext.Api;
			var instance = planApi.DomHelpers.SlcWorkflowHelper.GetRecurringJobs(new[] { recurringJob.Id }).Single();
			Assert.AreEqual(RecurringJobBehavior.StatusesEnum.Completed, instance.Status, "Expected the backend DOM instance status to be Completed.");
		}

		[TestMethod]
		public void Complete_AlreadyCompletedRecurringJob_ThrowsInvalidStateError()
		{
			var recurringJob = objectCreator.CreateRecurringJob(NewValidRecurringJob($"{Guid.NewGuid()}_RecurringJob"));
			objectCreator.CompleteRecurringJob(TestContext.Api.RecurringJobs.Read(recurringJob.Id));

			var completed = TestContext.Api.RecurringJobs.Read(recurringJob.Id);

			var exception = Assert.ThrowsException<MediaOpsException>(() => objectCreator.CompleteRecurringJob(completed));
			Assert.IsTrue(
				exception.TraceData.ErrorData.OfType<RecurringJobInvalidStateError>().Any(),
				"Expected a RecurringJobInvalidStateError when completing a recurring job that is not Active.");
		}

		[TestMethod]
		public void Complete_CancelledRecurringJob_ThrowsInvalidStateError()
		{
			var recurringJob = objectCreator.CreateRecurringJob(NewValidRecurringJob($"{Guid.NewGuid()}_RecurringJob"));
			TestContext.Api.RecurringJobs.Cancel(recurringJob);

			var cancelled = TestContext.Api.RecurringJobs.Read(recurringJob.Id);

			var exception = Assert.ThrowsException<MediaOpsException>(() => objectCreator.CompleteRecurringJob(cancelled));
			Assert.IsTrue(
				exception.TraceData.ErrorData.OfType<RecurringJobInvalidStateError>().Any(),
				"Expected a RecurringJobInvalidStateError when completing a cancelled recurring job.");
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
