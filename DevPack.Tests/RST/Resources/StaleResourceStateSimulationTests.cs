namespace RT_MediaOps.Plan.RST.Resources
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Net.ResourceManager.Objects;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Simulation;

	using PlanResource = Skyline.DataMiner.Solutions.MediaOps.Plan.API.Resource;

	/// <summary>
	/// A resource action can be called with a resource that was read before another user changed its state. These tests
	/// change the stored state through a second API instance, which has its own in-memory lock manager, and then call the
	/// handler directly with the stale resource, because the repository would re-read the resource by ID.
	/// </summary>
	[TestClass]
	public sealed class StaleResourceStateSimulationTests
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

			public PlanResource CreateDraftResource()
			{
				return Api.Resources.Create(new UnmanagedResource { Name = $"{Guid.NewGuid()}_Resource" });
			}

			public int CountCoreResources(string name)
			{
				return ResourceManagerHelper.GetResources(Skyline.DataMiner.Net.Messages.ResourceExposers.Name.Equal(name)).Count();
			}
		}

		private static void AssertInvalidState(DomInstanceBulkOperationResult<Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.ResourceInstance> result, Guid resourceId)
		{
			Assert.IsTrue(
				result.TraceDataPerItem[resourceId].ErrorData.OfType<ResourceInvalidStateError>().Any(),
				"Expected a ResourceInvalidStateError for the resource whose stored state changed.");
		}

		[TestMethod]
		public void Update_DraftResourceCompletedAfterRead_FailsWithoutChangingResource()
		{
			var context = new Context();
			var staleResource = context.CreateDraftResource();
			var originalName = staleResource.Name;

			// The update was validated, and its CORE changes were selected, for a Draft resource.
			context.OtherApi.Resources.Complete(staleResource.Id);

			staleResource.Name = $"{originalName}_Renamed";
			Assert.IsFalse(DomResourceHandler.TryCreateOrUpdate(context.PlanApi, [staleResource], out var result), "Expected the update of a resource whose state changed to fail.");
			AssertInvalidState(result, staleResource.Id);
			Assert.AreEqual(originalName, context.Api.Resources.Read(staleResource.Id).Name);
		}

		[TestMethod]
		public void Update_CompleteResourceDeprecatedAfterRead_FailsWithoutChangingResource()
		{
			var context = new Context();
			var staleResource = context.Api.Resources.Complete(context.CreateDraftResource());
			var originalName = staleResource.Name;

			context.OtherApi.Resources.Deprecate(staleResource.Id);

			staleResource.Name = $"{originalName}_Renamed";
			Assert.IsFalse(DomResourceHandler.TryCreateOrUpdate(context.PlanApi, [staleResource], out var result), "Expected the update of a Deprecated resource to fail.");
			AssertInvalidState(result, staleResource.Id);
			Assert.AreEqual(originalName, context.Api.Resources.Read(staleResource.Id).Name);
		}

		[TestMethod]
		public void Delete_DraftResourceCompletedAfterRead_FailsAndKeepsCoreResource()
		{
			var context = new Context();
			var staleResource = context.CreateDraftResource();

			var completedResource = context.OtherApi.Resources.Complete(staleResource.Id);

			Assert.IsFalse(DomResourceHandler.TryDelete(context.PlanApi, [staleResource], out var result), "Expected the delete of a Complete resource to fail.");
			AssertInvalidState(result, staleResource.Id);
			Assert.AreEqual(ResourceState.Complete, context.Api.Resources.Read(staleResource.Id).State);
			Assert.IsNotNull(context.ResourceManagerHelper.GetResource(completedResource.CoreResourceId), "Expected the CORE resource to be kept.");
		}

		[TestMethod]
		public void Delete_DeprecatedResourceRestoredAfterRead_FailsAndKeepsCoreResource()
		{
			var context = new Context();
			var staleResource = context.Api.Resources.Deprecate(context.Api.Resources.Complete(context.CreateDraftResource()));

			context.OtherApi.Resources.Restore(staleResource.Id);

			Assert.IsFalse(DomResourceHandler.TryDelete(context.PlanApi, [staleResource], out var result), "Expected the delete of a restored resource to fail.");
			AssertInvalidState(result, staleResource.Id);
			Assert.AreEqual(ResourceState.Complete, context.Api.Resources.Read(staleResource.Id).State);
			Assert.AreEqual(1, context.CountCoreResources(staleResource.Name), "Expected the CORE resource of the restored resource to be kept.");
		}

		[TestMethod]
		public void Delete_ResourceAlreadyDeletedAfterRead_Succeeds()
		{
			var context = new Context();
			var staleResource = context.CreateDraftResource();

			context.OtherApi.Resources.Delete(staleResource.Id);

			Assert.IsTrue(DomResourceHandler.TryDelete(context.PlanApi, [staleResource], out _), "Expected the delete of an already deleted resource to succeed.");
		}

		[TestMethod]
		public void Complete_DraftResourceCompletedAfterRead_SucceedsWithoutSecondCoreResource()
		{
			var context = new Context();
			var staleResource = context.CreateDraftResource();

			var completedResource = context.OtherApi.Resources.Complete(staleResource.Id);

			Assert.IsTrue(DomResourceHandler.TryComplete(context.PlanApi, [staleResource], out _), "Expected completing an already Complete resource to succeed.");
			Assert.AreEqual(1, context.CountCoreResources(staleResource.Name), "Expected no second CORE resource.");
			Assert.AreEqual(completedResource.CoreResourceId, context.Api.Resources.Read(staleResource.Id).CoreResourceId, "Expected the link to the CORE resource to be kept.");
		}

		[TestMethod]
		public void Complete_DraftResourceRenamedAfterRead_KeepsRename()
		{
			var context = new Context();
			var staleResource = context.CreateDraftResource();

			var otherApi = context.OtherApi;
			var renamedResource = otherApi.Resources.Read(staleResource.Id);
			renamedResource.Name = $"{staleResource.Name}_Renamed";
			otherApi.Resources.Update(renamedResource);

			Assert.IsTrue(DomResourceHandler.TryComplete(context.PlanApi, [staleResource], out _), "Expected the complete to succeed.");

			var storedResource = context.Api.Resources.Read(staleResource.Id);
			Assert.AreEqual(ResourceState.Complete, storedResource.State);
			Assert.AreEqual(renamedResource.Name, storedResource.Name, "Expected the stale resource not to overwrite the rename.");
		}

		[TestMethod]
		public void Restore_DeprecatedResourceRestoredAfterRead_Succeeds()
		{
			var context = new Context();
			var staleResource = context.Api.Resources.Deprecate(context.Api.Resources.Complete(context.CreateDraftResource()));

			context.OtherApi.Resources.Restore(staleResource.Id);

			Assert.IsTrue(DomResourceHandler.TryRestore(context.PlanApi, [staleResource], out _), "Expected restoring an already restored resource to succeed.");
			Assert.AreEqual(ResourceState.Complete, context.Api.Resources.Read(staleResource.Id).State);
		}

		[TestMethod]
		public void Deprecate_CompleteResourceDeprecatedAfterRead_Succeeds()
		{
			var context = new Context();
			var staleResource = context.Api.Resources.Complete(context.CreateDraftResource());

			context.OtherApi.Resources.Deprecate(staleResource.Id);

			Assert.IsTrue(DomResourceHandler.TryDeprecate(context.PlanApi, [staleResource], out _), "Expected deprecating an already Deprecated resource to succeed.");
			Assert.AreEqual(ResourceState.Deprecated, context.Api.Resources.Read(staleResource.Id).State);
		}
	}
}
