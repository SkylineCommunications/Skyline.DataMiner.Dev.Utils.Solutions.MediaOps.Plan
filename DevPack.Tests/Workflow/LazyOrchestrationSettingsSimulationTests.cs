namespace RT_MediaOps.Plan.Workflow
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using RT_MediaOps.Plan.Extensions;
	using RT_MediaOps.Plan.Simulation;

	using Skyline.DataMiner.Solutions.MediaOps.Live.API;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Extensions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Simulation;

	/// <summary>
	/// Verifies that the orchestration settings of jobs, recurring jobs, workflows and resource pools are only read when
	/// they are accessed, in a single read for the owner and all of its nodes.
	/// </summary>
	[TestClass]
	public sealed class LazyOrchestrationSettingsSimulationTests
	{
		[TestMethod]
		public void ReadJob_ReadsOrchestrationSettingsOnlyOnFirstAccess_ForTheJobAndAllNodesAtOnce()
		{
			var setup = new Setup();
			var jobId = setup.CreateJob(nodeCount: 2).Id;

			setup.Counter.Reset();
			var job = setup.ReadApi.Jobs.Read(jobId);
			_ = job.ConfigurationState;
			_ = job.NodeGraph.Nodes.Select(x => x.ConfigurationState).ToList();

			Assert.AreEqual(0, setup.Counter.ConfigurationReads, "Reading the job read its orchestration settings.");
			Assert.AreEqual(0, setup.Counter.ProfileParameterReads, "Reading the job read profile parameters.");
			Assert.IsFalse(job.HasChanges);

			var firstNode = job.NodeGraph.Nodes.First();
			Assert.AreEqual(10m, ((NumberCapacitySetting)firstNode.OrchestrationSettings.Capacities.Single()).Value);
			Assert.AreEqual(1, setup.Counter.ConfigurationReads);
			Assert.AreEqual(1, setup.Counter.ProfileParameterReads, "The profile parameters of the job, its nodes and its events are not read in one call.");

			Assert.AreEqual("Job", ((TextConfigurationSetting)job.OrchestrationSettings.Configurations.Single()).Value);
			Assert.AreEqual("Event", ((TextConfigurationSetting)job.OrchestrationSettings.OrchestrationEvents.Single().ExecutionDetails.Configurations.Single()).Value);
			Assert.AreEqual(10m, ((NumberCapacitySetting)job.NodeGraph.Nodes.Last().OrchestrationSettings.Capacities.Single()).Value);
			Assert.AreEqual(1, setup.Counter.ConfigurationReads, "The settings of the job and its other node were read separately.");
			Assert.AreEqual(1, setup.Counter.ProfileParameterReads);

			Assert.IsFalse(job.HasChanges, "Accessing the settings without changing them marked the job as changed.");
		}

		[TestMethod]
		public void ReadJobs_ReadsOrchestrationSettingsPerJob_AndSharesTheProfileParameters()
		{
			var setup = new Setup();
			var jobIds = new[] { setup.CreateJob(nodeCount: 1).Id, setup.CreateJob(nodeCount: 1).Id };

			setup.Counter.Reset();
			var jobs = setup.ReadApi.Jobs.Read(jobIds).ToList();

			_ = jobs[0].NodeGraph.Nodes.Single().OrchestrationSettings.Capacities;
			Assert.AreEqual(1, setup.Counter.ConfigurationReads);
			Assert.AreEqual(1, setup.Counter.ProfileParameterReads);

			_ = jobs[1].NodeGraph.Nodes.Single().OrchestrationSettings.Capacities;
			Assert.AreEqual(2, setup.Counter.ConfigurationReads);
			Assert.AreEqual(1, setup.Counter.ProfileParameterReads, "The profile parameters were read again for a job of the same read.");
		}

		[TestMethod]
		public void UpdateJob_SettingsNeverAccessed_KeepsTheStoredSettings()
		{
			var setup = new Setup();
			var created = setup.CreateJob(nodeCount: 1);
			var nodeSettingsId = created.NodeGraph.Nodes.Single().OrchestrationSettings.Id;

			var job = setup.ReadApi.Jobs.Read(created.Id);
			job.Description = "Changed";
			setup.ReadApi.Jobs.Update([job]);

			var stored = setup.Api.Jobs.Read(created.Id);
			Assert.AreEqual("Changed", stored.Description);
			Assert.AreEqual(created.OrchestrationSettings.Id, stored.OrchestrationSettings.Id);
			Assert.AreEqual(nodeSettingsId, stored.NodeGraph.Nodes.Single().OrchestrationSettings.Id);
			Assert.AreEqual("Job", ((TextConfigurationSetting)stored.OrchestrationSettings.Configurations.Single()).Value);
			Assert.AreEqual(10m, ((NumberCapacitySetting)stored.NodeGraph.Nodes.Single().OrchestrationSettings.Capacities.Single()).Value);
		}

		[TestMethod]
		public void UpdateJob_OnlyLazilyReadNodeSettingsChanged_IsTrackedAndPersisted()
		{
			var setup = new Setup();
			var jobId = setup.CreateJob(nodeCount: 1).Id;

			var job = setup.ReadApi.Jobs.Read(jobId);
			((NumberCapacitySetting)job.NodeGraph.Nodes.Single().OrchestrationSettings.Capacities.Single()).Value = 20;

			Assert.IsTrue(job.HasChanges, "A change to lazily read node settings is not tracked on the job.");

			setup.ReadApi.Jobs.Update([job]);

			var stored = setup.Api.Jobs.Read(jobId);
			Assert.AreEqual(20m, ((NumberCapacitySetting)stored.NodeGraph.Nodes.Single().OrchestrationSettings.Capacities.Single()).Value);
		}

		[TestMethod]
		public void ConfigurationStateCalculator_ForMultipleJobs_ReadsAllOrchestrationSettingsAtOnce()
		{
			var setup = new Setup();
			var jobIds = Enumerable.Range(0, 3).Select(_ => setup.CreateJob(nodeCount: 2).Id).ToList();
			var jobs = setup.ReadApi.Jobs.Read(jobIds).ToList();

			setup.Counter.Reset();
			var calculator = new ConfigurationStateCalculator(setup.ReadApi, setup.LiveApi, jobs);
			_ = jobs.Select(x => calculator.GetJobConfigurationState(x)).ToList();

			Assert.AreEqual(1, setup.Counter.ConfigurationReads);
			Assert.AreEqual(1, setup.Counter.ProfileParameterReads);
		}

		[TestMethod]
		public void ValidateJobs_ReadsAllOrchestrationSettingsAtOnce()
		{
			var setup = new Setup();
			var jobIds = Enumerable.Range(0, 3).Select(_ => setup.CreateJob(nodeCount: 2).Id).ToList();
			var jobs = setup.ReadApi.Jobs.Read(jobIds).ToList();

			setup.Counter.Reset();
			setup.ReadApi.Jobs.Validate(jobs);

			Assert.AreEqual(1, setup.Counter.ConfigurationReads, "The orchestration settings were read once per job.");
		}

		[TestMethod]
		public void ReadRecurringJob_ReadsOrchestrationSettingsOnlyOnFirstAccess()
		{
			var setup = new Setup();
			var recurringJob = new RecurringJob
			{
				Name = $"{setup.Prefix}_RecurringJob",
				Start = DateTime.UtcNow.AddHours(1),
				Duration = TimeSpan.FromHours(1),
			};
			recurringJob.Pattern.RepeatType = RepeatType.Daily;
			recurringJob.Pattern.RepeatEvery = 1;
			recurringJob.Pattern.EndDate = DateTime.UtcNow.AddDays(10);
			recurringJob.OrchestrationSettings.AddConfiguration(new TextConfigurationSetting(setup.TextConfiguration) { Value = "RecurringJob" });
			var recurringJobId = setup.Api.RecurringJobs.Create(recurringJob).Id;

			setup.Counter.Reset();
			var read = setup.ReadApi.RecurringJobs.Read(recurringJobId);
			Assert.AreEqual(0, setup.Counter.ConfigurationReads);
			Assert.IsFalse(read.HasChanges);

			Assert.AreEqual("RecurringJob", ((TextConfigurationSetting)read.OrchestrationSettings.Configurations.Single()).Value);
			Assert.AreEqual(1, setup.Counter.ConfigurationReads);
			Assert.AreEqual(1, setup.Counter.ProfileParameterReads);
		}

		[TestMethod]
		public void ReadWorkflow_ReadsOrchestrationSettingsOnlyOnFirstAccess()
		{
			var setup = new Setup();
			var workflow = new Workflow { Name = $"{setup.Prefix}_Workflow" };
			workflow.OrchestrationSettings.AddConfiguration(new TextConfigurationSetting(setup.TextConfiguration) { Value = "Workflow" });
			var workflowId = setup.Api.Workflows.Create(workflow).Id;

			setup.Counter.Reset();
			var read = setup.ReadApi.Workflows.Read(workflowId);
			Assert.AreEqual(0, setup.Counter.ConfigurationReads);
			Assert.IsFalse(read.HasChanges);

			Assert.AreEqual("Workflow", ((TextConfigurationSetting)read.OrchestrationSettings.Configurations.Single()).Value);
			Assert.AreEqual(1, setup.Counter.ConfigurationReads);
			Assert.AreEqual(1, setup.Counter.ProfileParameterReads);
		}

		[TestMethod]
		public void ReadResourcePool_ReadsOrchestrationSettingsOnlyOnFirstAccess_AndTracksChanges()
		{
			var setup = new Setup();
			var pool = new ResourcePool { Name = $"{setup.Prefix}_OtherPool" };
			pool.OrchestrationSettings.AddConfiguration(new TextConfigurationSetting(setup.TextConfiguration) { Value = "Pool" });
			var poolId = setup.Api.ResourcePools.Create(pool).Id;

			setup.Counter.Reset();
			var read = setup.ReadApi.ResourcePools.Read(poolId);
			Assert.AreEqual(0, setup.Counter.ConfigurationReads);
			Assert.IsFalse(read.HasChanges);

			var setting = (TextConfigurationSetting)read.OrchestrationSettings.Configurations.Single();
			Assert.AreEqual("Pool", setting.Value);
			Assert.AreEqual(1, setup.Counter.ConfigurationReads);
			Assert.AreEqual(1, setup.Counter.ProfileParameterReads);
			Assert.IsFalse(read.HasChanges);

			setting.Value = "Changed";
			Assert.IsTrue(read.HasChanges, "A change to lazily read pool settings is not tracked on the pool.");

			setup.ReadApi.ResourcePools.Update(read);
			Assert.AreEqual("Changed", ((TextConfigurationSetting)setup.Api.ResourcePools.Read(poolId).OrchestrationSettings.Configurations.Single()).Value);
		}

		private sealed class Setup
		{
			internal Setup()
			{
				var dms = MediaOpsPlanSimulation.Create();
				var connection = dms.CreateConnection();
				Api = connection.GetMediaOpsPlanApi();
				LiveApi = connection.GetMediaOpsLiveApi();

				Counter = new CountingConnection(dms.CreateConnection());
				ReadApi = Counter.GetMediaOpsPlanApi();

				Capacity = (NumberCapacity)Api.Capacities.Create(new NumberCapacity { Name = $"{Prefix}_Capacity" });
				TextConfiguration = (TextConfiguration)Api.Configurations.Create(new TextConfiguration { Name = $"{Prefix}_Text" });
				Pool = Api.ResourcePools.Complete(Api.ResourcePools.Create(new ResourcePool { Name = $"{Prefix}_Pool" }));

				var resource = new UnmanagedResource { Name = $"{Prefix}_Resource", Concurrency = 100 };
				resource.AddCapacity(new NumberCapacitySetting(Capacity) { Value = 1000 });
				resource.AssignToPool(Pool);
				Resource = Api.Resources.Complete(Api.Resources.Create(resource));
			}

			internal Guid Prefix { get; } = Guid.NewGuid();

			internal IMediaOpsPlanApi Api { get; }

			internal IMediaOpsLiveApi LiveApi { get; }

			internal IMediaOpsPlanApi ReadApi { get; }

			internal CountingConnection Counter { get; }

			internal NumberCapacity Capacity { get; }

			internal TextConfiguration TextConfiguration { get; }

			internal ResourcePool Pool { get; }

			internal Resource Resource { get; }

			internal Job CreateJob(int nodeCount)
			{
				var currentTime = DateTime.UtcNow.RoundToNextSecond();
				var job = new Job
				{
					Name = $"{Prefix}_Job_{Guid.NewGuid()}",
					Start = currentTime.AddHours(1),
					End = currentTime.AddHours(2),
					PreRollStart = currentTime.AddHours(1),
					PostRollEnd = currentTime.AddHours(2),
				};

				job.OrchestrationSettings.AddConfiguration(new TextConfigurationSetting(TextConfiguration) { Value = "Job" });
				job.OrchestrationSettings.AddOrchestrationEvent(new OrchestrationEvent
				{
					EventType = OrchestrationEventType.PrerollStart,
					ExecutionDetails = new ScriptExecutionDetails("SomeScript").AddConfiguration(new TextConfigurationSetting(TextConfiguration) { Value = "Event" }),
				});

				for (int i = 0; i < nodeCount; i++)
				{
					var node = new JobResourceNode(Pool, Resource);
					node.OrchestrationSettings.AddCapacity(new NumberCapacitySetting(Capacity) { Value = 10 });
					job.NodeGraph.Add(node);
				}

				return Api.Jobs.Create(job);
			}
		}
	}
}
