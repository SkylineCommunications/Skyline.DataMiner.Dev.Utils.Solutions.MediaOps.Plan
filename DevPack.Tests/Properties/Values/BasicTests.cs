namespace RT_MediaOps.Plan.Properties.Values
{
	using System;
	using System.Linq;

	using RT_MediaOps.Plan.RegressionTests;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	using SLDataGateway.API.Querying;

	[TestClass]
	[TestCategory("IntegrationTest")]
	public sealed class BasicTests
	{
		private static IntegrationTestContext TestContext => TestContextManager.SharedTestContext;

		[TestMethod]
		public void ReadWithEmptyFilterReturnsEmptyList()
		{
			var idsToRetrieve = new Guid[0];
			var emptyFilter = new ORFilterElement<PropertyValueCollection>(idsToRetrieve.Select(x => PropertyValueCollectionExposers.Id.Equal(x)).ToArray());

			var collections = TestContext.Api.PropertyValueCollections.Read(emptyFilter);
			Assert.IsNotNull(collections);
			Assert.AreEqual(0, collections.Count());
		}

		[TestMethod]
		public void ReadWithEmptyQueryReturnsEmptyList()
		{
			var idsToRetrieve = new Guid[0];
			var emptyFilter = new ORFilterElement<PropertyValueCollection>(idsToRetrieve.Select(x => PropertyValueCollectionExposers.Id.Equal(x)).ToArray());
			var queryWithEmptyFilter = emptyFilter.ToQuery();

			var collections = TestContext.Api.PropertyValueCollections.Read(queryWithEmptyFilter);
			Assert.IsNotNull(collections);
			Assert.AreEqual(0, collections.Count());
		}
	}
}
