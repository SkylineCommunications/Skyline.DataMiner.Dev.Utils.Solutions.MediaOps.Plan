namespace RT_MediaOps.Plan.RST.ResourcePools
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

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
		public void AddLinkedPoolWithDeprecatedPoolThrowsException()
		{
			var prefix = Guid.NewGuid();

			var pool1 = new ResourcePool()
			{
				Name = $"{prefix}_Pool1",
			};
			pool1 = objectCreator.CreateResourcePool(pool1);
			pool1 = TestContext.Api.ResourcePools.Complete(pool1);
			pool1 = TestContext.Api.ResourcePools.Deprecate(pool1);

			var pool2 = new ResourcePool()
			{
				Name = $"{prefix}_Pool2",
			}
			.AddLinkedResourcePool(new LinkedResourcePool(pool1));

			MediaOpsException? expectedException = null;
			try
			{
				objectCreator.CreateResourcePool(pool2);

			}
			catch (MediaOpsException ex)
			{
				expectedException = ex;
			}

			Assert.IsNotNull(expectedException, "Expected exception was not thrown.");

			var errorMessage = $"Linked resource pool with ID '{pool1.Id}' is deprecated.";
			Assert.AreEqual(errorMessage, expectedException.Message);

			Assert.AreEqual(1, expectedException.TraceData.ErrorData.Count);
			var resourcePoolError = expectedException.TraceData.ErrorData.OfType<ResourcePoolError>().SingleOrDefault();
			Assert.IsNotNull(resourcePoolError);

			var resourcePoolInvalidStatePoolLinkError = resourcePoolError as ResourcePoolInvalidStatePoolLinkError;
			Assert.IsNotNull(resourcePoolInvalidStatePoolLinkError);
			Assert.AreEqual(pool2.Id, resourcePoolInvalidStatePoolLinkError.Id);
			Assert.AreEqual(errorMessage, resourcePoolInvalidStatePoolLinkError.ErrorMessage);
			Assert.AreEqual(pool1.Id, resourcePoolInvalidStatePoolLinkError.LinkedResourcePoolId);

			var domResourcePool2 = TestContext.ResourceStudioDomHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(pool2.Id)).SingleOrDefault();
			Assert.IsNull(domResourcePool2);
		}
	}
}