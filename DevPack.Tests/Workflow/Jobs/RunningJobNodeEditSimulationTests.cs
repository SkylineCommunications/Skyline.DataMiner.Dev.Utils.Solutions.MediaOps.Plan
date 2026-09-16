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
	/// Deterministic, simulation-backed tests that verify that editing the node graph of a running job does not alter
	/// the status of its core reservation. The simulation has no SRM engine, so the reservation status is controlled
	/// explicitly to bring a job into the Running state and to reflect that its reservation has ended.
	/// </summary>
	[TestClass]
	public sealed class RunningJobNodeEditSimulationTests
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
		/// Creates a job that is already running. The job is created with a start time that lies in the (recent) past so
		/// the confirmed-to-running transition passes its pre-roll guard without a manual start.
		/// </summary>
		private static Job CreateRunningJob(IMediaOpsPlanApi api, ResourceManagerHelper resourceManagerHelper)
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var (pool, resource) = CreatePoolAndResource(api, prefix);

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime.AddMinutes(-10),
				End = currentTime.AddMinutes(20),
				PreRollStart = currentTime.AddMinutes(-10),
				PostRollEnd = currentTime.AddMinutes(20),
			};

			job.NodeGraph.Add(new JobResourceNode(pool, resource));
			job = api.Jobs.Create(job);

			var tentativeJob = api.Jobs.SaveAsTentative(job);
			var confirmedJob = api.Jobs.Confirm(tentativeJob);

			// Deterministically mark the reservation as ongoing, mirroring what SRM does on a live agent once the
			// reservation start time is reached, then complete the confirmed-to-running transition.
			SetReservationStatus(resourceManagerHelper, confirmedJob.Id, ReservationStatus.Ongoing);

			return api.Jobs.TransitionToRunning(confirmedJob);
		}

		/// <summary>
		/// Creates a job that is running in its post-roll. Stopping a running job with a new post-roll end moves its end
		/// to now while keeping it Running, which leaves only the post-roll ahead of it.
		/// </summary>
		private static Job CreateRunningJobInPostRoll(IMediaOpsPlanApi api, ResourceManagerHelper resourceManagerHelper)
		{
			var runningJob = CreateRunningJob(api, resourceManagerHelper);
			var newPostRollEnd = DateTime.UtcNow.RoundToNextSecond().AddMinutes(15);

			return api.Jobs.Stop(runningJob, new JobStopOptions { NewPostRollEnd = newPostRollEnd });
		}

		private static ReservationInstance GetReservation(ResourceManagerHelper resourceManagerHelper, Guid jobId)
		{
			var reservation = resourceManagerHelper.GetReservationInstances(
				ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(jobId))).FirstOrDefault();
			Assert.IsNotNull(reservation, "Expected a core reservation for the job.");

			return reservation;
		}

		private static void SetReservationStatus(ResourceManagerHelper resourceManagerHelper, Guid jobId, ReservationStatus status)
		{
			var reservation = GetReservation(resourceManagerHelper, jobId);

			reservation.Status = status;
			resourceManagerHelper.AddOrUpdateReservationInstances(reservation);
		}

		[TestMethod]
		public void AddNode_WhileRunning_KeepsReservationOngoing()
		{
			var (api, resourceManagerHelper) = CreateContext();

			var runningJob = CreateRunningJob(api, resourceManagerHelper);

			var (pool, resource) = CreatePoolAndResource(api, Guid.NewGuid());
			runningJob.NodeGraph.Add(new JobResourceNode(pool, resource));

			var updatedJob = api.Jobs.Update(runningJob);

			Assert.AreEqual(JobState.Running, updatedJob.State, "Expected the job to remain in the Running state.");
			Assert.AreEqual(
				ReservationStatus.Ongoing,
				GetReservation(resourceManagerHelper, updatedJob.Id).Status,
				"Expected the core reservation to stay ongoing after a node was added to a running job.");
		}

		[TestMethod]
		public void RemoveNode_WhileRunning_KeepsReservationOngoing()
		{
			var (api, resourceManagerHelper) = CreateContext();

			var runningJob = CreateRunningJob(api, resourceManagerHelper);

			var (pool, resource) = CreatePoolAndResource(api, Guid.NewGuid());
			runningJob.NodeGraph.Add(new JobResourceNode(pool, resource));
			runningJob = api.Jobs.Update(runningJob);

			runningJob.NodeGraph.Remove(runningJob.NodeGraph.Nodes.Last());
			var updatedJob = api.Jobs.Update(runningJob);

			Assert.AreEqual(
				ReservationStatus.Ongoing,
				GetReservation(resourceManagerHelper, updatedJob.Id).Status,
				"Expected the core reservation to stay ongoing after a node was removed from a running job.");
		}

		[TestMethod]
		public void TransitionToCompleted_AfterNodeEditWhileRunning_MovesJobToCompleted()
		{
			var (api, resourceManagerHelper) = CreateContext();

			var runningJob = CreateRunningJob(api, resourceManagerHelper);

			var (pool, resource) = CreatePoolAndResource(api, Guid.NewGuid());
			runningJob.NodeGraph.Add(new JobResourceNode(pool, resource));

			var updatedJob = api.Jobs.Update(runningJob);

			// Stopping the running job moves its end and post-roll end to (approximately) now while keeping it Running, so
			// the post-roll end time guard passes on the completion attempt below.
			var stoppedJob = api.Jobs.Stop(updatedJob);

			// Deterministically mark the reservation as ended, mirroring what SRM does on a live agent once the
			// reservation end time is reached.
			SetReservationStatus(resourceManagerHelper, stoppedJob.Id, ReservationStatus.Ended);

			var completedJob = api.Jobs.TransitionToCompleted(stoppedJob);

			Assert.AreEqual(
				JobState.Completed,
				completedJob.State,
				"Expected the job to be completed even though its node graph was edited while it was running.");
		}

		[TestMethod]
		public void SwapNode_WhileRunningInPostRoll_IsRejected()
		{
			var (api, resourceManagerHelper) = CreateContext();

			var job = CreateRunningJobInPostRoll(api, resourceManagerHelper);

			var originalNode = job.NodeGraph.Nodes.Single();
			var (pool, resource) = CreatePoolAndResource(api, Guid.NewGuid());
			var replacement = new JobResourceNode(pool, resource);

			job.NodeGraph.Swap(originalNode, replacement);

			var error = AssertUpdateFails<JobNodeSwappedInPostRollNotAllowedError>(api, job);
			Assert.AreEqual(originalNode.Id, error.NodeId);
			Assert.AreEqual(replacement.Id, error.TargetNodeId);

			var read = api.Jobs.Read(job.Id);
			Assert.AreEqual(originalNode.Id, read.NodeGraph.Nodes.Single().Id, "Expected the node graph to be left untouched.");
		}

		[TestMethod]
		public void AddNode_WhileRunningInPostRoll_IsRejected()
		{
			var (api, resourceManagerHelper) = CreateContext();

			var job = CreateRunningJobInPostRoll(api, resourceManagerHelper);

			var (pool, resource) = CreatePoolAndResource(api, Guid.NewGuid());
			var addedNode = new JobResourceNode(pool, resource);
			job.NodeGraph.Add(addedNode);

			var error = AssertUpdateFails<JobNodeAddedInPostRollNotAllowedError>(api, job);
			Assert.AreEqual(addedNode.Id, error.NodeId);
		}

		[TestMethod]
		public void RemoveNode_WhileRunningInPostRoll_IsRejected()
		{
			var (api, resourceManagerHelper) = CreateContext();

			var job = CreateRunningJobInPostRoll(api, resourceManagerHelper);

			var removedNode = job.NodeGraph.Nodes.Single();
			job.NodeGraph.Remove(removedNode);

			var error = AssertUpdateFails<JobNodeRemovedInPostRollNotAllowedError>(api, job);
			Assert.AreEqual(removedNode.Id, error.NodeId);
		}

		[TestMethod]
		public void UpdateWithoutNodeGraphChanges_WhileRunningInPostRoll_IsAllowed()
		{
			var (api, resourceManagerHelper) = CreateContext();

			var job = CreateRunningJobInPostRoll(api, resourceManagerHelper);
			job.Description = "Updated while running in the post-roll.";

			var updatedJob = api.Jobs.Update(job);

			Assert.AreEqual("Updated while running in the post-roll.", updatedJob.Description);
		}

		private static TError AssertUpdateFails<TError>(IMediaOpsPlanApi api, Job job)
			where TError : MediaOpsErrorData
		{
			try
			{
				api.Jobs.Update(job);
				Assert.Fail($"Expected a {typeof(TError).Name} but the update succeeded.");
				return null;
			}
			catch (MediaOpsException ex)
			{
				var error = ex.TraceData.ErrorData.OfType<TError>().SingleOrDefault();
				Assert.IsNotNull(error, $"Expected a {typeof(TError).Name}.");

				return error;
			}
		}
	}
}
