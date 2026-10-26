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
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

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

			var quarantinedJob = TestContext.ReportQuarantineOnJob(job.Id);
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
		public void DomJobHandler_Update_OneOfTwoQuarantinedNodesFixed_ReleasesOnlyTheFixedUsage()
		{
			var setup = CreateSetup();

			var resourceCapability = new CapabilitySettings(setup.Capability);
			resourceCapability.SetDiscretes(["Value 1", "Value 2"]);
			var otherResource = new UnmanagedResource { Name = $"{setup.Prefix}_Resource_2" };
			otherResource.AddCapability(resourceCapability);
			otherResource.AssignToPool(setup.Pool);
			var secondResource = TestContext.Api.Resources.Complete(objectCreator.CreateResource(otherResource));

			var job = CreateTentativeJob(setup.Prefix, x =>
			{
				foreach (var resource in new[] { setup.Resource, secondResource })
				{
					var node = new JobResourceNode(setup.Pool, resource);
					node.OrchestrationSettings.AddCapability(new CapabilitySetting(setup.Capability) { Value = "Value 1" });
					x.NodeGraph.Add(node);
				}
			});

			// Removing the required option from both resources with a forced update pushes both usages into quarantine.
			var coreResources = new[] { setup.Resource, secondResource }
				.Select(x => TestContext.ResourceManagerHelper.GetResource(x.CoreResourceId))
				.ToList();
			foreach (var coreResource in coreResources)
			{
				coreResource.Capabilities.Single(x => x.CapabilityProfileID == setup.Capability.Id).Value.Discreets = new List<string> { "Value 2" };
			}

			TestContext.ResourceManagerHelper.AddOrUpdateResources(true, coreResources.ToArray());

			Assert.AreEqual(2, GetReservation(job.Id).QuarantinedResources.Count, "Expected both usages to be quarantined after the forced capability downgrade.");

			var quarantinedJob = TestContext.ReportQuarantineOnJob(job.Id);
			Assert.IsTrue(quarantinedJob.Errors.Any(x => x.Code == QuarantinedReservationJobValidationError.ErrorCode), "Expected the quarantine error on the job.");

			// Only the node of the first resource is changed to an option that its resource still offers.
			quarantinedJob.NodeGraph.Nodes.OfType<JobResourceNode>().Single(x => x.ResourceId == setup.Resource.Id).OrchestrationSettings.Capabilities.Single().Value = "Value 2";
			var updatedJob = TestContext.Api.Jobs.Update(quarantinedJob);

			Assert.IsTrue(updatedJob.Errors.Any(x => x.Code == QuarantinedReservationJobValidationError.ErrorCode), "Expected the quarantine error to remain while the second node is still quarantined.");

			var reservation = GetReservation(job.Id);
			Assert.IsTrue(reservation.IsQuarantined, "Expected the reservation to remain quarantined.");

			var quarantinedUsage = reservation.QuarantinedResources.Select(x => x.QuarantinedResourceUsage).OfType<ServiceResourceUsageDefinition>().Single();
			Assert.AreEqual(secondResource.CoreResourceId, quarantinedUsage.GUID, "Expected only the usage of the unchanged node to remain quarantined.");

			var releasedUsage = reservation.ResourcesInReservationInstance.OfType<ServiceResourceUsageDefinition>().Single();
			Assert.AreEqual(setup.Resource.CoreResourceId, releasedUsage.GUID, "Expected the usage of the fixed node to be released from quarantine.");
			Assert.AreEqual("Value 2", releasedUsage.RequiredCapabilities.Single(x => x.CapabilityProfileID == setup.Capability.Id).RequiredDiscreet);
		}

		[TestMethod]
		public void DomJobHandler_SaveAsTentative_CapabilityValueNoLongerOfferedByResource_Fails()
		{
			var setup = CreateSetup();

			var coreResource = TestContext.ResourceManagerHelper.GetResource(setup.Resource.CoreResourceId);
			coreResource.Capabilities.Single(x => x.CapabilityProfileID == setup.Capability.Id).Value.Discreets = new List<string> { "Value 2" };
			TestContext.ResourceManagerHelper.AddOrUpdateResources(true, [coreResource]);

			var node = new JobResourceNode(setup.Pool, setup.Resource);
			node.OrchestrationSettings.AddCapability(new CapabilitySetting(setup.Capability) { Value = "Value 1" });

			Assert.ThrowsException<MediaOpsException>(() => CreateTentativeJob(setup.Prefix, x => x.NodeGraph.Add(node)));
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

		[TestMethod]
		public void DomJobHandler_SaveAsTentative_TimeDependentCapability_ReservationRequiresLinkedUsage()
		{
			var setup = CreateSetup(isTimeDependentCapability: true);
			var linkedId = GetLinkedTimeDependentCapabilityId(setup.Capability);

			var node = new JobResourceNode(setup.Pool, setup.Resource);
			node.OrchestrationSettings.AddCapability(new CapabilitySetting(setup.Capability) { Value = "Value 1" });

			var job = CreateTentativeJob(setup.Prefix, x => x.NodeGraph.Add(node));

			var usage = GetUsage(job.Id);
			Assert.AreEqual(2, usage.RequiredCapabilities.Count, "Expected a usage on the capability and on its linked time-dependent parameter.");
			Assert.AreEqual("Value 1", GetRequiredDiscrete(job.Id, setup.Capability.Id));
			Assert.AreEqual("Value 1", GetRequiredString(job.Id, linkedId), "Expected the value to be reserved on the linked time-dependent parameter.");
		}

		[TestMethod]
		public void DomJobHandler_SaveAsTentative_RegularCapability_ReservationHasNoLinkedUsage()
		{
			var setup = CreateSetup();

			var node = new JobResourceNode(setup.Pool, setup.Resource);
			node.OrchestrationSettings.AddCapability(new CapabilitySetting(setup.Capability) { Value = "Value 1" });

			var job = CreateTentativeJob(setup.Prefix, x => x.NodeGraph.Add(node));

			var usage = GetUsage(job.Id);
			Assert.AreEqual(1, usage.RequiredCapabilities.Count, "Expected only the usage on the capability itself.");
			Assert.AreEqual("Value 1", GetRequiredDiscrete(job.Id, setup.Capability.Id));
		}

		[TestMethod]
		public void DomJobHandler_SaveAsTentative_OverlappingJobsSameTimeDependentValue_Succeeds()
		{
			var setup = CreateSetup(isTimeDependentCapability: true, resourceConcurrency: 2);
			var linkedId = GetLinkedTimeDependentCapabilityId(setup.Capability);

			var firstJob = CreateTentativeJob(setup.Prefix, x => x.NodeGraph.Add(CreateNode(setup, "Value 1")), "Job 1");
			var secondJob = CreateTentativeJob(setup.Prefix, x => x.NodeGraph.Add(CreateNode(setup, "Value 1")), "Job 2");

			Assert.AreEqual("Value 1", GetRequiredString(firstJob.Id, linkedId));
			Assert.AreEqual("Value 1", GetRequiredString(secondJob.Id, linkedId));
		}

		[TestMethod]
		public void DomJobHandler_SaveAsTentative_OverlappingJobsDifferentTimeDependentValue_FailsAsResourceNotAvailable()
		{
			var setup = CreateSetup(isTimeDependentCapability: true, resourceConcurrency: 2);
			var linkedId = GetLinkedTimeDependentCapabilityId(setup.Capability);

			CreateTentativeJob(setup.Prefix, x => x.NodeGraph.Add(CreateNode(setup, "Value 1")), "Job 1");

			var currentTime = DateTime.UtcNow.RoundToNextSecond();
			var secondJob = new Job
			{
				Name = $"{setup.Prefix}_Job 2",
				Start = currentTime.AddMinutes(1),
				End = currentTime.AddMinutes(10),
				PreRollStart = currentTime.AddMinutes(1),
				PostRollEnd = currentTime.AddMinutes(10),
			};
			secondJob.NodeGraph.Add(CreateNode(setup, "Value 2"));
			secondJob = objectCreator.CreateJob(secondJob);

			var traceData = AssertSaveAsTentativeFails(secondJob);

			// The core software treats the value of a time-dependent capability as taken by the overlapping booking, so the
			// resource is reported as not available rather than as lacking the capability.
			var resourceErrors = traceData.ErrorData.OfType<JobResourceError>().ToList();
			Assert.IsTrue(resourceErrors.Count > 0, $"Expected a resource error, but got: {String.Join("; ", traceData.ErrorData.Select(x => $"{x.GetType().Name}: {x.ErrorMessage}"))}");
			Assert.IsTrue(resourceErrors.All(x => x.ResourceId == setup.Resource.Id), "Expected the errors to be reported on the resource of which the time-dependent value is taken.");
			Assert.IsTrue(resourceErrors.Any(x => x is JobResourceNotAvailableError || (x is JobResourceInvalidCapabilityError capabilityError && capabilityError.CapabilityId == setup.Capability.Id)));
			Assert.IsFalse(traceData.ErrorData.OfType<JobResourceInvalidCapabilityError>().Any(x => x.CapabilityId == linkedId), "Expected the linked time-dependent parameter not to be surfaced.");
		}

		[TestMethod]
		public void DomJobHandler_Update_TimeDependentCapabilityValueChanged_LinkedUsageFollows()
		{
			var setup = CreateSetup(isTimeDependentCapability: true);
			var linkedId = GetLinkedTimeDependentCapabilityId(setup.Capability);

			var job = CreateTentativeJob(setup.Prefix, x => x.NodeGraph.Add(CreateNode(setup, "Value 1")));
			Assert.AreEqual("Value 1", GetRequiredString(job.Id, linkedId));

			job.NodeGraph.Nodes.Single().OrchestrationSettings.Capabilities.Single().Value = "Value 2";
			TestContext.Api.Jobs.Update(job);

			Assert.AreEqual("Value 2", GetRequiredDiscrete(job.Id, setup.Capability.Id));
			Assert.AreEqual("Value 2", GetRequiredString(job.Id, linkedId));
		}

		[TestMethod]
		public void DomJobHandler_SaveAsTentative_TimeDependentCapabilityLinkedToJobConfiguration_LinkedUsageUsesResolvedValue()
		{
			var setup = CreateSetup(isTimeDependentCapability: true);
			var linkedId = GetLinkedTimeDependentCapabilityId(setup.Capability);

			var node = new JobResourceNode(setup.Pool, setup.Resource);
			node.OrchestrationSettings.AddCapability(new CapabilitySetting(setup.Capability) { Reference = new ConfigurationParameterReference(setup.Configuration.Id) });

			var job = CreateTentativeJob(setup.Prefix, x =>
			{
				x.OrchestrationSettings.AddConfiguration(new TextConfigurationSetting(setup.Configuration) { Value = "Value 1" });
				x.NodeGraph.Add(node);
			});

			Assert.AreEqual("Value 1", GetRequiredDiscrete(job.Id, setup.Capability.Id));
			Assert.AreEqual("Value 1", GetRequiredString(job.Id, linkedId));
		}

		private static JobResourceNode CreateNode((Guid Prefix, ResourcePool Pool, Resource Resource, Capability Capability, NumberCapacity Capacity, TextConfiguration Configuration) setup, string value)
		{
			var node = new JobResourceNode(setup.Pool, setup.Resource);
			node.OrchestrationSettings.AddCapability(new CapabilitySetting(setup.Capability) { Value = value });

			return node;
		}

		private StringProperty CreateJobProperty(Guid prefix)
		{
			return (StringProperty)objectCreator.CreateSchedulingProperties([new StringProperty { Name = $"{prefix}_Property", SectionName = "General" }]).Single();
		}

		private static string GetRequiredDiscrete(Guid jobId, Guid capabilityId)
		{
			return GetUsage(jobId).RequiredCapabilities?.SingleOrDefault(x => x.CapabilityProfileID == capabilityId)?.RequiredDiscreet;
		}

		private static string GetRequiredString(Guid jobId, Guid capabilityId)
		{
			return GetUsage(jobId).RequiredCapabilities?.SingleOrDefault(x => x.CapabilityProfileID == capabilityId)?.RequiredString;
		}

		private static Guid GetLinkedTimeDependentCapabilityId(Capability capability)
		{
			var linkedId = TestContext.Api.Capabilities.Read(capability.Id).LinkedTimeDependentCapabilityId;
			Assert.AreNotEqual(Guid.Empty, linkedId, "Precondition: the capability has no linked time-dependent parameter.");

			return linkedId;
		}

		private static MediaOpsTraceData AssertSaveAsTentativeFails(Job job)
		{
			var exception = Assert.ThrowsException<MediaOpsBulkException<Guid>>(() => TestContext.Api.Jobs.SaveAsTentative([job]));
			Assert.IsTrue(exception.Result.TraceDataPerItem.TryGetValue(job.Id, out var traceData), "No trace data reported for the job.");

			return traceData;
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

		private Job CreateTentativeJob(Guid prefix, Action<Job> configure, string nameSuffix = "Job")
		{
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var job = new Job
			{
				Name = $"{prefix}_{nameSuffix}",
				Start = currentTime.AddMinutes(1),
				End = currentTime.AddMinutes(10),
				PreRollStart = currentTime.AddMinutes(1),
				PostRollEnd = currentTime.AddMinutes(10),
			};

			configure(job);

			job = objectCreator.CreateJob(job);
			return TestContext.Api.Jobs.SaveAsTentative(job);
		}

		private (Guid Prefix, ResourcePool Pool, Resource Resource, Capability Capability, NumberCapacity Capacity, TextConfiguration Configuration) CreateSetup(bool isTimeDependentCapability = false, int resourceConcurrency = 1)
		{
			var prefix = Guid.NewGuid();

			var capability = new Capability { Name = $"{prefix}_Capability", IsTimeDependent = isTimeDependentCapability }.SetDiscretes(["Value 1", "Value 2"]);
			objectCreator.CreateCapability(capability);

			var capacity = new NumberCapacity { Name = $"{prefix}_Capacity" };
			objectCreator.CreateCapacities([capacity]);

			var configuration = objectCreator.CreateConfiguration(new TextConfiguration { Name = $"{prefix}_Configuration" });

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resourceCapability = new CapabilitySettings(capability);
			resourceCapability.SetDiscretes(["Value 1", "Value 2"]);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource", Concurrency = resourceConcurrency };
			resource.AddCapability(resourceCapability);
			resource.AddCapacity(new NumberCapacitySetting(capacity) { Value = 100 });
			resource.AssignToPool(pool);
			var completedResource = TestContext.Api.Resources.Complete(objectCreator.CreateResource(resource));

			return (prefix, pool, completedResource, capability, capacity, configuration);
		}
	}
}
