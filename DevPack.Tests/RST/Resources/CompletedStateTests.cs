namespace RT_MediaOps.Plan.RST.Resources
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	[TestCategory("IntegrationTest")]
	public sealed class CompletedStateTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public CompletedStateTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void UpdateName()
		{
			var prefix = Guid.NewGuid();
			var name = $"{prefix}_Resource";

			var unmanagedResource = new UnmanagedResource()
			{
				Name = name,
			};

			var resource = objectCreator.CreateResource(unmanagedResource) as Resource;
			Assert.IsNotNull(resource);
			Assert.AreEqual(name, resource.Name);

			// Complete
			resource = TestContext.Api.Resources.Complete(resource.Id);
			var coreResourceId = resource.CoreResourceId;
			Assert.AreNotEqual(Guid.Empty, coreResourceId);

			var coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);
			Assert.IsNotNull(coreResource);
			Assert.AreEqual(name, coreResource.Name);

			// Update name
			var updatedName = $"{name}_Updated";
			resource.Name = updatedName;

			resource = TestContext.Api.Resources.Update(resource);
			Assert.IsNotNull(resource);
			Assert.AreEqual(updatedName, resource.Name);

			Assert.AreEqual(coreResourceId, resource.CoreResourceId);
			coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);
			Assert.IsNotNull(coreResource);
			Assert.AreEqual(updatedName, coreResource.Name);
		}

		[TestMethod]
		public void UpdateConcurrency()
		{
			var prefix = Guid.NewGuid();

			var unmanagedResource = new UnmanagedResource()
			{
				Name = $"{prefix}_Resource",
			};
			var resource = objectCreator.CreateResource(unmanagedResource) as Resource;
			Assert.IsNotNull(resource);
			Assert.AreEqual(1, resource.Concurrency);

			// Complete
			resource = TestContext.Api.Resources.Complete(resource.Id);
			var coreResourceId = resource.CoreResourceId;
			Assert.AreNotEqual(Guid.Empty, coreResourceId);

			var coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);
			Assert.IsNotNull(coreResource);
			Assert.AreEqual(1, coreResource.MaxConcurrency);

			// Update concurrency
			resource.Concurrency = 2;

			resource = TestContext.Api.Resources.Update(resource);
			Assert.IsNotNull(resource);
			Assert.AreEqual(2, resource.Concurrency);

			Assert.AreEqual(coreResourceId, resource.CoreResourceId);
			coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);
			Assert.IsNotNull(coreResource);
			Assert.AreEqual(2, coreResource.MaxConcurrency);
		}
	}
}
