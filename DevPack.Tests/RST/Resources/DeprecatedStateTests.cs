namespace RT_MediaOps.Plan.RST.Resources
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	[TestClass]
	[TestCategory("IntegrationTest")]
	public sealed class DeprecatedStateTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public DeprecatedStateTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void UpdateNameThrowsException()
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

			// Deprecate
			resource = TestContext.Api.Resources.Deprecate(resource.Id);

			Assert.AreEqual(coreResourceId, resource.CoreResourceId);
			coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);
			Assert.IsNotNull(coreResource);
			Assert.AreEqual(Skyline.DataMiner.Net.Messages.ResourceMode.Unavailable, coreResource.Mode);
			Assert.AreEqual(name, coreResource.Name);

			// Update name
			var updatedName = $"{name}_Updated";
			resource.Name = updatedName;

			MediaOpsException? expectedException = null;
			try
			{
				resource = TestContext.Api.Resources.Update(resource);
			}
			catch (MediaOpsException ex)
			{
				expectedException = ex;
			}

			Assert.IsNotNull(expectedException, "Expected exception was not thrown.");

			var errorMessage = "Not allowed to update a resource in Deprecated state.";
			Assert.AreEqual(errorMessage, expectedException.Message);

			Assert.AreEqual(1, expectedException.TraceData.ErrorData.Count);
			var resourceError = expectedException.TraceData.ErrorData.OfType<ResourceError>().SingleOrDefault();
			Assert.IsNotNull(resourceError);

			var resourceInvalidStateError = resourceError as ResourceInvalidStateError;
			Assert.IsNotNull(resourceInvalidStateError);
			Assert.AreEqual(resource.Id, resourceInvalidStateError.Id);
			Assert.AreEqual(errorMessage, resourceInvalidStateError.ErrorMessage);
		}

		[TestMethod]
		public void UpdateConcurrencyThrowsException()
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

			// Deprecate
			resource = TestContext.Api.Resources.Deprecate(resource.Id);

			Assert.AreEqual(coreResourceId, resource.CoreResourceId);
			coreResource = TestContext.ResourceManagerHelper.GetResource(resource.CoreResourceId);
			Assert.IsNotNull(coreResource);
			Assert.AreEqual(Skyline.DataMiner.Net.Messages.ResourceMode.Unavailable, coreResource.Mode);
			Assert.AreEqual(1, coreResource.MaxConcurrency);

			// Update concurrency
			resource.Concurrency = 2;

			MediaOpsException? expectedException = null;
			try
			{
				resource = TestContext.Api.Resources.Update(resource);
			}
			catch (MediaOpsException ex)
			{
				expectedException = ex;
			}

			Assert.IsNotNull(expectedException, "Expected exception was not thrown.");

			var errorMessage = "Not allowed to update a resource in Deprecated state.";
			Assert.AreEqual(errorMessage, expectedException.Message);

			Assert.AreEqual(1, expectedException.TraceData.ErrorData.Count);
			var resourceError = expectedException.TraceData.ErrorData.OfType<ResourceError>().SingleOrDefault();
			Assert.IsNotNull(resourceError);

			var resourceInvalidStateError = resourceError as ResourceInvalidStateError;
			Assert.IsNotNull(resourceInvalidStateError);
			Assert.AreEqual(resource.Id, resourceInvalidStateError.Id);
			Assert.AreEqual(errorMessage, resourceInvalidStateError.ErrorMessage);
		}
	}
}
