namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.Extensions;
	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	/// <summary>
	/// Verifies that the orchestration settings of a job are only written once the whole save succeeds, so a refusal
	/// by the Service &amp; Resource Management never leaves a changed capability or capacity behind on a node.
	/// </summary>
	[TestClass]
	[TestCategory("IntegrationTest")]
	[DoNotParallelize]
	public sealed class JobSettingsAtomicityTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public JobSettingsAtomicityTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void Update_ReservationRefusesTheChange_LeavesTheStoredSettingsUntouched()
		{
			var setup = CreateSetup();

			// Occupies most of the resource's capacity for the shared window.
			Confirm(objectCreator.CreateJob(NewJob(setup, 80)));

			var job = Confirm(objectCreator.CreateJob(NewJob(setup, 10)));

			((NumberCapacitySetting)job.NodeGraph.Nodes.Single().OrchestrationSettings.Capacities.Single()).Value = 90;

			var exception = Assert.ThrowsException<MediaOpsBulkException<Guid>>(() => TestContext.Api.Jobs.Update([job]));
			Assert.IsTrue(exception.Result.TraceDataPerItem.ContainsKey(job.Id), "No trace data reported for the job.");

			var stored = TestContext.Api.Jobs.Read(job.Id).NodeGraph.Nodes.Single().OrchestrationSettings;
			Assert.AreEqual(10m, ((NumberCapacitySetting)stored.Capacities.Single()).Value, "The refused capacity was written to the node anyway.");
		}

		[TestMethod]
		public void Update_SettingsFailValidation_LeavesTheStoredSettingsUntouched()
		{
			var setup = CreateSetup();
			var job = objectCreator.CreateJob(NewJob(setup, 10));

			// Duplicate capacity settings are only rejected by the orchestration settings validation inside the lock.
			job.NodeGraph.Nodes.Single().OrchestrationSettings.AddCapacity(new NumberCapacitySetting(setup.Capacity) { Value = 20 });

			var exception = Assert.ThrowsException<MediaOpsBulkException<Guid>>(() => TestContext.Api.Jobs.Update([job]));
			Assert.IsTrue(exception.Result.TraceDataPerItem.ContainsKey(job.Id), "No trace data reported for the job.");

			var stored = TestContext.Api.Jobs.Read(job.Id).NodeGraph.Nodes.Single().OrchestrationSettings;
			Assert.AreEqual(1, stored.Capacities.Count, "The rejected settings were written to the node anyway.");
			Assert.AreEqual(10m, ((NumberCapacitySetting)stored.Capacities.Single()).Value);
		}

		[TestMethod]
		public void Update_AddsANodeWithSettings_PersistsTheSettingsOfTheNewNode()
		{
			var setup = CreateSetup();
			var job = objectCreator.CreateJob(NewJob(setup, 10));

			var added = new JobResourceNode(setup.Pool, setup.Resource);
			added.OrchestrationSettings.AddCapacity(new NumberCapacitySetting(setup.Capacity) { Value = 20 });
			job.NodeGraph.Add(added);

			TestContext.Api.Jobs.Update([job]);

			var stored = TestContext.Api.Jobs.Read(job.Id);
			var storedNode = stored.NodeGraph.Nodes.Single(x => x.Id == added.Id);
			Assert.AreEqual(20m, ((NumberCapacitySetting)storedNode.OrchestrationSettings.Capacities.Single()).Value);
		}

		private static Job Confirm(Job job)
		{
			return TestContext.Api.Jobs.Confirm(TestContext.Api.Jobs.SaveAsTentative(job));
		}

		private static Job NewJob(Setup setup, decimal capacity)
		{
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var node = new JobResourceNode(setup.Pool, setup.Resource);
			node.OrchestrationSettings.AddCapacity(new NumberCapacitySetting(setup.Capacity) { Value = capacity });

			var job = new Job
			{
				Name = $"{setup.Prefix}_{Guid.NewGuid()}_Job",
				Start = currentTime.AddHours(1),
				End = currentTime.AddHours(2),
				PreRollStart = currentTime.AddHours(1),
				PostRollEnd = currentTime.AddHours(2),
			};

			job.NodeGraph.Add(node);
			return job;
		}

		private Setup CreateSetup()
		{
			var prefix = Guid.NewGuid();

			var capacity = new NumberCapacity { Name = $"{prefix}_Capacity" };
			objectCreator.CreateCapacities([capacity]);

			var pool = TestContext.Api.ResourcePools.Complete(objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" }));

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource", Concurrency = 10 };
			resource.AddCapacity(new NumberCapacitySetting(capacity) { Value = 100 });
			resource.AssignToPool(pool);

			return new Setup(prefix, pool, TestContext.Api.Resources.Complete(objectCreator.CreateResource(resource)), capacity);
		}

		private sealed class Setup
		{
			public Setup(Guid prefix, ResourcePool pool, Resource resource, NumberCapacity capacity)
			{
				Prefix = prefix;
				Pool = pool;
				Resource = resource;
				Capacity = capacity;
			}

			public Guid Prefix { get; }

			public ResourcePool Pool { get; }

			public Resource Resource { get; }

			public NumberCapacity Capacity { get; }
		}
	}
}
