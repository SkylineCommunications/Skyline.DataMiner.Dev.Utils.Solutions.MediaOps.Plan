namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.Extensions;

	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.ResourceManager.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Simulation;

	using ResourcePool = Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool;

	/// <summary>
	/// Deterministic, simulation-backed tests for the confirmed-to-running transition. Unlike the live-agent
	/// integration tests, these do not depend on SRM promoting the reservation to <see cref="ReservationStatus.Ongoing"/>
	/// on its own timeline, so the reservation status is controlled explicitly and there is no race with the scheduling
	/// script.
	/// </summary>
	[TestClass]
	public sealed class TransitionToRunningSimulationTests
	{
		private static (IMediaOpsPlanApi Api, ResourceManagerHelper ResourceManagerHelper) CreateContext()
		{
			var dms = MediaOpsPlanSimulation.Create();
			var connection = dms.CreateConnection();

			return (connection.GetMediaOpsPlanApi(), new ResourceManagerHelper(connection.HandleSingleResponseMessage));
		}

		private static Job CreateConfirmedStartedJob(IMediaOpsPlanApi api, out Guid jobId)
		{
			return api.Jobs.Start(CreateConfirmedJob(api, out jobId));
		}

		private static Job CreateConfirmedJob(IMediaOpsPlanApi api, out Guid jobId)
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = api.ResourcePools.Create(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = api.ResourcePools.Complete(pool);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(pool);
			resource = api.Resources.Create(resource);
			resource = api.Resources.Complete(resource);

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime.AddMinutes(10),
				End = currentTime.AddMinutes(20),
				PreRollStart = currentTime.AddMinutes(10),
				PostRollEnd = currentTime.AddMinutes(20),
			};

			job.NodeGraph.Add(new JobResourceNode(pool, resource));
			job = api.Jobs.Create(job);

			var tentativeJob = api.Jobs.SaveAsTentative(job);
			var confirmedJob = api.Jobs.Confirm(tentativeJob);

			jobId = confirmedJob.Id;
			return confirmedJob;
		}

		private static Job CreateTentativeJobStartedInThePast(IMediaOpsPlanApi api, out Guid jobId)
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = api.ResourcePools.Create(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = api.ResourcePools.Complete(pool);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(pool);
			resource = api.Resources.Create(resource);
			resource = api.Resources.Complete(resource);

			// The job start (and pre-roll start) lies in the past, so its reservation runs the moment it is confirmed.
			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime.AddMinutes(-5),
				End = currentTime.AddMinutes(20),
				PreRollStart = currentTime.AddMinutes(-5),
				PostRollEnd = currentTime.AddMinutes(20),
			};

			job.NodeGraph.Add(new JobResourceNode(pool, resource));
			job = api.Jobs.Create(job);

			var tentativeJob = api.Jobs.SaveAsTentative(job);

			jobId = tentativeJob.Id;
			return tentativeJob;
		}

		private static void MarkReservationAsOngoing(ResourceManagerHelper resourceManagerHelper, Guid jobId)
		{
			var reservation = resourceManagerHelper.GetReservationInstances(
				ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(jobId))).FirstOrDefault();
			Assert.IsNotNull(reservation, "Expected a core reservation for the job.");

			reservation.Status = ReservationStatus.Ongoing;
			resourceManagerHelper.AddOrUpdateReservationInstances(reservation);
		}

		[TestMethod]
		public void TransitionToRunning_JobConfirmedAfterItWasRead_MovesJobToRunning()
		{
			var (api, resourceManagerHelper) = CreateContext();

			// The reservation of a job whose start time already passed runs as soon as the job is confirmed, so the
			// reservation start event that drives this transition is handled with a job that was read before (or while)
			// it was confirmed. The stale tentative job below represents that read.
			var staleTentativeJob = CreateTentativeJobStartedInThePast(api, out var jobId);

			var confirmedJob = api.Jobs.Confirm(staleTentativeJob);
			Assert.AreEqual(JobState.Confirmed, confirmedJob.State, "Expected the job to be confirmed.");

			MarkReservationAsOngoing(resourceManagerHelper, jobId);

			var runningJob = api.Jobs.TransitionToRunning(staleTentativeJob);

			Assert.AreEqual(JobState.Running, runningJob.State, "Expected the job that was confirmed after it was read to be moved to running.");
			Assert.AreEqual(JobState.Running, api.Jobs.Read(jobId).State, "Expected the stored job to be running.");
		}

		[TestMethod]
		public void TransitionToRunning_JobThatIsAlreadyRunning_IsReportedAsSuccessful()
		{
			var (api, resourceManagerHelper) = CreateContext();

			var confirmedJob = CreateConfirmedStartedJob(api, out var jobId);
			MarkReservationAsOngoing(resourceManagerHelper, jobId);

			var runningJob = api.Jobs.TransitionToRunning(confirmedJob);
			Assert.AreEqual(JobState.Running, runningJob.State, "Expected the job to be running.");

			// A second reservation start event for the same job must not fail on the job that already reached the
			// requested state.
			var stillRunningJob = api.Jobs.TransitionToRunning(runningJob);

			Assert.AreEqual(JobState.Running, stillRunningJob.State, "Expected the already running job to be returned as running.");
			Assert.AreEqual(JobState.Running, api.Jobs.Read(jobId).State, "Expected the stored job to stay running.");
		}

		[TestMethod]
		public void TransitionToRunning_ConfirmedJobWithoutRunningReservation_ThrowsReservationNotRunningError()
		{
			var (api, _) = CreateContext();

			var startedJob = CreateConfirmedStartedJob(api, out _);

			var exception = Assert.ThrowsException<MediaOpsException>(() => api.Jobs.TransitionToRunning(startedJob));
			Assert.IsTrue(
				exception.TraceData.ErrorData.OfType<JobReservationNotRunningError>().Any(),
				"Expected a JobReservationNotRunningError when the core reservation is not ongoing.");
		}

		[TestMethod]
		public void TransitionToRunning_RunningReservation_MovesJobToRunning()
		{
			var (api, resourceManagerHelper) = CreateContext();

			var startedJob = CreateConfirmedStartedJob(api, out var jobId);

			// Deterministically mark the reservation as ongoing, mirroring what SRM does on a live agent once the
			// reservation start time is reached.
			var reservation = resourceManagerHelper.GetReservationInstances(
				ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(jobId))).FirstOrDefault();
			Assert.IsNotNull(reservation, "Expected a core reservation for the job.");

			reservation.Status = ReservationStatus.Ongoing;
			resourceManagerHelper.AddOrUpdateReservationInstances(reservation);

			var runningJob = api.Jobs.TransitionToRunning(startedJob);

			Assert.IsNotNull(runningJob, "Expected the transition to return the updated job.");
			Assert.AreEqual(
				JobState.Running,
				runningJob.State,
				"Expected the job to be moved to the Running state.");
		}

		[TestMethod]
		public void TransitionToRunning_RunningReservationWithFuturePreRoll_MovesJobToRunning()
		{
			var (api, resourceManagerHelper) = CreateContext();

			var confirmedJob = CreateConfirmedJob(api, out var jobId);

			var reservation = resourceManagerHelper.GetReservationInstances(
				ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(jobId))).FirstOrDefault();
			Assert.IsNotNull(reservation, "Expected a core reservation for the job.");

			reservation.Status = ReservationStatus.Ongoing;
			resourceManagerHelper.AddOrUpdateReservationInstances(reservation);

			var runningJob = api.Jobs.TransitionToRunning(confirmedJob);

			Assert.AreEqual(JobState.Running, runningJob.State);
		}
	}
}
