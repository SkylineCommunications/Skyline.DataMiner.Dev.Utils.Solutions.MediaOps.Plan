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

	[TestClass]
	public sealed class ConfirmSimulationTests
	{
		private static Job CreateTentativeJob(IMediaOpsPlanApi api, DateTime start, DateTime end)
		{
			var prefix = Guid.NewGuid();

			var pool = api.ResourcePools.Complete(api.ResourcePools.Create(new ResourcePool { Name = $"{prefix}_Pool" }));
			var resource = api.Resources.Complete(api.Resources.Create(new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(pool)));

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = start,
				End = end,
				PreRollStart = start,
				PostRollEnd = end,
			};
			job.NodeGraph.Add(new JobResourceNode(pool, resource));

			return api.Jobs.SaveAsTentative(api.Jobs.Create(job));
		}

		private static ReservationStatus GetReservationStatus(ResourceManagerHelper resourceManagerHelper, Guid jobId)
		{
			return resourceManagerHelper.GetReservationInstances(
				ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(jobId))).Single().Status;
		}

		[TestMethod]
		public void Confirm_JobAlreadyConfirmedWhileConfirmWaitedForLock_JobFollowsConfirmedReservation()
		{
			var dms = MediaOpsPlanSimulation.Create();
			var connection = dms.CreateConnection();
			var api = connection.GetMediaOpsPlanApi();
			var resourceManagerHelper = new ResourceManagerHelper(connection.HandleSingleResponseMessage);

			var currentTime = DateTime.UtcNow.RoundToNextSecond();
			var staleTentativeJob = CreateTentativeJob(api, currentTime.AddMinutes(10), currentTime.AddMinutes(20));

			// A second API instance has its own in-memory lock manager, so it confirms the job behind the stale snapshot's back.
			dms.CreateConnection().GetMediaOpsPlanApi().Jobs.Confirm(staleTentativeJob.Id);

			// The handler is invoked directly because the repository re-reads the job by ID and would hide the stale snapshot.
			Assert.IsTrue(DomJobHandler.TryConfirm((MediaOpsPlanApi)api, [staleTentativeJob], out _), "Expected the confirm to succeed for a job that already reached Confirmed.");
			Assert.AreEqual(JobState.Confirmed, api.Jobs.Read(staleTentativeJob.Id).State);
			Assert.AreEqual(ReservationStatus.Confirmed, GetReservationStatus(resourceManagerHelper, staleTentativeJob.Id));
		}

		[TestMethod]
		public void Confirm_JobAlreadyRunningWhileConfirmWaitedForLock_JobAndReservationStayRunning()
		{
			var dms = MediaOpsPlanSimulation.Create();
			var connection = dms.CreateConnection();
			var api = connection.GetMediaOpsPlanApi();
			var resourceManagerHelper = new ResourceManagerHelper(connection.HandleSingleResponseMessage);

			var currentTime = DateTime.UtcNow.RoundToNextSecond();
			var staleTentativeJob = CreateTentativeJob(api, currentTime.AddMinutes(-5), currentTime.AddMinutes(20));

			// A reservation with a start time in the past starts running the moment it is confirmed.
			dms.StartConfirmedReservationsImmediately = true;
			var runningJob = dms.CreateConnection().GetMediaOpsPlanApi().Jobs.Confirm(staleTentativeJob.Id);
			Assert.AreEqual(JobState.Running, runningJob.State);

			Assert.IsTrue(DomJobHandler.TryConfirm((MediaOpsPlanApi)api, [staleTentativeJob], out _), "Expected the confirm to succeed for a job that already reached Running.");
			Assert.AreEqual(JobState.Running, api.Jobs.Read(staleTentativeJob.Id).State);
			Assert.AreEqual(ReservationStatus.Ongoing, GetReservationStatus(resourceManagerHelper, staleTentativeJob.Id), "Expected the running reservation not to be pushed back.");
		}

		[TestMethod]
		public void Confirm_JobCanceledWhileConfirmWaitedForLock_FailsWithoutTouchingReservation()
		{
			var dms = MediaOpsPlanSimulation.Create();
			var connection = dms.CreateConnection();
			var api = connection.GetMediaOpsPlanApi();
			var resourceManagerHelper = new ResourceManagerHelper(connection.HandleSingleResponseMessage);

			var currentTime = DateTime.UtcNow.RoundToNextSecond();
			var staleTentativeJob = CreateTentativeJob(api, currentTime.AddMinutes(-5), currentTime.AddMinutes(20));

			dms.CreateConnection().GetMediaOpsPlanApi().Jobs.Cancel(staleTentativeJob.Id);

			Assert.IsFalse(DomJobHandler.TryConfirm((MediaOpsPlanApi)api, [staleTentativeJob], out var result), "Expected the confirm of a canceled job to fail.");
			Assert.IsTrue(result.TraceDataPerItem[staleTentativeJob.Id].ErrorData.OfType<JobInvalidStateError>().Any());
			Assert.AreEqual(JobState.Canceled, api.Jobs.Read(staleTentativeJob.Id).State);
			Assert.AreEqual(ReservationStatus.Canceled, GetReservationStatus(resourceManagerHelper, staleTentativeJob.Id), "Expected the reservation of the canceled job to stay canceled.");
		}

		[TestMethod]
		public void Confirm_StoredJobLostResourceWhileConfirmWaitedForLock_FailsWithoutTouchingReservation()
		{
			var dms = MediaOpsPlanSimulation.Create();
			var connection = dms.CreateConnection();
			var api = connection.GetMediaOpsPlanApi();
			var resourceManagerHelper = new ResourceManagerHelper(connection.HandleSingleResponseMessage);

			var currentTime = DateTime.UtcNow.RoundToNextSecond();
			var staleTentativeJob = CreateTentativeJob(api, currentTime.AddMinutes(10), currentTime.AddMinutes(20));

			// Behind the stale snapshot's back, the node is swapped for a pool node without a resource assigned.
			var otherApi = dms.CreateConnection().GetMediaOpsPlanApi();
			var storedJob = otherApi.Jobs.Read(staleTentativeJob.Id);
			storedJob.NodeGraph.Swap(storedJob.NodeGraph.Nodes.Single(), new JobResourcePoolNode(((JobResourceNode)staleTentativeJob.NodeGraph.Nodes.Single()).ResourcePoolId));
			otherApi.Jobs.Update(storedJob);

			// The stale snapshot still has the resource assigned, but the stored job is what gets confirmed.
			Assert.IsFalse(DomJobHandler.TryConfirm((MediaOpsPlanApi)api, [staleTentativeJob], out var result), "Expected the confirm to validate the stored job.");
			Assert.IsTrue(result.TraceDataPerItem[staleTentativeJob.Id].ErrorData.OfType<JobResourceNotAssignedError>().Any());
			Assert.AreEqual(JobState.Tentative, api.Jobs.Read(staleTentativeJob.Id).State);
			Assert.AreEqual(ReservationStatus.Pending, GetReservationStatus(resourceManagerHelper, staleTentativeJob.Id));
		}

		[TestMethod]
		public void Confirm_JobConfirmedAndReservationStartedEventLost_JobFollowsRunningReservation()
		{
			var dms = MediaOpsPlanSimulation.Create();
			var connection = dms.CreateConnection();
			var api = connection.GetMediaOpsPlanApi();
			var resourceManagerHelper = new ResourceManagerHelper(connection.HandleSingleResponseMessage);

			var currentTime = DateTime.UtcNow.RoundToNextSecond();
			var staleTentativeJob = CreateTentativeJob(api, currentTime.AddMinutes(10), currentTime.AddMinutes(20));

			dms.CreateConnection().GetMediaOpsPlanApi().Jobs.Confirm(staleTentativeJob.Id);

			// The reservation starts, but its start event never reaches the job (the simulation fires no reservation events).
			var reservation = resourceManagerHelper.GetReservationInstances(
				ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(staleTentativeJob.Id))).Single();
			reservation.Status = ReservationStatus.Ongoing;
			resourceManagerHelper.AddOrUpdateReservationInstances(reservation);

			Assert.IsTrue(DomJobHandler.TryConfirm((MediaOpsPlanApi)api, [staleTentativeJob], out _), "Expected the confirm to succeed for a job that already reached Confirmed.");
			Assert.AreEqual(JobState.Running, api.Jobs.Read(staleTentativeJob.Id).State, "Expected the job to follow its running reservation.");
			Assert.AreEqual(ReservationStatus.Ongoing, GetReservationStatus(resourceManagerHelper, staleTentativeJob.Id));
		}

		[TestMethod]
		public void Confirm_TentativeJobWithEndedReservation_JobFollowsEndedReservation()
		{
			var dms = MediaOpsPlanSimulation.Create();
			var connection = dms.CreateConnection();
			var api = connection.GetMediaOpsPlanApi();
			var resourceManagerHelper = new ResourceManagerHelper(connection.HandleSingleResponseMessage);

			var currentTime = DateTime.UtcNow.RoundToNextSecond();
			var tentativeJob = CreateTentativeJob(api, currentTime.AddMinutes(-5), currentTime.AddMinutes(20));

			// The reservation was confirmed and ran to its end before (for example by a confirm whose job transition
			// failed), while the job stayed Tentative.
			var reservation = resourceManagerHelper.GetReservationInstances(
				ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(tentativeJob.Id))).Single();
			reservation.Status = ReservationStatus.Ended;
			resourceManagerHelper.AddOrUpdateReservationInstances(reservation);

			var confirmedJob = api.Jobs.Confirm(tentativeJob.Id);

			Assert.AreEqual(JobState.Completed, confirmedJob.State, "Expected the job to follow its ended reservation.");
			Assert.AreEqual(JobState.Completed, api.Jobs.Read(tentativeJob.Id).State);
			Assert.AreEqual(ReservationStatus.Ended, GetReservationStatus(resourceManagerHelper, tentativeJob.Id), "Expected the ended reservation not to be pushed back to Confirmed.");
		}
	}
}
