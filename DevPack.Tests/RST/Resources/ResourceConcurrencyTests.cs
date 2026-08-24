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
		public void SaveAsTentative_SecondJobWithSameResourceExceedingConcurrency_Fails()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resource = new UnmanagedResource
			{
				Name = $"{prefix}_Resource",
				Concurrency = 1,
			}.AssignToPool(pool);
			resource = objectCreator.CreateResource(resource);
			resource = TestContext.Api.Resources.Complete(resource);

			Job CreateJob(string name)
			{
				var job = new Job
				{
					Name = $"{prefix}_{name}",
					Start = currentTime.AddHours(1),
					End = currentTime.AddHours(2),
					PreRollStart = currentTime.AddHours(1),
					PostRollEnd = currentTime.AddHours(2),
				};

				job.NodeGraph.Add(new JobResourceNode(pool, resource));

				return objectCreator.CreateJob(job);
			}

			var jobA = CreateJob("Job_1");
			var jobB = CreateJob("Job_2");

			jobA = TestContext.Api.Jobs.SaveAsTentative(jobA);
			Assert.AreEqual(JobState.Tentative, jobA.State, "Expected the first job to be saved as tentative.");

			// The resource is already reserved by the pending reservation of the first job, so the second job cannot
			// claim it for the same time range.
			Assert.ThrowsException<MediaOpsException>(
				() => TestContext.Api.Jobs.SaveAsTentative(jobB),
				"Expected the second job not to be saved as tentative while the resource concurrency is exceeded.");

			var storedJobB = TestContext.Api.Jobs.Read(jobB.Id);
			Assert.AreEqual(JobState.Draft, storedJobB.State, "Expected the second job to remain in draft state.");

			var reservationsOfJobB = TestContext.ResourceManagerHelper.GetReservationInstances(
				ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(jobB.Id))).ToList();

			Assert.AreEqual(0, reservationsOfJobB.Count, "Expected no core reservation to be created for the second job.");
		}

		[TestMethod]
		public void UpdateConcurrency_WithOverlappingTentativeReservations_QuarantinesReservation()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var capacity = new NumberCapacity
			{
				Name = $"{prefix}_Capacity",
				RangeMin = 0,
				RangeMax = 100,
			};
			objectCreator.CreateCapacity(capacity);

			var resource = new UnmanagedResource
			{
				Name = $"{prefix}_ResourceA",
				Concurrency = 2,
			}.AssignToPool(pool);
			resource.AddCapacity(new NumberCapacitySetting(capacity) { Value = 100 });
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
			var nodeA = new JobResourceNode(pool, resource);
			nodeA.OrchestrationSettings.AddCapacity(new NumberCapacitySetting(capacity) { Value = 10 });
			jobA.NodeGraph.Add(nodeA);

			var jobB = new Job
			{
				Name = $"{prefix}_Job_2",
				Start = currentTime.AddHours(1),
				End = currentTime.AddHours(2),
				PreRollStart = currentTime.AddHours(1),
				PostRollEnd = currentTime.AddHours(2),
			};
			var nodeB = new JobResourceNode(pool, resource);
			nodeB.OrchestrationSettings.AddCapacity(new NumberCapacitySetting(capacity) { Value = 10 });
			jobB.NodeGraph.Add(nodeB);

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
			Assert.AreEqual(1, quarantinedReservations.Count, "Expected exactly one reservation to be quarantined after lowering the concurrency of an overlapping resource.");
			Assert.IsTrue(
				quarantinedReservations.Any(x => x.QuarantinedResources.Any(y => y.QuarantinedResourceUsage.GUID == resource.CoreResourceId)),
				"Expected the lowered resource to be present in the quarantined resources.");
		}
	}
}
