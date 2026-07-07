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
	/// Deterministic, simulation-backed tests for stopping a running job early. The simulation has no SRM engine, so the
	/// reservation status is controlled explicitly to bring a job into the Running state without depending on a live
	/// scheduling timeline.
	/// </summary>
	[TestClass]
	public sealed class StopSimulationTests
	{
		private static readonly TimeSpan NowTolerance = TimeSpan.FromSeconds(60);

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
			var reservation = resourceManagerHelper.GetReservationInstances(
				ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(startedJob.Id))).FirstOrDefault();
			Assert.IsNotNull(reservation, "Expected a core reservation for the job.");

			reservation.Status = ReservationStatus.Ongoing;
			resourceManagerHelper.AddOrUpdateReservationInstances(reservation);

			return api.Jobs.TransitionToRunning(startedJob);
		}

		[TestMethod]
		public void Stop_JobNotRunning_ThrowsInvalidStateError()
		{
			var (api, _) = CreateContext();

			var confirmedJob = CreateConfirmedJob(api);

			var exception = Assert.ThrowsException<MediaOpsException>(() => api.Jobs.Stop(confirmedJob));
			Assert.IsTrue(
				exception.TraceData.ErrorData.OfType<JobInvalidStateError>().Any(),
				"Expected a JobInvalidStateError when the job is not in the Running state.");
		}

		[TestMethod]
		public void Stop_NewPostRollEndWithinGuardTime_ThrowsInvalidPostRollError()
		{
			var (api, resourceManagerHelper) = CreateContext();

			var runningJob = CreateRunningJob(api, resourceManagerHelper, withPostRoll: true);

			var options = new JobStopOptions { NewPostRollEnd = DateTimeOffset.UtcNow };

			var exception = Assert.ThrowsException<MediaOpsException>(() => api.Jobs.Stop(runningJob, options));
			Assert.IsTrue(
				exception.TraceData.ErrorData.OfType<JobInvalidPostRollError>().Any(),
				"Expected a JobInvalidPostRollError when the new post-roll end time lies within the guard time.");
		}

		[TestMethod]
		public void Stop_WithPostRollDefault_KeepsPostRollEnd()
		{
			var (api, resourceManagerHelper) = CreateContext();

			var runningJob = CreateRunningJob(api, resourceManagerHelper, withPostRoll: true);
			var originalEnd = runningJob.End;
			var originalPostRollEnd = runningJob.PostRollEnd;

			var stoppedJob = api.Jobs.Stop(runningJob);

			Assert.IsNotNull(stoppedJob, "Expected the stop to return the updated job.");
			Assert.IsTrue(stoppedJob.End < originalEnd, "Expected the end time to be moved earlier.");
			Assert.IsTrue(
				(DateTimeOffset.UtcNow - stoppedJob.End).Duration() < NowTolerance,
				"Expected the end time to be set to (approximately) the current time.");
			Assert.AreEqual(
				originalPostRollEnd.UtcDateTime,
				stoppedJob.PostRollEnd.UtcDateTime,
				"Expected the post-roll end time to be kept when a post-roll is configured.");
		}

		[TestMethod]
		public void Stop_WithNewPostRollEnd_MovesPostRollEnd()
		{
			var (api, resourceManagerHelper) = CreateContext();

			var runningJob = CreateRunningJob(api, resourceManagerHelper, withPostRoll: true);
			var originalEnd = runningJob.End;

			var newPostRollEnd = DateTime.UtcNow.RoundToNextSecond().AddMinutes(15);
			var options = new JobStopOptions { NewPostRollEnd = newPostRollEnd };

			var stoppedJob = api.Jobs.Stop(runningJob, options);

			Assert.IsNotNull(stoppedJob, "Expected the stop to return the updated job.");
			Assert.IsTrue(stoppedJob.End < originalEnd, "Expected the end time to be moved earlier.");
			Assert.AreEqual(
				newPostRollEnd,
				stoppedJob.PostRollEnd.UtcDateTime,
				"Expected the post-roll end time to be replaced by the requested value.");
		}

		[TestMethod]
		public void Stop_WithoutPostRollAndNewPostRollEnd_MovesPostRollEnd()
		{
			var (api, resourceManagerHelper) = CreateContext();

			var runningJob = CreateRunningJob(api, resourceManagerHelper, withPostRoll: false);
			var originalEnd = runningJob.End;

			var newPostRollEnd = DateTime.UtcNow.RoundToNextSecond().AddMinutes(15);
			var options = new JobStopOptions { NewPostRollEnd = newPostRollEnd };

			var stoppedJob = api.Jobs.Stop(runningJob, options);

			Assert.IsNotNull(stoppedJob, "Expected the stop to return the updated job.");
			Assert.IsTrue(stoppedJob.End < originalEnd, "Expected the end time to be moved earlier.");
			Assert.AreEqual(
				newPostRollEnd,
				stoppedJob.PostRollEnd.UtcDateTime,
				"Expected the post-roll end time to be replaced by the requested value.");
			Assert.AreNotEqual(
				stoppedJob.End.UtcDateTime,
				stoppedJob.PostRollEnd.UtcDateTime,
				"Expected the post-roll end time to no longer be aligned with the end time.");
		}

		[TestMethod]
		public void Stop_WithoutPostRollDefault_AlignsPostRollEndWithEnd()
		{
			var (api, resourceManagerHelper) = CreateContext();

			var runningJob = CreateRunningJob(api, resourceManagerHelper, withPostRoll: false);
			var originalEnd = runningJob.End;

			var stoppedJob = api.Jobs.Stop(runningJob);

			Assert.IsNotNull(stoppedJob, "Expected the stop to return the updated job.");
			Assert.IsTrue(stoppedJob.End < originalEnd, "Expected the end time to be moved earlier.");
			Assert.AreEqual(
				stoppedJob.End.UtcDateTime,
				stoppedJob.PostRollEnd.UtcDateTime,
				"Expected the post-roll end time to be aligned with the new end time when no post-roll is configured.");
		}
	}
}
