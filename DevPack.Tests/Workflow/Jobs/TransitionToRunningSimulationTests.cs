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

			// A manual start moves the reservation start (and the pre-roll) to now. The simulation has no SRM engine, so
			// the reservation stays Confirmed until a test explicitly marks it ongoing.
			return api.Jobs.Start(confirmedJob);
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
	}
}
