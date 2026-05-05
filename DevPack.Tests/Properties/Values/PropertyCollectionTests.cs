namespace RT_MediaOps.Plan.Properties.Values
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	public sealed class PropertyCollectionTests
	{
		[TestMethod]
		public void DefaultConstructor_GeneratesId()
		{
			var collection = new PropertyValueCollection();

			Assert.AreNotEqual(Guid.Empty, collection.Id);
		}

		[TestMethod]
		public void DefaultConstructor_NameIsNull()
		{
			var collection = new PropertyValueCollection();

			Assert.IsNull(collection.Name);
			Assert.IsNotNull(collection.CustomValues);
			Assert.AreEqual(0, collection.CustomValues.Count);
		}

		[TestMethod]
		public void SetName_ValueIsSet()
		{
			var collection = new PropertyValueCollection { Name = "MyCollection" };

			Assert.AreEqual("MyCollection", collection.Name);
		}

		[TestMethod]
		public void DefaultConstructor_ValueCollectionsAreEmptyButNotNull()
		{
			var collection = new PropertyValueCollection();

			Assert.IsNotNull(collection.CustomValues);
			Assert.IsNotNull(collection.StringValues);
			Assert.IsNotNull(collection.BooleanValues);
			Assert.IsNotNull(collection.DiscreteValues);
			Assert.IsNotNull(collection.PropertyValues);

			Assert.AreEqual(0, collection.CustomValues.Count);
			Assert.AreEqual(0, collection.StringValues.Count);
			Assert.AreEqual(0, collection.BooleanValues.Count);
			Assert.AreEqual(0, collection.DiscreteValues.Count);
			Assert.AreEqual(0, collection.PropertyValues.Count);
		}

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
			Assert.AreEqual(0, collection.Count);
		}

		[TestMethod]
		public void ICollection_IsReadOnly_ReturnsFalse()
		{
			var collection = new PropertyValueCollection();

			Assert.IsFalse(collection.IsReadOnly);
		}

		[TestMethod]
		public void ICollection_Add_CustomPropertyValue_IncreasesCount()
		{
			var collection = new PropertyValueCollection();
			collection.Add(new CustomPropertyValue("myProp"));

			Assert.AreEqual(1, collection.Count);
			Assert.AreEqual(1, collection.CustomValues.Count);
		}

		[TestMethod]
		public void ICollection_Add_StringPropertyValue_IncreasesCount()
		{
			var collection = new PropertyValueCollection();
			collection.Add(new StringPropertyValue(new StringProperty()));

			Assert.AreEqual(1, collection.Count);
			Assert.AreEqual(1, collection.StringValues.Count);
		}

		[TestMethod]
		public void ICollection_Add_BooleanPropertyValue_IncreasesCount()
		{
			var collection = new PropertyValueCollection();
			collection.Add(new BooleanPropertyValue(new BooleanProperty()));

			Assert.AreEqual(1, collection.Count);
			Assert.AreEqual(1, collection.BooleanValues.Count);
		}

		[TestMethod]
		public void ICollection_Add_DiscretePropertyValue_IncreasesCount()
		{
			var collection = new PropertyValueCollection();
			collection.Add(new DiscretePropertyValue(new DiscreteProperty()));

			Assert.AreEqual(1, collection.Count);
			Assert.AreEqual(1, collection.DiscreteValues.Count);
		}

		[TestMethod]
		public void ICollection_Add_Null_Throws()
		{
			var collection = new PropertyValueCollection();

			Assert.ThrowsException<ArgumentNullException>(() => collection.Add(null));
		}

		[TestMethod]
		public void ICollection_Remove_CustomPropertyValue_DecreasesCount()
		{
			var item = new CustomPropertyValue("myProp");
			var collection = new PropertyValueCollection();
			collection.Add(item);

			var removed = collection.Remove(item);

			Assert.IsTrue(removed);
			Assert.AreEqual(0, collection.Count);
		}

		[TestMethod]
		public void ICollection_Remove_Null_ReturnsFalse()
		{
			var collection = new PropertyValueCollection();

			Assert.IsFalse(collection.Remove(null));
		}

		[TestMethod]
		public void ICollection_Contains_ExistingItem_ReturnsTrue()
		{
			var item = new CustomPropertyValue("myProp");
			var collection = new PropertyValueCollection();
			collection.Add(item);

			Assert.IsTrue(collection.Contains(item));
		}

		[TestMethod]
		public void ICollection_Contains_MissingItem_ReturnsFalse()
		{
			var collection = new PropertyValueCollection();

			Assert.IsFalse(collection.Contains(new CustomPropertyValue("myProp")));
		}

		[TestMethod]
		public void ICollection_Contains_Null_ReturnsFalse()
		{
			var collection = new PropertyValueCollection();

			Assert.IsFalse(collection.Contains(null));
		}

		[TestMethod]
		public void ICollection_Clear_RemovesAllItems()
		{
			var collection = new PropertyValueCollection();
			collection.Add(new CustomPropertyValue("myProp"));
			collection.Add(new StringPropertyValue(new StringProperty()));

			collection.Clear();

			Assert.AreEqual(0, collection.Count);
		}

		[TestMethod]
		public void ICollection_GetEnumerator_ReturnsAllItems()
		{
			var custom = new CustomPropertyValue("myProp");
			var str = new StringPropertyValue(new StringProperty());
			var collection = new PropertyValueCollection();
			collection.Add(custom);
			collection.Add(str);

			var items = collection.ToList();

			Assert.AreEqual(2, items.Count);
			CollectionAssert.Contains(items, custom);
			CollectionAssert.Contains(items, str);
		}

		[TestMethod]
		public void ICollection_CopyTo_CopiesAllItems()
		{
			var item = new CustomPropertyValue("myProp");
			var collection = new PropertyValueCollection();
			collection.Add(item);

			var array = new PropertyValueBase[1];
			collection.CopyTo(array, 0);

			Assert.AreEqual(item, array[0]);
		}
	}
}
