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
	public sealed class ReturnToTentativeTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public ReturnToTentativeTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void ReturnToTentative_ConfirmedJob_SetsCoreReservationToPending()
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
			Assert.AreEqual(JobState.Confirmed, confirmedJob.State, "Expected the job to be Confirmed before returning to tentative.");

			var returnedJob = TestContext.Api.Jobs.ReturnToTentative(confirmedJob);
			Assert.IsNotNull(returnedJob, "Expected the job to transition back to the Tentative state.");
			Assert.AreEqual(JobState.Tentative, returnedJob.State, "Expected the job state to be Tentative.");

			var reservations = TestContext.ResourceManagerHelper.GetReservationInstances(
				ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(job.Id))).ToList();

			Assert.AreEqual(1, reservations.Count, "Expected exactly one core reservation for the job.");
			Assert.AreEqual(Skyline.DataMiner.Net.Messages.ReservationStatus.Pending, reservations[0].Status, "Expected the core reservation to be set back to pending.");
		}

		[TestMethod]
		public void ReturnToTentative_TentativeJob_ThrowsInvalidStateError()
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

			var exception = Assert.ThrowsException<MediaOpsException>(() => TestContext.Api.Jobs.ReturnToTentative(tentativeJob));
			Assert.IsTrue(
				exception.TraceData.ErrorData.OfType<JobInvalidStateError>().Any(),
				"Expected a JobInvalidStateError when returning a job that is not in the Confirmed state to tentative.");
		}

		[TestMethod]
		public void ReturnToTentative_DraftJob_ThrowsInvalidStateError()
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

			var exception = Assert.ThrowsException<MediaOpsException>(() => TestContext.Api.Jobs.ReturnToTentative(job));
			Assert.IsTrue(
				exception.TraceData.ErrorData.OfType<JobInvalidStateError>().Any(),
				"Expected a JobInvalidStateError when returning a job that is not in the Confirmed state to tentative.");
		}
	}
}
