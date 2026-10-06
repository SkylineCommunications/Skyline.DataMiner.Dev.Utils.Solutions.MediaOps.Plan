namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.Extensions;
	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	/// <summary>
	/// Verifies that capability and capacity requirements that the assigned resource cannot meet are rejected before
	/// anything is persisted, instead of only being refused by the Service &amp; Resource Management afterwards.
	/// </summary>
	[TestClass]
	[TestCategory("IntegrationTest")]
	[DoNotParallelize]
	public sealed class NodeResourceRequirementsTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public NodeResourceRequirementsTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void Update_UnsupportedCapabilityValue_IsRejectedAndDoesNotChangeStoredSettings()
		{
			var setup = CreateSetup();
			var job = CreateJobWithNode(setup, node => node.OrchestrationSettings.AddCapability(new CapabilitySetting(setup.Capability) { Value = "Value 1" }));

			job.NodeGraph.Nodes.Single().OrchestrationSettings.Capabilities.Single().Value = "Unsupported";

			var traceData = AssertUpdateFails(job);
			var error = traceData.ErrorData.OfType<JobResourceInvalidCapabilityError>().Single();
			Assert.AreEqual(setup.Capability.Id, error.CapabilityId);
			Assert.AreEqual(setup.Resource.Id, error.ResourceId);

			var stored = TestContext.Api.Jobs.Read(job.Id);
			Assert.AreEqual("Value 1", stored.NodeGraph.Nodes.Single().OrchestrationSettings.Capabilities.Single().Value);
		}

		[TestMethod]
		public void Create_CapabilityNotAssignedToResource_IsRejected()
		{
			var setup = CreateSetup();

			var unassigned = new Capability { Name = $"{setup.Prefix}_Other" }.SetDiscretes(["Value 1"]);
			objectCreator.CreateCapability(unassigned);

			var node = new JobResourceNode(setup.Pool, setup.Resource);
			node.OrchestrationSettings.AddCapability(new CapabilitySetting(unassigned) { Value = "Value 1" });

			var traceData = AssertCreateFails(NewJob(setup.Prefix, node));
			Assert.AreEqual(unassigned.Id, traceData.ErrorData.OfType<JobResourceInvalidCapabilityError>().Single().CapabilityId);
		}

		[TestMethod]
		public void Create_CapacityAboveResourceMaximum_IsRejected()
		{
			var setup = CreateSetup();

			var node = new JobResourceNode(setup.Pool, setup.Resource);
			node.OrchestrationSettings.AddCapacity(new NumberCapacitySetting(setup.Capacity) { Value = 101 });

			var traceData = AssertCreateFails(NewJob(setup.Prefix, node));
			Assert.AreEqual(setup.Capacity.Id, traceData.ErrorData.OfType<JobResourceInvalidCapacityError>().Single().CapacityId);
		}

		[TestMethod]
		public void Create_RangeCapacityOnResourceBounds_IsAccepted()
		{
			var setup = CreateSetup();

			var node = new JobResourceNode(setup.Pool, setup.Resource);
			node.OrchestrationSettings.AddCapacity(new RangeCapacitySetting(setup.RangeCapacity) { MinValue = 10, MaxValue = 100 });

			var job = objectCreator.CreateJob(NewJob(setup.Prefix, node));

			var stored = (RangeCapacitySetting)TestContext.Api.Jobs.Read(job.Id).NodeGraph.Nodes.Single().OrchestrationSettings.Capacities.Single();
			Assert.AreEqual(10m, stored.MinValue);
			Assert.AreEqual(100m, stored.MaxValue);
		}

		[TestMethod]
		public void Create_RangeCapacityBelowResourceMinimum_IsRejected()
		{
			var setup = CreateSetup();

			var node = new JobResourceNode(setup.Pool, setup.Resource);
			node.OrchestrationSettings.AddCapacity(new RangeCapacitySetting(setup.RangeCapacity) { MinValue = 9, MaxValue = 50 });

			var traceData = AssertCreateFails(NewJob(setup.Prefix, node));
			var error = traceData.ErrorData.OfType<JobResourceInvalidCapacityError>().Single();
			Assert.AreEqual(setup.RangeCapacity.Id, error.CapacityId);
			StringAssert.Contains(error.ErrorMessage, "below the minimum of 10");
		}

		[TestMethod]
		public void Create_RangeCapacityAboveResourceMaximum_IsRejected()
		{
			var setup = CreateSetup();

			var node = new JobResourceNode(setup.Pool, setup.Resource);
			node.OrchestrationSettings.AddCapacity(new RangeCapacitySetting(setup.RangeCapacity) { MinValue = 50, MaxValue = 101 });

			var traceData = AssertCreateFails(NewJob(setup.Prefix, node));
			var error = traceData.ErrorData.OfType<JobResourceInvalidCapacityError>().Single();
			Assert.AreEqual(setup.RangeCapacity.Id, error.CapacityId);
			StringAssert.Contains(error.ErrorMessage, "exceeds the maximum of 100");
		}

		[TestMethod]
		public void Create_InvalidCapabilityAndCapacity_ReportsBothErrors()
		{
			var setup = CreateSetup();

			var node = new JobResourceNode(setup.Pool, setup.Resource);
			node.OrchestrationSettings.AddCapability(new CapabilitySetting(setup.Capability) { Value = "Unsupported" });
			node.OrchestrationSettings.AddCapacity(new NumberCapacitySetting(setup.Capacity) { Value = 101 });

			var traceData = AssertCreateFails(NewJob(setup.Prefix, node));
			Assert.AreEqual(setup.Capability.Id, traceData.ErrorData.OfType<JobResourceInvalidCapabilityError>().Single().CapabilityId);
			Assert.AreEqual(setup.Capacity.Id, traceData.ErrorData.OfType<JobResourceInvalidCapacityError>().Single().CapacityId);
		}

		[TestMethod]
		public void Create_SupportedCapabilityAndCapacity_IsAccepted()
		{
			var setup = CreateSetup();

			var node = new JobResourceNode(setup.Pool, setup.Resource);
			node.OrchestrationSettings.AddCapability(new CapabilitySetting(setup.Capability) { Value = "Value 2" });
			node.OrchestrationSettings.AddCapacity(new NumberCapacitySetting(setup.Capacity) { Value = 100 });

			var job = objectCreator.CreateJob(NewJob(setup.Prefix, node));

			var stored = TestContext.Api.Jobs.Read(job.Id).NodeGraph.Nodes.Single().OrchestrationSettings;
			Assert.AreEqual("Value 2", stored.Capabilities.Single().Value);
			Assert.AreEqual(100m, ((NumberCapacitySetting)stored.Capacities.Single()).Value);
		}

		[TestMethod]
		public void Create_CapabilityWithReference_IsNotValidatedAgainstTheResource()
		{
			var setup = CreateSetup();
			var configuration = objectCreator.CreateConfiguration(new TextConfiguration { Name = $"{setup.Prefix}_Configuration" });

			var node = new JobResourceNode(setup.Pool, setup.Resource);
			node.OrchestrationSettings.AddCapability(new CapabilitySetting(setup.Capability) { Reference = new ConfigurationParameterReference(configuration.Id) });

			var job = NewJob(setup.Prefix, node);
			job.OrchestrationSettings.AddConfiguration(new TextConfigurationSetting(configuration) { Value = "Value 1" });

			var created = objectCreator.CreateJob(job);
			Assert.IsTrue(TestContext.Api.Jobs.Read(created.Id).NodeGraph.Nodes.Single().OrchestrationSettings.Capabilities.Single().HasReference);
		}

		private static MediaOpsTraceData AssertCreateFails(Job job)
		{
			var exception = Assert.ThrowsException<MediaOpsBulkException<Guid>>(() => TestContext.Api.Jobs.Create([job]));
			return GetTraceData(exception, job.Id);
		}

		private static MediaOpsTraceData AssertUpdateFails(Job job)
		{
			var exception = Assert.ThrowsException<MediaOpsBulkException<Guid>>(() => TestContext.Api.Jobs.Update([job]));
			return GetTraceData(exception, job.Id);
		}

		private static MediaOpsTraceData GetTraceData(MediaOpsBulkException<Guid> exception, Guid jobId)
		{
			Assert.IsTrue(exception.Result.TraceDataPerItem.TryGetValue(jobId, out var traceData), "No trace data reported for the job.");
			return traceData;
		}

		private static Job NewJob(Guid prefix, JobNode node)
		{
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var job = new Job
			{
				Name = $"{prefix}_{Guid.NewGuid()}_Job",
				Start = currentTime.AddHours(1),
				End = currentTime.AddHours(2),
				PreRollStart = currentTime.AddHours(1),
				PostRollEnd = currentTime.AddHours(2),
			};

			job.NodeGraph.Add(node);
			return job;
		}

		private Job CreateJobWithNode(Setup setup, Action<JobResourceNode> configure)
		{
			var node = new JobResourceNode(setup.Pool, setup.Resource);
			configure(node);

			return objectCreator.CreateJob(NewJob(setup.Prefix, node));
		}

		private Setup CreateSetup()
		{
			var prefix = Guid.NewGuid();

			var capability = new Capability { Name = $"{prefix}_Capability" }.SetDiscretes(["Value 1", "Value 2"]);
			objectCreator.CreateCapability(capability);

			var capacity = new NumberCapacity { Name = $"{prefix}_Capacity" };
			var rangeCapacity = new RangeCapacity { Name = $"{prefix}_RangeCapacity" };
			objectCreator.CreateCapacities([capacity, rangeCapacity]);

			var pool = TestContext.Api.ResourcePools.Complete(objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" }));

			var resourceCapability = new CapabilitySettings(capability);
			resourceCapability.SetDiscretes(["Value 1", "Value 2"]);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" };
			resource.AddCapability(resourceCapability);
			resource.AddCapacity(new NumberCapacitySetting(capacity) { Value = 100 });
			resource.AddCapacity(new RangeCapacitySetting(rangeCapacity) { MinValue = 10, MaxValue = 100 });
			resource.AssignToPool(pool);

			return new Setup(prefix, pool, TestContext.Api.Resources.Complete(objectCreator.CreateResource(resource)), capability, capacity, rangeCapacity);
		}

		private sealed class Setup
		{
			public Setup(Guid prefix, ResourcePool pool, Resource resource, Capability capability, NumberCapacity capacity, RangeCapacity rangeCapacity)
			{
				Prefix = prefix;
				Pool = pool;
				Resource = resource;
				Capability = capability;
				Capacity = capacity;
				RangeCapacity = rangeCapacity;
			}

			public Guid Prefix { get; }

			public ResourcePool Pool { get; }

			public Resource Resource { get; }

			public Capability Capability { get; }

			public NumberCapacity Capacity { get; }

			public RangeCapacity RangeCapacity { get; }
		}
	}
}
