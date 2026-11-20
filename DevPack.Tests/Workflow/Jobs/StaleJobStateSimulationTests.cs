namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;
	using System.Collections.Generic;
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
	/// A job action can be called with a job that was read before another user (or SRM) changed its state. These tests
	/// change the stored state through a second API instance, which has its own in-memory lock manager, and then call the
	/// handler directly with the stale job, because the repository would re-read the job by ID and hide the stale state.
	/// </summary>
	[TestClass]
	public sealed class StaleJobStateSimulationTests
	{
		private sealed class Context
		{
			public Context()
			{
				Dms = MediaOpsPlanSimulation.Create();
				var connection = Dms.CreateConnection();
				Api = connection.GetMediaOpsPlanApi();
				ResourceManagerHelper = new ResourceManagerHelper(connection.HandleSingleResponseMessage);
			}

			public SimulatedDms Dms { get; }

			public IMediaOpsPlanApi Api { get; }

			public MediaOpsPlanApi PlanApi => (MediaOpsPlanApi)Api;

			public ResourceManagerHelper ResourceManagerHelper { get; }

			public IMediaOpsPlanApi OtherApi => Dms.CreateConnection().GetMediaOpsPlanApi();

			public List<ReservationInstance> GetReservations(Guid jobId)
			{
				return ResourceManagerHelper.GetReservationInstances(
					ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(jobId))).ToList();
			}

			public Job CreateDraftJob(DateTime start, DateTime end)
			{
				var prefix = Guid.NewGuid();

				var pool = Api.ResourcePools.Complete(Api.ResourcePools.Create(new ResourcePool { Name = $"{prefix}_Pool" }));
				var resource = Api.Resources.Complete(Api.Resources.Create(new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(pool)));

				var job = new Job
				{
					Name = $"{prefix}_Job",
					Start = start,
					End = end,
					PreRollStart = start,
					PostRollEnd = end,
				};
				job.NodeGraph.Add(new JobResourceNode(pool, resource));

				return Api.Jobs.Create(job);
			}

			public Job CreateFutureDraftJob()
			{
				var currentTime = DateTime.UtcNow.RoundToNextSecond();
				return CreateDraftJob(currentTime.AddMinutes(10), currentTime.AddMinutes(20));
			}

			public Job CreateRunningJob()
			{
				var confirmedJob = Api.Jobs.Confirm(Api.Jobs.SaveAsTentative(CreateFutureDraftJob()));
				var startedJob = Api.Jobs.Start(confirmedJob);

				// The simulation has no SRM engine, so the reservation is marked as ongoing like SRM does on a live agent.
				var reservation = GetReservations(startedJob.Id).Single();
				reservation.Status = ReservationStatus.Ongoing;
				ResourceManagerHelper.AddOrUpdateReservationInstances(reservation);

				return Api.Jobs.TransitionToRunning(startedJob);
			}
		}

		private static void AssertInvalidState(DomInstanceBulkOperationResult<Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow.JobsInstance> result, Guid jobId)
		{
			Assert.IsTrue(
				result.TraceDataPerItem[jobId].ErrorData.OfType<JobInvalidStateError>().Any(),
				"Expected a JobInvalidStateError for the job whose stored state changed.");
		}

		[TestMethod]
		public void Update_JobCanceledAfterRead_FailsWithoutChangingJob()
		{
			var context = new Context();
			var staleJob = context.Api.Jobs.SaveAsTentative(context.CreateFutureDraftJob());

			context.OtherApi.Jobs.Cancel(staleJob.Id);

			staleJob.Description = "Changed after the job was canceled";
			Assert.IsFalse(DomJobHandler.TryCreateOrUpdate(context.PlanApi, [staleJob], out var result), "Expected the update of a canceled job to fail.");
			AssertInvalidState(result, staleJob.Id);

			var storedJob = context.Api.Jobs.Read(staleJob.Id);
			Assert.AreEqual(JobState.Canceled, storedJob.State);
			Assert.AreNotEqual("Changed after the job was canceled", storedJob.Description, "Expected the canceled job not to be changed.");
		}

		[TestMethod]
		public void Update_JobConfirmedAfterRead_FailsWithoutChangingJob()
		{
			var context = new Context();
			var staleJob = context.Api.Jobs.SaveAsTentative(context.CreateFutureDraftJob());

			// The update was validated for a Tentative job, which is not what is stored anymore.
			context.OtherApi.Jobs.Confirm(staleJob.Id);

			staleJob.Description = "Changed after the job was confirmed";
			Assert.IsFalse(DomJobHandler.TryCreateOrUpdate(context.PlanApi, [staleJob], out var result), "Expected the update of a job whose state changed to fail.");
			AssertInvalidState(result, staleJob.Id);
			Assert.AreNotEqual("Changed after the job was confirmed", context.Api.Jobs.Read(staleJob.Id).Description);
		}

		[TestMethod]
		public void Update_StateUnchangedSinceRead_Succeeds()
		{
			var context = new Context();
			var job = context.Api.Jobs.SaveAsTentative(context.CreateFutureDraftJob());

			job.Description = "Changed";
			Assert.IsTrue(DomJobHandler.TryCreateOrUpdate(context.PlanApi, [job], out _), "Expected the update to succeed.");
			Assert.AreEqual("Changed", context.Api.Jobs.Read(job.Id).Description);
		}

		[TestMethod]
		public void Delete_DraftJobSavedAsTentativeAfterRead_FailsAndKeepsJobAndReservation()
		{
			var context = new Context();
			var staleDraftJob = context.CreateFutureDraftJob();

			context.OtherApi.Jobs.SaveAsTentative(staleDraftJob.Id);

			Assert.IsFalse(DomJobHandler.TryDelete(context.PlanApi, [staleDraftJob], out var result), "Expected the delete of a Tentative job to fail.");
			AssertInvalidState(result, staleDraftJob.Id);
			Assert.AreEqual(JobState.Tentative, context.Api.Jobs.Read(staleDraftJob.Id).State);
			Assert.AreEqual(1, context.GetReservations(staleDraftJob.Id).Count, "Expected the reservation of the job to be kept.");
		}

		[TestMethod]
		public void Delete_ForcedOnDraftJobSavedAsTentativeAfterRead_DeletesJobAndReservation()
		{
			var context = new Context();
			var staleDraftJob = context.CreateFutureDraftJob();

			context.OtherApi.Jobs.SaveAsTentative(staleDraftJob.Id);

			Assert.IsTrue(DomJobHandler.TryDelete(context.PlanApi, [staleDraftJob], out _, new JobDeleteOptions { ForceDelete = true }), "Expected the forced delete to succeed.");
			Assert.IsNull(context.Api.Jobs.Read(staleDraftJob.Id));
			Assert.AreEqual(0, context.GetReservations(staleDraftJob.Id).Count, "Expected the reservation that was created after the job was read not to be orphaned.");
		}

		[TestMethod]
		public void Delete_JobAlreadyDeletedAfterRead_Succeeds()
		{
			var context = new Context();
			var staleDraftJob = context.CreateFutureDraftJob();

			context.OtherApi.Jobs.Delete(staleDraftJob.Id);

			Assert.IsTrue(DomJobHandler.TryDelete(context.PlanApi, [staleDraftJob], out _), "Expected the delete of an already deleted job to succeed.");
		}

		[TestMethod]
		public void MarkAsCompleted_TentativeJobConfirmedAfterRead_FailsAndKeepsReservation()
		{
			var context = new Context();
			var tentativeJob = context.Api.Jobs.SaveAsTentative(context.CreateFutureDraftJob());

			// The job was read while its end was in the past. Another user moved the end into the future and confirmed it.
			var staleJob = context.Api.Jobs.Read(tentativeJob.Id);
			var pastTime = DateTime.UtcNow.RoundToNextSecond().AddMinutes(-30);
			staleJob.PreRollStart = pastTime;
			staleJob.Start = pastTime;
			staleJob.End = pastTime.AddMinutes(10);
			staleJob.PostRollEnd = pastTime.AddMinutes(10);
			context.OtherApi.Jobs.Confirm(tentativeJob.Id);

			Assert.IsFalse(DomJobHandler.TryMarkAsCompleted(context.PlanApi, [staleJob], out var result), "Expected marking a Confirmed job as completed to fail.");
			AssertInvalidState(result, staleJob.Id);
			Assert.AreEqual(JobState.Confirmed, context.Api.Jobs.Read(staleJob.Id).State);
			Assert.AreEqual(ReservationStatus.Confirmed, context.GetReservations(staleJob.Id).Single().Status, "Expected the reservation of the Confirmed job to be kept.");
		}

		[TestMethod]
		public void SaveAsTentative_DraftJobMovedIntoPastAfterRead_FailsWithoutReservation()
		{
			var context = new Context();
			var staleDraftJob = context.CreateFutureDraftJob();

			// The state stays Draft, but another user moved the whole job into the past.
			var otherApi = context.OtherApi;
			var movedJob = otherApi.Jobs.Read(staleDraftJob.Id);
			var pastTime = DateTime.UtcNow.RoundToNextSecond().AddMinutes(-30);
			movedJob.PreRollStart = pastTime;
			movedJob.Start = pastTime;
			movedJob.End = pastTime.AddMinutes(10);
			movedJob.PostRollEnd = pastTime.AddMinutes(10);
			otherApi.Jobs.Update(movedJob);

			Assert.IsFalse(DomJobHandler.TrySaveAsTentative(context.PlanApi, [staleDraftJob], out var result), "Expected saving a job whose stored end is in the past as tentative to fail.");
			Assert.IsTrue(result.TraceDataPerItem[staleDraftJob.Id].ErrorData.OfType<JobInvalidEndTimeError>().Any(), "Expected a JobInvalidEndTimeError for the stored end time.");
			Assert.AreEqual(JobState.Draft, context.Api.Jobs.Read(staleDraftJob.Id).State);
			Assert.AreEqual(0, context.GetReservations(staleDraftJob.Id).Count, "Expected no reservation for the job.");
		}

		[TestMethod]
		public void MarkAsCompleted_DraftJobMovedIntoFutureAfterRead_FailsWithoutChangingJob()
		{
			var context = new Context();
			var pastTime = DateTime.UtcNow.RoundToNextSecond().AddMinutes(-30);
			var staleDraftJob = context.CreateDraftJob(pastTime, pastTime.AddMinutes(10));

			// The state stays Draft, but another user moved the whole job into the future.
			var otherApi = context.OtherApi;
			var movedJob = otherApi.Jobs.Read(staleDraftJob.Id);
			var futureTime = DateTime.UtcNow.RoundToNextSecond().AddMinutes(10);
			movedJob.PostRollEnd = futureTime.AddMinutes(10);
			movedJob.End = futureTime.AddMinutes(10);
			movedJob.Start = futureTime;
			movedJob.PreRollStart = futureTime;
			otherApi.Jobs.Update(movedJob);

			Assert.IsFalse(DomJobHandler.TryMarkAsCompleted(context.PlanApi, [staleDraftJob], out var result), "Expected marking a job whose stored end is in the future as completed to fail.");
			Assert.IsTrue(result.TraceDataPerItem[staleDraftJob.Id].ErrorData.OfType<JobInvalidEndTimeError>().Any(), "Expected a JobInvalidEndTimeError for the stored end time.");
			Assert.AreEqual(JobState.Draft, context.Api.Jobs.Read(staleDraftJob.Id).State);
		}

		[TestMethod]
		public void SaveAsTentative_DraftJobCanceledAfterRead_FailsWithoutTouchingReservation()
		{
			var context = new Context();
			var staleDraftJob = context.CreateFutureDraftJob();

			var otherApi = context.OtherApi;
			otherApi.Jobs.Cancel(otherApi.Jobs.SaveAsTentative(staleDraftJob.Id));
			Assert.AreEqual(ReservationStatus.Canceled, context.GetReservations(staleDraftJob.Id).Single().Status);

			Assert.IsFalse(DomJobHandler.TrySaveAsTentative(context.PlanApi, [staleDraftJob], out var result), "Expected saving a Canceled job as tentative to fail.");
			AssertInvalidState(result, staleDraftJob.Id);
			Assert.AreEqual(JobState.Canceled, context.Api.Jobs.Read(staleDraftJob.Id).State);
			Assert.AreEqual(ReservationStatus.Canceled, context.GetReservations(staleDraftJob.Id).Single().Status, "Expected the canceled reservation not to be reactivated.");
		}

		[TestMethod]
		public void Start_ConfirmedJobReturnedToTentativeAfterRead_FailsWithoutMovingReservation()
		{
			var context = new Context();
			var staleConfirmedJob = context.Api.Jobs.Confirm(context.Api.Jobs.SaveAsTentative(context.CreateFutureDraftJob()));
			var reservationStart = context.GetReservations(staleConfirmedJob.Id).Single().Start;

			context.OtherApi.Jobs.ReturnToTentative(staleConfirmedJob.Id);

			Assert.IsFalse(DomJobHandler.TryStart(context.PlanApi, [staleConfirmedJob], out var result), "Expected starting a Tentative job to fail.");
			AssertInvalidState(result, staleConfirmedJob.Id);
			Assert.AreEqual(JobState.Tentative, context.Api.Jobs.Read(staleConfirmedJob.Id).State);
			Assert.AreEqual(reservationStart, context.GetReservations(staleConfirmedJob.Id).Single().Start, "Expected the reservation start not to be moved.");
		}

		[TestMethod]
		public void Stop_RunningJobCompletedAfterRead_FailsWithoutChangingJob()
		{
			var context = new Context();
			var staleRunningJob = context.CreateRunningJob();

			var otherApi = context.OtherApi;
			var completedJob = otherApi.Jobs.TransitionToCompleted(otherApi.Jobs.Stop(staleRunningJob.Id));
			Assert.AreEqual(JobState.Completed, completedJob.State);

			Assert.IsFalse(
				DomJobHandler.TryStop(context.PlanApi, [staleRunningJob], out var result, new JobStopOptions { NewPostRollEnd = DateTimeOffset.UtcNow.AddMinutes(5) }),
				"Expected stopping a Completed job to fail.");
			AssertInvalidState(result, staleRunningJob.Id);
			Assert.AreEqual(completedJob.PostRollEnd, context.Api.Jobs.Read(staleRunningJob.Id).PostRollEnd, "Expected the completed job not to be changed.");
		}
	}
}
