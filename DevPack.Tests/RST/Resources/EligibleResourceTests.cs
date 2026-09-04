namespace RT_MediaOps.Plan.RST.Resources
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using RT_MediaOps.Plan.Extensions;
	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	/// <summary>
	/// Tests for <see cref="IResourcesRepository.GetEligibleResources(EligibleResourcesContext)"/>
	/// and <see cref="Job.AssignEligibleResources(IMediaOpsPlanApi)"/>.
	/// </summary>
	/// <remarks>
	/// Resource eligibility is resolved by the DataMiner Resource Manager for real connections and by the in-memory
	/// Resource Manager store for simulated connections.
	/// </remarks>
	[TestClass]
	[TestCategory("IntegrationTest")]
	public sealed class EligibleResourceTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public EligibleResourceTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void GetEligibleResources_RequestedCapability_ReturnsOnlyResourcesWithThatCapabilityValue()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var capability = new Capability { Name = $"{prefix}_Resolution" };
			capability.SetDiscretes(new[] { "4K", "HD" });
			objectCreator.CreateCapability(capability);

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var matchingResource = CreateCompleteResource($"{prefix}_Resource_4K", pool, resource => resource.AddCapability(CreateCapabilitySettings(capability, "4K")));
			var otherValueResource = CreateCompleteResource($"{prefix}_Resource_HD", pool, resource => resource.AddCapability(CreateCapabilitySettings(capability, "HD")));
			var withoutCapabilityResource = CreateCompleteResource($"{prefix}_Resource_None", pool, null);

			var eligibleIds = GetEligibleResourceIds(
				currentTime.AddHours(1),
				currentTime.AddHours(2),
				new[] { new CapabilitySetting(capability.Id) { Value = "4K" } },
				Array.Empty<CapacitySetting>(),
				pool);

			CollectionAssert.Contains(eligibleIds, matchingResource.Id, "Expected the resource that has the requested capability value to be eligible.");
			CollectionAssert.DoesNotContain(eligibleIds, otherValueResource.Id, "Expected the resource that has another value for the requested capability not to be eligible.");
			CollectionAssert.DoesNotContain(eligibleIds, withoutCapabilityResource.Id, "Expected the resource that does not have the requested capability not to be eligible.");
		}

		[TestMethod]
		public void GetEligibleResources_RequestedCapacity_ReturnsOnlyResourcesWithEnoughCapacity()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var capacity = new NumberCapacity
			{
				Name = $"{prefix}_Bandwidth",
				RangeMin = 0,
				RangeMax = 100,
			};
			objectCreator.CreateCapacity(capacity);

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var sufficientResource = CreateCompleteResource($"{prefix}_Resource_100", pool, resource => resource.AddCapacity(new NumberCapacitySetting(capacity.Id) { Value = 100 }));
			var insufficientResource = CreateCompleteResource($"{prefix}_Resource_10", pool, resource => resource.AddCapacity(new NumberCapacitySetting(capacity.Id) { Value = 10 }));
			var withoutCapacityResource = CreateCompleteResource($"{prefix}_Resource_None", pool, null);

			var eligibleIds = GetEligibleResourceIds(
				currentTime.AddHours(1),
				currentTime.AddHours(2),
				Array.Empty<CapabilitySetting>(),
				new CapacitySetting[] { new NumberCapacitySetting(capacity.Id) { Value = 50 } },
				pool);

			CollectionAssert.Contains(eligibleIds, sufficientResource.Id, "Expected the resource that has enough capacity to be eligible.");
			CollectionAssert.DoesNotContain(eligibleIds, insufficientResource.Id, "Expected the resource that does not have enough capacity not to be eligible.");
			CollectionAssert.DoesNotContain(eligibleIds, withoutCapacityResource.Id, "Expected the resource that does not have the requested capacity not to be eligible.");
		}

		[TestMethod]
		public void GetEligibleResources_CapacityUsedByConfirmedJob_ReturnsResourceOnlyOutsideTheBookedTimeRange()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var capacity = new NumberCapacity
			{
				Name = $"{prefix}_Bandwidth",
				RangeMin = 0,
				RangeMax = 100,
			};
			objectCreator.CreateCapacity(capacity);

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resource = CreateCompleteResource($"{prefix}_Resource", pool, x => x.AddCapacity(new NumberCapacitySetting(capacity.Id) { Value = 100 }));

			// Book 80 of the 100 available capacity between +1h and +2h.
			var bookedStart = currentTime.AddHours(1);
			var bookedEnd = currentTime.AddHours(2);

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = bookedStart,
				End = bookedEnd,
				PreRollStart = bookedStart,
				PostRollEnd = bookedEnd,
			};

			var resourceNode = new JobResourceNode(pool, resource);
			resourceNode.OrchestrationSettings.AddCapacity(new NumberCapacitySetting(capacity.Id) { Value = 80 });
			job.NodeGraph.Add(resourceNode);

			job = objectCreator.CreateJob(job);
			job = TestContext.Api.Jobs.SaveAsTentative(job);
			job = TestContext.Api.Jobs.Confirm(job);

			var requestedCapacities = new CapacitySetting[] { new NumberCapacitySetting(capacity.Id) { Value = 50 } };

			var eligibleDuringBooking = GetEligibleResourceIds(
				bookedStart.AddMinutes(15),
				bookedEnd.AddMinutes(-15),
				Array.Empty<CapabilitySetting>(),
				requestedCapacities,
				pool);

			CollectionAssert.DoesNotContain(eligibleDuringBooking, resource.Id, "Expected the resource not to be eligible while its capacity is used by the confirmed job.");

			var eligibleAfterBooking = GetEligibleResourceIds(
				bookedEnd.AddHours(1),
				bookedEnd.AddHours(2),
				Array.Empty<CapabilitySetting>(),
				requestedCapacities,
				pool);

			CollectionAssert.Contains(eligibleAfterBooking, resource.Id, "Expected the resource to be eligible outside the time range of the confirmed job.");
		}

		[TestMethod]
		public void GetEligibleResources_ResourceWithExistingUsage_ReturnsCapacityAndConcurrencyUsage()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();
			var capacity = new NumberCapacity { Name = $"{prefix}_Bandwidth", RangeMin = 0, RangeMax = 100 };
			objectCreator.CreateCapacity(capacity);

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resource = CreateCompleteResource($"{prefix}_Resource", pool, value =>
			{
				value.Concurrency = 3;
				value.AddCapacity(new NumberCapacitySetting(capacity.Id) { Value = 100 });
			});

			var bookedStart = currentTime.AddHours(1);
			var bookedEnd = currentTime.AddHours(2);
			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = bookedStart,
				End = bookedEnd,
				PreRollStart = bookedStart,
				PostRollEnd = bookedEnd,
			};

			var resourceNode = new JobResourceNode(pool, resource);
			resourceNode.OrchestrationSettings.AddCapacity(new NumberCapacitySetting(capacity.Id) { Value = 40 });
			job.NodeGraph.Add(resourceNode);
			job = objectCreator.CreateJob(job);
			job = TestContext.Api.Jobs.SaveAsTentative(job);
			TestContext.Api.Jobs.Confirm(job);

			var result = TestContext.Api.Resources.GetEligibleResources(new EligibleResourcesContext(bookedStart, bookedEnd)
			{
				CapacitySettings = new[] { new NumberCapacitySetting(capacity.Id) { Value = 10 } },
				Filter = ResourceExposers.Id.Equal(resource.Id),
			});

			var eligibleResource = result.EligibleResources.Single();
			var capacityUsage = eligibleResource.Usage.CapacityUsages.OfType<NumberCapacityUsage>().Single(x => x.CapacityId == capacity.Id);

			Assert.AreEqual(resource.Id, eligibleResource.Resource.Id);
			Assert.AreEqual(1, eligibleResource.Usage.ConcurrencyConsumption);
			Assert.AreEqual(2, eligibleResource.Usage.RemainingConcurrency);
			Assert.AreEqual(40m, capacityUsage.CurrentConsumption);
			Assert.AreEqual(60m, capacityUsage.Remaining);
		}

		[TestMethod]
		public void GetEligibleResources_ResourceWithRangeUsage_ReturnsConsumedAndRemainingRanges()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();
			var capacity = new RangeCapacity { Name = $"{prefix}_Frequency" };
			objectCreator.CreateCapacity(capacity);

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resource = CreateCompleteResource($"{prefix}_Resource", pool, value =>
			{
				value.Concurrency = 2;
				value.AddCapacity(new RangeCapacitySetting(capacity.Id) { MinValue = 0, MaxValue = 100 });
			});

			var bookedStart = currentTime.AddHours(1);
			var bookedEnd = currentTime.AddHours(2);
			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = bookedStart,
				End = bookedEnd,
				PreRollStart = bookedStart,
				PostRollEnd = bookedEnd,
			};

			var resourceNode = new JobResourceNode(pool, resource);
			resourceNode.OrchestrationSettings.AddCapacity(new RangeCapacitySetting(capacity.Id) { MinValue = 20, MaxValue = 40 });
			job.NodeGraph.Add(resourceNode);
			job = objectCreator.CreateJob(job);
			job = TestContext.Api.Jobs.SaveAsTentative(job);
			TestContext.Api.Jobs.Confirm(job);

			var result = TestContext.Api.Resources.GetEligibleResources(new EligibleResourcesContext(bookedStart, bookedEnd)
			{
				CapacitySettings = new[] { new RangeCapacitySetting(capacity.Id) { MinValue = 60, MaxValue = 70 } },
				Filter = ResourceExposers.Id.Equal(resource.Id),
			});

			var usage = result.EligibleResources.Single().Usage.CapacityUsages.OfType<RangeCapacityUsage>().Single();
			var consumed = usage.CurrentConsumption.Single();
			var remaining = usage.Remaining.ToList();

			Assert.AreEqual(20m, consumed.Start);
			Assert.AreEqual(40m, consumed.End);
			Assert.AreEqual(2, remaining.Count);
			Assert.AreEqual(0m, remaining[0].Start);
			Assert.AreEqual(20m, remaining[0].End);
			Assert.AreEqual(40m, remaining[1].Start);
			Assert.AreEqual(100m, remaining[1].End);
		}

		[TestMethod]
		public void GetEligibleResources_CompleteResourceAlreadyBooked_ReturnsResourceOnlyOutsideTheBookedTimeRange()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resource = CreateCompleteResource($"{prefix}_Resource", pool, null);

			var bookedStart = currentTime.AddHours(1);
			var bookedEnd = currentTime.AddHours(2);

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = bookedStart,
				End = bookedEnd,
				PreRollStart = bookedStart,
				PostRollEnd = bookedEnd,
			};

			job.NodeGraph.Add(new JobResourceNode(pool, resource));
			job = objectCreator.CreateJob(job);
			job = TestContext.Api.Jobs.SaveAsTentative(job);
			job = TestContext.Api.Jobs.Confirm(job);

			var eligibleDuringBooking = GetEligibleResourceIds(
				bookedStart.AddMinutes(15),
				bookedEnd.AddMinutes(-15),
				Array.Empty<CapabilitySetting>(),
				Array.Empty<CapacitySetting>(),
				pool);

			CollectionAssert.DoesNotContain(eligibleDuringBooking, resource.Id, "Expected the resource not to be eligible while its concurrency is fully used by the confirmed job.");

			var eligibleAfterBooking = GetEligibleResourceIds(
				bookedEnd.AddHours(1),
				bookedEnd.AddHours(2),
				Array.Empty<CapabilitySetting>(),
				Array.Empty<CapacitySetting>(),
				pool);

			CollectionAssert.Contains(eligibleAfterBooking, resource.Id, "Expected the resource to be eligible outside the time range of the confirmed job.");
		}

		[TestMethod]
		public void GetEligibleResources_CompleteResourceBookedByIgnoredJob_ReturnsResourceDuringBooking()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resource = CreateCompleteResource($"{prefix}_Resource", pool, null);
			var bookedStart = currentTime.AddHours(1);
			var bookedEnd = currentTime.AddHours(2);

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = bookedStart,
				End = bookedEnd,
				PreRollStart = bookedStart,
				PostRollEnd = bookedEnd,
			};

			job.NodeGraph.Add(new JobResourceNode(pool, resource));
			job = objectCreator.CreateJob(job);
			job = TestContext.Api.Jobs.SaveAsTentative(job);
			job = TestContext.Api.Jobs.Confirm(job);

			var context = new EligibleResourcesContext(bookedStart.AddMinutes(15), bookedEnd.AddMinutes(-15))
			{
				Filter = ResourceExposers.Id.Equal(resource.Id),
				JobIdToIgnore = job.Id,
			};

			var result = TestContext.Api.Resources.GetEligibleResources(context);
			var eligibleResource = result.EligibleResources.Single();

			Assert.AreEqual(resource.Id, eligibleResource.Resource.Id, "Expected the resource to be eligible when the consuming job is ignored.");
			Assert.AreEqual(0, eligibleResource.Usage.ConcurrencyConsumption, "Expected the ignored job not to contribute to concurrency consumption.");
			Assert.AreEqual(1, eligibleResource.Usage.RemainingConcurrency, "Expected the ignored job's concurrency slot to remain available.");
		}

		[TestMethod]
		public void GetEligibleResources_DeprecatedResource_DoesNotReturnResource()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resource = CreateCompleteResource($"{prefix}_Resource", pool, null);
			resource = TestContext.Api.Resources.Deprecate(resource);

			var eligibleIds = GetEligibleResourceIds(
				currentTime.AddHours(1),
				currentTime.AddHours(2),
				Array.Empty<CapabilitySetting>(),
				Array.Empty<CapacitySetting>(),
				pool);

			CollectionAssert.DoesNotContain(eligibleIds, resource.Id, "Expected a deprecated resource not to be eligible.");
		}

		[TestMethod]
		public void GetEligibleResources_WithFilter_OnlyConsidersResourcesMatchingTheFilter()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var firstResource = CreateCompleteResource($"{prefix}_Resource_1", pool, null);
			var secondResource = CreateCompleteResource($"{prefix}_Resource_2", pool, null);

			var eligible = TestContext.Api.Resources.GetEligibleResources(new EligibleResourcesContext(currentTime.AddHours(1), currentTime.AddHours(2))
			{
				Filter = ResourceExposers.Id.Equal(firstResource.Id),
			});

			var eligibleIds = eligible.EligibleResources.Select(x => x.Resource.Id).ToList();

			CollectionAssert.Contains(eligibleIds, firstResource.Id, "Expected the resource that matches the filter to be eligible.");
			CollectionAssert.DoesNotContain(eligibleIds, secondResource.Id, "Expected a resource that does not match the filter not to be returned.");
		}

		[TestMethod]
		public void GetEligibleResources_WithPropertyValueFilter_OnlyConsidersResourcesWithThatPropertyValue()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var property = objectCreator.CreateResourceProperty(new ResourceProperty { Name = $"{prefix}_Location" });

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var matchingValue = $"{prefix}_StudioA";
			var otherValue = $"{prefix}_StudioB";

			var matchingResource = CreateCompleteResource($"{prefix}_Resource_A", pool, resource => resource.AddProperty(new ResourcePropertySettings(property.Id) { Value = matchingValue }));
			var otherValueResource = CreateCompleteResource($"{prefix}_Resource_B", pool, resource => resource.AddProperty(new ResourcePropertySettings(property.Id) { Value = otherValue }));
			var withoutPropertyResource = CreateCompleteResource($"{prefix}_Resource_None", pool, null);

			var eligible = TestContext.Api.Resources.GetEligibleResources(new EligibleResourcesContext(currentTime.AddHours(1), currentTime.AddHours(2))
			{
				Filter = ResourceExposers.ResourcePoolIds.Contains(pool.Id)
					.AND(ResourceExposers.Properties.PropertyId.Equal(property.Id))
					.AND(ResourceExposers.Properties.Value.Equal(matchingValue)),
			});

			var eligibleIds = eligible.EligibleResources.Select(x => x.Resource.Id).ToList();

			CollectionAssert.Contains(eligibleIds, matchingResource.Id, "Expected the resource that has the requested property value to be eligible.");
			CollectionAssert.DoesNotContain(eligibleIds, otherValueResource.Id, "Expected the resource that has another value for the property not to be eligible.");
			CollectionAssert.DoesNotContain(eligibleIds, withoutPropertyResource.Id, "Expected the resource that does not have the property not to be eligible.");
		}

		[TestMethod]
		public void AssignEligibleResources_PoolNodesWithCapability_AssignsADistinctEligibleResourcePerNode()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var capability = new Capability { Name = $"{prefix}_Resolution" };
			capability.SetDiscretes(new[] { "4K", "HD" });
			objectCreator.CreateCapability(capability);

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var firstMatchingResource = CreateCompleteResource($"{prefix}_Resource_4K_1", pool, resource => resource.AddCapability(CreateCapabilitySettings(capability, "4K")));
			var secondMatchingResource = CreateCompleteResource($"{prefix}_Resource_4K_2", pool, resource => resource.AddCapability(CreateCapabilitySettings(capability, "4K")));
			var otherValueResource = CreateCompleteResource($"{prefix}_Resource_HD", pool, resource => resource.AddCapability(CreateCapabilitySettings(capability, "HD")));

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime.AddHours(1),
				End = currentTime.AddHours(2),
				PreRollStart = currentTime.AddHours(1),
				PostRollEnd = currentTime.AddHours(2),
			};

			var firstPoolNode = new JobResourcePoolNode(pool);
			firstPoolNode.OrchestrationSettings.AddCapability(new CapabilitySetting(capability.Id) { Value = "4K" });

			var secondPoolNode = new JobResourcePoolNode(pool);
			secondPoolNode.OrchestrationSettings.AddCapability(new CapabilitySetting(capability.Id) { Value = "4K" });

			job.NodeGraph
				.Add(firstPoolNode)
				.Add(secondPoolNode);

			job.AssignEligibleResources(TestContext.Api);

			Assert.AreEqual(0, job.NodeGraph.Nodes.OfType<JobResourcePoolNode>().Count(), "Expected every resource pool node to be replaced by a resource node.");

			var assignedResourceIds = job.NodeGraph.Nodes.OfType<JobResourceNode>().Select(x => x.ResourceId).ToList();

			Assert.AreEqual(2, assignedResourceIds.Count, "Expected a resource node for every resource pool node.");
			Assert.AreEqual(2, assignedResourceIds.Distinct().Count(), "Expected a distinct resource to be assigned to every node.");
			CollectionAssert.DoesNotContain(assignedResourceIds, otherValueResource.Id, "Expected the resource that does not have the requested capability value not to be assigned.");

			foreach (var assignedResourceId in assignedResourceIds)
			{
				Assert.IsTrue(
					assignedResourceId == firstMatchingResource.Id || assignedResourceId == secondMatchingResource.Id,
					"Expected only resources that have the requested capability value to be assigned.");
			}
		}

		[TestMethod]
		public void AssignEligibleResources_WithAlreadyAssignedResources_AssignsOnlyUnusedResources()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var firstResource = CreateCompleteResource($"{prefix}_Resource_1", pool, null);
			var secondResource = CreateCompleteResource($"{prefix}_Resource_2", pool, null);
			var thirdResource = CreateCompleteResource($"{prefix}_Resource_3", pool, null);
			var fourthResource = CreateCompleteResource($"{prefix}_Resource_4", pool, null);

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime.AddHours(1),
				End = currentTime.AddHours(2),
				PreRollStart = currentTime.AddHours(1),
				PostRollEnd = currentTime.AddHours(2),
			};

			job.NodeGraph
				.Add(new JobResourceNode(pool, firstResource))
				.Add(new JobResourceNode(pool, secondResource))
				.Add(new JobResourcePoolNode(pool))
				.Add(new JobResourcePoolNode(pool));

			job.AssignEligibleResources(TestContext.Api);

			Assert.AreEqual(0, job.NodeGraph.Nodes.OfType<JobResourcePoolNode>().Count(), "Expected every resource pool node to be replaced by a resource node.");

			var assignedResourceIds = job.NodeGraph.Nodes.OfType<JobResourceNode>().Select(x => x.ResourceId).ToList();

			Assert.AreEqual(4, assignedResourceIds.Count, "Expected a resource node for every assigned resource and resource pool node.");
			Assert.AreEqual(4, assignedResourceIds.Distinct().Count(), "Expected a distinct resource to be assigned to every node.");
			CollectionAssert.Contains(assignedResourceIds, firstResource.Id, "Expected the already assigned resource to be kept.");
			CollectionAssert.Contains(assignedResourceIds, secondResource.Id, "Expected the already assigned resource to be kept.");
			CollectionAssert.Contains(assignedResourceIds, thirdResource.Id, "Expected the unused resource to be assigned.");
			CollectionAssert.Contains(assignedResourceIds, fourthResource.Id, "Expected the unused resource to be assigned.");
		}

		[TestMethod]
		public void AssignEligibleResources_WithMorePoolNodesThanAvailableResources_KeepsUnresolvedPoolNodes()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var firstResource = CreateCompleteResource($"{prefix}_Resource_1", pool, null);
			var secondResource = CreateCompleteResource($"{prefix}_Resource_2", pool, null);
			var thirdResource = CreateCompleteResource($"{prefix}_Resource_3", pool, null);
			var fourthResource = CreateCompleteResource($"{prefix}_Resource_4", pool, null);

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime.AddHours(1),
				End = currentTime.AddHours(2),
				PreRollStart = currentTime.AddHours(1),
				PostRollEnd = currentTime.AddHours(2),
			};

			job.NodeGraph
				.Add(new JobResourceNode(pool, firstResource))
				.Add(new JobResourceNode(pool, secondResource))
				.Add(new JobResourceNode(pool, thirdResource))
				.Add(new JobResourcePoolNode(pool))
				.Add(new JobResourcePoolNode(pool))
				.Add(new JobResourcePoolNode(pool));

			job.AssignEligibleResources(TestContext.Api);

			Assert.AreEqual(2, job.NodeGraph.Nodes.OfType<JobResourcePoolNode>().Count(), "Expected unresolved resource pool nodes to be kept when no resource is eligible.");

			var assignedResourceIds = job.NodeGraph.Nodes.OfType<JobResourceNode>().Select(x => x.ResourceId).ToList();

			Assert.AreEqual(4, assignedResourceIds.Count, "Expected one resource pool node to be replaced by the remaining eligible resource.");
			Assert.AreEqual(4, assignedResourceIds.Distinct().Count(), "Expected a distinct resource to be assigned to every resource node.");
			CollectionAssert.Contains(assignedResourceIds, firstResource.Id, "Expected the already assigned resource to be kept.");
			CollectionAssert.Contains(assignedResourceIds, secondResource.Id, "Expected the already assigned resource to be kept.");
			CollectionAssert.Contains(assignedResourceIds, thirdResource.Id, "Expected the already assigned resource to be kept.");
			CollectionAssert.Contains(assignedResourceIds, fourthResource.Id, "Expected the unused resource to be assigned.");
		}

		[TestMethod]
		public void AssignEligibleResources_WithAllResourcesAlreadyAssigned_KeepsPoolNodes()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var firstResource = CreateCompleteResource($"{prefix}_Resource_1", pool, null);
			var secondResource = CreateCompleteResource($"{prefix}_Resource_2", pool, null);
			var thirdResource = CreateCompleteResource($"{prefix}_Resource_3", pool, null);
			var fourthResource = CreateCompleteResource($"{prefix}_Resource_4", pool, null);

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime.AddHours(1),
				End = currentTime.AddHours(2),
				PreRollStart = currentTime.AddHours(1),
				PostRollEnd = currentTime.AddHours(2),
			};

			job.NodeGraph
				.Add(new JobResourceNode(pool, firstResource))
				.Add(new JobResourceNode(pool, secondResource))
				.Add(new JobResourceNode(pool, thirdResource))
				.Add(new JobResourceNode(pool, fourthResource))
				.Add(new JobResourcePoolNode(pool))
				.Add(new JobResourcePoolNode(pool));

			job.AssignEligibleResources(TestContext.Api);

			Assert.AreEqual(2, job.NodeGraph.Nodes.OfType<JobResourcePoolNode>().Count(), "Expected unresolved resource pool nodes to be kept when no resource is eligible.");

			var assignedResourceIds = job.NodeGraph.Nodes.OfType<JobResourceNode>().Select(x => x.ResourceId).ToList();

			Assert.AreEqual(4, assignedResourceIds.Count, "Expected no additional resource nodes to be created when no resource is eligible.");
			Assert.AreEqual(4, assignedResourceIds.Distinct().Count(), "Expected a distinct resource to be assigned to every resource node.");
			CollectionAssert.Contains(assignedResourceIds, firstResource.Id, "Expected the already assigned resource to be kept.");
			CollectionAssert.Contains(assignedResourceIds, secondResource.Id, "Expected the already assigned resource to be kept.");
			CollectionAssert.Contains(assignedResourceIds, thirdResource.Id, "Expected the already assigned resource to be kept.");
			CollectionAssert.Contains(assignedResourceIds, fourthResource.Id, "Expected the already assigned resource to be kept.");
		}

		[TestMethod]
		public void AssignEligibleResources_WithoutEligibleResource_KeepsTheResourcePoolNode()
		{
			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var capability = new Capability { Name = $"{prefix}_Resolution" };
			capability.SetDiscretes(new[] { "4K", "HD" });
			objectCreator.CreateCapability(capability);

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			CreateCompleteResource($"{prefix}_Resource_HD", pool, resource => resource.AddCapability(CreateCapabilitySettings(capability, "HD")));

			var job = new Job
			{
				Name = $"{prefix}_Job",
				Start = currentTime.AddHours(1),
				End = currentTime.AddHours(2),
				PreRollStart = currentTime.AddHours(1),
				PostRollEnd = currentTime.AddHours(2),
			};

			var poolNode = new JobResourcePoolNode(pool);
			poolNode.OrchestrationSettings.AddCapability(new CapabilitySetting(capability.Id) { Value = "4K" });
			job.NodeGraph.Add(poolNode);

			job.AssignEligibleResources(TestContext.Api);

			Assert.AreEqual(0, job.NodeGraph.Nodes.OfType<JobResourceNode>().Count(), "Expected no resource node to be created when no resource is eligible.");
			Assert.AreEqual(1, job.NodeGraph.Nodes.OfType<JobResourcePoolNode>().Count(), "Expected the resource pool node to be kept when no resource is eligible.");
		}

		private static CapabilitySettings CreateCapabilitySettings(Capability capability, string value)
		{
			var capabilitySettings = new CapabilitySettings(capability.Id);
			capabilitySettings.AddDiscrete(value);
			return capabilitySettings;
		}

		private static List<Guid> GetEligibleResourceIds(
			DateTimeOffset start,
			DateTimeOffset end,
			IReadOnlyCollection<CapabilitySetting> capabilitySettings,
			IReadOnlyCollection<CapacitySetting> capacitySettings,
			ResourcePool pool)
		{
			var eligibleResources = TestContext.Api.Resources.GetEligibleResources(new EligibleResourcesContext(start, end)
			{
				CapabilitySettings = capabilitySettings,
				CapacitySettings = capacitySettings,
				Filter = ResourceExposers.ResourcePoolIds.Contains(pool.Id),
			});

			return eligibleResources.EligibleResources.Select(x => x.Resource.Id).ToList();
		}

		private Resource CreateCompleteResource(string name, ResourcePool pool, Action<Resource> configure)
		{
			var resource = new UnmanagedResource { Name = name }.AssignToPool(pool);
			configure?.Invoke(resource);

			resource = objectCreator.CreateResource(resource);

			return TestContext.Api.Resources.Complete(resource);
		}
	}
}
