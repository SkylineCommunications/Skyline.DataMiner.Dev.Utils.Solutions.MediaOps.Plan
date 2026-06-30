namespace RT_MediaOps.Plan.Workflow.Jobs
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.Extensions;
	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.ResourceManager.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	[TestCategory("IntegrationTest")]
	[DoNotParallelize]
	public sealed class TentativeStateTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public TentativeStateTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void SaveAsTentative_CreatesCoreReservation_WithExpectedNameResourcesAndTimings()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" }.AssignToPool(pool);
			resource = objectCreator.CreateResource(resource);
			resource = TestContext.Api.Resources.Complete(resource);

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(10),
				PreRollStart = currentTime,
				PostRollEnd = currentTime.AddMinutes(10),
			};

			var resourceNode = new JobResourceNode(pool, resource);
			var poolNode = new JobResourcePoolNode(pool);

			job.NodeGraph
				.Add(resourceNode)
				.Add(poolNode)
				.Connect(resourceNode, poolNode);

			job = objectCreator.CreateJob(job);

			var tentativeJob = TestContext.Api.Jobs.SaveAsTentative(job);
			Assert.IsNotNull(tentativeJob, "Expected the job to transition to the Tentative state.");

			var reservations = TestContext.ResourceManagerHelper.GetReservationInstances(
				ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(job.Id))).ToList();

			Assert.AreEqual(1, reservations.Count, "Expected exactly one core reservation for the tentative job.");

			var reservation = reservations[0];
			Assert.AreEqual($"{tentativeJob.Name} [{tentativeJob.Key}]", reservation.Name);
			Assert.AreEqual(currentTime, reservation.Start);
			Assert.AreEqual(currentTime.AddMinutes(10), reservation.End);

			var usages = reservation.ResourcesInReservationInstance.OfType<ServiceResourceUsageDefinition>().ToList();
			Assert.AreEqual(1, usages.Count, "Expected the resource node to be booked as a single reservation usage.");
			Assert.AreEqual(resource.CoreResourceId, usages[0].GUID);
		}

		[TestMethod]
		public void SaveAsTentative_CreatesCoreReservation_WithCapabilityAndCapacityApplied()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			// Capability and capacity definitions the resource and node settings reference.
			var capability = new Capability { Name = $"{prefix}_Capability" }.SetDiscretes(["Value 1", "Value 2"]);
			objectCreator.CreateCapability(capability);

			var capacity = new NumberCapacity { Name = $"{prefix}_Capacity" };
			objectCreator.CreateCapacities([capacity]);

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			// Completed resource with the capability and capacity assigned.
			var resourceCapability = new CapabilitySettings(capability);
			resourceCapability.SetDiscretes(["Value 1", "Value 2"]);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource" };
			resource.AddCapability(resourceCapability);
			resource.AddCapacity(new NumberCapacitySetting(capacity) { Value = 100 });
			resource.AssignToPool(pool);
			var completedResource = TestContext.Api.Resources.Complete(objectCreator.CreateResource(resource));

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime,
				End = currentTime.AddMinutes(10),
				PreRollStart = currentTime,
				PostRollEnd = currentTime.AddMinutes(10),
			};

			// Resource node with capability and capacity orchestration settings.
			var resourceNode = new JobResourceNode(pool, completedResource);
			resourceNode.OrchestrationSettings
				.AddCapability(new CapabilitySetting(capability) { Value = "Value 1" })
				.AddCapacity(new NumberCapacitySetting(capacity) { Value = 25 });

			var poolNode = new JobResourcePoolNode(pool);

			job.NodeGraph
				.Add(resourceNode)
				.Add(poolNode)
				.Connect(resourceNode, poolNode);

			job = objectCreator.CreateJob(job);

			var tentativeJob = TestContext.Api.Jobs.SaveAsTentative(job);
			Assert.IsNotNull(tentativeJob, "Expected the job to transition to the Tentative state.");

			var reservations = TestContext.ResourceManagerHelper.GetReservationInstances(
				ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(job.Id))).ToList();

			Assert.AreEqual(1, reservations.Count, "Expected exactly one core reservation for the tentative job.");

			var usages = reservations[0].ResourcesInReservationInstance.OfType<ServiceResourceUsageDefinition>().ToList();
			Assert.AreEqual(1, usages.Count, "Expected the resource node to be booked as a single reservation usage.");

			var usage = usages[0];

			var requiredCapability = usage.RequiredCapabilities.Single();
			Assert.AreEqual(capability.Id, requiredCapability.CapabilityProfileID, "The capability profile should match the configured capability.");
			Assert.AreEqual("Value 1", requiredCapability.RequiredDiscreet, "The required discrete should match the configured capability value.");

			var requiredCapacity = usage.RequiredCapacities.Single();
			Assert.AreEqual(capacity.Id, requiredCapacity.CapacityProfileID, "The capacity profile should match the configured capacity.");
			Assert.AreEqual(25m, requiredCapacity.DecimalQuantity, "The required capacity quantity should match the configured capacity value.");
		}
	}
}
