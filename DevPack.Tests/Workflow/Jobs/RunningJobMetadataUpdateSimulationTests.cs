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
	using PlanResource = Skyline.DataMiner.Solutions.MediaOps.Plan.API.Resource;

	/// <summary>
	/// Deterministic, simulation-backed tests that verify that the metadata of a running job can still be updated. The
	/// simulation has no SRM engine, so the reservation status is controlled explicitly to bring a job into the Running
	/// state without depending on a live scheduling timeline.
	/// </summary>
	[TestClass]
	public sealed class RunningJobMetadataUpdateSimulationTests
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

		/// <summary>
		/// Creates a job that is running after a manual start, so its pre-roll start (and, without a pre-roll, its start)
		/// reflect the actual core reservation start instead of the originally planned times.
		/// </summary>
		private static Job CreateManuallyStartedJob(IMediaOpsPlanApi api, ResourceManagerHelper resourceManagerHelper, bool withPreRoll)
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var (pool, resource) = CreatePoolAndResource(api, prefix);

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime.AddMinutes(10),
				End = currentTime.AddMinutes(20),
				PreRollStart = withPreRoll ? currentTime.AddMinutes(9) : currentTime.AddMinutes(10),
				PostRollEnd = currentTime.AddMinutes(20),
			};

			job.NodeGraph.Add(new JobResourceNode(pool, resource));
			job = api.Jobs.Create(job);

			var tentativeJob = api.Jobs.SaveAsTentative(job);
			var confirmedJob = api.Jobs.Confirm(tentativeJob);

			var startedJob = api.Jobs.Start(confirmedJob);

			var reservation = resourceManagerHelper.GetReservationInstances(
				ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(startedJob.Id))).FirstOrDefault();
			Assert.IsNotNull(reservation, "Expected a core reservation for the job.");

			reservation.Status = ReservationStatus.Ongoing;
			resourceManagerHelper.AddOrUpdateReservationInstances(reservation);

			return api.Jobs.TransitionToRunning(startedJob);
		}

		[DataTestMethod]
		[DataRow(true)]
		[DataRow(false)]
		public void Update_DescriptionOfManuallyStartedRunningJob_Succeeds(bool withPreRoll)
		{
			var (api, resourceManagerHelper) = CreateContext();

			var runningJob = CreateManuallyStartedJob(api, resourceManagerHelper, withPreRoll);
			Assert.AreEqual(JobState.Running, runningJob.State, "Expected the job to be running.");

			runningJob.Description = "Updated description";

			var updatedJob = api.Jobs.Update(runningJob);

			Assert.AreEqual("Updated description", updatedJob.Description, "Expected the description of the running job to be updated.");
			Assert.AreEqual(JobState.Running, updatedJob.State, "Expected the job to remain in the Running state.");
		}
	}
}
