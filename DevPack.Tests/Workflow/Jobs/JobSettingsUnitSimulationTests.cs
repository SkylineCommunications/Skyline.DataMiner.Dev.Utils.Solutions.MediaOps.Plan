namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;
	using System.Linq;
	using System.Threading;

	using RT_MediaOps.Plan.Extensions;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Simulation;

	/// <summary>
	/// Verifies that the orchestration settings of one job are saved as a unit: either all of them or none.
	/// </summary>
	[TestClass]
	public sealed class JobSettingsUnitSimulationTests
	{
		[TestMethod]
		public void Update_OneNodeSettingsCannotBeLocked_LeavesTheOtherNodeSettingsUntouched()
		{
			var api = MediaOpsPlanSimulation.Create().CreateConnection().GetMediaOpsPlanApi();
			var prefix = Guid.NewGuid();

			var capacity = (NumberCapacity)api.Capacities.Create(new NumberCapacity { Name = $"{prefix}_Capacity" });
			var pool = api.ResourcePools.Complete(api.ResourcePools.Create(new ResourcePool { Name = $"{prefix}_Pool" }));

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource", Concurrency = 10 };
			resource.AddCapacity(new NumberCapacitySetting(capacity) { Value = 100 });
			resource.AssignToPool(pool);
			var completedResource = api.Resources.Complete(api.Resources.Create(resource));

			var currentTime = DateTime.UtcNow.RoundToNextSecond();
			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime.AddHours(1),
				End = currentTime.AddHours(2),
				PreRollStart = currentTime.AddHours(1),
				PostRollEnd = currentTime.AddHours(2),
			};

			for (int i = 0; i < 2; i++)
			{
				var node = new JobResourceNode(pool, completedResource);
				node.OrchestrationSettings.AddCapacity(new NumberCapacitySetting(capacity) { Value = 10 });
				job.NodeGraph.Add(node);
			}

			job = api.Jobs.Create(job);

			var lockedNode = job.NodeGraph.Nodes.First();
			var otherNode = job.NodeGraph.Nodes.Last();
			((NumberCapacitySetting)lockedNode.OrchestrationSettings.Capacities.Single()).Value = 20;
			((NumberCapacitySetting)otherNode.OrchestrationSettings.Capacities.Single()).Value = 30;

			// The in-memory lock manager of the API refuses a second lock on the same settings, like a concurrent save.
			((MediaOpsPlanApi)api).LockManager.LockAndExecute([lockedNode.OrchestrationSettings], _ =>
			{
				var exception = Assert.ThrowsException<MediaOpsBulkException<Guid>>(() => api.Jobs.Update([job]));
				Assert.IsTrue(exception.Result.TraceDataPerItem.ContainsKey(job.Id), "No trace data reported for the job.");
			});

			var stored = api.Jobs.Read(job.Id).NodeGraph.Nodes;
			Assert.AreEqual(10m, ((NumberCapacitySetting)stored.Single(x => x.Id == lockedNode.Id).OrchestrationSettings.Capacities.Single()).Value);
			Assert.AreEqual(10m, ((NumberCapacitySetting)stored.Single(x => x.Id == otherNode.Id).OrchestrationSettings.Capacities.Single()).Value, "The settings of the other node were saved without the locked ones.");
		}

		[TestMethod]
		public void Read_AfterRejectedUpdateWhileAnotherThreadHasAConfigurationScopeOpen_ReturnsTheStoredSettings()
		{
			var api = MediaOpsPlanSimulation.Create().CreateConnection().GetMediaOpsPlanApi();
			var prefix = Guid.NewGuid();

			var capacity = (NumberCapacity)api.Capacities.Create(new NumberCapacity { Name = $"{prefix}_Capacity" });
			var pool = api.ResourcePools.Complete(api.ResourcePools.Create(new ResourcePool { Name = $"{prefix}_Pool" }));

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" };
			resource.AddCapacity(new NumberCapacitySetting(capacity) { Value = 100 });
			resource.AssignToPool(pool);
			var completedResource = api.Resources.Complete(api.Resources.Create(resource));

			var currentTime = DateTime.UtcNow.RoundToNextSecond();
			var node = new JobResourceNode(pool, completedResource);
			node.OrchestrationSettings.AddCapacity(new NumberCapacitySetting(capacity) { Value = 10 });

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime.AddHours(1),
				End = currentTime.AddHours(2),
				PreRollStart = currentTime.AddHours(1),
				PostRollEnd = currentTime.AddHours(2),
			};
			job.NodeGraph.Add(node);
			job = api.Jobs.Create(job);

			using (var scopeOpened = new ManualResetEventSlim())
			using (var release = new ManualResetEventSlim())
			{
				// Another operation on the same API instance, for example a concurrent read on another thread.
				var otherThread = new Thread(() =>
				{
					using (((MediaOpsPlanApi)api).DomHelpers.SlcWorkflowHelper.BeginConfigurationScope())
					{
						scopeOpened.Set();
						release.Wait();
					}
				});
				otherThread.Start();
				scopeOpened.Wait();

				try
				{
					// Duplicate capacity settings are rejected by the settings validation inside the lock.
					job.NodeGraph.Nodes.Single().OrchestrationSettings.AddCapacity(new NumberCapacitySetting(capacity) { Value = 20 });
					Assert.ThrowsException<MediaOpsBulkException<Guid>>(() => api.Jobs.Update([job]));

					var stored = api.Jobs.Read(job.Id).NodeGraph.Nodes.Single().OrchestrationSettings;
					Assert.AreEqual(1, stored.Capacities.Count, "The settings of the rejected update leaked to another thread.");
				}
				finally
				{
					release.Set();
					otherThread.Join();
				}
			}
		}
	}
}
