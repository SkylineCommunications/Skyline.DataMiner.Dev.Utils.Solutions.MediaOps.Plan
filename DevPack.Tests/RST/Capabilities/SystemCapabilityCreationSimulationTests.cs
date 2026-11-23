namespace RT_MediaOps.Plan.RST.Capabilities
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Simulation;

	/// <summary>
	/// Verifies that the public repository cannot create a system capability while it does not exist yet. Each test uses
	/// its own simulation, because the shared integration test context already creates the Resource Type capability.
	/// </summary>
	[TestClass]
	public sealed class SystemCapabilityCreationSimulationTests
	{
		private static readonly Guid ResourceTypeId = CoreCapabilities.ResourceType.Id;

		[TestMethod]
		public void CreateSystemCapabilityBeforeInitializationThrowsException()
		{
			var api = CreateApi();

			var ex = Assert.ThrowsException<MediaOpsException>(() => api.Capabilities.Create(CreateResourceTypeCapability()));

			AssertSystemCapabilityCreationError(ex.TraceData);
			Assert.IsNull(api.Capabilities.Read(ResourceTypeId));
			AssertSystemCapabilitiesCreatesResourceType(api);
		}

		[TestMethod]
		public void CreateOrUpdateSystemCapabilityBeforeInitializationThrowsException()
		{
			var api = CreateApi();

			var ex = Assert.ThrowsException<MediaOpsBulkException<Guid>>(() => api.Capabilities.CreateOrUpdate(new[] { CreateResourceTypeCapability() }));

			Assert.AreEqual(0, ex.Result.SuccessfulIds.Count);
			CollectionAssert.AreEquivalent(new[] { ResourceTypeId }, ex.Result.UnsuccessfulIds.ToArray());
			Assert.IsTrue(ex.Result.TraceDataPerItem.TryGetValue(ResourceTypeId, out var traceData));
			AssertSystemCapabilityCreationError(traceData);
			Assert.IsNull(api.Capabilities.Read(ResourceTypeId));
			AssertSystemCapabilitiesCreatesResourceType(api);
		}

		[TestMethod]
		public void CreateMixedBatchWithSystemCapabilityOnlyRefusesSystemCapability()
		{
			var api = CreateApi();
			var capability = CreateRegularCapability();

			var ex = Assert.ThrowsException<MediaOpsBulkException<Guid>>(() => api.Capabilities.Create(new[] { CreateResourceTypeCapability(), capability }));

			AssertMixedBatchResult(api, ex, capability);
			AssertSystemCapabilitiesCreatesResourceType(api);
		}

		[TestMethod]
		public void CreateOrUpdateMixedBatchWithSystemCapabilityOnlyRefusesSystemCapability()
		{
			var api = CreateApi();
			var capability = CreateRegularCapability();

			var ex = Assert.ThrowsException<MediaOpsBulkException<Guid>>(() => api.Capabilities.CreateOrUpdate(new[] { CreateResourceTypeCapability(), capability }));

			AssertMixedBatchResult(api, ex, capability);
			AssertSystemCapabilitiesCreatesResourceType(api);
		}

		private static IMediaOpsPlanApi CreateApi()
		{
			var api = MediaOpsPlanSimulation.Create().CreateConnection().GetMediaOpsPlanApi();
			Assert.IsNull(api.Capabilities.Read(ResourceTypeId), "The simulation must start without the Resource Type capability.");
			return api;
		}

		private static Capability CreateResourceTypeCapability()
		{
			return new Capability(ResourceTypeId)
			{
				Name = CoreCapabilities.ResourceType.Name,
			}
			.SetDiscretes(new[] { "Value 1" });
		}

		private static Capability CreateRegularCapability()
		{
			return new Capability
			{
				Name = $"Capability_{Guid.NewGuid()}",
			}
			.SetDiscretes(new[] { "Value 1" });
		}

		private static void AssertMixedBatchResult(IMediaOpsPlanApi api, MediaOpsBulkException<Guid> ex, Capability capability)
		{
			CollectionAssert.AreEquivalent(new[] { capability.Id }, ex.Result.SuccessfulIds.ToArray());
			CollectionAssert.AreEquivalent(new[] { ResourceTypeId }, ex.Result.UnsuccessfulIds.ToArray());
			Assert.IsTrue(ex.Result.TraceDataPerItem.TryGetValue(ResourceTypeId, out var traceData));
			AssertSystemCapabilityCreationError(traceData);

			Assert.IsNull(api.Capabilities.Read(ResourceTypeId));
			var storedCapability = api.Capabilities.Read(capability.Id);
			Assert.IsNotNull(storedCapability);
			Assert.AreEqual(capability.Name, storedCapability.Name);
		}

		private static void AssertSystemCapabilityCreationError(MediaOpsTraceData traceData)
		{
			Assert.IsNotNull(traceData);
			Assert.AreEqual(1, traceData.ErrorData.Count);

			var error = traceData.ErrorData.OfType<CapabilitySystemCapabilityError>().SingleOrDefault();
			Assert.IsNotNull(error);
			Assert.AreEqual(ResourceTypeId, error.Id);
			Assert.AreEqual($"Capability '{CoreCapabilities.ResourceType.Name}' is a system capability and cannot be created.", error.ErrorMessage);
		}

		private static void AssertSystemCapabilitiesCreatesResourceType(IMediaOpsPlanApi api)
		{
			var resourceType = api.Capabilities.SystemCapabilities.ResourceType;

			Assert.IsNotNull(resourceType);
			Assert.AreEqual(ResourceTypeId, resourceType.Id);
			Assert.AreEqual(CoreCapabilities.ResourceType.Name, resourceType.Name);
			CollectionAssert.AreEquivalent(new[] { "Element", "Pool Resource", "Service", "Unlinked Resource", "Virtual Function" }, resourceType.Discretes.ToArray());

			var storedResourceType = api.Capabilities.Read(ResourceTypeId);
			Assert.IsNotNull(storedResourceType);
			Assert.AreEqual(CoreCapabilities.ResourceType.Name, storedResourceType.Name);
		}
	}
}
