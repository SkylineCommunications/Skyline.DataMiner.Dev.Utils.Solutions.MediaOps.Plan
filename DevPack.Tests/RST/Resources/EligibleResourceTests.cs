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
	/// Tests for <see cref="IResourcesRepository.GetEligibleResources(DateTimeOffset, DateTimeOffset, IReadOnlyCollection{CapabilitySetting}, IReadOnlyCollection{CapacitySetting})"/>
	/// and <see cref="Job.AssignEligibleResources(IMediaOpsPlanApi)"/>.
	/// </summary>
	/// <remarks>
	/// Resource eligibility is resolved by the DataMiner Resource Manager, which the simulated connection does not
	/// implement, so every test is skipped when the tests do not run against a real DataMiner Agent.
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
			SkipWhenNotRunningAgainstRealDma();

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
			SkipWhenNotRunningAgainstRealDma();

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
			SkipWhenNotRunningAgainstRealDma();

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
		public void GetEligibleResources_WithFilter_OnlyConsidersResourcesMatchingTheFilter()
		{
			SkipWhenNotRunningAgainstRealDma();

			var prefix = Guid.NewGuid();
			var currentTime = DateTime.UtcNow.RoundToNextSecond();

			var pool = objectCreator.CreateResourcePool(new ResourcePool { Name = $"{prefix}_Pool" });
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var firstResource = CreateCompleteResource($"{prefix}_Resource_1", pool, null);
			var secondResource = CreateCompleteResource($"{prefix}_Resource_2", pool, null);

			var eligible = TestContext.Api.Resources.GetEligibleResources(
				currentTime.AddHours(1),
				currentTime.AddHours(2),
				Array.Empty<CapabilitySetting>(),
				Array.Empty<CapacitySetting>(),
				ResourceExposers.Id.Equal(firstResource.Id));

			var eligibleIds = eligible.Select(x => x.Id).ToList();

			CollectionAssert.Contains(eligibleIds, firstResource.Id, "Expected the resource that matches the filter to be eligible.");
			CollectionAssert.DoesNotContain(eligibleIds, secondResource.Id, "Expected a resource that does not match the filter not to be returned.");
		}

		[TestMethod]
		public void AssignEligibleResources_PoolNodesWithCapability_AssignsADistinctEligibleResourcePerNode()
		{
			SkipWhenNotRunningAgainstRealDma();

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
		public void AssignEligibleResources_WithoutEligibleResource_KeepsTheResourcePoolNode()
		{
			SkipWhenNotRunningAgainstRealDma();

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

		private static void SkipWhenNotRunningAgainstRealDma()
		{
			if (!TestContext.UseRealDma)
			{
				Assert.Inconclusive("Resource eligibility is resolved by the DataMiner Resource Manager, which the simulated connection does not implement.");
			}
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
			var eligibleResources = TestContext.Api.Resources.GetEligibleResources(
				start,
				end,
				capabilitySettings,
				capacitySettings,
				ResourceExposers.ResourcePoolIds.Contains(pool.Id));

			return eligibleResources.Select(x => x.Id).ToList();
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
