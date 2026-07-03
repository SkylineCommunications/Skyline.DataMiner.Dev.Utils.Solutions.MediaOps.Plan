namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.Extensions;
	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.ResourceManager.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

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
		public void MarkAsCompleted_DraftJobWithEndInPast_TransitionsToCompleted()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime.AddMinutes(-20),
				End = currentTime.AddMinutes(-10),
				PreRollStart = currentTime.AddMinutes(-20),
				PostRollEnd = currentTime.AddMinutes(-10),
			};

			job = objectCreator.CreateJob(job);

			var completedJob = TestContext.Api.Jobs.MarkAsCompleted(job);
			Assert.IsNotNull(completedJob, "Expected the job to transition to the Completed state.");
			Assert.AreEqual(JobState.Completed, completedJob.State, "Expected the job state to be Completed.");

			var reservations = TestContext.ResourceManagerHelper.GetReservationInstances(
				ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(job.Id))).ToList();

			Assert.AreEqual(0, reservations.Count, "Expected no core reservation for a completed draft job.");
		}

		[TestMethod]
		public void MarkAsCompleted_JobWithEndInFuture_ThrowsInvalidEndTimeError()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(10),
				PreRollStart = currentTime,
				PostRollEnd = currentTime.AddMinutes(10),
			};

			job = objectCreator.CreateJob(job);

			var exception = Assert.ThrowsException<MediaOpsException>(() => TestContext.Api.Jobs.MarkAsCompleted(job));
			Assert.IsTrue(
				exception.TraceData.ErrorData.OfType<JobInvalidEndTimeError>().Any(),
				"Expected a JobInvalidEndTimeError when marking a job whose end time is not in the past as completed.");
		}

		[TestMethod]
		public void MarkAsCompleted_ConfirmedJob_ThrowsInvalidStateError()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(pool);
			resource = objectCreator.CreateResource(resource);
			resource = TestContext.Api.Resources.Complete(resource);

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(10),
				PreRollStart = currentTime,
				PostRollEnd = currentTime.AddMinutes(10),
			};

			job.NodeGraph.Add(new JobResourceNode(pool, resource));
			job = objectCreator.CreateJob(job);

			var tentativeJob = TestContext.Api.Jobs.SaveAsTentative(job);
			var confirmedJob = TestContext.Api.Jobs.Confirm(tentativeJob);
			Assert.AreEqual(JobState.Confirmed, confirmedJob.State, "Expected the job to be Confirmed before the mark-as-completed attempt.");

			var exception = Assert.ThrowsException<MediaOpsException>(() => TestContext.Api.Jobs.MarkAsCompleted(confirmedJob));
			Assert.IsTrue(
				exception.TraceData.ErrorData.OfType<JobInvalidStateError>().Any(),
				"Expected a JobInvalidStateError when marking a job that is not in the Draft or Tentative state as completed.");
		}
	}
}
