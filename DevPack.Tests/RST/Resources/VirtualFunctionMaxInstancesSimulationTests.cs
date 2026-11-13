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
		}
	}
}
