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
	public sealed class ConfirmedStateTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public ConfirmedStateTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void Confirm_TentativeJobWithResourceNode_SetsCoreReservationToConfirmed()
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

			var resourceNode = new JobResourceNode(pool, resource);
			job.NodeGraph.Add(resourceNode);

			job = objectCreator.CreateJob(job);

			var tentativeJob = TestContext.Api.Jobs.SaveAsTentative(job);
			Assert.IsNotNull(tentativeJob, "Expected the job to transition to the Tentative state.");

			var confirmedJob = TestContext.Api.Jobs.Confirm(tentativeJob);
			Assert.IsNotNull(confirmedJob, "Expected the job to transition to the Confirmed state.");
			Assert.AreEqual(JobState.Confirmed, confirmedJob.State, "Expected the job state to be Confirmed.");

			var reservations = TestContext.ResourceManagerHelper.GetReservationInstances(
				ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(job.Id))).ToList();

			Assert.AreEqual(1, reservations.Count, "Expected exactly one core reservation for the confirmed job.");
			Assert.AreEqual(Skyline.DataMiner.Net.Messages.ReservationStatus.Confirmed, reservations[0].Status, "Expected the core reservation to be confirmed.");
		}

		[TestMethod]
		public void Confirm_DraftJob_ThrowsInvalidStateError()
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

			var resourceNode = new JobResourceNode(pool, resource);
			job.NodeGraph.Add(resourceNode);

			job = objectCreator.CreateJob(job);

			var exception = Assert.ThrowsException<MediaOpsException>(() => TestContext.Api.Jobs.Confirm(job));
			Assert.IsTrue(
				exception.TraceData.ErrorData.OfType<JobInvalidStateError>().Any(),
				"Expected a JobInvalidStateError when confirming a job that is not in the Tentative state.");
		}

		[TestMethod]
		public void Confirm_TentativeJobWithResourcePoolNode_ThrowsResourceNotAssignedError()
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

			var resourceNode = new JobResourceNode(pool, resource);
			var poolNode = new JobResourcePoolNode(pool);

			job.NodeGraph
				.Add(resourceNode)
				.Add(poolNode)
				.Connect(resourceNode, poolNode);

			job = objectCreator.CreateJob(job);

			var tentativeJob = TestContext.Api.Jobs.SaveAsTentative(job);
			Assert.IsNotNull(tentativeJob, "Expected the job to transition to the Tentative state.");

			var exception = Assert.ThrowsException<MediaOpsException>(() => TestContext.Api.Jobs.Confirm(tentativeJob));
			Assert.IsTrue(
				exception.TraceData.ErrorData.OfType<JobNodeResourceNotAssignedError>().Any(),
				"Expected a JobNodeResourceNotAssignedError when confirming a job that still has a resource pool node.");
		}

		[TestMethod]
		public void Confirm_TentativeJobWithoutNodes_SetsCoreReservationToConfirmed()
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

			var tentativeJob = TestContext.Api.Jobs.SaveAsTentative(job);
			Assert.IsNotNull(tentativeJob, "Expected the job to transition to the Tentative state.");

			var confirmedJob = TestContext.Api.Jobs.Confirm(tentativeJob);
			Assert.IsNotNull(confirmedJob, "Expected the node-less job to transition to the Confirmed state.");
			Assert.AreEqual(JobState.Confirmed, confirmedJob.State, "Expected the job state to be Confirmed.");

			var reservations = TestContext.ResourceManagerHelper.GetReservationInstances(
				ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(job.Id))).ToList();

			Assert.AreEqual(1, reservations.Count, "Expected a core reservation for the confirmed node-less job.");
			Assert.AreEqual(Skyline.DataMiner.Net.Messages.ReservationStatus.Confirmed, reservations[0].Status, "Expected the core reservation to be confirmed.");
		}

		[TestMethod]
		public void Update_ConfirmedJobWithAddedResourcePoolNode_ThrowsResourcePoolNodeNotAllowedError()
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
			Assert.AreEqual(JobState.Confirmed, confirmedJob.State, "Expected the job to be Confirmed before the update.");

			confirmedJob.NodeGraph.Add(new JobResourcePoolNode(pool));

			var exception = Assert.ThrowsException<MediaOpsException>(() => TestContext.Api.Jobs.Update(confirmedJob));
			Assert.IsTrue(
				exception.TraceData.ErrorData.OfType<JobResourcePoolNodeNotAllowedError>().Any(),
				"Expected a JobResourcePoolNodeNotAllowedError when adding a resource pool node to a Confirmed job.");
		}
	}
}
