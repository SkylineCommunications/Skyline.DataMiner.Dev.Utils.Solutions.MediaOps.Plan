namespace RT_MediaOps.Plan.RST.Resources
{
	using System;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	[TestCategory("IntegrationTest")]
	public sealed class DraftStateTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public DraftStateTests()
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

			// Update name
			var updatedName = $"{name}_Updated";
			resource.Name = updatedName;

			resource = TestContext.Api.Resources.Update(resource);
			Assert.IsNotNull(resource);
			Assert.AreEqual(updatedName, resource.Name);
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

			// Update concurrency
			resource.Concurrency = 2;

			resource = TestContext.Api.Resources.Update(resource);
			Assert.IsNotNull(resource);
			Assert.AreEqual(2, resource.Concurrency);
		}
	}
}
