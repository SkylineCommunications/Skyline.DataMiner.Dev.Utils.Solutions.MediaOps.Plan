namespace RT_MediaOps.Plan.RST.Synchronization
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	using CoreResource = Skyline.DataMiner.Net.Messages.Resource;
	using CoreResourcePool = Skyline.DataMiner.Net.Messages.ResourcePool;

	[TestClass]
	[TestCategory("IntegrationTest")]
	public sealed class SynchronizationTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public SynchronizationTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void CompletedItemsAreInSync()
		{
			var (pool, _) = CreateCompletedPoolWithResource(Guid.NewGuid());

			var report = TestContext.Api.ResourcePools.GetOutOfSyncItems([pool]);

			Assert.IsTrue(report.IsSynchronized, "A freshly completed resource pool and resource should be in sync.");
			Assert.AreEqual(0, report.ResourcePools.Count);
			Assert.AreEqual(0, report.Resources.Count);
		}

		[TestMethod]
		public void ResourceNameDriftIsDetectedAndSynchronized()
		{
			var prefix = Guid.NewGuid();
			var (pool, resource) = CreateCompletedPoolWithResource(prefix);

			var driftedName = $"{prefix}_Drifted";
			DriftCoreResource(resource, x => x.Name = driftedName);

			var report = TestContext.Api.ResourcePools.GetOutOfSyncItems([pool]);

			var item = report.Resources.Single();
			Assert.AreEqual(resource.Id, item.Id);
			Assert.IsTrue(item.CoreObjectExists);
			Assert.IsTrue(item.CanSynchronize);

			var difference = item.Differences.OfType<NameDifference>().Single();
			Assert.AreEqual(resource.Name, difference.DomValue);
			Assert.AreEqual(driftedName, difference.CoreValue);

			// Detecting differences may not change anything in CORE.
			Assert.AreEqual(driftedName, GetCoreResource(resource).Name);

			var result = TestContext.Api.ResourcePools.Synchronize([item]);

			Assert.IsFalse(result.HasFailures);
			CollectionAssert.AreEquivalent(new[] { resource.Id }, result.SynchronizedResourceIds.ToArray());
			Assert.AreEqual(resource.Name, GetCoreResource(resource).Name);
			Assert.IsTrue(TestContext.Api.ResourcePools.GetOutOfSyncItems([pool]).IsSynchronized);
		}

		[TestMethod]
		public void MaxConcurrencyDriftIsDetectedAndSynchronized()
		{
			var (pool, resource) = CreateCompletedPoolWithResource(Guid.NewGuid());

			DriftCoreResource(resource, x => x.MaxConcurrency = 7);

			var item = TestContext.Api.ResourcePools.GetOutOfSyncItems([pool]).Resources.Single();
			var difference = item.Differences.OfType<MaxConcurrencyDifference>().Single();
			Assert.AreEqual(1, difference.DomValue);
			Assert.AreEqual(7, difference.CoreValue);

			TestContext.Api.ResourcePools.Synchronize([item]);

			Assert.AreEqual(1, GetCoreResource(resource).MaxConcurrency);
		}

		[TestMethod]
		public void CapacityValueDriftIsDetectedAndSynchronized()
		{
			var prefix = Guid.NewGuid();

			var capacity = new NumberCapacity
			{
				Name = $"{prefix}_Capacity",
				RangeMin = 0,
				RangeMax = 200,
				StepSize = 1,
			};
			objectCreator.CreateCapacity(capacity);

			var pool = CreateCompletedPool(prefix);

			var unmanagedResource = new UnmanagedResource
			{
				Name = $"{prefix}_Resource",
			};
			unmanagedResource.AddCapacity(new NumberCapacitySetting(capacity.Id) { Value = 100 });

			var resource = CompleteAndAssignToPool(unmanagedResource, pool);

			DriftCoreResource(resource, x => x.Capacities.Single().Value.MaxDecimalQuantity = 50);

			var item = TestContext.Api.ResourcePools.GetOutOfSyncItems([pool]).Resources.Single();
			var difference = item.Differences.OfType<CapacityDifference>().Single();
			Assert.AreEqual(SynchronizationDifferenceKind.ValueMismatch, difference.Kind);
			Assert.AreEqual(100m, difference.DomMaxValue);
			Assert.AreEqual(50m, difference.CoreMaxValue);

			TestContext.Api.ResourcePools.Synchronize([item]);

			Assert.AreEqual(100m, GetCoreResource(resource).Capacities.Single().Value.MaxDecimalQuantity);
		}

		[TestMethod]
		public void ResourcePoolNameDriftIsDetectedAndSynchronized()
		{
			var prefix = Guid.NewGuid();
			var pool = CreateCompletedPool(prefix);

			var corePool = TestContext.ResourceManagerHelper.GetResourcePool(pool.CoreResourcePoolId);
			corePool.Name = $"{prefix}_DriftedPool";
			TestContext.ResourceManagerHelper.AddOrUpdateResourcePools(corePool);

			var item = TestContext.Api.ResourcePools.GetOutOfSyncItems([pool]).ResourcePools.Single();
			Assert.AreEqual(pool.Id, item.Id);
			Assert.IsTrue(item.CoreObjectExists);

			var difference = item.Differences.OfType<NameDifference>().Single();
			Assert.AreEqual(pool.Name, difference.DomValue);
			Assert.AreEqual($"{prefix}_DriftedPool", difference.CoreValue);

			var result = TestContext.Api.ResourcePools.Synchronize([item]);

			Assert.IsFalse(result.HasFailures);
			CollectionAssert.AreEquivalent(new[] { pool.Id }, result.SynchronizedResourcePoolIds.ToArray());
			Assert.AreEqual(pool.Name, TestContext.ResourceManagerHelper.GetResourcePool(pool.CoreResourcePoolId).Name);
		}

		[TestMethod]
		public void MissingCoreResourcePoolIsDetectedAndRecreated()
		{
			var prefix = Guid.NewGuid();
			var pool = CreateCompletedPool(prefix);
			var originalCoreResourcePoolId = pool.CoreResourcePoolId;

			TestContext.ResourceManagerHelper.RemoveResourcePools([new CoreResourcePool(originalCoreResourcePoolId)]);

			var item = TestContext.Api.ResourcePools.GetOutOfSyncItems([pool]).ResourcePools.Single();
			Assert.IsFalse(item.CoreObjectExists);
			Assert.AreEqual(1, item.Differences.OfType<MissingCoreObjectDifference>().Count());
			Assert.IsTrue(item.CanSynchronize);

			var result = TestContext.Api.ResourcePools.Synchronize([item]);
			Assert.IsFalse(result.HasFailures);

			var recreated = TestContext.Api.ResourcePools.Read(pool.Id);
			Assert.AreEqual(originalCoreResourcePoolId, recreated.CoreResourcePoolId, "The CORE resource pool must be recreated under its original ID.");
			Assert.AreEqual(pool.Name, TestContext.ResourceManagerHelper.GetResourcePool(originalCoreResourcePoolId).Name);
		}

		[TestMethod]
		public void MissingCoreResourceIsSelectableInFullReport()
		{
			var (_, resource) = CreateCompletedPoolWithResource(Guid.NewGuid());

			TestContext.ResourceManagerHelper.RemoveResources([new CoreResource(resource.CoreResourceId)]);

			// The script uses the unscoped overload, so it has to behave the same as the scoped one.
			var item = TestContext.Api.ResourcePools.GetOutOfSyncItems().Resources.Single(x => x.Id == resource.Id);

			Assert.IsFalse(item.CoreObjectExists);
			Assert.IsTrue(item.CanSynchronize, String.Join(" | ", item.Blockers.Select(x => x.ErrorMessage)));
		}

		[TestMethod]
		public void MissingCoreResourceIsDetectedAndRecreated()
		{
			var (pool, resource) = CreateCompletedPoolWithResource(Guid.NewGuid());
			var originalCoreResourceId = resource.CoreResourceId;

			TestContext.ResourceManagerHelper.RemoveResources([new CoreResource(originalCoreResourceId)]);

			var item = TestContext.Api.ResourcePools.GetOutOfSyncItems([pool]).Resources.Single();
			Assert.IsFalse(item.CoreObjectExists);
			Assert.AreEqual(1, item.Differences.OfType<MissingCoreObjectDifference>().Count());
			Assert.IsTrue(item.CanSynchronize, "A dangling CORE link is repairable by synchronizing.");

			var result = TestContext.Api.ResourcePools.Synchronize([item]);
			Assert.IsFalse(result.HasFailures);

			var recreated = TestContext.Api.Resources.Read(resource.Id);
			Assert.AreEqual(originalCoreResourceId, recreated.CoreResourceId, "The CORE resource must be recreated under its original ID.");
			Assert.AreEqual(resource.Name, GetCoreResource(recreated).Name);
			Assert.IsTrue(TestContext.Api.ResourcePools.GetOutOfSyncItems([pool]).IsSynchronized);
		}

		[TestMethod]
		public void OnlySelectedItemsAreSynchronized()
		{
			var prefix = Guid.NewGuid();
			var pool = CreateCompletedPool(prefix);

			var selected = CompleteAndAssignToPool(new UnmanagedResource { Name = $"{prefix}_Selected" }, pool);
			var unselected = CompleteAndAssignToPool(new UnmanagedResource { Name = $"{prefix}_Unselected" }, pool);

			DriftCoreResource(selected, x => x.MaxConcurrency = 5);
			DriftCoreResource(unselected, x => x.MaxConcurrency = 5);

			var report = TestContext.Api.ResourcePools.GetOutOfSyncItems([pool]);
			Assert.AreEqual(2, report.Resources.Count);

			var selectedItem = report.Resources.Single(x => x.Id == selected.Id);
			var result = TestContext.Api.ResourcePools.Synchronize([selectedItem]);

			Assert.IsFalse(result.HasFailures);
			CollectionAssert.AreEquivalent(new[] { selected.Id }, result.SynchronizedResourceIds.ToArray());

			Assert.AreEqual(1, GetCoreResource(selected).MaxConcurrency);
			Assert.AreEqual(5, GetCoreResource(unselected).MaxConcurrency, "The unselected resource should still be out of sync.");
		}

		[TestMethod]
		public void NameAlreadyUsedInCoreIsReportedAsBlocker()
		{
			var prefix = Guid.NewGuid();
			var (pool, resource) = CreateCompletedPoolWithResource(prefix);

			// Drift the name so the resource needs name validation, then hand its DOM name to another CORE resource.
			DriftCoreResource(resource, x => x.Name = $"{prefix}_Drifted");

			objectCreator.CreateCoreResource(new CoreResource(Guid.NewGuid())
			{
				Name = resource.Name,
				MaxConcurrency = 1,
			});

			var item = TestContext.Api.ResourcePools.GetOutOfSyncItems([pool]).Resources.Single();
			Assert.IsTrue(item.CoreObjectExists);
			Assert.IsFalse(item.CanSynchronize);
			Assert.IsTrue(item.Blockers.OfType<ResourceNameExistsError>().Any());

			var result = TestContext.Api.ResourcePools.Synchronize([item]);

			Assert.IsTrue(result.HasFailures);
			Assert.IsTrue(result.Failures.ContainsKey(resource.Id));
			Assert.AreEqual(0, result.SynchronizedResourceIds.Count);
		}

		[TestMethod]
		public void BlockedItemDoesNotPreventOtherItemsFromBeingSynchronized()
		{
			var prefix = Guid.NewGuid();
			var pool = CreateCompletedPool(prefix);

			var blocked = CompleteAndAssignToPool(new UnmanagedResource { Name = $"{prefix}_Blocked" }, pool);
			var healthy = CompleteAndAssignToPool(new UnmanagedResource { Name = $"{prefix}_Healthy" }, pool);

			DriftCoreResource(blocked, x => x.Name = $"{prefix}_BlockedDrifted");
			objectCreator.CreateCoreResource(new CoreResource(Guid.NewGuid())
			{
				Name = blocked.Name,
				MaxConcurrency = 1,
			});

			DriftCoreResource(healthy, x => x.MaxConcurrency = 9);

			var report = TestContext.Api.ResourcePools.GetOutOfSyncItems([pool]);
			var result = TestContext.Api.ResourcePools.Synchronize(report.GetAllItems());

			Assert.IsTrue(result.Failures.ContainsKey(blocked.Id));
			CollectionAssert.AreEquivalent(new[] { healthy.Id }, result.SynchronizedResourceIds.ToArray());
			Assert.AreEqual(1, GetCoreResource(healthy).MaxConcurrency);
		}

		[TestMethod]
		public void ResourcePropertiesAreNotPushedToCore()
		{
			var prefix = Guid.NewGuid();

			var property = new ResourceProperty
			{
				Name = $"{prefix}_Property",
			};
			objectCreator.CreateResourceProperty(property);

			var pool = CreateCompletedPool(prefix);

			var unmanagedResource = new UnmanagedResource
			{
				Name = $"{prefix}_Resource",
			};
			unmanagedResource.AddProperty(new ResourcePropertySettings(property) { Value = "Test" });

			var resource = CompleteAndAssignToPool(unmanagedResource, pool);

			Assert.AreEqual(0, GetCoreResource(resource).Properties.Count, "DOM resource properties must not be pushed to CORE.");

			var report = TestContext.Api.ResourcePools.GetOutOfSyncItems([pool]);
			Assert.IsTrue(report.IsSynchronized, "A resource property may never make a resource out of sync.");
		}

		[TestMethod]
		public void DetectionDoesNotChangeCore()
		{
			var prefix = Guid.NewGuid();

			var capacity = new NumberCapacity
			{
				Name = $"{prefix}_Capacity",
				RangeMin = 0,
				RangeMax = 200,
				StepSize = 1,
			};
			objectCreator.CreateCapacity(capacity);

			var capability = new Capability
			{
				Name = $"{prefix}_Capability",
			}
			.SetDiscretes(new[] { "A", "B" });
			objectCreator.CreateCapability(capability);

			var pool = CreateCompletedPool(prefix);

			var unmanagedResource = new UnmanagedResource
			{
				Name = $"{prefix}_Resource",
			};
			unmanagedResource.AddCapability(new CapabilitySettings(capability.Id).SetDiscretes(new[] { "A" }));
			unmanagedResource.AddCapacity(new NumberCapacitySetting(capacity.Id) { Value = 100 });

			var resource = CompleteAndAssignToPool(unmanagedResource, pool);

			// A real agent rejects a membership of a pool that does not exist, so drift towards a real one.
			var driftedPool = CreateCompletedPool(prefix, "DriftedPool");

			DriftCoreResource(resource, x =>
			{
				x.Name = $"{prefix}_Drifted";
				x.MaxConcurrency = 4;
				x.Capacities.Single().Value.MaxDecimalQuantity = 50;
				x.Capabilities.Single(c => c.CapabilityProfileID == capability.Id).Value.Discreets = ["B"];
				x.PoolGUIDs.Clear();
				x.PoolGUIDs.Add(driftedPool.CoreResourcePoolId);
			});

			TestContext.Api.ResourcePools.GetOutOfSyncItems([pool]);
			var secondReport = TestContext.Api.ResourcePools.GetOutOfSyncItems([pool]);

			var coreResource = GetCoreResource(resource);
			Assert.AreEqual($"{prefix}_Drifted", coreResource.Name);
			Assert.AreEqual(4, coreResource.MaxConcurrency);
			Assert.AreEqual(50m, coreResource.Capacities.Single().Value.MaxDecimalQuantity);
			CollectionAssert.AreEquivalent(new[] { "B" }, coreResource.Capabilities.Single(c => c.CapabilityProfileID == capability.Id).Value.Discreets.ToArray());
			CollectionAssert.AreEquivalent(new[] { driftedPool.CoreResourcePoolId }, coreResource.PoolGUIDs.ToArray());
			Assert.AreEqual(1, secondReport.Resources.Count, "Repeated detection should keep reporting the same item.");
		}

		private static CoreResource GetCoreResource(Resource resource)
		{
			return TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);
		}

		private static void DriftCoreResource(Resource resource, Action<CoreResource> drift)
		{
			var coreResource = GetCoreResource(resource);
			drift(coreResource);
			TestContext.ResourceManagerHelper.AddOrUpdateResources(coreResource);

			// A real agent rejects an invalid update without throwing, which would otherwise surface as a confusing assert further down.
			var storedCoreResource = GetCoreResource(resource);
			Assert.AreEqual(coreResource.Name, storedCoreResource.Name, "Precondition: the CORE drift was not applied.");
			Assert.AreEqual(coreResource.MaxConcurrency, storedCoreResource.MaxConcurrency, "Precondition: the CORE drift was not applied.");
		}

		private ResourcePool CreateCompletedPool(Guid prefix, string suffix = "Pool")
		{
			var pool = new ResourcePool(Guid.NewGuid())
			{
				Name = $"{prefix}_{suffix}",
			};

			objectCreator.CreateResourcePool(pool);

			return TestContext.Api.ResourcePools.Complete(pool.Id);
		}

		private Resource CompleteAndAssignToPool(UnmanagedResource unmanagedResource, ResourcePool pool)
		{
			objectCreator.CreateResource(unmanagedResource);

			var resource = TestContext.Api.Resources.Complete(unmanagedResource.Id);
			TestContext.Api.ResourcePools.AssignResourcesToPool(pool, [resource]);

			return TestContext.Api.Resources.Read(unmanagedResource.Id);
		}

		private (ResourcePool Pool, Resource Resource) CreateCompletedPoolWithResource(Guid prefix)
		{
			var pool = CreateCompletedPool(prefix);
			var resource = CompleteAndAssignToPool(new UnmanagedResource { Name = $"{prefix}_Resource" }, pool);

			return (pool, resource);
		}
	}
}
