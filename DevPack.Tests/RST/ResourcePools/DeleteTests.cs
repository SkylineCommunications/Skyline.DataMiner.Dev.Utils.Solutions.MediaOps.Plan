namespace RT_MediaOps.Plan.RST.ResourcePools
{
	using System;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	[TestCategory("IntegrationTest")]
	public sealed class DeleteTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public DeleteTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void Delete_DeprecatedPool_RemovesAssociationFromActiveResource()
		{
			var prefix = Guid.NewGuid();

			var pool = new ResourcePool
			{
				Name = $"{prefix}_Pool",
			};
			pool = objectCreator.CreateResourcePool(pool);
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resource = new UnmanagedResource
			{
				Name = $"{prefix}_Resource",
			}
			.AssignToPool(pool);
			resource = objectCreator.CreateResource(resource);
			resource = TestContext.Api.Resources.Complete(resource);
			Assert.IsNotNull(resource);
			Assert.AreEqual(1, resource.ResourcePoolIds.Count);

			var corePoolId = pool.CoreResourcePoolId;
			var coreResourceId = resource.CoreResourceId;
			Assert.AreNotEqual(Guid.Empty, corePoolId);
			Assert.AreNotEqual(Guid.Empty, coreResourceId);

			var coreResource = TestContext.ResourceManagerHelper.GetResource(coreResourceId);
			Assert.AreEqual(1, coreResource.PoolGUIDs.Count);
			Assert.AreEqual(corePoolId, coreResource.PoolGUIDs.Single());

			pool = TestContext.Api.ResourcePools.Deprecate(pool);
			Assert.IsNotNull(pool);

			TestContext.Api.ResourcePools.Delete(pool);

			resource = TestContext.Api.Resources.Read(resource.Id);
			Assert.IsNotNull(resource);
			Assert.AreEqual(0, resource.ResourcePoolIds.Count);

			coreResource = TestContext.ResourceManagerHelper.GetResource(coreResourceId);
			Assert.IsNotNull(coreResource);
			Assert.AreEqual(0, coreResource.PoolGUIDs.Count);
		}

		[TestMethod]
		public void Delete_DeprecatedPool_RemovesAssociationFromDeprecatedResource()
		{
			var prefix = Guid.NewGuid();

			var pool = new ResourcePool
			{
				Name = $"{prefix}_Pool",
			};
			pool = objectCreator.CreateResourcePool(pool);
			pool = TestContext.Api.ResourcePools.Complete(pool);

			var resource = new UnmanagedResource
			{
				Name = $"{prefix}_Resource",
			}
			.AssignToPool(pool);
			resource = objectCreator.CreateResource(resource);
			resource = TestContext.Api.Resources.Complete(resource);
			Assert.IsNotNull(resource);
			Assert.AreEqual(1, resource.ResourcePoolIds.Count);

			var corePoolId = pool.CoreResourcePoolId;
			var coreResourceId = resource.CoreResourceId;
			Assert.AreNotEqual(Guid.Empty, corePoolId);
			Assert.AreNotEqual(Guid.Empty, coreResourceId);

			var coreResource = TestContext.ResourceManagerHelper.GetResource(coreResourceId);
			Assert.AreEqual(1, coreResource.PoolGUIDs.Count);
			Assert.AreEqual(corePoolId, coreResource.PoolGUIDs.Single());

			pool = TestContext.Api.ResourcePools.Deprecate(pool, new ResourcePoolDeprecateOptions { AllowResourceDeprecation = true });
			Assert.IsNotNull(pool);

			resource = TestContext.Api.Resources.Read(resource.Id);
			Assert.IsNotNull(resource);
			Assert.AreEqual(ResourceState.Deprecated, resource.State);

			TestContext.Api.ResourcePools.Delete(pool);

			resource = TestContext.Api.Resources.Read(resource.Id);
			Assert.IsNotNull(resource);
			Assert.AreEqual(0, resource.ResourcePoolIds.Count);

			coreResource = TestContext.ResourceManagerHelper.GetResource(coreResourceId);
			Assert.IsNotNull(coreResource);
			Assert.AreEqual(0, coreResource.PoolGUIDs.Count);
		}
	}
}
