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
		public void DeprecateWithoutCoreResourceThrowsException()
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

			// Deprecate
			MediaOpsException? expectedException = null;
			try
			{
				TestContext.Api.Resources.Deprecate(resource.Id);
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

		[TestMethod]
		public void BulkDeprecateWithoutCoreResourceThrowsException()
		{
			var prefix = Guid.NewGuid();

			var firstResource = objectCreator.CreateResource(new UnmanagedResource() { Name = $"{prefix}_Resource_1" }) as Resource;
			var secondResource = objectCreator.CreateResource(new UnmanagedResource() { Name = $"{prefix}_Resource_2" }) as Resource;
			Assert.IsNotNull(firstResource);
			Assert.IsNotNull(secondResource);

			// Complete
			var completedResources = TestContext.Api.Resources.Complete(new[] { firstResource.Id, secondResource.Id });
			Assert.AreEqual(2, completedResources.Count);

			var validResource = completedResources.Single(x => x.Id == firstResource.Id);
			var invalidResource = completedResources.Single(x => x.Id == secondResource.Id);

			var coreResourceId = invalidResource.CoreResourceId;
			Assert.AreNotEqual(Guid.Empty, coreResourceId);

			// Remove the CORE resource of one of the resources, the DOM resource keeps its reference to it.
			TestContext.ResourceManagerHelper.RemoveResources(new Skyline.DataMiner.Net.Messages.Resource(coreResourceId));
			Assert.IsNull(TestContext.ResourceManagerHelper.GetResource(coreResourceId));

			// Deprecate
			MediaOpsBulkException<Guid>? expectedException = null;
			try
			{
				TestContext.Api.Resources.Deprecate(new[] { validResource.Id, invalidResource.Id });
			}
			catch (MediaOpsBulkException<Guid> ex)
			{
				expectedException = ex;
			}

			Assert.IsNotNull(expectedException, "Expected exception was not thrown.");

			var errorMessage = $"The linked CORE resource with ID '{coreResourceId}' no longer exists.";
			Assert.AreEqual(errorMessage, expectedException.Message);

			Assert.AreEqual(1, expectedException.Result.UnsuccessfulIds.Count);
			Assert.AreEqual(invalidResource.Id, expectedException.Result.UnsuccessfulIds.Single());

			Assert.AreEqual(1, expectedException.Result.SuccessfulIds.Count);
			Assert.AreEqual(validResource.Id, expectedException.Result.SuccessfulIds.Single());

			Assert.AreEqual(1, expectedException.Result.TraceDataPerItem.Count);
			var traceData = expectedException.Result.TraceDataPerItem[invalidResource.Id];
			Assert.AreEqual(1, traceData.ErrorData.Count);

			var resourceNotFoundError = traceData.ErrorData.OfType<ResourceNotFoundError>().SingleOrDefault();
			Assert.IsNotNull(resourceNotFoundError);
			Assert.AreEqual(invalidResource.Id, resourceNotFoundError.Id);
			Assert.AreEqual(errorMessage, resourceNotFoundError.ErrorMessage);

			// The resource with a valid CORE resource is deprecated, the other one is untouched.
			Assert.AreEqual(ResourceState.Deprecated, TestContext.Api.Resources.Read(validResource.Id).State);
			Assert.AreEqual(ResourceState.Complete, TestContext.Api.Resources.Read(invalidResource.Id).State);
		}

		[TestMethod]
		public void BulkDeprecateWithoutAnyCoreResourceThrowsException()
		{
			var prefix = Guid.NewGuid();

			var firstResource = objectCreator.CreateResource(new UnmanagedResource() { Name = $"{prefix}_Resource_1" }) as Resource;
			var secondResource = objectCreator.CreateResource(new UnmanagedResource() { Name = $"{prefix}_Resource_2" }) as Resource;
			Assert.IsNotNull(firstResource);
			Assert.IsNotNull(secondResource);

			// Complete
			var completedResources = TestContext.Api.Resources.Complete(new[] { firstResource.Id, secondResource.Id });
			Assert.AreEqual(2, completedResources.Count);

			// Remove all CORE resources, the DOM resources keep their reference to them.
			foreach (var completedResource in completedResources)
			{
				Assert.AreNotEqual(Guid.Empty, completedResource.CoreResourceId);
				TestContext.ResourceManagerHelper.RemoveResources(new Skyline.DataMiner.Net.Messages.Resource(completedResource.CoreResourceId));
				Assert.IsNull(TestContext.ResourceManagerHelper.GetResource(completedResource.CoreResourceId));
			}

			// Deprecate
			MediaOpsBulkException<Guid>? expectedException = null;
			try
			{
				TestContext.Api.Resources.Deprecate(completedResources.Select(x => x.Id).ToList());
			}
			catch (MediaOpsBulkException<Guid> ex)
			{
				expectedException = ex;
			}

			Assert.IsNotNull(expectedException, "Expected exception was not thrown.");

			Assert.AreEqual(0, expectedException.Result.SuccessfulIds.Count);
			CollectionAssert.AreEquivalent(
				completedResources.Select(x => x.Id).ToList(),
				expectedException.Result.UnsuccessfulIds.ToList());

			// Reading the message of a bulk exception with more than one failure must not recurse into ToString().
			var message = expectedException.Message;

			foreach (var completedResource in completedResources)
			{
				var traceData = expectedException.Result.TraceDataPerItem[completedResource.Id];
				var resourceNotFoundError = traceData.ErrorData.OfType<ResourceNotFoundError>().SingleOrDefault();
				Assert.IsNotNull(resourceNotFoundError);
				Assert.AreEqual(completedResource.Id, resourceNotFoundError.Id);
				Assert.AreEqual($"The linked CORE resource with ID '{completedResource.CoreResourceId}' no longer exists.", resourceNotFoundError.ErrorMessage);

				StringAssert.Contains(message, resourceNotFoundError.ErrorMessage);

				Assert.AreEqual(ResourceState.Complete, TestContext.Api.Resources.Read(completedResource.Id).State);
			}
		}

		[TestMethod]
		public void RestoreWithoutCoreResourceThrowsException()
		{
			var prefix = Guid.NewGuid();

			var unmanagedResource = new UnmanagedResource()
			{
				Name = $"{prefix}_Resource",
			};

			var resource = objectCreator.CreateResource(unmanagedResource) as Resource;
			Assert.IsNotNull(resource);

			// Complete & deprecate
			resource = TestContext.Api.Resources.Complete(resource.Id);
			var coreResourceId = resource.CoreResourceId;
			Assert.AreNotEqual(Guid.Empty, coreResourceId);

			resource = TestContext.Api.Resources.Deprecate(resource.Id);
			Assert.AreEqual(ResourceState.Deprecated, resource.State);

			// Remove the CORE resource, the DOM resource keeps its reference to it.
			TestContext.ResourceManagerHelper.RemoveResources(new Skyline.DataMiner.Net.Messages.Resource(coreResourceId));
			Assert.IsNull(TestContext.ResourceManagerHelper.GetResource(coreResourceId));

			// Restore
			MediaOpsException? expectedException = null;
			try
			{
				TestContext.Api.Resources.Restore(resource.Id);
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
