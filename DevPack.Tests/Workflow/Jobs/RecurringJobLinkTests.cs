namespace RT_MediaOps.Plan.Workflow.Jobs
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
	public sealed class RecurringJobLinkTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public RecurringJobLinkTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void UpdateJob_SetRecurringJobIdOfRecurringJobCreatedFromJob_LinksJobToRecurringJob()
		{
			var job = objectCreator.CreateJob(NewValidJob());
			Assert.AreEqual(Guid.Empty, job.RecurringJobId);

			var recurringJob = RecurringJob.FromJob(job);
			recurringJob.Pattern.RepeatType = RepeatType.Daily;
			recurringJob.Pattern.RepeatEvery = 1;
			recurringJob.Pattern.EndDate = job.Start.AddDays(10);
			recurringJob = objectCreator.CreateRecurringJob(recurringJob);

			job.RecurringJobId = recurringJob.Id;
			var updatedJob = TestContext.Api.Jobs.Update(job);

			Assert.AreEqual(recurringJob.Id, updatedJob.RecurringJobId);
			Assert.AreEqual(recurringJob.Id, TestContext.Api.Jobs.Read(job.Id).RecurringJobId);

			var jobsInSeries = TestContext.Api.Jobs.Read(JobExposers.RecurringJobId.Equal(recurringJob.Id)).ToList();
			Assert.IsTrue(jobsInSeries.Any(x => x.Id == job.Id), "Expected the job to be found when filtering on the recurring job ID.");
		}

		[TestMethod]
		public void CreateJob_WithUnknownRecurringJobId_ThrowsRecurringJobNotFoundError()
		{
			var unknownRecurringJobId = Guid.NewGuid();
			var job = NewValidJob();
			job.RecurringJobId = unknownRecurringJobId;

			var exception = Assert.ThrowsException<MediaOpsException>(() => objectCreator.CreateJob(job));

			var error = exception.TraceData.ErrorData.OfType<JobRecurringJobNotFoundError>().SingleOrDefault();
			Assert.IsNotNull(error, "Expected a JobRecurringJobNotFoundError.");
			Assert.AreEqual(unknownRecurringJobId, error.RecurringJobId);
		}

		private static Job NewValidJob()
		{
			var start = DateTime.UtcNow.RoundToNextSecond().AddHours(1);
			return new Job
			{
				Name = $"{Guid.NewGuid()}_Job",
				Start = start,
				End = start.AddMinutes(5),
				PreRollStart = start,
				PostRollEnd = start.AddMinutes(5),
			};
		}
	}
}
