namespace RT_MediaOps.Plan.RST.Resources
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Simulation;

	using CoreResource = Skyline.DataMiner.Net.Messages.Resource;
	using PlanResource = Skyline.DataMiner.Solutions.MediaOps.Plan.API.Resource;
	using ResourcePool = Skyline.DataMiner.Solutions.MediaOps.Plan.API.ResourcePool;

	/// <summary>
	/// Simulation-backed tests for the <see cref="FunctionDefinition.MaxInstances"/> check on virtual function resources.
	/// Only functions without entry points are limited by MaxInstances; the instances are counted as CORE function resources
	/// linked to the same function on the same element.
	/// </summary>
	[TestClass]
	public sealed class VirtualFunctionMaxInstancesSimulationTests
	{
		private const string ProtocolName = "Limited Function Connector";

		[TestMethod]
		public void Complete_ElementAlreadyHostsMaxInstances_ReportsMaxInstancesExceeded()
		{
			var setup = CreateSetup(maxInstances: 1);
			var first = setup.Api.Resources.Complete(setup.Api.Resources.Create(setup.NewResource(setup.ElementA)));
			Assert.AreNotEqual(Guid.Empty, first.CoreResourceId);

			var second = setup.Api.Resources.Create(setup.NewResource(setup.ElementA));

			var exception = Assert.ThrowsException<MediaOpsException>(() => setup.Api.Resources.Complete(second));

			var error = exception.TraceData.ErrorData.OfType<ResourceFunctionMaxInstancesExceededError>().Single();
			Assert.AreEqual(second.Id, error.Id);
			Assert.AreEqual(setup.ElementA.AgentId, error.AgentId);
			Assert.AreEqual(setup.ElementA.ElementId, error.ElementId);
			Assert.AreEqual(setup.FunctionId, error.FunctionId);
			Assert.AreEqual(1, error.MaxInstances);
			Assert.AreEqual(ResourceState.Draft, setup.Api.Resources.Read(second.Id).State, "A refused resource must stay in draft.");
		}

		[TestMethod]
		public void Complete_OtherElementStillHasRoom_Succeeds()
		{
			var setup = CreateSetup(maxInstances: 1);
			setup.Api.Resources.Complete(setup.Api.Resources.Create(setup.NewResource(setup.ElementA)));

			var onOtherElement = setup.Api.Resources.Complete(setup.Api.Resources.Create(setup.NewResource(setup.ElementB)));

			Assert.AreEqual(ResourceState.Complete, onOtherElement.State);
			Assert.AreNotEqual(Guid.Empty, onOtherElement.CoreResourceId);
		}

		[TestMethod]
		public void Complete_UnlimitedFunction_AllowsMultipleInstancesOnOneElement()
		{
			var setup = CreateSetup(maxInstances: 0);

			var first = setup.Api.Resources.Complete(setup.Api.Resources.Create(setup.NewResource(setup.ElementA)));
			var second = setup.Api.Resources.Complete(setup.Api.Resources.Create(setup.NewResource(setup.ElementA)));

			Assert.AreEqual(ResourceState.Complete, first.State);
			Assert.AreEqual(ResourceState.Complete, second.State);
		}

		[TestMethod]
		public void Complete_BatchExceedsLimitOnEmptyElement_RefusesEveryNewResource()
		{
			var setup = CreateSetup(maxInstances: 1);
			var drafts = setup.Api.Resources.Create([setup.NewResource(setup.ElementA), setup.NewResource(setup.ElementA)]).ToList();

			var exception = Assert.ThrowsException<MediaOpsBulkException<Guid>>(() => setup.Api.Resources.Complete(drafts));

			CollectionAssert.AreEquivalent(drafts.Select(x => x.Id).ToArray(), exception.Result.TraceDataPerItem.Keys.ToArray());
			foreach (var draft in drafts)
			{
				var error = exception.Result.TraceDataPerItem[draft.Id].ErrorData.OfType<ResourceFunctionMaxInstancesExceededError>().Single();
				Assert.AreEqual(draft.Id, error.Id);
				Assert.AreEqual(1, error.MaxInstances);
			}

			Assert.AreEqual(0, setup.CountCoreInstances(setup.ElementA), "No CORE function resource may be created when the batch is refused.");
		}

		[TestMethod]
		public void Complete_BatchFillsRemainingSlots_Succeeds()
		{
			var setup = CreateSetup(maxInstances: 3);
			setup.Api.Resources.Complete(setup.Api.Resources.Create(setup.NewResource(setup.ElementA)));
			var drafts = setup.Api.Resources.Create([setup.NewResource(setup.ElementA), setup.NewResource(setup.ElementA)]).ToList();

			var completed = setup.Api.Resources.Complete(drafts);

			Assert.AreEqual(2, completed.Count);
			Assert.IsTrue(completed.All(x => x.State == ResourceState.Complete));
			Assert.AreEqual(3, setup.CountCoreInstances(setup.ElementA));
		}

		[TestMethod]
		public void Complete_CoreInstanceCreatedOutsidePlan_CountsTowardsLimit()
		{
			var setup = CreateSetup(maxInstances: 1);
			setup.ResourceManagerHelper.AddOrUpdateResources(new Skyline.DataMiner.Net.ResourceManager.Objects.FunctionResource
			{
				ID = Guid.NewGuid(),
				Name = "External instance",
				FunctionGUID = setup.FunctionId,
				MainDVEDmaID = setup.ElementA.AgentId,
				MainDVEElementID = setup.ElementA.ElementId,
			});
			Assert.AreEqual(1, setup.CountCoreInstances(setup.ElementA), "The external instance must be visible in the Resource Manager.");

			var draft = setup.Api.Resources.Create(setup.NewResource(setup.ElementA));

			var exception = Assert.ThrowsException<MediaOpsException>(() => setup.Api.Resources.Complete(draft));
			Assert.IsTrue(exception.TraceData.ErrorData.OfType<ResourceFunctionMaxInstancesExceededError>().Any());
		}

		[TestMethod]
		public void OutOfSyncDetection_CompletedResourceOccupyingTheOnlySlot_IsNotBlockedByItsOwnInstance()
		{
			var setup = CreateSetup(maxInstances: 1);
			var pool = setup.Api.ResourcePools.Complete(setup.Api.ResourcePools.Create(new ResourcePool { Name = "Pool" }));
			var resource = setup.Api.Resources.Complete(setup.Api.Resources.Create(setup.NewResource(setup.ElementA).AssignToPool(pool)));
			Assert.AreNotEqual(Guid.Empty, resource.CoreResourceId);

			var report = setup.Api.ResourcePools.GetOutOfSyncItems([pool]);

			Assert.IsTrue(report.IsSynchronized, String.Join(" | ", report.Resources.SelectMany(x => x.Blockers).Select(x => x.ErrorMessage)));
		}

		[TestMethod]
		public void ConvertToVirtualFunctionResource_ElementAlreadyHostsMaxInstances_ReportsMaxInstancesExceeded()
		{
			var setup = CreateSetup(maxInstances: 1);
			setup.Api.Resources.Complete(setup.Api.Resources.Create(setup.NewResource(setup.ElementA)));
			var unmanaged = setup.Api.Resources.Create(new UnmanagedResource { Name = "Unmanaged" });

			var exception = Assert.ThrowsException<MediaOpsBulkException<Guid>>(() => setup.Api.Resources.ConvertToVirtualFunctionResource(unmanaged, setup.NewLinkSetting(setup.ElementA)));

			var error = exception.Result.TraceDataPerItem[unmanaged.Id].ErrorData.OfType<ResourceFunctionMaxInstancesExceededError>().Single();
			Assert.AreEqual(unmanaged.Id, error.Id);
			Assert.AreEqual(setup.FunctionId, error.FunctionId);
			Assert.AreEqual(1, error.MaxInstances);
			Assert.IsInstanceOfType(setup.Api.Resources.Read(unmanaged.Id), typeof(UnmanagedResource), "A refused conversion must not change the resource type.");
		}

		[TestMethod]
		public void ConvertToVirtualFunctionResource_ElementHasRoom_Succeeds()
		{
			var setup = CreateSetup(maxInstances: 1);
			var unmanaged = setup.Api.Resources.Create(new UnmanagedResource { Name = "Unmanaged" });

			var converted = setup.Api.Resources.ConvertToVirtualFunctionResource(unmanaged, setup.NewLinkSetting(setup.ElementA));

			Assert.AreEqual(setup.FunctionId, converted.FunctionId);
			Assert.AreEqual(setup.ElementA.ElementId, converted.ElementId);
		}

		[TestMethod]
		public void Complete_FunctionInstancesLockedByAnotherWriter_IsRefusedWithoutCreatingAnInstance()
		{
			var setup = CreateSetup(maxInstances: 1);
			var draft = setup.Api.Resources.Create(setup.NewResource(setup.ElementA));

			MediaOpsException? exception = null;
			var lockResult = setup.HoldFunctionInstanceLock(setup.ElementA, () => exception = Assert.ThrowsException<MediaOpsException>(() => setup.Api.Resources.Complete(draft)));

			Assert.IsTrue(lockResult.AllLocksGranted);
			StringAssert.Contains(exception!.TraceData.ErrorData.Single().ErrorMessage, "Failed to lock the function instances");
			Assert.AreEqual(0, setup.CountCoreInstances(setup.ElementA), "No CORE instance may be created while another writer holds the function instances.");

			var completed = setup.Api.Resources.Complete(setup.Api.Resources.Read(draft.Id));
			Assert.AreEqual(ResourceState.Complete, completed.State, "The lock must be released after the refused attempt.");
		}

		[TestMethod]
		public void Complete_FunctionInstancesOfOtherElementLocked_IsNotBlocked()
		{
			var setup = CreateSetup(maxInstances: 1);
			var draft = setup.Api.Resources.Create(setup.NewResource(setup.ElementA));

			PlanResource? completed = null;
			setup.HoldFunctionInstanceLock(setup.ElementB, () => completed = setup.Api.Resources.Complete(draft));

			Assert.AreEqual(ResourceState.Complete, completed!.State);
		}

		[TestMethod]
		public void Synchronize_FunctionInstancesLockedByAnotherWriter_DoesNotRecreateTheInstance()
		{
			var setup = CreateSetup(maxInstances: 1);
			var pool = setup.Api.ResourcePools.Complete(setup.Api.ResourcePools.Create(new ResourcePool { Name = "Pool" }));
			var resource = setup.Api.Resources.Complete(setup.Api.Resources.Create(setup.NewResource(setup.ElementA).AssignToPool(pool)));
			setup.ResourceManagerHelper.RemoveResources([new CoreResource(resource.CoreResourceId)]);
			var item = setup.Api.ResourcePools.GetOutOfSyncItems([pool]).Resources.Single();

			SynchronizationResult? result = null;
			setup.HoldFunctionInstanceLock(setup.ElementA, () => result = setup.Api.ResourcePools.Synchronize([item]));

			Assert.IsTrue(result!.Failures.ContainsKey(resource.Id));
			Assert.AreEqual(0, setup.CountCoreInstances(setup.ElementA));

			Assert.IsFalse(setup.Api.ResourcePools.Synchronize([item]).HasFailures, "The lock must be released after the refused attempt.");
			Assert.AreEqual(1, setup.CountCoreInstances(setup.ElementA));
		}

		[TestMethod]
		public void Complete_MixedBatchWithLockedFunctionInstances_OnlyRefusesTheLockedResources()
		{
			var setup = CreateSetup(maxInstances: 1);
			var unmanaged = setup.Api.Resources.Create(new UnmanagedResource { Name = "Unmanaged" });
			var onElementA = setup.Api.Resources.Create(setup.NewResource(setup.ElementA));
			var onElementB = setup.Api.Resources.Create(setup.NewResource(setup.ElementB));

			MediaOpsBulkException<Guid>? exception = null;
			setup.HoldFunctionInstanceLock(setup.ElementA, () => exception = Assert.ThrowsException<MediaOpsBulkException<Guid>>(() => setup.Api.Resources.Complete([unmanaged, onElementA, onElementB])));

			CollectionAssert.AreEquivalent(new[] { onElementA.Id }, exception!.Result.TraceDataPerItem.Keys.ToArray());
			StringAssert.Contains(exception.Result.TraceDataPerItem[onElementA.Id].ErrorData.Single().ErrorMessage, "Failed to lock the function instances");
			Assert.AreEqual(ResourceState.Complete, setup.Api.Resources.Read(unmanaged.Id).State);
			Assert.AreEqual(ResourceState.Complete, setup.Api.Resources.Read(onElementB.Id).State);
			Assert.AreEqual(ResourceState.Draft, setup.Api.Resources.Read(onElementA.Id).State);
			Assert.AreEqual(0, setup.CountCoreInstances(setup.ElementA));
		}

		[TestMethod]
		public void Synchronize_MixedBatchWithLockedFunctionInstances_OnlyFailsTheLockedResources()
		{
			var setup = CreateSetup(maxInstances: 1);
			var pool = setup.CreateCompletedPool();
			var unmanaged = setup.Api.Resources.Complete(setup.Api.Resources.Create(new UnmanagedResource { Name = "Unmanaged" }.AssignToPool(pool)));
			var onElementA = setup.Api.Resources.Complete(setup.Api.Resources.Create(setup.NewResource(setup.ElementA).AssignToPool(pool)));
			var onElementB = setup.Api.Resources.Complete(setup.Api.Resources.Create(setup.NewResource(setup.ElementB).AssignToPool(pool)));
			setup.ResourceManagerHelper.RemoveResources([new CoreResource(unmanaged.CoreResourceId), new CoreResource(onElementA.CoreResourceId), new CoreResource(onElementB.CoreResourceId)]);
			var items = setup.Api.ResourcePools.GetOutOfSyncItems([pool]).Resources;

			SynchronizationResult? result = null;
			setup.HoldFunctionInstanceLock(setup.ElementA, () => result = setup.Api.ResourcePools.Synchronize(items));

			CollectionAssert.AreEquivalent(new[] { onElementA.Id }, result!.Failures.Keys.ToArray());
			CollectionAssert.AreEquivalent(new[] { unmanaged.Id, onElementB.Id }, result.SynchronizedResourceIds.ToArray());
			Assert.AreEqual(0, setup.CountCoreInstances(setup.ElementA));
			Assert.AreEqual(1, setup.CountCoreInstances(setup.ElementB));
		}

		[TestMethod]
		public void Synchronize_MissingInstanceWhileExternalInstanceFillsTheLimit_IsBlockedAndNotRecreated()
		{
			var setup = CreateSetup(maxInstances: 1);
			var pool = setup.CreateCompletedPool();
			var resource = setup.Api.Resources.Complete(setup.Api.Resources.Create(setup.NewResource(setup.ElementA).AssignToPool(pool)));
			setup.ResourceManagerHelper.RemoveResources([new CoreResource(resource.CoreResourceId)]);
			setup.AddExternalInstance(setup.ElementA);

			var item = setup.Api.ResourcePools.GetOutOfSyncItems([pool]).Resources.Single();

			Assert.IsFalse(item.CanSynchronize);
			var blocker = item.Blockers.OfType<ResourceFunctionMaxInstancesExceededError>().Single();
			Assert.AreEqual(setup.ElementA.ElementId, blocker.ElementId);
			Assert.AreEqual(1, blocker.MaxInstances);

			var result = setup.Api.ResourcePools.Synchronize([item]);

			Assert.IsTrue(result.Failures[resource.Id].ErrorData.OfType<ResourceFunctionMaxInstancesExceededError>().Any());
			Assert.AreEqual(1, setup.CountCoreInstances(setup.ElementA), "Only the external instance may exist.");
		}

		[TestMethod]
		public void Synchronize_LinkMovedToElementAtTheLimit_IsBlockedAndNotMoved()
		{
			var setup = CreateSetup(maxInstances: 1);
			var pool = setup.CreateCompletedPool();
			var resource = setup.Api.Resources.Complete(setup.Api.Resources.Create(setup.NewResource(setup.ElementA).AssignToPool(pool)));
			setup.Api.Resources.Complete(setup.Api.Resources.Create(setup.NewResource(setup.ElementB).AssignToPool(pool)));
			setup.MoveDomLink(resource.Id, setup.ElementB);

			var item = setup.Api.ResourcePools.GetOutOfSyncItems([pool]).Resources.Single();

			Assert.IsFalse(item.CanSynchronize);
			Assert.AreEqual(setup.ElementB.ElementId, item.Blockers.OfType<ResourceFunctionMaxInstancesExceededError>().Single().ElementId);

			setup.Api.ResourcePools.Synchronize([item]);

			Assert.AreEqual(1, setup.CountCoreInstances(setup.ElementA), "The CORE instance may not be moved away.");
			Assert.AreEqual(1, setup.CountCoreInstances(setup.ElementB));
		}

		[TestMethod]
		public void Synchronize_LinkMovedToElementWithRoom_MovesTheInstance()
		{
			var setup = CreateSetup(maxInstances: 1);
			var pool = setup.CreateCompletedPool();
			var resource = setup.Api.Resources.Complete(setup.Api.Resources.Create(setup.NewResource(setup.ElementA).AssignToPool(pool)));
			setup.MoveDomLink(resource.Id, setup.ElementB);

			var item = setup.Api.ResourcePools.GetOutOfSyncItems([pool]).Resources.Single();
			Assert.IsTrue(item.CanSynchronize, String.Join(" | ", item.Blockers.Select(x => x.ErrorMessage)));

			Assert.IsFalse(setup.Api.ResourcePools.Synchronize([item]).HasFailures);

			Assert.AreEqual(0, setup.CountCoreInstances(setup.ElementA));
			Assert.AreEqual(1, setup.CountCoreInstances(setup.ElementB));
		}

		private static TestSetup CreateSetup(int maxInstances)
		{
			var dms = MediaOpsPlanSimulation.Create();
			dms.RegisterProtocol(ProtocolName, "Production");

			var functionDefinition = new FunctionDefinition
			{
				GUID = Guid.NewGuid(),
				Name = "Limited Function",
				MaxInstances = maxInstances,
				EntryPoints = Array.Empty<FunctionEntryPointDefinition>(),
			};
			dms.RegisterProtocolFunction(ProtocolName, functionDefinition);

			var elementA = dms.AddElement("Element A", dmaId: 1, elementId: 101, protocolName: ProtocolName);
			var elementB = dms.AddElement("Element B", dmaId: 1, elementId: 102, protocolName: ProtocolName);

			var connection = dms.CreateConnection();

			return new TestSetup(
				connection.GetMediaOpsPlanApi(),
				new ResourceManagerHelper(connection.HandleSingleResponseMessage),
				functionDefinition.GUID,
				new ElementRef(elementA.DmaId, elementA.ElementId),
				new ElementRef(elementB.DmaId, elementB.ElementId));
		}

		private sealed class ElementRef
		{
			public ElementRef(int agentId, int elementId)
			{
				AgentId = agentId;
				ElementId = elementId;
			}

			public int AgentId { get; }

			public int ElementId { get; }
		}

		private sealed class TestSetup
		{
			private int _counter;

			public TestSetup(IMediaOpsPlanApi api, ResourceManagerHelper resourceManagerHelper, Guid functionId, ElementRef elementA, ElementRef elementB)
			{
				Api = api;
				ResourceManagerHelper = resourceManagerHelper;
				FunctionId = functionId;
				ElementA = elementA;
				ElementB = elementB;
			}

			public IMediaOpsPlanApi Api { get; }

			public ResourceManagerHelper ResourceManagerHelper { get; }

			public Guid FunctionId { get; }

			public ElementRef ElementA { get; }

			public ElementRef ElementB { get; }

			public VirtualFunctionResource NewResource(ElementRef element)
			{
				return new VirtualFunctionResource
				{
					Name = $"VF Resource {++_counter}",
					FunctionId = FunctionId,
					AgentId = element.AgentId,
					ElementId = element.ElementId,
				};
			}

			public ResourceVirtualFunctionLinkSetting NewLinkSetting(ElementRef element)
			{
				return new ResourceVirtualFunctionLinkSetting
				{
					FunctionId = FunctionId,
					AgentId = element.AgentId,
					ElementId = element.ElementId,
				};
			}

			public int CountCoreInstances(ElementRef element)
			{
				return ResourceManagerHelper.GetResources(new TRUEFilterElement<CoreResource>())
					.OfType<Skyline.DataMiner.Net.ResourceManager.Objects.FunctionResource>()
					.Count(x => x.FunctionGUID == FunctionId && x.MainDVEDmaID == element.AgentId && x.MainDVEElementID == element.ElementId);
			}

			/// <summary>Simulates another writer that holds the function instances of the element while the action runs.</summary>
			public Skyline.DataMiner.Solutions.MediaOps.Plan.Tools.LockManager.LockResult<CoreResourceHandler.FunctionInstanceSlotLock> HoldFunctionInstanceLock(ElementRef element, Action action)
			{
				var slotLock = CoreResourceHandler.FunctionInstanceSlotLock.For(FunctionId, element.AgentId, element.ElementId);
				return ((MediaOpsPlanApi)Api).LockManager.LockAllAndExecute([slotLock], action);
			}

			public ResourcePool CreateCompletedPool()
			{
				return Api.ResourcePools.Complete(Api.ResourcePools.Create(new ResourcePool { Name = $"Pool {++_counter}" }));
			}

			/// <summary>Adds a CORE function instance that was not created through MediaOps Plan.</summary>
			public void AddExternalInstance(ElementRef element)
			{
				ResourceManagerHelper.AddOrUpdateResources(new Skyline.DataMiner.Net.ResourceManager.Objects.FunctionResource
				{
					ID = Guid.NewGuid(),
					Name = $"External instance {++_counter}",
					FunctionGUID = FunctionId,
					MainDVEDmaID = element.AgentId,
					MainDVEElementID = element.ElementId,
				});
			}

			/// <summary>Points the stored element link to another element without touching the CORE resource, so the two drift apart.</summary>
			public void MoveDomLink(Guid resourceId, ElementRef element)
			{
				var domHelpers = ((MediaOpsPlanApi)Api).DomHelpers;
				var domResource = domHelpers.SlcResourceStudioHelper.GetResources([resourceId]).Single();
				var metadata = domResource.ResourceInternalProperties.Metadata;
				domResource.ResourceInternalProperties.Metadata = new Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.ResourceMetadata
				{
					LinkedElementInfo = new Skyline.DataMiner.Core.DataMinerSystem.Common.DmsElementId(element.AgentId, element.ElementId).Value,
					LinkedFunctionId = metadata.LinkedFunctionId,
					LinkedFunctionTableIndex = metadata.LinkedFunctionTableIndex,
				};

				domHelpers.SlcResourceStudioHelper.DomHelper.DomInstances.Update(domResource.ToInstance());
			}
		}
	}
}
