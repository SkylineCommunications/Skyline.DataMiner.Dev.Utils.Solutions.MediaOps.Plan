namespace RT_MediaOps.Plan.Properties.Values
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	public sealed class DomPropertyValueCollectionHandlerTests
	{
		[TestMethod]
		public void GetCollectionsWithDuplicateLinkedObjectAndSubId_DuplicateCombination_ReturnsDuplicateCollections()
		{
			var duplicateA = new PropertyValueCollection { LinkedObjectId = "obj-1", SubId = "sub-1" };
			var duplicateB = new PropertyValueCollection { LinkedObjectId = "obj-1", SubId = "sub-1" };
			var unique = new PropertyValueCollection { LinkedObjectId = "obj-1", SubId = "sub-2" };

			var result = DomPropertyValueCollectionHandler.GetCollectionsWithDuplicateLinkedObjectAndSubId([duplicateA, duplicateB, unique]);

			CollectionAssert.AreEquivalent(new[] { duplicateA.Id, duplicateB.Id }, result.Select(x => x.Id).ToArray());
		}

		[TestMethod]
		public void GetCollectionsWithDuplicateLinkedObjectAndSubId_EmptyLinkedObjectId_IgnoresCollection()
		{
			var invalidA = new PropertyValueCollection { LinkedObjectId = String.Empty, SubId = "sub-1" };
			var invalidB = new PropertyValueCollection { LinkedObjectId = String.Empty, SubId = "sub-1" };

			var result = DomPropertyValueCollectionHandler.GetCollectionsWithDuplicateLinkedObjectAndSubId([invalidA, invalidB]);

			Assert.AreEqual(0, result.Count);
		}

		[TestMethod]
		public void TryCreateOrUpdate_DuplicateLinkedObjectIdAndSubId_ReturnsFailure()
		{
			var duplicateA = new PropertyValueCollection { LinkedObjectId = "obj-1", SubId = "sub-1" };
			var duplicateB = new PropertyValueCollection { LinkedObjectId = "obj-1", SubId = "sub-1" };

			var isSuccess = DomPropertyValueCollectionHandler.TryCreateOrUpdate(null!, [duplicateA, duplicateB], out var result);

			Assert.IsFalse(isSuccess);
			CollectionAssert.AreEquivalent(new[] { duplicateA.Id, duplicateB.Id }, result.UnsuccessfulIds.ToArray());
			Assert.AreEqual("The combination of LinkedObjectId and SubId must be unique.", result.TraceDataPerItem[duplicateA.Id].ErrorData.Single().ErrorMessage);
		}
	}
}
