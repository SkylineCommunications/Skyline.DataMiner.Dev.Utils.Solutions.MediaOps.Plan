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
	public sealed class ManualStartTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public ManualStartTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		/*[TestMethod]
		public void Start_ConfirmedJobWithPreRoll_MovesTimingsToNowAndKeepsStart()
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
				Start = currentTime.AddMinutes(10),
				End = currentTime.AddMinutes(20),
				PreRollStart = currentTime.AddMinutes(9),
				PostRollEnd = currentTime.AddMinutes(20),
			};

			job.NodeGraph.Add(new JobResourceNode(pool, resource));
			job = objectCreator.CreateJob(job);

			var tentativeJob = TestContext.Api.Jobs.SaveAsTentative(job);
			var confirmedJob = TestContext.Api.Jobs.Confirm(tentativeJob);
			Assert.AreEqual(JobState.Confirmed, confirmedJob.State, "Expected the job to be Confirmed before the manual start.");

			var startedJob = TestContext.Api.Jobs.Start(confirmedJob);
			Assert.IsNotNull(startedJob, "Expected the manual start to return the updated job.");

			// The pre-roll and every node start are moved to (approximately) now; the original future pre-roll was at +9 minutes.
			Assert.IsTrue(
				startedJob.PreRollStart < currentTime.AddMinutes(9),
				"Expected the pre-roll start to be moved earlier than the original future pre-roll start.");
			Assert.IsTrue(
				startedJob.NodeGraph.Nodes.All(x => x.Start < currentTime.AddMinutes(9)),
				"Expected every node start to be moved earlier than the original future pre-roll start.");

			// The current start time is kept because a pre-roll is configured and no new start time was provided.
			Assert.AreEqual(
				currentTime.AddMinutes(10),
				startedJob.Start.UtcDateTime,
				"Expected the job start time to be kept when a pre-roll is configured and no new start time is provided.");

			var reservations = TestContext.ResourceManagerHelper.GetReservationInstances(
				ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(job.Id))).ToList();

			Assert.AreEqual(1, reservations.Count, "Expected exactly one core reservation for the started job.");
			Assert.IsTrue(
				reservations[0].Start < currentTime.AddMinutes(9),
				"Expected the core reservation start to be moved earlier than the original future pre-roll start.");

			// The pre-roll start reflects the actual persisted core reservation start.
			Assert.AreEqual(
				reservations[0].Start,
				startedJob.PreRollStart.UtcDateTime,
				"Expected the pre-roll start to equal the core reservation start.");
		}

		[TestMethod]
		public void Start_ConfirmedJobWithoutPreRoll_UpdatesJobStartToReservationStart()
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
				Start = currentTime.AddMinutes(10),
				End = currentTime.AddMinutes(20),
				PreRollStart = currentTime.AddMinutes(10),
				PostRollEnd = currentTime.AddMinutes(20),
			};

			job.NodeGraph.Add(new JobResourceNode(pool, resource));
			job = objectCreator.CreateJob(job);

			var tentativeJob = TestContext.Api.Jobs.SaveAsTentative(job);
			var confirmedJob = TestContext.Api.Jobs.Confirm(tentativeJob);

			var startedJob = TestContext.Api.Jobs.Start(confirmedJob);
			Assert.IsNotNull(startedJob, "Expected the manual start to return the updated job.");

			// Without a pre-roll the start follows the reservation start, so start and pre-roll start stay equal and both move to now.
			Assert.AreEqual(
				startedJob.PreRollStart,
				startedJob.Start,
				"Expected the job start to stay equal to the pre-roll start when no pre-roll is configured.");
			Assert.IsTrue(
				startedJob.Start < currentTime.AddMinutes(10),
				"Expected the job start to be moved to the reservation start when no pre-roll is configured.");

			var reservations = TestContext.ResourceManagerHelper.GetReservationInstances(
				ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(job.Id))).ToList();

			Assert.AreEqual(1, reservations.Count, "Expected exactly one core reservation for the started job.");

			// Both the pre-roll start and the job start reflect the actual persisted core reservation start.
			Assert.AreEqual(
				reservations[0].Start,
				startedJob.PreRollStart.UtcDateTime,
				"Expected the pre-roll start to equal the core reservation start.");
			Assert.AreEqual(
				reservations[0].Start,
				startedJob.Start.UtcDateTime,
				"Expected the job start to equal the core reservation start when no pre-roll is configured.");
		}

		[TestMethod]
		public void Start_ConfirmedJobWithoutPreRollAndNewStartTime_DecouplesStartFromPreRoll()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(pool);
			resource = objectCreator.CreateResource(resource);
			resource = TestContext.Api.Resources.Complete(resource);

			// No pre-roll: the pre-roll start equals the start.
			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime.AddMinutes(10),
				End = currentTime.AddMinutes(20),
				PreRollStart = currentTime.AddMinutes(10),
				PostRollEnd = currentTime.AddMinutes(20),
			};

			job.NodeGraph.Add(new JobResourceNode(pool, resource));
			job = objectCreator.CreateJob(job);

			var tentativeJob = TestContext.Api.Jobs.SaveAsTentative(job);
			var confirmedJob = TestContext.Api.Jobs.Confirm(tentativeJob);

			var newStartTime = currentTime.AddMinutes(5);
			var startedJob = TestContext.Api.Jobs.Start(confirmedJob, new JobStartOptions { NewStartTime = newStartTime });
			Assert.IsNotNull(startedJob, "Expected the manual start to return the updated job.");

			Assert.AreEqual(
				newStartTime,
				startedJob.Start.UtcDateTime,
				"Expected the job start time to be replaced with the provided new start time.");
			Assert.IsTrue(
				startedJob.PreRollStart < currentTime.AddMinutes(10),
				"Expected the pre-roll start to be moved to the reservation start.");

			// A new start time was provided, so the start no longer follows the pre-roll start even though there was no pre-roll.
			Assert.AreNotEqual(
				startedJob.PreRollStart,
				startedJob.Start,
				"Expected the job start to be decoupled from the pre-roll start when a new start time is provided.");

			var reservations = TestContext.ResourceManagerHelper.GetReservationInstances(
				ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(job.Id))).ToList();

			Assert.AreEqual(1, reservations.Count, "Expected exactly one core reservation for the started job.");
			Assert.AreEqual(
				reservations[0].Start,
				startedJob.PreRollStart.UtcDateTime,
				"Expected the pre-roll start to equal the core reservation start.");
		}

		[TestMethod]
		public void Start_ConfirmedJobWithPreRollAndNewStartTime_AdaptsJobStartAndMovesPreRoll()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(pool);
			resource = objectCreator.CreateResource(resource);
			resource = TestContext.Api.Resources.Complete(resource);

			// A pre-roll is configured: the pre-roll start precedes the start.
			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime.AddMinutes(10),
				End = currentTime.AddMinutes(20),
				PreRollStart = currentTime.AddMinutes(9),
				PostRollEnd = currentTime.AddMinutes(20),
			};

			job.NodeGraph.Add(new JobResourceNode(pool, resource));
			job = objectCreator.CreateJob(job);

			var tentativeJob = TestContext.Api.Jobs.SaveAsTentative(job);
			var confirmedJob = TestContext.Api.Jobs.Confirm(tentativeJob);

			var newStartTime = currentTime.AddMinutes(5);
			var startedJob = TestContext.Api.Jobs.Start(confirmedJob, new JobStartOptions { NewStartTime = newStartTime });
			Assert.IsNotNull(startedJob, "Expected the manual start to return the updated job.");

			Assert.AreEqual(
				newStartTime,
				startedJob.Start.UtcDateTime,
				"Expected the job start time to be replaced with the provided new start time.");
			Assert.IsTrue(
				startedJob.PreRollStart < currentTime.AddMinutes(9),
				"Expected the pre-roll start to be moved to the reservation start.");

			var reservations = TestContext.ResourceManagerHelper.GetReservationInstances(
				ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(job.Id))).ToList();

			Assert.AreEqual(1, reservations.Count, "Expected exactly one core reservation for the started job.");

			// Only the pre-roll start follows the reservation start; the start reflects the provided new start time.
			Assert.AreEqual(
				reservations[0].Start,
				startedJob.PreRollStart.UtcDateTime,
				"Expected the pre-roll start to equal the core reservation start.");
			Assert.AreNotEqual(
				reservations[0].Start,
				startedJob.Start.UtcDateTime,
				"Expected the job start to reflect the new start time rather than the reservation start.");
		}*/

		[TestMethod]
		public void Start_DraftJob_ThrowsInvalidStateError()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime.AddMinutes(10),
				End = currentTime.AddMinutes(20),
				PreRollStart = currentTime.AddMinutes(10),
				PostRollEnd = currentTime.AddMinutes(20),
			};

			job = objectCreator.CreateJob(job);

			var exception = Assert.ThrowsException<MediaOpsException>(() => TestContext.Api.Jobs.Start(job));
			Assert.IsTrue(
				exception.TraceData.ErrorData.OfType<JobInvalidStateError>().Any(),
				"Expected a JobInvalidStateError when manually starting a job that is not in the Confirmed state.");
		}

		[TestMethod]
		public void Start_TentativeJob_ThrowsInvalidStateError()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime.AddMinutes(10),
				End = currentTime.AddMinutes(20),
				PreRollStart = currentTime.AddMinutes(10),
				PostRollEnd = currentTime.AddMinutes(20),
			};

			job = objectCreator.CreateJob(job);

			var tentativeJob = TestContext.Api.Jobs.SaveAsTentative(job);

			var exception = Assert.ThrowsException<MediaOpsException>(() => TestContext.Api.Jobs.Start(tentativeJob));
			Assert.IsTrue(
				exception.TraceData.ErrorData.OfType<JobInvalidStateError>().Any(),
				"Expected a JobInvalidStateError when manually starting a job that is not in the Confirmed state.");
		}

		[TestMethod]
		public void Start_WithNewStartTimeInPast_ThrowsInvalidStartTimeError()
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
				Start = currentTime.AddMinutes(10),
				End = currentTime.AddMinutes(20),
				PreRollStart = currentTime.AddMinutes(10),
				PostRollEnd = currentTime.AddMinutes(20),
			};

			job.NodeGraph.Add(new JobResourceNode(pool, resource));
			job = objectCreator.CreateJob(job);

			var tentativeJob = TestContext.Api.Jobs.SaveAsTentative(job);
			var confirmedJob = TestContext.Api.Jobs.Confirm(tentativeJob);

			var options = new JobStartOptions { NewStartTime = currentTime.AddMinutes(-5) };

			var exception = Assert.ThrowsException<MediaOpsException>(() => TestContext.Api.Jobs.Start(confirmedJob, options));
			Assert.IsTrue(
				exception.TraceData.ErrorData.OfType<JobInvalidStartTimeError>().Any(),
				"Expected a JobInvalidStartTimeError when the new start time lies in the past.");
		}

		[TestMethod]
		public void Start_WithNewStartTimeAfterEnd_ThrowsInvalidStartTimeError()
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
				Start = currentTime.AddMinutes(10),
				End = currentTime.AddMinutes(20),
				PreRollStart = currentTime.AddMinutes(10),
				PostRollEnd = currentTime.AddMinutes(20),
			};

			job.NodeGraph.Add(new JobResourceNode(pool, resource));
			job = objectCreator.CreateJob(job);

			var tentativeJob = TestContext.Api.Jobs.SaveAsTentative(job);
			var confirmedJob = TestContext.Api.Jobs.Confirm(tentativeJob);

			var options = new JobStartOptions { NewStartTime = currentTime.AddMinutes(30) };

			var exception = Assert.ThrowsException<MediaOpsException>(() => TestContext.Api.Jobs.Start(confirmedJob, options));
			Assert.IsTrue(
				exception.TraceData.ErrorData.OfType<JobInvalidStartTimeError>().Any(),
				"Expected a JobInvalidStartTimeError when the new start time is later than the job's end time.");
		}

		[TestMethod]
		public void Start_WithNewStartTimeWithinGuardTime_ThrowsInvalidStartTimeError()
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
				Start = currentTime.AddMinutes(10),
				End = currentTime.AddMinutes(20),
				PreRollStart = currentTime.AddMinutes(10),
				PostRollEnd = currentTime.AddMinutes(20),
			};

			job.NodeGraph.Add(new JobResourceNode(pool, resource));
			job = objectCreator.CreateJob(job);

			var tentativeJob = TestContext.Api.Jobs.SaveAsTentative(job);
			var confirmedJob = TestContext.Api.Jobs.Confirm(tentativeJob);

			// A new start time in the future but within the guard time window must be rejected.
			var options = new JobStartOptions { NewStartTime = currentTime.AddSeconds(JobNodeTimingResolver.GuardTime.TotalSeconds - 2) };

			var exception = Assert.ThrowsException<MediaOpsException>(() => TestContext.Api.Jobs.Start(confirmedJob, options));
			Assert.IsTrue(
				exception.TraceData.ErrorData.OfType<JobInvalidStartTimeError>().Any(),
				"Expected a JobInvalidStartTimeError when the new start time is within the guard time from now.");
		}

		[TestMethod]
		public void Start_WithNewStartTimeWithinGuardTimeOfEnd_ThrowsInvalidStartTimeError()
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
				Start = currentTime.AddMinutes(10),
				End = currentTime.AddMinutes(20),
				PreRollStart = currentTime.AddMinutes(10),
				PostRollEnd = currentTime.AddMinutes(20),
			};

			job.NodeGraph.Add(new JobResourceNode(pool, resource));
			job = objectCreator.CreateJob(job);

			var tentativeJob = TestContext.Api.Jobs.SaveAsTentative(job);
			var confirmedJob = TestContext.Api.Jobs.Confirm(tentativeJob);

			// A new start time that leaves less than the guard time until the job's end must be rejected.
			var options = new JobStartOptions { NewStartTime = currentTime.AddMinutes(20).AddSeconds(-(JobNodeTimingResolver.GuardTime.TotalSeconds - 2)) };

			var exception = Assert.ThrowsException<MediaOpsException>(() => TestContext.Api.Jobs.Start(confirmedJob, options));
			Assert.IsTrue(
				exception.TraceData.ErrorData.OfType<JobInvalidStartTimeError>().Any(),
				"Expected a JobInvalidStartTimeError when the new start time is within the guard time of the job's end.");
		}
	}
}
