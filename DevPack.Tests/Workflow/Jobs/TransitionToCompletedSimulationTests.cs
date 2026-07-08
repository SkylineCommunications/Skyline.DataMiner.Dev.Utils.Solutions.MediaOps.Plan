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
	using PlanResource = Skyline.DataMiner.Solutions.MediaOps.Plan.API.Resource;

	/// <summary>
	/// Deterministic, simulation-backed tests for the running-to-completed transition. The simulation has no SRM engine,
	/// so the reservation status is controlled explicitly to bring a job into the Running state and to reflect that its
	/// reservation has ended, without depending on a live scheduling timeline.
	/// </summary>
	[TestClass]
	public sealed class TransitionToCompletedSimulationTests
	{
		private static (IMediaOpsPlanApi Api, ResourceManagerHelper ResourceManagerHelper) CreateContext()
		{
			var dms = MediaOpsPlanSimulation.Create();
			var connection = dms.CreateConnection();

			return (connection.GetMediaOpsPlanApi(), new ResourceManagerHelper(connection.HandleSingleResponseMessage));
		}

		private static (ResourcePool Pool, PlanResource Resource) CreatePoolAndResource(IMediaOpsPlanApi api, Guid prefix)
		{
			var pool = api.ResourcePools.Create(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = api.ResourcePools.Complete(pool);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(pool);
			resource = api.Resources.Create(resource);
			resource = api.Resources.Complete(resource);

			return (pool, resource);
		}

		private static Job CreateConfirmedJob(IMediaOpsPlanApi api)
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var (pool, resource) = CreatePoolAndResource(api, prefix);

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
			return api.Jobs.Confirm(tentativeJob);
		}

		private static Job CreateRunningJob(IMediaOpsPlanApi api, ResourceManagerHelper resourceManagerHelper, bool withPostRoll)
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var (pool, resource) = CreatePoolAndResource(api, prefix);

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime.AddMinutes(10),
				End = currentTime.AddMinutes(20),
				PreRollStart = currentTime.AddMinutes(10),
				PostRollEnd = withPostRoll ? currentTime.AddMinutes(30) : currentTime.AddMinutes(20),
			};

			job.NodeGraph.Add(new JobResourceNode(pool, resource));
			job = api.Jobs.Create(job);

			var tentativeJob = api.Jobs.SaveAsTentative(job);
			var confirmedJob = api.Jobs.Confirm(tentativeJob);

			// A manual start moves the reservation start (and the pre-roll) to now while keeping the end and post-roll end.
			var startedJob = api.Jobs.Start(confirmedJob);

			// Deterministically mark the reservation as ongoing, mirroring what SRM does on a live agent once the
			// reservation start time is reached, then complete the confirmed-to-running transition.
			SetReservationStatus(resourceManagerHelper, startedJob.Id, ReservationStatus.Ongoing);

			return api.Jobs.TransitionToRunning(startedJob);
		}

		private static void SetReservationStatus(ResourceManagerHelper resourceManagerHelper, Guid jobId, ReservationStatus status)
		{
			var reservation = resourceManagerHelper.GetReservationInstances(
				ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(jobId))).FirstOrDefault();
			Assert.IsNotNull(reservation, "Expected a core reservation for the job.");

			reservation.Status = status;
			resourceManagerHelper.AddOrUpdateReservationInstances(reservation);
		}

		[TestMethod]
		public void TransitionToCompleted_JobNotRunning_ThrowsInvalidStateError()
		{
			var (api, _) = CreateContext();

			var confirmedJob = CreateConfirmedJob(api);

			var exception = Assert.ThrowsException<MediaOpsException>(() => api.Jobs.TransitionToCompleted(confirmedJob));
			Assert.IsTrue(
				exception.TraceData.ErrorData.OfType<JobInvalidStateError>().Any(),
				"Expected a JobInvalidStateError when the job is not in the Running state.");
		}

		[TestMethod]
		public void TransitionToCompleted_PostRollEndNotReached_ThrowsPostRollEndNotReachedError()
		{
			var (api, resourceManagerHelper) = CreateContext();

			// The running job keeps its post-roll end in the future, so the transition must be rejected on the time guard.
			var runningJob = CreateRunningJob(api, resourceManagerHelper, withPostRoll: true);

			var exception = Assert.ThrowsException<MediaOpsException>(() => api.Jobs.TransitionToCompleted(runningJob));
			Assert.IsTrue(
				exception.TraceData.ErrorData.OfType<JobPostRollEndNotReachedError>().Any(),
				"Expected a JobPostRollEndNotReachedError when the post-roll end time has not yet passed.");
		}

		[TestMethod]
		public void TransitionToCompleted_ReservationNotEnded_ThrowsReservationNotEndedError()
		{
			var (api, resourceManagerHelper) = CreateContext();

			var runningJob = CreateRunningJob(api, resourceManagerHelper, withPostRoll: false);

			// Stopping the running job moves its end and post-roll end to (approximately) now while keeping it Running, so
			// the post-roll end time guard passes on the completion attempt below.
			var stoppedJob = api.Jobs.Stop(runningJob);

			// The reservation is left ongoing, mirroring a reservation that has not actually finished yet.

			var exception = Assert.ThrowsException<MediaOpsException>(() => api.Jobs.TransitionToCompleted(stoppedJob));
			Assert.IsTrue(
				exception.TraceData.ErrorData.OfType<JobReservationNotEndedError>().Any(),
				"Expected a JobReservationNotEndedError when the core reservation has not ended.");
		}

		[TestMethod]
		public void TransitionToCompleted_EndedReservationPastPostRollEnd_MovesJobToCompleted()
		{
			var (api, resourceManagerHelper) = CreateContext();

			var runningJob = CreateRunningJob(api, resourceManagerHelper, withPostRoll: false);

			// Stopping the running job moves its end and post-roll end to (approximately) now while keeping it Running, so
			// the post-roll end time guard passes on the completion attempt below.
			var stoppedJob = api.Jobs.Stop(runningJob);

			// Deterministically mark the reservation as ended, mirroring what SRM does on a live agent once the
			// reservation end time is reached.
			SetReservationStatus(resourceManagerHelper, stoppedJob.Id, ReservationStatus.Ended);

			var completedJob = api.Jobs.TransitionToCompleted(stoppedJob);

			Assert.IsNotNull(completedJob, "Expected the transition to return the updated job.");
			Assert.AreEqual(
				JobState.Completed,
				completedJob.State,
				"Expected the job to be moved to the Completed state.");
		}
	}
}
