namespace RT_MediaOps.Plan.RST.Resources
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

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

		[TestMethod]
		public void UpdateWithoutCoreResourceThrowsException()
		{
			var prefix = Guid.NewGuid();

			var unmanagedResource = new UnmanagedResource()
			{
				Name = $"{prefix}_Resource",
			};

			var resource = objectCreator.CreateResource(unmanagedResource) as Resource;
			Assert.IsNotNull(resource);

			// Complete
			resource = TestContext.Api.Resources.Complete(resource.Id);
			var coreResourceId = resource.CoreResourceId;
			Assert.AreNotEqual(Guid.Empty, coreResourceId);

			// Remove the CORE resource, the DOM resource keeps its reference to it.
			TestContext.ResourceManagerHelper.RemoveResources(new Skyline.DataMiner.Net.Messages.Resource(coreResourceId));
			Assert.IsNull(TestContext.ResourceManagerHelper.GetResource(coreResourceId));

			// Update
			resource.Name = $"{prefix}_Resource_Updated";

			MediaOpsException? expectedException = null;
			try
			{
				TestContext.Api.Resources.Update(resource);
			}
			catch (MediaOpsException ex)
			{
				expectedException = ex;
			}

			Assert.IsNotNull(expectedException, "Expected exception was not thrown.");

			var errorMessage = $"The linked CORE resource with ID '{coreResourceId}' no longer exists.";
			Assert.AreEqual(errorMessage, expectedException.Message);

			Assert.AreEqual(1, expectedException.TraceData.ErrorData.Count);
			var resourceNotFoundError = expectedException.TraceData.ErrorData.OfType<ResourceNotFoundError>().SingleOrDefault();
			Assert.IsNotNull(resourceNotFoundError);
			Assert.AreEqual(resource.Id, resourceNotFoundError.Id);
			Assert.AreEqual(errorMessage, resourceNotFoundError.ErrorMessage);
		}
	}
}
