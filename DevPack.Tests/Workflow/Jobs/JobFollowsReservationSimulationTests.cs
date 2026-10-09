namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.Extensions;

	using Skyline.DataMiner.Net.Helper;
	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.ResourceManager.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Simulation;

	using ResourcePool = Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool;

	/// <summary>
	/// Verifies that a job follows its core reservation when the reservation event that normally drives the job was lost
	/// (the simulation has no SRM engine, so setting the reservation status directly mirrors a lost event), and that a job
	/// whose reservation already started can't go back to Tentative or be canceled.
	/// </summary>
	[TestClass]
	public sealed class JobFollowsReservationSimulationTests
	{
		private static (SimulatedDms Dms, IMediaOpsPlanApi Api, ResourceManagerHelper ResourceManagerHelper) CreateContext()
		{
			var dms = MediaOpsPlanSimulation.Create();
			var connection = dms.CreateConnection();

			return (dms, connection.GetMediaOpsPlanApi(), new ResourceManagerHelper(connection.HandleSingleResponseMessage));
		}

		private static Job CreateConfirmedJob(IMediaOpsPlanApi api)
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = api.ResourcePools.Complete(api.ResourcePools.Create(new ResourcePool { Name = $"{prefix}_Pool" }));
			var resource = api.Resources.Complete(api.Resources.Create(new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(pool)));

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime.AddMinutes(10),
				End = currentTime.AddMinutes(20),
				PreRollStart = currentTime.AddMinutes(10),
				PostRollEnd = currentTime.AddMinutes(20),
			};
			job.NodeGraph.Add(new JobResourceNode(pool, resource));

			return api.Jobs.Confirm(api.Jobs.SaveAsTentative(api.Jobs.Create(job)));
		}

		private static ReservationInstance GetReservation(ResourceManagerHelper resourceManagerHelper, Guid jobId)
		{
			return resourceManagerHelper.GetReservationInstances(
				ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(jobId))).Single();
		}

		private static void SetReservationStatus(ResourceManagerHelper resourceManagerHelper, Guid jobId, ReservationStatus status)
		{
			var reservation = GetReservation(resourceManagerHelper, jobId);
			reservation.Status = status;
			resourceManagerHelper.AddOrUpdateReservationInstances(reservation);
		}

		[TestMethod]
		public void ReturnToTentative_ReservationAlreadyRunning_IsRejectedAndJobFollowsReservation()
		{
			var (_, api, resourceManagerHelper) = CreateContext();
			var confirmedJob = CreateConfirmedJob(api);

			SetReservationStatus(resourceManagerHelper, confirmedJob.Id, ReservationStatus.Ongoing);

			var exception = Assert.ThrowsException<MediaOpsException>(() => api.Jobs.ReturnToTentative(confirmedJob.Id));
			Assert.IsTrue(exception.TraceData.ErrorData.OfType<JobInvalidStateError>().Any(), "Expected a JobInvalidStateError.");

			Assert.AreEqual(JobState.Running, api.Jobs.Read(confirmedJob.Id).State, "Expected the job to follow its running reservation.");
			Assert.AreEqual(ReservationStatus.Ongoing, GetReservation(resourceManagerHelper, confirmedJob.Id).Status, "Expected the running reservation not to be pushed back.");
		}

		[TestMethod]
		public void ReturnToTentative_JobRunningWhileWaitingForLock_IsRejectedWithoutTouchingReservation()
		{
			var (dms, api, resourceManagerHelper) = CreateContext();
			var staleConfirmedJob = CreateConfirmedJob(api);

			// The reservation starts and its start event moves the job to Running behind the stale snapshot's back.
			SetReservationStatus(resourceManagerHelper, staleConfirmedJob.Id, ReservationStatus.Ongoing);
			dms.CreateConnection().GetMediaOpsPlanApi().Jobs.TransitionToRunning(staleConfirmedJob.Id);

			// The handler is invoked directly because the repository re-reads the job by ID and would hide the stale snapshot.
			Assert.IsFalse(DomJobHandler.TryReturnToTentative((MediaOpsPlanApi)api, [staleConfirmedJob], out var result));
			Assert.IsTrue(result.TraceDataPerItem[staleConfirmedJob.Id].ErrorData.OfType<JobInvalidStateError>().Any(), "Expected a JobInvalidStateError.");

			Assert.AreEqual(JobState.Running, api.Jobs.Read(staleConfirmedJob.Id).State);
			Assert.AreEqual(ReservationStatus.Ongoing, GetReservation(resourceManagerHelper, staleConfirmedJob.Id).Status, "Expected the running reservation not to be pushed back.");
		}

		[TestMethod]
		public void Cancel_ReservationAlreadyRunning_IsRejectedAndJobFollowsReservation()
		{
			var (_, api, resourceManagerHelper) = CreateContext();
			var confirmedJob = CreateConfirmedJob(api);

			SetReservationStatus(resourceManagerHelper, confirmedJob.Id, ReservationStatus.Ongoing);

			var exception = Assert.ThrowsException<MediaOpsException>(() => api.Jobs.Cancel(confirmedJob.Id));
			Assert.IsTrue(exception.TraceData.ErrorData.OfType<JobInvalidStateError>().Any(), "Expected a JobInvalidStateError.");

			Assert.AreEqual(JobState.Running, api.Jobs.Read(confirmedJob.Id).State, "Expected the job to follow its running reservation.");
			Assert.AreEqual(ReservationStatus.Ongoing, GetReservation(resourceManagerHelper, confirmedJob.Id).Status, "Expected the running reservation not to be canceled.");
		}

		[TestMethod]
		public void Cancel_JobRunningWhileWaitingForLock_IsRejectedWithoutTouchingReservation()
		{
			var (dms, api, resourceManagerHelper) = CreateContext();
			var staleConfirmedJob = CreateConfirmedJob(api);

			SetReservationStatus(resourceManagerHelper, staleConfirmedJob.Id, ReservationStatus.Ongoing);
			dms.CreateConnection().GetMediaOpsPlanApi().Jobs.TransitionToRunning(staleConfirmedJob.Id);

			Assert.IsFalse(DomJobHandler.TryCancel((MediaOpsPlanApi)api, [staleConfirmedJob], out var result));
			Assert.IsTrue(result.TraceDataPerItem[staleConfirmedJob.Id].ErrorData.OfType<JobInvalidStateError>().Any(), "Expected a JobInvalidStateError.");

			Assert.AreEqual(JobState.Running, api.Jobs.Read(staleConfirmedJob.Id).State);
			Assert.AreEqual(ReservationStatus.Ongoing, GetReservation(resourceManagerHelper, staleConfirmedJob.Id).Status, "Expected the running reservation not to be canceled.");
		}

		[TestMethod]
		public void Cancel_ConfirmedJobWithConfirmedReservation_CancelsJobAndReservation()
		{
			var (_, api, resourceManagerHelper) = CreateContext();
			var confirmedJob = CreateConfirmedJob(api);

			var canceledJob = api.Jobs.Cancel(confirmedJob.Id);

			Assert.AreEqual(JobState.Canceled, canceledJob.State);
			Assert.AreEqual(ReservationStatus.Canceled, GetReservation(resourceManagerHelper, confirmedJob.Id).Status);
		}

		[TestMethod]
		public void TransitionToCompleted_ConfirmedJobWithEndedReservation_MovesJobToCompleted()
		{
			var (_, api, resourceManagerHelper) = CreateContext();
			var confirmedJob = CreateConfirmedJob(api);

			// Both reservation events were lost, so the job is still Confirmed while its reservation already ended.
			SetReservationStatus(resourceManagerHelper, confirmedJob.Id, ReservationStatus.Ended);

			var completedJob = api.Jobs.TransitionToCompleted(confirmedJob.Id);

			Assert.AreEqual(JobState.Completed, completedJob.State);
			Assert.AreEqual(JobState.Completed, api.Jobs.Read(confirmedJob.Id).State);
		}

		[TestMethod]
		public void Cancel_ReservationStartsWithinGuardTime_IsRejected()
		{
			var (_, api, resourceManagerHelper) = CreateContext();
			var confirmedJob = CreateConfirmedJob(api);

			MoveReservationStartWithinGuardTime(resourceManagerHelper, confirmedJob.Id);

			var exception = Assert.ThrowsException<MediaOpsException>(() => api.Jobs.Cancel(confirmedJob.Id));
			Assert.IsTrue(exception.TraceData.ErrorData.OfType<JobInvalidStateError>().Any(), "Expected a JobInvalidStateError.");

			Assert.AreEqual(JobState.Confirmed, api.Jobs.Read(confirmedJob.Id).State);
			Assert.AreEqual(ReservationStatus.Confirmed, GetReservation(resourceManagerHelper, confirmedJob.Id).Status, "Expected the reservation that is about to start not to be canceled.");
		}

		[TestMethod]
		public void ReturnToTentative_ReservationStartsWithinGuardTime_IsRejected()
		{
			var (_, api, resourceManagerHelper) = CreateContext();
			var confirmedJob = CreateConfirmedJob(api);

			MoveReservationStartWithinGuardTime(resourceManagerHelper, confirmedJob.Id);

			var exception = Assert.ThrowsException<MediaOpsException>(() => api.Jobs.ReturnToTentative(confirmedJob.Id));
			Assert.IsTrue(exception.TraceData.ErrorData.OfType<JobInvalidStateError>().Any(), "Expected a JobInvalidStateError.");

			Assert.AreEqual(JobState.Confirmed, api.Jobs.Read(confirmedJob.Id).State);
			Assert.AreEqual(ReservationStatus.Confirmed, GetReservation(resourceManagerHelper, confirmedJob.Id).Status, "Expected the reservation that is about to start not to be pushed back.");
		}

		[TestMethod]
		public void Cancel_TentativeJobWithReservationStartingWithinGuardTime_IsCanceled()
		{
			var (_, api, resourceManagerHelper) = CreateContext();
			var tentativeJob = api.Jobs.ReturnToTentative(CreateConfirmedJob(api).Id);

			// A Pending reservation is never started by SRM, so the guard does not apply.
			MoveReservationStartWithinGuardTime(resourceManagerHelper, tentativeJob.Id);

			var canceledJob = api.Jobs.Cancel(tentativeJob.Id);

			Assert.AreEqual(JobState.Canceled, canceledJob.State);
			Assert.AreEqual(ReservationStatus.Canceled, GetReservation(resourceManagerHelper, tentativeJob.Id).Status);
		}

		[TestMethod]
		public void CancelUnlessStarted_OngoingReservation_IsRefused()
		{
			var (_, api, resourceManagerHelper) = CreateContext();
			var confirmedJob = CreateConfirmedJob(api);

			SetReservationStatus(resourceManagerHelper, confirmedJob.Id, ReservationStatus.Ongoing);

			var planApi = (MediaOpsPlanApi)api;
			var domJob = planApi.DomHelpers.SlcWorkflowHelper.GetJobs([confirmedJob.Id]).Single();

			Assert.IsFalse(CoreJobHandler.TryCancelUnlessStarted(planApi, [domJob], out var result, out var startedJobIds));
			Assert.IsTrue(startedJobIds.Contains(confirmedJob.Id));
			Assert.IsTrue(result.UnsuccessfulIds.Contains(confirmedJob.Id));
			Assert.AreEqual(ReservationStatus.Ongoing, GetReservation(resourceManagerHelper, confirmedJob.Id).Status, "Expected the running reservation not to be canceled.");
		}

		private static void MoveReservationStartWithinGuardTime(ResourceManagerHelper resourceManagerHelper, Guid jobId)
		{
			var reservation = GetReservation(resourceManagerHelper, jobId);
			foreach (var existingEvent in reservation.Events)
			{
				reservation.RemoveEvent(existingEvent.Key, existingEvent.Value);
			}

			reservation = reservation.NewTimeRange(new Skyline.DataMiner.Net.Time.TimeRangeUtc(DateTime.UtcNow.AddSeconds(2), reservation.TimeRange.Stop));
			resourceManagerHelper.AddOrUpdateReservationInstances(reservation);
		}
	}
}
