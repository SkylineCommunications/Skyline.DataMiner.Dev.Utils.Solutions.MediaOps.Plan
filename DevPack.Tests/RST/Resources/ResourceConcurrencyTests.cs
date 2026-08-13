namespace RT_MediaOps.Plan.RST.Resources
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.Extensions;
	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.ResourceManager.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	[TestClass]
	[TestCategory("IntegrationTest")]
	public sealed class ResourceConcurrencyTests
	{
		private readonly TestObjectCreator objectCreator;

		public ResourceConcurrencyTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void CreateWithInvalidConcurrencyThrowsException()
		{
			var prefix = Guid.NewGuid();
			var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
			{
				Name = $"{prefix}_Resource",
				Concurrency = 0, // Invalid concurrency
			};

			try
			{
				objectCreator.CreateResource(unmanagedResource);
			}
			catch (MediaOpsException ex)
			{
				var errorMessage = $"Concurrency must be greater than or equal to 1.";
				Assert.AreEqual(errorMessage, ex.Message);
				Assert.AreEqual(1, ex.TraceData.ErrorData.Count);

				var resourceConfigurationError = ex.TraceData.ErrorData.OfType<ResourceInvalidConcurrencyError>().SingleOrDefault();
				Assert.IsNotNull(resourceConfigurationError);

				return;
			}

			Assert.Fail("Exception not thrown");
		}

		[TestMethod]
		public void UpdateWithInvalidConcurrencyThrowsException()
		{
			var prefix = Guid.NewGuid();

			var unmanagedResource = new Skyline.DataMiner.Solutions.MediaOps.Plan.API.UnmanagedResource()
			{
				Name = $"{prefix}_Resource",
				Concurrency = 10,
			};

			objectCreator.CreateResource(unmanagedResource);

			var resource = TestContext.Api.Resources.Read(unmanagedResource.Id);
			resource.Concurrency = -10; // Invalid concurrency

			try
			{
				TestContext.Api.Resources.Update(resource);
			}
			catch (MediaOpsException ex)
			{
				var errorMessage = $"Concurrency must be greater than or equal to 1.";
				Assert.AreEqual(errorMessage, ex.Message);
				Assert.AreEqual(1, ex.TraceData.ErrorData.Count);

				var resourceConfigurationError = ex.TraceData.ErrorData.OfType<ResourceInvalidConcurrencyError>().SingleOrDefault();
				Assert.IsNotNull(resourceConfigurationError);

				return;
			}

			Assert.Fail("Exception not thrown");
		}

		[TestMethod]
		public void UpdateConcurrency_WithOverlappingTentativeReservations_QuarantinesReservation()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resource = new UnmanagedResource
			{
				Name = $"{prefix}_ResourceA",
				Concurrency = 2,
			}.AssignToPool(pool);
			resource = objectCreator.CreateResource(resource);
			resource = TestContext.Api.Resources.Complete(resource);

			var jobA = new Job
			{
				Name = $"{prefix}_Job_1",
				Start = currentTime.AddHours(1),
				End = currentTime.AddHours(2),
				PreRollStart = currentTime.AddHours(1),
				PostRollEnd = currentTime.AddHours(2),
			};
			jobA.NodeGraph
				.Add(new JobResourceNode(pool, resource));

			var jobB = new Job
			{
				Name = $"{prefix}_Job_2",
				Start = currentTime.AddHours(1),
				End = currentTime.AddHours(2),
				PreRollStart = currentTime.AddHours(1),
				PostRollEnd = currentTime.AddHours(2),
			};
			jobB.NodeGraph
				.Add(new JobResourceNode(pool, resource));

			jobA = objectCreator.CreateJob(jobA);
			jobB = objectCreator.CreateJob(jobB);

			jobA = TestContext.Api.Jobs.SaveAsTentative(jobA);
			jobB = TestContext.Api.Jobs.SaveAsTentative(jobB);

			var coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);
			Assert.IsNotNull(coreResource);
			coreResource.MaxConcurrency = 1;
			TestContext.ResourceManagerHelper.AddOrUpdateResources(true, [coreResource]);

			var reservations = TestContext.ResourceManagerHelper.GetReservationInstances(
				ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(jobA.Id)))
				.Concat(TestContext.ResourceManagerHelper.GetReservationInstances(
					ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(jobB.Id))))
				.ToList();

			Assert.AreEqual(2, reservations.Count, "Expected exactly one core reservation for each tentative job.");

			var quarantinedReservations = reservations.Where(x => x.IsQuarantined).ToList();
			Assert.IsTrue(quarantinedReservations.Count > 0, "Expected at least one reservation to be quarantined after lowering the concurrency of an overlapping resource.");
			Assert.IsTrue(
				quarantinedReservations.Any(x => x.QuarantinedResources.Any(y => y.QuarantinedResourceUsage.GUID == resource.CoreResourceId)),
				"Expected the lowered resource to be present in the quarantined resources.");
		}
	}
}
