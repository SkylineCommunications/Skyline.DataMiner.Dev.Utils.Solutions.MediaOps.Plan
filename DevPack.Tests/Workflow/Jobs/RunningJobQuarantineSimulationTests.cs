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

	/// <summary>
	/// Verifies that lifting the quarantine of a running job leaves its reservation running. The simulation has no SRM
	/// engine, so the reservation status is set explicitly to bring the job into the Running state.
	/// </summary>
	[TestClass]
	public sealed class RunningJobQuarantineSimulationTests
	{
		[TestMethod]
		public void LowerRequiredCapacityOfQuarantinedNode_WhileRunning_ClearsTheQuarantineAndKeepsReservationOngoing()
		{
			var (api, resourceManagerHelper) = CreateContext();
			var quarantinedJob = CreateQuarantinedRunningJob(api, resourceManagerHelper);
			var eventCount = GetReservation(resourceManagerHelper, quarantinedJob.Id).Events.Count;

			// The resource only offers 20.
			((NumberCapacitySetting)quarantinedJob.NodeGraph.Nodes.Single().OrchestrationSettings.Capacities.Single()).Value = 10;
			var updatedJob = api.Jobs.Update(quarantinedJob);

			Assert.AreEqual(JobState.Running, updatedJob.State, "Expected the job to remain in the Running state.");
			Assert.IsFalse(updatedJob.Errors.Any(x => x.Code == QuarantinedReservationJobValidationError.ErrorCode), "Expected the quarantine error to be cleared.");

			var reservation = GetReservation(resourceManagerHelper, updatedJob.Id);
			Assert.IsFalse(reservation.IsQuarantined, "Expected the reservation to be out of quarantine.");
			Assert.AreEqual(ReservationStatus.Ongoing, reservation.Status, "Expected the reservation to stay ongoing.");
			Assert.AreEqual(eventCount, reservation.Events.Count, "Expected no reservation events to be added.");
		}

		[TestMethod]
		public void LowerRequiredCapacityOfQuarantinedNodeToValueThatStillDoesNotFit_WhileRunning_KeepsTheQuarantineAndReservationOngoing()
		{
			var (api, resourceManagerHelper) = CreateContext();
			var quarantinedJob = CreateQuarantinedRunningJob(api, resourceManagerHelper);
			var eventCount = GetReservation(resourceManagerHelper, quarantinedJob.Id).Events.Count;

			// The resource only offers 20.
			((NumberCapacitySetting)quarantinedJob.NodeGraph.Nodes.Single().OrchestrationSettings.Capacities.Single()).Value = 30;
			var updatedJob = api.Jobs.Update(quarantinedJob);

			Assert.AreEqual(JobState.Running, updatedJob.State, "Expected the job to remain in the Running state.");
			Assert.IsTrue(updatedJob.Errors.Any(x => x.Code == QuarantinedReservationJobValidationError.ErrorCode), "Expected the quarantine error to remain.");

			var reservation = GetReservation(resourceManagerHelper, updatedJob.Id);
			Assert.IsTrue(reservation.IsQuarantined, "Expected the reservation to remain quarantined.");
			Assert.AreEqual(ReservationStatus.Ongoing, reservation.Status, "Expected the reservation to stay ongoing.");
			Assert.AreEqual(eventCount, reservation.Events.Count, "Expected no reservation events to be added.");
		}

		private static (IMediaOpsPlanApi Api, ResourceManagerHelper ResourceManagerHelper) CreateContext()
		{
			var dms = MediaOpsPlanSimulation.Create();
			var connection = dms.CreateConnection();

			return (connection.GetMediaOpsPlanApi(), new ResourceManagerHelper(connection.HandleSingleResponseMessage));
		}

		// A running job requiring 50 of a resource of which the capacity is forced down from 100 to 20.
		private static Job CreateQuarantinedRunningJob(IMediaOpsPlanApi api, ResourceManagerHelper resourceManagerHelper)
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var capacity = (NumberCapacity)api.Capacities.Create(new NumberCapacity { Name = $"{prefix}_Capacity", RangeMin = 0, RangeMax = 100 });

			var pool = api.ResourcePools.Complete(api.ResourcePools.Create(new ResourcePool { Name = $"{prefix}_Pool" }));

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(pool);
			resource.AddCapacity(new NumberCapacitySetting(capacity) { Value = 100 });
			resource = api.Resources.Complete(api.Resources.Create(resource));

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime.AddMinutes(-10),
				End = currentTime.AddMinutes(20),
				PreRollStart = currentTime.AddMinutes(-10),
				PostRollEnd = currentTime.AddMinutes(20),
			};
			var node = new JobResourceNode(pool, resource);
			node.OrchestrationSettings.AddCapacity(new NumberCapacitySetting(capacity) { Value = 50 });
			job.NodeGraph.Add(node);

			var confirmedJob = api.Jobs.Confirm(api.Jobs.SaveAsTentative(api.Jobs.Create(job)));

			var reservation = GetReservation(resourceManagerHelper, confirmedJob.Id);
			reservation.Status = ReservationStatus.Ongoing;
			resourceManagerHelper.AddOrUpdateReservationInstances(reservation);
			var runningJob = api.Jobs.TransitionToRunning(confirmedJob);

			var coreResource = resourceManagerHelper.GetResource(resource.CoreResourceId);
			coreResource.Capacities.Single().Value.MaxDecimalQuantity = 20;
			resourceManagerHelper.AddOrUpdateResources(true, [coreResource]);

			Assert.IsTrue(GetReservation(resourceManagerHelper, runningJob.Id).IsQuarantined, "Expected the reservation to be quarantined after lowering the capacity of the resource.");

			var quarantinedJob = api.Jobs.Read(runningJob.Id);
			api.Jobs.Validate([quarantinedJob]).Single().SyncToJob();
			quarantinedJob = api.Jobs.Update(quarantinedJob);

			Assert.AreEqual(JobState.Running, quarantinedJob.State, "Expected the job to be running.");
			Assert.IsTrue(quarantinedJob.Errors.Any(x => x.Code == QuarantinedReservationJobValidationError.ErrorCode), "Expected the quarantine error on the job.");

			return quarantinedJob;
		}

		private static ReservationInstance GetReservation(ResourceManagerHelper resourceManagerHelper, Guid jobId)
		{
			return resourceManagerHelper.GetReservationInstances(
				ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(jobId))).Single();
		}
	}
}
