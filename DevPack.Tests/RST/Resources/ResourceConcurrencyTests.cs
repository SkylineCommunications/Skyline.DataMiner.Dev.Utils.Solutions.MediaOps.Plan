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
	[DoNotParallelize]
	public sealed class ResourceConcurrencyTests : IDisposable
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

		[TestMethod]
		public void UpdateConcurrency_WithOverlappingTentativeReservations_ReportsTheJobsThatWouldBeQuarantined()
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
			resource = TestContext.Api.Resources.Complete(objectCreator.CreateResource(resource));

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
				return job;
			}

			var jobA = TestContext.Api.Jobs.SaveAsTentative(objectCreator.CreateJob(CreateJob("Job_1")));
			var jobB = TestContext.Api.Jobs.SaveAsTentative(objectCreator.CreateJob(CreateJob("Job_2")));

			resource.Concurrency = 1;
			var exception = Assert.ThrowsException<MediaOpsException>(() => TestContext.Api.Resources.Update(resource));

			var reservations = TestContext.ResourceManagerHelper.GetReservationInstances(
				ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(jobA.Id)))
				.Concat(TestContext.ResourceManagerHelper.GetReservationInstances(
					ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(jobB.Id))))
				.ToList();
			Assert.AreEqual(2, reservations.Count);
			Assert.IsFalse(reservations.Any(x => x.IsQuarantined), "Expected a refused update not to quarantine any reservations.");
			Assert.AreEqual(2, TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId).MaxConcurrency);
			Assert.AreEqual(2, TestContext.Api.Resources.Read(resource.Id).Concurrency);

			var error = exception.TraceData.ErrorData.OfType<ResourceUpdateWouldQuarantineJobsError>().Single();
			Assert.AreEqual(resource.Id, error.Id);
			CollectionAssert.AreEqual(new[] { jobB.Id }, error.JobIds.ToArray());
			Assert.AreEqual($"Updating resource '{resource.Name}' would move 1 job(s) to quarantine.", error.ErrorMessage);
			Assert.IsFalse(exception.Message.Contains(resource.Id.ToString()), "Expected the message not to contain the MediaOps resource ID.");
			Assert.IsFalse(exception.Message.Contains(resource.CoreResourceId.ToString()), "Expected the message not to contain the CORE resource ID.");
			Assert.IsFalse(reservations.Any(x => exception.Message.Contains(x.ID.ToString())), "Expected the message not to contain reservation IDs.");
		}

		[TestMethod]
		public void UpdateConcurrency_WithUnresolvableJobLookup_CountsTheReservationWithoutReportingAJobId()
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
			resource = TestContext.Api.Resources.Complete(objectCreator.CreateResource(resource));

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
				return job;
			}

			var jobA = TestContext.Api.Jobs.SaveAsTentative(objectCreator.CreateJob(CreateJob("Job_1")));
			var jobB = TestContext.Api.Jobs.SaveAsTentative(objectCreator.CreateJob(CreateJob("Job_2")));

			var fallbackReservationName = $"{prefix}_DetachedReservation";
			var fallbackJobId = Guid.NewGuid();
			var reservationToMutate = TestContext.ResourceManagerHelper
				.GetReservationInstances(ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(jobB.Id)))
				.Single();
			reservationToMutate.Name = fallbackReservationName;
			reservationToMutate.Properties.AddOrUpdate("Job ID", Convert.ToString(fallbackJobId));
			TestContext.ResourceManagerHelper.AddOrUpdateReservationInstances(reservationToMutate);

			resource.Concurrency = 1;
			var exception = Assert.ThrowsException<MediaOpsException>(() => TestContext.Api.Resources.Update(resource));

			var error = exception.TraceData.ErrorData.OfType<ResourceUpdateWouldQuarantineJobsError>().Single();
			Assert.AreEqual(0, error.JobIds.Count, "Expected a job that no longer exists not to be reported.");
			Assert.AreEqual($"Updating resource '{resource.Name}' would move 1 job(s) to quarantine.", error.ErrorMessage);
			Assert.IsFalse(exception.Message.Contains(fallbackJobId.ToString()), "Expected the message not to contain the stored job ID.");
			Assert.IsFalse(exception.Message.Contains(reservationToMutate.ID.ToString()), "Expected the message not to contain the reservation ID.");
			Assert.AreEqual(2, TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId).MaxConcurrency);
			Assert.AreEqual(2, TestContext.Api.Resources.Read(resource.Id).Concurrency);
			Assert.IsFalse(TestContext.ResourceManagerHelper.GetReservationInstances(
				ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(fallbackJobId)))
				.Single().IsQuarantined);
		}

		[TestMethod]
		public void SwapQuarantinedResource_ClearsTheQuarantineErrorFromTheJob()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			Resource CreateResource(string name, int concurrency)
			{
				var resource = new UnmanagedResource
				{
					Name = $"{prefix}_{name}",
					Concurrency = concurrency,
				}.AssignToPool(pool);

				return TestContext.Api.Resources.Complete(objectCreator.CreateResource(resource));
			}

			var sharedResource = CreateResource("SharedResource", 2);
			var freeResource = CreateResource("FreeResource", 1);

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
				job.NodeGraph.Add(new JobResourceNode(pool, sharedResource));

				return TestContext.Api.Jobs.SaveAsTentative(objectCreator.CreateJob(job));
			}

			var jobA = CreateJob("Job_1");
			var jobB = CreateJob("Job_2");

			// Lowering the concurrency of the shared resource pushes the resource usage of one of the overlapping
			// reservations into quarantine.
			var coreResource = TestContext.ResourceManagerHelper.GetResource(sharedResource.CoreResourceId);
			coreResource.MaxConcurrency = 1;
			TestContext.ResourceManagerHelper.AddOrUpdateResources(true, [coreResource]);

			var reservations = new[] { jobA, jobB }
				.SelectMany(x => TestContext.ResourceManagerHelper.GetReservationInstances(
					ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(x.Id))))
				.ToList();
			Assert.AreEqual(1, reservations.Count(x => x.IsQuarantined), "Expected exactly one reservation to be quarantined.");

			// The quarantine handling reports the error on the impacted job, just like the SRM quarantine script does.
			var quarantinedJob = new[] { jobA, jobB }
				.Select(x => TestContext.Api.Jobs.Read(x.Id))
				.Single(x => TestContext.Api.Jobs.Validate([x]).Single().HasError(QuarantinedReservationJobValidationError.ErrorCode));

			quarantinedJob = SyncQuarantineError(quarantinedJob);

			// Swapping the quarantined resource for an available one resolves the quarantine on the reservation, so the
			// error must no longer be reported on the job.
			var quarantinedNode = quarantinedJob.NodeGraph.Nodes.OfType<JobResourceNode>().Single();
			quarantinedJob.NodeGraph.Swap(quarantinedNode, new JobResourceNode(pool, freeResource));

			var updatedJob = TestContext.Api.Jobs.Update(quarantinedJob);

			Assert.IsFalse(
				updatedJob.Errors.Any(x => x.Code == QuarantinedReservationJobValidationError.ErrorCode),
				"Expected the quarantine error to be cleared after swapping the quarantined resource.");
			Assert.IsFalse(
				TestContext.Api.Jobs.Read(updatedJob.Id).Errors.Any(x => x.Code == QuarantinedReservationJobValidationError.ErrorCode),
				"Expected the quarantine error to be cleared on the stored job after swapping the quarantined resource.");
		}

		[TestMethod]
		public void LowerRequiredCapacityOfQuarantinedNode_ClearsTheQuarantine()
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
				Name = $"{prefix}_Resource",
				Concurrency = 1,
			}.AssignToPool(pool);
			resource.AddCapacity(new NumberCapacitySetting(capacity) { Value = 100 });
			resource = TestContext.Api.Resources.Complete(objectCreator.CreateResource(resource));

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime.AddHours(1),
				End = currentTime.AddHours(2),
				PreRollStart = currentTime.AddHours(1),
				PostRollEnd = currentTime.AddHours(2),
			};
			var node = new JobResourceNode(pool, resource);
			node.OrchestrationSettings.AddCapacity(new NumberCapacitySetting(capacity) { Value = 50 });
			job.NodeGraph.Add(node);

			job = TestContext.Api.Jobs.SaveAsTentative(objectCreator.CreateJob(job));

			// Lowering the capacity of the resource below what the job requires pushes the resource usage of the
			// reservation into quarantine.
			var coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);
			coreResource.Capacities.Single().Value.MaxDecimalQuantity = 20;
			TestContext.ResourceManagerHelper.AddOrUpdateResources(true, [coreResource]);

			Assert.IsTrue(GetReservation(job).IsQuarantined, "Expected the reservation to be quarantined after lowering the capacity of the resource.");

			// The quarantine handling reports the error on the job, just like the SRM quarantine script does.
			var quarantinedJob = SyncQuarantineError(job);

			Assert.IsTrue(
				quarantinedJob.Errors.Any(x => x.Code == QuarantinedReservationJobValidationError.ErrorCode),
				"Expected the quarantine error to be reported on the job while its resource is quarantined.");
			Assert.IsTrue(
				quarantinedJob.NodeGraph.Nodes.OfType<JobResourceNode>().Single().HasError,
				"Expected the resource node to be flagged while its resource is quarantined.");

			// Lowering the required capacity on the node configuration to a value the resource can provide resolves
			// the quarantine, so the error must no longer be reported on the job.
			var quarantinedNode = quarantinedJob.NodeGraph.Nodes.OfType<JobResourceNode>().Single();
			((NumberCapacitySetting)quarantinedNode.OrchestrationSettings.Capacities.Single()).Value = 10;

			var updatedJob = TestContext.Api.Jobs.Update(quarantinedJob);

			Assert.IsFalse(
				updatedJob.Errors.Any(x => x.Code == QuarantinedReservationJobValidationError.ErrorCode),
				"Expected the quarantine error to be cleared after lowering the required capacity.");

			var storedJob = TestContext.Api.Jobs.Read(updatedJob.Id);
			Assert.IsFalse(
				storedJob.Errors.Any(x => x.Code == QuarantinedReservationJobValidationError.ErrorCode),
				"Expected the quarantine error to be cleared on the stored job after lowering the required capacity.");
			Assert.IsFalse(
				storedJob.NodeGraph.Nodes.OfType<JobResourceNode>().Single().HasError,
				"Expected the resource node to no longer be flagged after lowering the required capacity.");

			var reservation = GetReservation(job);
			Assert.IsFalse(reservation.IsQuarantined, "Expected the reservation to be out of quarantine after lowering the required capacity.");
			Assert.AreEqual(0, reservation.QuarantinedResources.Count, "Expected no quarantined resources on the reservation.");

			var usage = reservation.ResourcesInReservationInstance.OfType<ServiceResourceUsageDefinition>().Single();
			Assert.AreEqual(resource.CoreResourceId, usage.GUID);
			Assert.AreEqual(10m, usage.RequiredCapacities.Single().DecimalQuantity);
		}

		[TestMethod]
		public void ChangeRequiredCapacityOfConcurrencyQuarantinedNode_SavesTheJobAndKeepsTheQuarantine()
		{
			var setup = CreateConcurrencyQuarantineSetup();
			var quarantinedJob = setup.QuarantinedJob;

			// Changing the requirements does not resolve a concurrency conflict, but must not prevent the job from being saved.
			((NumberCapacitySetting)quarantinedJob.NodeGraph.Nodes.Single().OrchestrationSettings.Capacities.Single()).Value = 5;
			var updatedJob = TestContext.Api.Jobs.Update(quarantinedJob);

			Assert.AreEqual(5m, ((NumberCapacitySetting)TestContext.Api.Jobs.Read(updatedJob.Id).NodeGraph.Nodes.Single().OrchestrationSettings.Capacities.Single()).Value);
			Assert.IsTrue(GetReservation(updatedJob).IsQuarantined, "Expected the reservation to remain quarantined while the concurrency is still exceeded.");
			Assert.IsTrue(
				updatedJob.Errors.Any(x => x.Code == QuarantinedReservationJobValidationError.ErrorCode),
				"Expected the quarantine error to remain on the job while the concurrency is still exceeded.");
			Assert.IsFalse(GetReservation(setup.OtherJob).IsQuarantined, "Expected the refused release not to quarantine the other booking.");
		}

		[TestMethod]
		public void MoveConcurrencyQuarantinedJobOutOfTheOverlap_ClearsTheQuarantine()
		{
			var setup = CreateConcurrencyQuarantineSetup();
			var quarantinedJob = setup.QuarantinedJob;

			quarantinedJob.PostRollEnd = setup.CurrentTime.AddHours(6);
			quarantinedJob.End = setup.CurrentTime.AddHours(6);
			quarantinedJob.Start = setup.CurrentTime.AddHours(5);
			quarantinedJob.PreRollStart = setup.CurrentTime.AddHours(5);
			var updatedJob = TestContext.Api.Jobs.Update(quarantinedJob);

			Assert.IsFalse(GetReservation(updatedJob).IsQuarantined, "Expected the reservation to leave quarantine once it no longer overlaps.");
			Assert.IsFalse(
				updatedJob.Errors.Any(x => x.Code == QuarantinedReservationJobValidationError.ErrorCode),
				"Expected the quarantine error to be cleared once the job no longer overlaps.");
			Assert.IsFalse(GetReservation(setup.OtherJob).IsQuarantined, "Expected the other booking to stay out of quarantine.");
		}

		[TestMethod]
		public void LowerRequiredCapacityOfQuarantinedNodeToValueThatStillDoesNotFit_SavesTheJobAndKeepsTheQuarantine()
		{
			var setup = CreateCapacityQuarantineSetup();
			var quarantinedJob = setup.QuarantinedJob;

			// The resource only offers 20.
			((NumberCapacitySetting)quarantinedJob.NodeGraph.Nodes.Single().OrchestrationSettings.Capacities.Single()).Value = 30;
			var updatedJob = TestContext.Api.Jobs.Update(quarantinedJob);

			Assert.AreEqual(30m, ((NumberCapacitySetting)TestContext.Api.Jobs.Read(updatedJob.Id).NodeGraph.Nodes.Single().OrchestrationSettings.Capacities.Single()).Value);
			Assert.IsTrue(GetReservation(updatedJob).IsQuarantined, "Expected the reservation to remain quarantined while the required capacity still does not fit.");
			Assert.IsTrue(
				updatedJob.Errors.Any(x => x.Code == QuarantinedReservationJobValidationError.ErrorCode),
				"Expected the quarantine error to remain on the job while the required capacity still does not fit.");
		}

		[TestMethod]
		public void LowerRequiredCapacityOfQuarantinedNodeToFitNextToOtherBooking_ClearsTheQuarantine()
		{
			var setup = CreateSharedCapacityQuarantineSetup();
			var quarantinedJob = setup.QuarantinedJob;

			// The other booking uses 40 of the 50 that the resource offers.
			((NumberCapacitySetting)quarantinedJob.NodeGraph.Nodes.Single().OrchestrationSettings.Capacities.Single()).Value = 10;
			var updatedJob = TestContext.Api.Jobs.Update(quarantinedJob);

			Assert.IsFalse(GetReservation(updatedJob).IsQuarantined, "Expected the reservation to leave quarantine once the required capacity fits.");
			Assert.IsFalse(
				updatedJob.Errors.Any(x => x.Code == QuarantinedReservationJobValidationError.ErrorCode),
				"Expected the quarantine error to be cleared once the required capacity fits.");
			Assert.IsFalse(GetReservation(setup.OtherJob).IsQuarantined, "Expected the other booking to stay out of quarantine.");
		}

		[TestMethod]
		public void LowerRequiredCapacityOfQuarantinedNodeNotEnoughNextToOtherBooking_KeepsTheQuarantineOfOnlyThisJob()
		{
			var setup = CreateSharedCapacityQuarantineSetup();
			var quarantinedJob = setup.QuarantinedJob;

			// The other booking uses 40 of the 50 that the resource offers.
			((NumberCapacitySetting)quarantinedJob.NodeGraph.Nodes.Single().OrchestrationSettings.Capacities.Single()).Value = 20;
			var updatedJob = TestContext.Api.Jobs.Update(quarantinedJob);

			Assert.AreEqual(20m, ((NumberCapacitySetting)TestContext.Api.Jobs.Read(updatedJob.Id).NodeGraph.Nodes.Single().OrchestrationSettings.Capacities.Single()).Value);
			Assert.IsTrue(GetReservation(updatedJob).IsQuarantined, "Expected the reservation to remain quarantined while the required capacity does not fit.");
			Assert.IsTrue(
				updatedJob.Errors.Any(x => x.Code == QuarantinedReservationJobValidationError.ErrorCode),
				"Expected the quarantine error to remain on the job.");

			var otherReservation = GetReservation(setup.OtherJob);
			Assert.IsFalse(otherReservation.IsQuarantined, "Expected the refused release not to quarantine the other booking.");
			Assert.AreEqual(40m, otherReservation.ResourcesInReservationInstance.OfType<ServiceResourceUsageDefinition>().Single().RequiredCapacities.Single().DecimalQuantity);
		}

		[TestMethod]
		public void RestoreCapacityOfQuarantinedResource_NextJobUpdateClearsTheQuarantine()
		{
			var setup = CreateCapacityQuarantineSetup();
			var quarantinedJob = setup.QuarantinedJob;

			// The quarantine is resolved outside of the job, which itself keeps requiring the same capacity.
			RestoreCapacity(setup.Resource);

			// Only updates that impact the reservation reach the core software, so the job is renamed.
			quarantinedJob.Name = $"{quarantinedJob.Name}_Renamed";
			var updatedJob = TestContext.Api.Jobs.Update(quarantinedJob);

			Assert.IsFalse(
				updatedJob.Errors.Any(x => x.Code == QuarantinedReservationJobValidationError.ErrorCode),
				"Expected the quarantine error to be cleared once the resource offers the required capacity again.");

			var storedJob = TestContext.Api.Jobs.Read(updatedJob.Id);
			Assert.IsFalse(
				storedJob.Errors.Any(x => x.Code == QuarantinedReservationJobValidationError.ErrorCode),
				"Expected the quarantine error to be cleared on the stored job.");
			Assert.IsFalse(storedJob.NodeGraph.Nodes.OfType<JobResourceNode>().Single().HasError, "Expected the resource node to no longer be flagged.");

			var reservation = GetReservation(updatedJob);
			Assert.IsFalse(reservation.IsQuarantined, "Expected the reservation to be out of quarantine.");
			Assert.AreEqual(50m, reservation.ResourcesInReservationInstance.OfType<ServiceResourceUsageDefinition>().Single().RequiredCapacities.Single().DecimalQuantity);
		}

		[TestMethod]
		public void RestoreCapacityOfQuarantinedResource_SaveWithoutChanges_KeepsTheQuarantine()
		{
			var setup = CreateCapacityQuarantineSetup();

			RestoreCapacity(setup.Resource);

			// A save without reservation-impacting changes does not reach the core software.
			var updatedJob = TestContext.Api.Jobs.Update(setup.QuarantinedJob);

			Assert.IsTrue(GetReservation(updatedJob).IsQuarantined, "Expected the reservation to remain quarantined.");
			Assert.IsTrue(
				TestContext.Api.Jobs.Read(updatedJob.Id).Errors.Any(x => x.Code == QuarantinedReservationJobValidationError.ErrorCode),
				"Expected the quarantine error to remain on the stored job.");
		}

		[TestMethod]
		public void ChangeRequiredCapacityOfQuarantinedJobWithUnavailableResource_ReportsTheUnrelatedError()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = TestContext.Api.ResourcePools.Complete(objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" }));

			var capacity = new NumberCapacity { Name = $"{prefix}_Capacity", RangeMin = 0, RangeMax = 100 };
			objectCreator.CreateCapacity(capacity);

			var sharedResource = new UnmanagedResource { Name = $"{prefix}_SharedResource", Concurrency = 2 }.AssignToPool(pool);
			sharedResource.AddCapacity(new NumberCapacitySetting(capacity) { Value = 100 });
			sharedResource = TestContext.Api.Resources.Complete(objectCreator.CreateResource(sharedResource));

			var otherResource = TestContext.Api.Resources.Complete(objectCreator.CreateResource(
				new UnmanagedResource { Name = $"{prefix}_OtherResource", Concurrency = 2 }.AssignToPool(pool)));

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
				var sharedNode = new JobResourceNode(pool, sharedResource);
				sharedNode.OrchestrationSettings.AddCapacity(new NumberCapacitySetting(capacity) { Value = 10 });
				job.NodeGraph.Add(sharedNode);
				job.NodeGraph.Add(new JobResourceNode(pool, otherResource));

				return TestContext.Api.Jobs.SaveAsTentative(objectCreator.CreateJob(job));
			}

			var jobs = new[] { CreateJob("Job_1"), CreateJob("Job_2") };

			var sharedCoreResource = TestContext.ResourceManagerHelper.GetResource(sharedResource.CoreResourceId);
			sharedCoreResource.MaxConcurrency = 1;
			TestContext.ResourceManagerHelper.AddOrUpdateResources(true, [sharedCoreResource]);

			var quarantinedJob = SyncQuarantineError(jobs.Single(x => GetReservation(x).IsQuarantined));

			var otherCoreResource = TestContext.ResourceManagerHelper.GetResource(otherResource.CoreResourceId);
			otherCoreResource.Mode = Skyline.DataMiner.Net.Messages.ResourceMode.Unavailable;
			TestContext.ResourceManagerHelper.AddOrUpdateResources(true, [otherCoreResource]);

			// The core software refuses every submission because of the unavailable resource, including the fallback.
			var sharedNodeToChange = quarantinedJob.NodeGraph.Nodes.OfType<JobResourceNode>().Single(x => x.ResourceId == sharedResource.Id);
			((NumberCapacitySetting)sharedNodeToChange.OrchestrationSettings.Capacities.Single()).Value = 5;
			var exception = Assert.ThrowsException<MediaOpsException>(() => TestContext.Api.Jobs.Update(quarantinedJob));

			StringAssert.Contains(exception.Message, "ResourceNotAvailable");
			StringAssert.Contains(exception.Message, otherResource.CoreResourceId.ToString());

			var storedJob = TestContext.Api.Jobs.Read(quarantinedJob.Id);
			Assert.AreEqual(10m, ((NumberCapacitySetting)storedJob.NodeGraph.Nodes.OfType<JobResourceNode>().Single(x => x.ResourceId == sharedResource.Id).OrchestrationSettings.Capacities.Single()).Value);
			Assert.IsTrue(GetReservation(storedJob).IsQuarantined, "Expected the reservation to remain quarantined.");
			Assert.IsTrue(storedJob.Errors.Any(x => x.Code == QuarantinedReservationJobValidationError.ErrorCode), "Expected the quarantine error to remain on the job.");
		}

		private static ReservationInstance GetReservation(Job job)
		{
			return TestContext.ResourceManagerHelper.GetReservationInstances(
				ReservationInstanceExposers.Properties.StringField("Job ID").Equal(Convert.ToString(job.Id)))
				.Single();
		}

		private static void RestoreCapacity(Resource resource)
		{
			var coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);
			coreResource.Capacities.Single().Value.MaxDecimalQuantity = 100;
			TestContext.ResourceManagerHelper.AddOrUpdateResources(true, [coreResource]);
		}

		private static Job SyncQuarantineError(Job job)
		{
			var quarantinedJob = TestContext.ReportQuarantineOnJob(job.Id);

			Assert.IsTrue(
				quarantinedJob.Errors.Any(x => x.Code == QuarantinedReservationJobValidationError.ErrorCode),
				"Expected the quarantine error to be reported on the job while its resource is quarantined.");

			return quarantinedJob;
		}

		// One job requiring 50 of a resource of which the capacity is forced down from 100 to 20.
		private (Job QuarantinedJob, Resource Resource) CreateCapacityQuarantineSetup()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = TestContext.Api.ResourcePools.Complete(objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" }));

			var capacity = new NumberCapacity { Name = $"{prefix}_Capacity", RangeMin = 0, RangeMax = 100 };
			objectCreator.CreateCapacity(capacity);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource", Concurrency = 1 }.AssignToPool(pool);
			resource.AddCapacity(new NumberCapacitySetting(capacity) { Value = 100 });
			resource = TestContext.Api.Resources.Complete(objectCreator.CreateResource(resource));

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime.AddHours(1),
				End = currentTime.AddHours(2),
				PreRollStart = currentTime.AddHours(1),
				PostRollEnd = currentTime.AddHours(2),
			};
			var node = new JobResourceNode(pool, resource);
			node.OrchestrationSettings.AddCapacity(new NumberCapacitySetting(capacity) { Value = 50 });
			job.NodeGraph.Add(node);
			job = TestContext.Api.Jobs.SaveAsTentative(objectCreator.CreateJob(job));

			var coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);
			coreResource.Capacities.Single().Value.MaxDecimalQuantity = 20;
			TestContext.ResourceManagerHelper.AddOrUpdateResources(true, [coreResource]);

			return (SyncQuarantineError(job), resource);
		}

		// Two overlapping jobs requiring 40 each of a resource of which the capacity is forced down from 100 to 50.
		private (Job QuarantinedJob, Job OtherJob) CreateSharedCapacityQuarantineSetup()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = TestContext.Api.ResourcePools.Complete(objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" }));

			var capacity = new NumberCapacity { Name = $"{prefix}_Capacity", RangeMin = 0, RangeMax = 100 };
			objectCreator.CreateCapacity(capacity);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource", Concurrency = 2 }.AssignToPool(pool);
			resource.AddCapacity(new NumberCapacitySetting(capacity) { Value = 100 });
			resource = TestContext.Api.Resources.Complete(objectCreator.CreateResource(resource));

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
				var node = new JobResourceNode(pool, resource);
				node.OrchestrationSettings.AddCapacity(new NumberCapacitySetting(capacity) { Value = 40 });
				job.NodeGraph.Add(node);

				return TestContext.Api.Jobs.SaveAsTentative(objectCreator.CreateJob(job));
			}

			var jobs = new[] { CreateJob("Job_1"), CreateJob("Job_2") };

			var coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);
			coreResource.Capacities.Single().Value.MaxDecimalQuantity = 50;
			TestContext.ResourceManagerHelper.AddOrUpdateResources(true, [coreResource]);

			var quarantinedJob = jobs.Single(x => GetReservation(x).IsQuarantined);
			return (SyncQuarantineError(quarantinedJob), jobs.Single(x => x.Id != quarantinedJob.Id));
		}

		private (Job QuarantinedJob, Job OtherJob, DateTime CurrentTime) CreateConcurrencyQuarantineSetup()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = TestContext.Api.ResourcePools.Complete(objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" }));

			var capacity = new NumberCapacity { Name = $"{prefix}_Capacity", RangeMin = 0, RangeMax = 100 };
			objectCreator.CreateCapacity(capacity);

			var resource = new UnmanagedResource { Name = $"{prefix}_Resource", Concurrency = 2 }.AssignToPool(pool);
			resource.AddCapacity(new NumberCapacitySetting(capacity) { Value = 100 });
			resource = TestContext.Api.Resources.Complete(objectCreator.CreateResource(resource));

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
				var node = new JobResourceNode(pool, resource);
				node.OrchestrationSettings.AddCapacity(new NumberCapacitySetting(capacity) { Value = 10 });
				job.NodeGraph.Add(node);

				return TestContext.Api.Jobs.SaveAsTentative(objectCreator.CreateJob(job));
			}

			var jobs = new[] { CreateJob("Job_1"), CreateJob("Job_2") };

			var coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);
			coreResource.MaxConcurrency = 1;
			TestContext.ResourceManagerHelper.AddOrUpdateResources(true, [coreResource]);

			var quarantinedJob = jobs.Single(x => GetReservation(x).IsQuarantined);
			return (SyncQuarantineError(quarantinedJob), jobs.Single(x => x.Id != quarantinedJob.Id), currentTime);
		}
	}
}
