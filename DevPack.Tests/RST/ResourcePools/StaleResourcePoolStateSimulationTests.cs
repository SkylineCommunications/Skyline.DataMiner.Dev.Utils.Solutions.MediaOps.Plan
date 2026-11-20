namespace RT_MediaOps.Plan.RST.ResourcePools
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.ResourceManager.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Simulation;

	using PlanResource = Skyline.DataMiner.Solutions.MediaOps.Plan.API.Resource;
	using ResourcePool = Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool;

	/// <summary>
	/// A resource pool action can be called with a pool that was read before another user changed its state. These tests
	/// change the stored state through a second API instance, which has its own in-memory lock manager, and then call the
	/// handler directly with the stale pool, because the repository would re-read the pool by ID.
	/// </summary>
	[TestClass]
	public sealed class StaleResourcePoolStateSimulationTests
	{
		private sealed class Context
		{
			public Context()
			{
				Dms = MediaOpsPlanSimulation.Create();
				var connection = Dms.CreateConnection();
				Api = connection.GetMediaOpsPlanApi();
				ResourceManagerHelper = new ResourceManagerHelper(connection.HandleSingleResponseMessage);
			}

			public SimulatedDms Dms { get; }

			public IMediaOpsPlanApi Api { get; }

			public MediaOpsPlanApi PlanApi => (MediaOpsPlanApi)Api;

			public ResourceManagerHelper ResourceManagerHelper { get; }

			public IMediaOpsPlanApi OtherApi => Dms.CreateConnection().GetMediaOpsPlanApi();

			public ResourcePool CreateDraftPool()
			{
				return Api.ResourcePools.Create(new ResourcePool { Name = $"{Guid.NewGuid()}_Pool" });
			}
		}

		private static void AssertInvalidState(DomInstanceBulkOperationResult<Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.ResourcepoolInstance> result, Guid poolId)
		{
			Assert.IsTrue(
				result.TraceDataPerItem[poolId].ErrorData.OfType<ResourcePoolInvalidStateError>().Any(),
				"Expected a ResourcePoolInvalidStateError for the pool whose stored state changed.");
		}

		[TestMethod]
		public void Update_DraftPoolCompletedAfterRead_FailsWithoutChangingPool()
		{
			var context = new Context();
			var stalePool = context.CreateDraftPool();
			var originalName = stalePool.Name;

			// The update was validated, and its CORE changes were selected, for a Draft pool.
			context.OtherApi.ResourcePools.Complete(stalePool.Id);

			stalePool.Name = $"{originalName}_Renamed";
			Assert.IsFalse(DomResourcePoolHandler.TryCreateOrUpdate(context.PlanApi, [stalePool], out var result), "Expected the update of a pool whose state changed to fail.");
			AssertInvalidState(result, stalePool.Id);
			Assert.AreEqual(originalName, context.Api.ResourcePools.Read(stalePool.Id).Name);
		}

		[TestMethod]
		public void Update_CompletePoolDeprecatedAfterRead_FailsWithoutChangingPool()
		{
			var context = new Context();
			var stalePool = context.Api.ResourcePools.Complete(context.CreateDraftPool());
			var originalName = stalePool.Name;

			context.OtherApi.ResourcePools.Deprecate(stalePool.Id);

			stalePool.Name = $"{originalName}_Renamed";
			Assert.IsFalse(DomResourcePoolHandler.TryCreateOrUpdate(context.PlanApi, [stalePool], out var result), "Expected the update of a Deprecated pool to fail.");
			AssertInvalidState(result, stalePool.Id);
			Assert.AreEqual(originalName, context.Api.ResourcePools.Read(stalePool.Id).Name);
		}

		[TestMethod]
		public void Delete_DraftPoolCompletedAfterRead_FailsAndKeepsPoolCorePoolAndResources()
		{
			var context = new Context();
			var stalePool = context.CreateDraftPool();
			var resource = context.Api.Resources.Create(new UnmanagedResource { Name = $"{Guid.NewGuid()}_Resource" }.AssignToPool(stalePool));

			var completedPool = context.OtherApi.ResourcePools.Complete(stalePool.Id);

			Assert.IsFalse(
				DomResourcePoolHandler.TryDelete(context.PlanApi, [stalePool], out var result, new ResourcePoolDeleteOptions { DeleteDraftResources = true }),
				"Expected the delete of a Complete pool to fail.");
			AssertInvalidState(result, stalePool.Id);
			Assert.AreEqual(ResourcePoolState.Complete, context.Api.ResourcePools.Read(stalePool.Id).State);
			Assert.IsNotNull(context.ResourceManagerHelper.GetResourcePool(completedPool.CoreResourcePoolId), "Expected the CORE resource pool to be kept.");

			var storedResource = context.Api.Resources.Read(resource.Id);
			Assert.IsNotNull(storedResource, "Expected the resource of the pool not to be deleted.");
			Assert.IsTrue(storedResource.ResourcePoolIds.Contains(stalePool.Id), "Expected the resource to stay assigned to the pool.");
		}

		[TestMethod]
		public void Delete_PoolAlreadyDeletedAfterRead_Succeeds()
		{
			var context = new Context();
			var stalePool = context.CreateDraftPool();

			context.OtherApi.ResourcePools.Delete(stalePool.Id, new ResourcePoolDeleteOptions());

			Assert.IsTrue(DomResourcePoolHandler.TryDelete(context.PlanApi, [stalePool], out _), "Expected the delete of an already deleted pool to succeed.");
		}

		[TestMethod]
		public void Delete_ResourceCannotBeUnassigned_FailsAndKeepsPool()
		{
			var context = new Context();
			var pool = context.CreateDraftPool();
			var resource = context.Api.Resources.Create(new UnmanagedResource { Name = $"{Guid.NewGuid()}_Resource" }.AssignToPool(pool));

			// The resource is locked by another action of the same API, so it cannot be unassigned from the pool.
			DomInstanceBulkOperationResult<Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.ResourcepoolInstance> result = null;
			context.PlanApi.LockManager.LockAndExecute<PlanResource>([resource], _ => DomResourcePoolHandler.TryDelete(context.PlanApi, [pool], out result));

			Assert.IsTrue(result.UnsuccessfulIds.Contains(pool.Id), "Expected the delete of a pool that is still assigned to a resource to fail.");
			Assert.IsNotNull(context.Api.ResourcePools.Read(pool.Id), "Expected the pool not to be deleted.");
			Assert.IsTrue(context.Api.Resources.Read(resource.Id).ResourcePoolIds.Contains(pool.Id), "Expected the resource to stay assigned to the pool.");
		}

		[TestMethod]
		public void Complete_DraftPoolCompletedAfterRead_SucceedsWithoutSecondCorePool()
		{
			var context = new Context();
			var stalePool = context.CreateDraftPool();

			var completedPool = context.OtherApi.ResourcePools.Complete(stalePool.Id);

			Assert.IsTrue(DomResourcePoolHandler.TryComplete(context.PlanApi, [stalePool], out _), "Expected completing an already Complete pool to succeed.");
			Assert.AreEqual(completedPool.CoreResourcePoolId, context.Api.ResourcePools.Read(stalePool.Id).CoreResourcePoolId, "Expected the link to the CORE resource pool to be kept.");
		}

		[TestMethod]
		public void Complete_DraftPoolRenamedAfterRead_KeepsRename()
		{
			var context = new Context();
			var stalePool = context.CreateDraftPool();

			var otherApi = context.OtherApi;
			var renamedPool = otherApi.ResourcePools.Read(stalePool.Id);
			renamedPool.Name = $"{stalePool.Name}_Renamed";
			otherApi.ResourcePools.Update(renamedPool);

			Assert.IsTrue(DomResourcePoolHandler.TryComplete(context.PlanApi, [stalePool], out _), "Expected the complete to succeed.");

			var storedPool = context.Api.ResourcePools.Read(stalePool.Id);
			Assert.AreEqual(ResourcePoolState.Complete, storedPool.State);
			Assert.AreEqual(renamedPool.Name, storedPool.Name, "Expected the stale pool not to overwrite the rename.");
		}

		[TestMethod]
		public void Deprecate_CompletePoolDeprecatedAfterRead_Succeeds()
		{
			var context = new Context();
			var stalePool = context.Api.ResourcePools.Complete(context.CreateDraftPool());

			context.OtherApi.ResourcePools.Deprecate(stalePool.Id);

			Assert.IsTrue(DomResourcePoolHandler.TryDeprecate(context.PlanApi, [stalePool], out _), "Expected deprecating an already Deprecated pool to succeed.");
			Assert.AreEqual(ResourcePoolState.Deprecated, context.Api.ResourcePools.Read(stalePool.Id).State);
		}
	}
}
