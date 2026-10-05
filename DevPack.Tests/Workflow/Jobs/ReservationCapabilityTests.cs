namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using RT_MediaOps.Plan.Extensions;
	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.ResourceManager.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	[TestCategory("IntegrationTest")]
	[DoNotParallelize]
	public sealed class ReservationCapabilityTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public ReservationCapabilityTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void DomJobHandler_Update_NodeCapabilityChangedAfterForcedCapabilityDowngrade_ClearsTheQuarantine()
		{
			var setup = CreateSetup();

			var node = new JobResourceNode(setup.Pool, setup.Resource);
			node.OrchestrationSettings.AddCapability(new CapabilitySetting(setup.Capability) { Value = "Value 1" });

			var job = CreateTentativeJob(setup.Prefix, x => x.NodeGraph.Add(node));

			// Removing the required option from the resource with a forced update pushes the usage into quarantine.
			var coreResource = TestContext.ResourceManagerHelper.GetResource(setup.Resource.CoreResourceId);
			coreResource.Capabilities.Single(x => x.CapabilityProfileID == setup.Capability.Id).Value.Discreets = new List<string> { "Value 2" };
			TestContext.ResourceManagerHelper.AddOrUpdateResources(true, [coreResource]);

			Assert.IsTrue(GetReservation(job.Id).IsQuarantined, "Expected the reservation to be quarantined after the forced capability downgrade.");

			var quarantinedJob = TestContext.Api.Jobs.Read(job.Id);
			TestContext.Api.Jobs.Validate([quarantinedJob]).Single().SyncToJob();
			quarantinedJob = TestContext.Api.Jobs.Update(quarantinedJob);
			Assert.IsTrue(quarantinedJob.Errors.Any(x => x.Code == QuarantinedReservationJobValidationError.ErrorCode), "Expected the quarantine error on the job.");

			// Requiring an option that the resource still offers resolves the quarantine.
			quarantinedJob.NodeGraph.Nodes.Single().OrchestrationSettings.Capabilities.Single().Value = "Value 2";
			var updatedJob = TestContext.Api.Jobs.Update(quarantinedJob);

			Assert.IsFalse(updatedJob.Errors.Any(x => x.Code == QuarantinedReservationJobValidationError.ErrorCode), "Expected the quarantine error to be cleared.");
			Assert.IsFalse(TestContext.Api.Jobs.Read(job.Id).Errors.Any(x => x.Code == QuarantinedReservationJobValidationError.ErrorCode), "Expected the quarantine error to be cleared on the stored job.");

			var reservation = GetReservation(job.Id);
			Assert.IsFalse(reservation.IsQuarantined, "Expected the reservation to be out of quarantine.");
			Assert.AreEqual("Value 2", GetRequiredDiscrete(job.Id, setup.Capability.Id));
		}

		[TestMethod]
		public void DomJobHandler_Update_NodeCapabilityChanged_ReservationRequiresNewValue()
		{
			var setup = CreateSetup();

			var node = new JobResourceNode(setup.Pool, setup.Resource);
			node.OrchestrationSettings.AddCapability(new CapabilitySetting(setup.Capability) { Value = "Value 1" });

			var job = CreateTentativeJob(setup.Prefix, x => x.NodeGraph.Add(node));
			Assert.AreEqual("Value 1", GetRequiredDiscrete(job.Id, setup.Capability.Id));

			var storedNode = job.NodeGraph.Nodes.Single();
			storedNode.OrchestrationSettings.Capabilities.Single().Value = "Value 2";
			TestContext.Api.Jobs.Update(job);

			Assert.AreEqual("Value 2", GetRequiredDiscrete(job.Id, setup.Capability.Id));
		}

		[TestMethod]
		public void DomJobHandler_Update_LinkedJobConfigurationChanged_ReservationRequiresNewValue()
		{
			var setup = CreateSetup();

			var node = new JobResourceNode(setup.Pool, setup.Resource);
			node.OrchestrationSettings.AddCapability(new CapabilitySetting(setup.Capability) { Reference = new ConfigurationParameterReference(setup.Configuration.Id) });

			var job = CreateTentativeJob(setup.Prefix, x =>
			{
				x.OrchestrationSettings.AddConfiguration(new TextConfigurationSetting(setup.Configuration) { Value = "Value 1" });
				x.NodeGraph.Add(node);
			});
			Assert.AreEqual("Value 1", GetRequiredDiscrete(job.Id, setup.Capability.Id));

			((TextConfigurationSetting)job.OrchestrationSettings.Configurations.Single()).Value = "Value 2";
			TestContext.Api.Jobs.Update(job);

			Assert.AreEqual("Value 2", GetRequiredDiscrete(job.Id, setup.Capability.Id));
		}

		[TestMethod]
		public void DomJobHandler_Update_AddNodeLinkedToJobConfiguration_ReservationRequiresResolvedValue()
		{
			var setup = CreateSetup();

			var job = CreateTentativeJob(setup.Prefix, x => x.OrchestrationSettings.AddConfiguration(new TextConfigurationSetting(setup.Configuration) { Value = "Value 1" }));

			var node = new JobResourceNode(setup.Pool, setup.Resource);
			node.OrchestrationSettings.AddCapability(new CapabilitySetting(setup.Capability) { Reference = new ConfigurationParameterReference(setup.Configuration.Id) });
			job.NodeGraph.Add(node);
			TestContext.Api.Jobs.Update(job);

			Assert.AreEqual("Value 1", GetRequiredDiscrete(job.Id, setup.Capability.Id));
		}

		[TestMethod]
		public void DomJobHandler_Update_LinkedJobPropertyChanged_ReservationRequiresNewValue()
		{
			var setup = CreateSetup();
			var property = CreateJobProperty(setup.Prefix);

			var node = new JobResourceNode(setup.Pool, setup.Resource);
			node.OrchestrationSettings.AddCapability(new CapabilitySetting(setup.Capability) { Reference = new JobPropertyReference(property.Id) });

			var job = CreateTentativeJob(setup.Prefix, x =>
			{
				x.AddProperty(new StringPropertySetting(property) { Value = "Value 1" });
				x.NodeGraph.Add(node);
			});
			Assert.AreEqual("Value 1", GetRequiredDiscrete(job.Id, setup.Capability.Id));

			job.SetProperties([new StringPropertySetting(property) { Value = "Value 2" }]);
			TestContext.Api.Jobs.Update(job);

			Assert.AreEqual("Value 2", GetRequiredDiscrete(job.Id, setup.Capability.Id));
		}

		[TestMethod]
		public void DomJobHandler_Update_LinkedNodePropertyChanged_ReservationRequiresNewValue()
		{
			var setup = CreateSetup();
			var property = CreateJobProperty(setup.Prefix);

			var node = new JobResourceNode(setup.Pool, setup.Resource);
			node.AddProperty(new StringPropertySetting(property) { Value = "Value 1" });
			node.OrchestrationSettings.AddCapability(new CapabilitySetting(setup.Capability) { Reference = new JobPropertyReference(property.Id, node.Id) });

			var job = CreateTentativeJob(setup.Prefix, x => x.NodeGraph.Add(node));
			Assert.AreEqual("Value 1", GetRequiredDiscrete(job.Id, setup.Capability.Id));

			job.NodeGraph.Nodes.Single().SetProperties([new StringPropertySetting(property) { Value = "Value 2" }]);
			TestContext.Api.Jobs.Update(job);

			Assert.AreEqual("Value 2", GetRequiredDiscrete(job.Id, setup.Capability.Id));
		}

		[TestMethod]
		public void DomJobHandler_Update_LinkedJobConfigurationChanged_ReservationRequiresNewCapacity()
		{
			var setup = CreateSetup();
			var configuration = objectCreator.CreateConfiguration(new NumberConfiguration { Name = $"{setup.Prefix}_NumberConfiguration" });

			var node = new JobResourceNode(setup.Pool, setup.Resource);
			node.OrchestrationSettings.AddCapacity(new NumberCapacitySetting(setup.Capacity) { Reference = new ConfigurationParameterReference(configuration.Id) });

			var job = CreateTentativeJob(setup.Prefix, x =>
			{
				x.OrchestrationSettings.AddConfiguration(new NumberConfigurationSetting(configuration) { Value = 10 });
				x.NodeGraph.Add(node);
			});
			Assert.AreEqual(10m, GetRequiredQuantity(job.Id, setup.Capacity.Id));

			((NumberConfigurationSetting)job.OrchestrationSettings.Configurations.Single()).Value = 20;
			TestContext.Api.Jobs.Update(job);

			Assert.AreEqual(20m, GetRequiredQuantity(job.Id, setup.Capacity.Id));
		}

		[TestMethod]
		public void DomJobHandler_Update_LinkedJobPropertyChanged_ReservationRequiresNewCapacity()
		{
			var setup = CreateSetup();
			var property = CreateJobProperty(setup.Prefix);

			var node = new JobResourceNode(setup.Pool, setup.Resource);
			node.OrchestrationSettings.AddCapacity(new NumberCapacitySetting(setup.Capacity) { Reference = new JobPropertyReference(property.Id) });

			var job = CreateTentativeJob(setup.Prefix, x =>
			{
				x.AddProperty(new StringPropertySetting(property) { Value = "25" });
				x.NodeGraph.Add(node);
			});
			Assert.AreEqual(25m, GetRequiredQuantity(job.Id, setup.Capacity.Id));

			job.SetProperties([new StringPropertySetting(property) { Value = "30.5" }]);
			TestContext.Api.Jobs.Update(job);

			Assert.AreEqual(30.5m, GetRequiredQuantity(job.Id, setup.Capacity.Id));
		}

		[TestMethod]
		public void DomJobHandler_SaveAsTentative_LinkedJobPropertyNotNumeric_ReservationHasNoCapacity()
		{
			var setup = CreateSetup();
			var property = CreateJobProperty(setup.Prefix);

			var node = new JobResourceNode(setup.Pool, setup.Resource);
			node.OrchestrationSettings.AddCapacity(new NumberCapacitySetting(setup.Capacity) { Reference = new JobPropertyReference(property.Id) });

			var job = CreateTentativeJob(setup.Prefix, x =>
			{
				x.AddProperty(new StringPropertySetting(property) { Value = "1,5" });
				x.NodeGraph.Add(node);
			});

			Assert.IsNull(GetRequiredQuantity(job.Id, setup.Capacity.Id));
		}

		private StringProperty CreateJobProperty(Guid prefix)
		{
			return (StringProperty)objectCreator.CreateSchedulingProperties([new StringProperty { Name = $"{prefix}_Property", SectionName = "General" }]).Single();
		}

		private static string GetRequiredDiscrete(Guid jobId, Guid capabilityId)
		{
			return GetUsage(jobId).RequiredCapabilities?.SingleOrDefault(x => x.CapabilityProfileID == capabilityId)?.RequiredDiscreet;
		}

		private static decimal? GetRequiredQuantity(Guid jobId, Guid capacityId)
		{
			return GetUsage(jobId).RequiredCapacities?.SingleOrDefault(x => x.CapacityProfileID == capacityId)?.DecimalQuantity;
		}

		private static ServiceResourceUsageDefinition GetUsage(Guid jobId)
		{
			return GetReservation(jobId).ResourcesInReservationInstance.OfType<ServiceResourceUsageDefinition>().Single();
		}

		private static ReservationInstance GetReservation(Guid jobId)
		{
			var reservations = TestContext.ResourceManagerHelper.GetReservationInstances(
				ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(jobId))).ToList();

			Assert.AreEqual(1, reservations.Count, "Expected exactly one core reservation for the job.");

			return reservations[0];
		}

		private Job CreateTentativeJob(Guid prefix, Action<Job> configure)
		{
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime.AddMinutes(1),
				End = currentTime.AddMinutes(10),
				PreRollStart = currentTime.AddMinutes(1),
				PostRollEnd = currentTime.AddMinutes(10),
			};

			configure(job);

			job = objectCreator.CreateJob(job);
			return TestContext.Api.Jobs.SaveAsTentative(job);
		}

		private (Guid Prefix, ResourcePool Pool, Resource Resource, Capability Capability, NumberCapacity Capacity, TextConfiguration Configuration) CreateSetup()
		{
			var prefix = Guid.NewGuid();

			var capability = new Capability { Name = $"{prefix}_Capability" }.SetDiscretes(["Value 1", "Value 2"]);
			objectCreator.CreateCapability(capability);

			var capacity = new NumberCapacity { Name = $"{prefix}_Capacity" };
			objectCreator.CreateCapacities([capacity]);

			var configuration = objectCreator.CreateConfiguration(new TextConfiguration { Name = $"{prefix}_Configuration" });

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resourceCapability = new CapabilitySettings(capability);
			resourceCapability.SetDiscretes(["Value 1", "Value 2"]);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" };
			resource.AddCapability(resourceCapability);
			resource.AddCapacity(new NumberCapacitySetting(capacity) { Value = 100 });
			resource.AssignToPool(pool);
			var completedResource = TestContext.Api.Resources.Complete(objectCreator.CreateResource(resource));

			return (prefix, pool, completedResource, capability, capacity, configuration);
		}
	}
}
