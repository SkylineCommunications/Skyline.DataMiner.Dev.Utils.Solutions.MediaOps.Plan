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
	public sealed class DeleteJobTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public DeleteJobTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void Delete_ConfirmedJob_WithoutForce_ThrowsInvalidStateError()
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
			Assert.AreEqual(JobState.Confirmed, confirmedJob.State, "Expected the job to be Confirmed before deletion.");

			var exception = Assert.ThrowsException<MediaOpsException>(() => TestContext.Api.Jobs.Delete(confirmedJob.Id));
			Assert.IsTrue(
				exception.TraceData.ErrorData.OfType<JobInvalidStateError>().Any(),
				"Expected a JobInvalidStateError when deleting a Confirmed job without the force option.");

			Assert.IsNotNull(TestContext.Api.Jobs.Read(confirmedJob.Id), "Expected the job to still exist after a failed delete.");
		}

		[TestMethod]
		public void Delete_ConfirmedJob_WithForce_RemovesJobAndReservation()
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
			Assert.AreEqual(JobState.Confirmed, confirmedJob.State, "Expected the job to be Confirmed before deletion.");

			TestContext.Api.Jobs.Delete(confirmedJob, new JobDeleteOptions { ForceDelete = true });

			Assert.IsNull(TestContext.Api.Jobs.Read(confirmedJob.Id), "Expected the job to be deleted when the force option is used.");

			var reservations = TestContext.ResourceManagerHelper.GetReservationInstances(
				ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(job.Id))).ToList();

			Assert.AreEqual(0, reservations.Count, "Expected the core reservation to be removed when force deleting the job.");
		}

		[TestMethod]
		public void Delete_CanceledJob_RemovesJobAndReservation()
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
			var canceledJob = TestContext.Api.Jobs.Cancel(tentativeJob);
			Assert.AreEqual(JobState.Canceled, canceledJob.State, "Expected the job to be Canceled before deletion.");

			TestContext.Api.Jobs.Delete(canceledJob.Id);

			Assert.IsNull(TestContext.Api.Jobs.Read(canceledJob.Id), "Expected a Canceled job to be deleted without the force option.");

			var reservations = TestContext.ResourceManagerHelper.GetReservationInstances(
				ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(job.Id))).ToList();

			Assert.AreEqual(0, reservations.Count, "Expected the core reservation to be removed when deleting the canceled job.");
		}

		[TestMethod]
		public void Delete_CompletedJob_RemovesJob()
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
			Assert.AreEqual(JobState.Completed, completedJob.State, "Expected the job to be Completed before deletion.");

			TestContext.Api.Jobs.Delete(completedJob.Id);

			Assert.IsNull(TestContext.Api.Jobs.Read(completedJob.Id), "Expected a Completed job to be deleted without the force option.");
		}
	}
}
