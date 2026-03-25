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
	public sealed class DeprecateTests : IDisposable
	{
		private readonly TestObjectCreator objectCreator;

		public DeprecateTests()
		{
			objectCreator = new TestObjectCreator(TestContext);
		}

		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		public void Dispose()
		{
			objectCreator.Dispose();
		}

		[TestMethod]
		public void WhenReferencedAsLinkedPoolThrowsException()
		{
			var prefix = Guid.NewGuid();

			var pool1 = new ResourcePool()
			{
				Name = $"{prefix}_Pool1",
			};
			var pool2 = new ResourcePool()
			{
				Name = $"{prefix}_Pool2",
			};
			var createdPools = objectCreator.CreateResourcePools([pool1, pool2]);

			pool2 = createdPools.Single(x => x.Id == pool2.Id);
			pool2.AddLinkedResourcePool(new LinkedResourcePool(pool1));
			pool2 = TestContext.Api.ResourcePools.Update(pool2);

			TestContext.Api.ResourcePools.Complete(pool1);

			MediaOpsException? expectedException = null;
			try
			{
				TestContext.Api.ResourcePools.Deprecate(pool1);
			}
			catch (MediaOpsException ex)
			{
				expectedException = ex;
			}

			Assert.IsNotNull(expectedException, "Expected exception was not thrown.");

			var errorMessage = $"Resource pool '{pool1.Name}' is in use by 1 linked resource pool(s).";
			Assert.AreEqual(errorMessage, expectedException.Message);

			Assert.AreEqual(1, expectedException.TraceData.ErrorData.Count);
			var resourcePoolError = expectedException.TraceData.ErrorData.OfType<ResourcePoolError>().SingleOrDefault();
			Assert.IsNotNull(resourcePoolError);

			var resourcePoolInuseByLinkedPoolsError = resourcePoolError as ResourcePoolInUseByLinkedPoolsError;
			Assert.IsNotNull(resourcePoolInuseByLinkedPoolsError);
			Assert.AreEqual(pool1.Id, resourcePoolInuseByLinkedPoolsError.Id);
			Assert.AreEqual(errorMessage, resourcePoolInuseByLinkedPoolsError.ErrorMessage);
			Assert.AreEqual(1, resourcePoolInuseByLinkedPoolsError.LinkedResourcePoolIds.Count);
			Assert.IsTrue(resourcePoolInuseByLinkedPoolsError.LinkedResourcePoolIds.Contains(pool2.Id));

			var domResourcePool2 = TestContext.ResourceStudioDomHelper.DomInstances.Read(DomInstanceExposers.Id.Equal(pool2.Id)).SingleOrDefault();
			Assert.IsNotNull(domResourcePool2);
			Assert.AreEqual(1, domResourcePool2.Sections.Count(x => x.SectionDefinitionID.Id == Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Sections.ResourcePoolLinks.Id.Id));
		}
	}
}
