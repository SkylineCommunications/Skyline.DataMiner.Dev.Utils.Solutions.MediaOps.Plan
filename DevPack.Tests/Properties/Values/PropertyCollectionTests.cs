namespace RT_MediaOps.Plan.Properties.Values
{
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	public sealed class PropertyCollectionTests
	{
		[TestMethod]
		public void PropertyCollection_InitLinkedObjectId_ValueIsSet()
		{
			var collection = new PropertyValueCollection { LinkedObjectId = "obj-1" };

			Assert.AreEqual("obj-1", collection.LinkedObjectId);
		}

		[TestMethod]
		public void PropertyCollection_InitScope_ValueIsSet()
		{
			var collection = new PropertyValueCollection { Scope = "global" };

			Assert.AreEqual("global", collection.Scope);
		}

		[TestMethod]
		public void PropertyCollection_InitSubId_ValueIsSet()
		{
			var collection = new PropertyValueCollection { SubId = "sub-1" };

			Assert.AreEqual("sub-1", collection.SubId);
		}

		[TestMethod]
		public void PropertyCollection_InitAllProperties_ValuesAreSet()
		{
			var collection = new PropertyValueCollection
			{
				LinkedObjectId = "obj-1",
				Scope = "global",
				SubId = "sub-1",
			};

			Assert.AreEqual("obj-1", collection.LinkedObjectId);
			Assert.AreEqual("global", collection.Scope);
			Assert.AreEqual("sub-1", collection.SubId);
		}

		[TestMethod]
		public void PropertyCollection_DefaultConstructor_PropertiesAreNull()
		{
			var collection = new PropertyValueCollection();

			Assert.IsNull(collection.LinkedObjectId);
			Assert.IsNull(collection.Scope);
			Assert.IsNull(collection.SubId);
		}
	}
}
