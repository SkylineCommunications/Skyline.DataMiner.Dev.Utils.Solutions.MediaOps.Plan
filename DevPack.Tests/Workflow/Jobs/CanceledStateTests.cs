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
		public void Cancel_TentativeJob_SetsCoreReservationToCanceled()
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
			Assert.IsNotNull(tentativeJob, "Expected the job to transition to the Tentative state.");

			var canceledJob = TestContext.Api.Jobs.Cancel(tentativeJob);
			Assert.IsNotNull(canceledJob, "Expected the job to transition to the Canceled state.");
			Assert.AreEqual(JobState.Canceled, canceledJob.State, "Expected the job state to be Canceled.");

			var reservations = TestContext.ResourceManagerHelper.GetReservationInstances(
				ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(job.Id))).ToList();

			Assert.AreEqual(1, reservations.Count, "Expected exactly one core reservation for the canceled job.");
			Assert.AreEqual(Skyline.DataMiner.Net.Messages.ReservationStatus.Canceled, reservations[0].Status, "Expected the core reservation to be canceled.");
		}

		[TestMethod]
		public void Cancel_ConfirmedJob_SetsCoreReservationToCanceled()
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
			Assert.AreEqual(JobState.Confirmed, confirmedJob.State, "Expected the job to be Confirmed before cancellation.");

			var canceledJob = TestContext.Api.Jobs.Cancel(confirmedJob);
			Assert.IsNotNull(canceledJob, "Expected the job to transition to the Canceled state.");
			Assert.AreEqual(JobState.Canceled, canceledJob.State, "Expected the job state to be Canceled.");

			var reservations = TestContext.ResourceManagerHelper.GetReservationInstances(
				ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(job.Id))).ToList();

			Assert.AreEqual(1, reservations.Count, "Expected exactly one core reservation for the canceled job.");
			Assert.AreEqual(Skyline.DataMiner.Net.Messages.ReservationStatus.Canceled, reservations[0].Status, "Expected the core reservation to be canceled.");
		}

		[TestMethod]
		public void Cancel_DraftJob_ThrowsInvalidStateError()
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

			var exception = Assert.ThrowsException<MediaOpsException>(() => TestContext.Api.Jobs.Cancel(job));
			Assert.IsTrue(
				exception.TraceData.ErrorData.OfType<JobInvalidStateError>().Any(),
				"Expected a JobInvalidStateError when canceling a job that is not in the Tentative or Confirmed state.");
		}
	}
}
