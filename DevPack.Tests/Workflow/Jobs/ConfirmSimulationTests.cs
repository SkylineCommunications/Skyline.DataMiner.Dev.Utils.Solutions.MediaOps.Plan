namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.Extensions;

	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.ResourceManager.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
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
	}
}
