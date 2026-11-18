namespace RT_MediaOps.Plan.RST.Capabilities
{
	using System.Linq;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	[TestClass]
	[TestCategory("IntegrationTest")]
	public sealed class SystemCapabilityTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public SystemCapabilityTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		private static Guid ResourceTypeId => TestContext.Api.Capabilities.SystemCapabilities.ResourceType.Id;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void SystemCapabilitiesProvidesResourceType()
		{
			var resourceType = TestContext.Api.Capabilities.SystemCapabilities.ResourceType;

			Assert.IsNotNull(resourceType);
			Assert.AreEqual(CoreCapabilities.ResourceType.Id, resourceType.Id);
			Assert.AreEqual(CoreCapabilities.ResourceType.Name, resourceType.Name);
		}

		[TestMethod]
		public void UpdateSystemCapabilityThrowsException()
		{
			var resourceType = TestContext.Api.Capabilities.Read(ResourceTypeId);
			var originalDiscretes = resourceType.Discretes.ToList();
			resourceType.AddDiscrete($"Discrete_{Guid.NewGuid()}");

			var ex = Assert.ThrowsException<MediaOpsException>(() => TestContext.Api.Capabilities.Update(resourceType));

			AssertSystemCapabilityError(ex.TraceData, "updated");
			AssertResourceTypeUnchanged(originalDiscretes);
		}

		[TestMethod]
		public void UpdateSystemCapabilityNameThrowsException()
		{
			var resourceType = TestContext.Api.Capabilities.Read(ResourceTypeId);
			var originalDiscretes = resourceType.Discretes.ToList();
			resourceType.Name = $"Capability_{Guid.NewGuid()}";

			var ex = Assert.ThrowsException<MediaOpsException>(() => TestContext.Api.Capabilities.Update(resourceType));

			var error = ex.TraceData.ErrorData.OfType<CapabilitySystemCapabilityError>().SingleOrDefault();
			Assert.IsNotNull(error);
			Assert.AreEqual(ResourceTypeId, error.Id);
			AssertResourceTypeUnchanged(originalDiscretes);
		}

		[TestMethod]
		public void BulkUpdateWithSystemCapabilityOnlyRefusesSystemCapability()
		{
			var capability = objectCreator.CreateCapability(new Capability
			{
				Name = $"Capability_{Guid.NewGuid()}",
			}
			.SetDiscretes(new[] { "Value 1" }));

			var resourceType = TestContext.Api.Capabilities.Read(ResourceTypeId);
			var originalDiscretes = resourceType.Discretes.ToList();
			resourceType.AddDiscrete($"Discrete_{Guid.NewGuid()}");

			capability = TestContext.Api.Capabilities.Read(capability.Id);
			capability.AddDiscrete("Value 2");

			var ex = Assert.ThrowsException<MediaOpsBulkException<Guid>>(() => TestContext.Api.Capabilities.Update(new[] { resourceType, capability }));

			CollectionAssert.AreEquivalent(new[] { capability.Id }, ex.Result.SuccessfulIds.ToArray());
			CollectionAssert.AreEquivalent(new[] { ResourceTypeId }, ex.Result.UnsuccessfulIds.ToArray());
			Assert.IsTrue(ex.Result.TraceDataPerItem.TryGetValue(ResourceTypeId, out var traceData));
			AssertSystemCapabilityError(traceData, "updated");

			AssertResourceTypeUnchanged(originalDiscretes);
			CollectionAssert.AreEquivalent(new[] { "Value 1", "Value 2" }, TestContext.Api.Capabilities.Read(capability.Id).Discretes.ToArray());
		}

		[TestMethod]
		public void CreateOrUpdateSystemCapabilityThrowsException()
		{
			var resourceType = TestContext.Api.Capabilities.Read(ResourceTypeId);
			var originalDiscretes = resourceType.Discretes.ToList();
			resourceType.AddDiscrete($"Discrete_{Guid.NewGuid()}");

			var ex = Assert.ThrowsException<MediaOpsBulkException<Guid>>(() => TestContext.Api.Capabilities.CreateOrUpdate(new[] { resourceType }));

			Assert.IsTrue(ex.Result.TraceDataPerItem.TryGetValue(ResourceTypeId, out var traceData));
			AssertSystemCapabilityError(traceData, "updated");
			AssertResourceTypeUnchanged(originalDiscretes);
		}

		[TestMethod]
		public void CreateSystemCapabilityThroughRepositoryThrowsException()
		{
			var originalDiscretes = TestContext.Api.Capabilities.Read(ResourceTypeId).Discretes.ToList();

			var capability = new Capability(ResourceTypeId)
			{
				Name = CoreCapabilities.ResourceType.Name,
			}
			.SetDiscretes(new[] { "Value 1" });

			var ex = Assert.ThrowsException<MediaOpsException>(() => TestContext.Api.Capabilities.Create(capability));

			var error = ex.TraceData.ErrorData.OfType<CapabilitySystemCapabilityError>().SingleOrDefault();
			Assert.IsNotNull(error);
			Assert.AreEqual(ResourceTypeId, error.Id);
			Assert.AreEqual($"Capability '{CoreCapabilities.ResourceType.Name}' is a system capability and cannot be created.", error.ErrorMessage);
			AssertResourceTypeUnchanged(originalDiscretes);
		}

		[TestMethod]
		public void DeleteSystemCapabilityThrowsException()
		{
			var resourceType = TestContext.Api.Capabilities.Read(ResourceTypeId);
			var originalDiscretes = resourceType.Discretes.ToList();

			var ex = Assert.ThrowsException<MediaOpsException>(() => TestContext.Api.Capabilities.Delete(resourceType));
			AssertSystemCapabilityError(ex.TraceData, "deleted");

			ex = Assert.ThrowsException<MediaOpsException>(() => TestContext.Api.Capabilities.Delete(ResourceTypeId));
			AssertSystemCapabilityError(ex.TraceData, "deleted");

			AssertResourceTypeUnchanged(originalDiscretes);
		}

		[TestMethod]
		public void BulkDeleteWithSystemCapabilityOnlyRefusesSystemCapability()
		{
			var capability = objectCreator.CreateCapability(new Capability
			{
				Name = $"Capability_{Guid.NewGuid()}",
			}
			.SetDiscretes(new[] { "Value 1" }));

			var originalDiscretes = TestContext.Api.Capabilities.Read(ResourceTypeId).Discretes.ToList();

			var ex = Assert.ThrowsException<MediaOpsBulkException<Guid>>(() => TestContext.Api.Capabilities.Delete(new[] { ResourceTypeId, capability.Id }));

			CollectionAssert.AreEquivalent(new[] { capability.Id }, ex.Result.SuccessfulIds.ToArray());
			CollectionAssert.AreEquivalent(new[] { ResourceTypeId }, ex.Result.UnsuccessfulIds.ToArray());
			Assert.IsTrue(ex.Result.TraceDataPerItem.TryGetValue(ResourceTypeId, out var traceData));
			AssertSystemCapabilityError(traceData, "deleted");

			Assert.IsNull(TestContext.Api.Capabilities.Read(capability.Id));
			AssertResourceTypeUnchanged(originalDiscretes);
		}

		private static void AssertSystemCapabilityError(MediaOpsTraceData traceData, string operation)
		{
			Assert.IsNotNull(traceData);

			var error = traceData.ErrorData.OfType<CapabilitySystemCapabilityError>().SingleOrDefault();
			Assert.IsNotNull(error);
			Assert.AreEqual(ResourceTypeId, error.Id);
			Assert.AreEqual($"Capability '{CoreCapabilities.ResourceType.Name}' is a system capability and cannot be {operation}.", error.ErrorMessage);
		}

		private static void AssertResourceTypeUnchanged(IReadOnlyCollection<string> originalDiscretes)
		{
			var resourceType = TestContext.Api.Capabilities.Read(ResourceTypeId);
			Assert.IsNotNull(resourceType);
			Assert.AreEqual(CoreCapabilities.ResourceType.Name, resourceType.Name);
			CollectionAssert.AreEquivalent(originalDiscretes.ToArray(), resourceType.Discretes.ToArray());
		}
	}
}
