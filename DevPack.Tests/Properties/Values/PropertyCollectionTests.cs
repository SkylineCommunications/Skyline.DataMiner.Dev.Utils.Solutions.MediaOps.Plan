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
			var collection = new PropertySettingCollection();

			Assert.AreNotEqual(Guid.Empty, collection.Id);
		}

		[TestMethod]
		public void DefaultConstructor_NameIsNull()
		{
			var collection = new PropertySettingCollection();

			Assert.IsNotNull(collection.CustomSettings);
			Assert.AreEqual(0, collection.CustomSettings.Count);
		}

		[TestMethod]
		public void DefaultConstructor_SettingCollectionsAreEmptyButNotNull()
		{
			var collection = new PropertySettingCollection();

			Assert.IsNotNull(collection.CustomSettings);
			Assert.IsNotNull(collection.StringSettings);
			Assert.IsNotNull(collection.BooleanSettings);
			Assert.IsNotNull(collection.DiscreteSettings);
			Assert.IsNotNull(collection.PropertySettings);

			Assert.AreEqual(0, collection.CustomSettings.Count);
			Assert.AreEqual(0, collection.StringSettings.Count);
			Assert.AreEqual(0, collection.BooleanSettings.Count);
			Assert.AreEqual(0, collection.DiscreteSettings.Count);
			Assert.AreEqual(0, collection.PropertySettings.Count);
		}

		[TestMethod]
		public void PropertyCollection_InitLinkedObjectId_LinkedObjectIdIsSet()
		{
			var collection = new PropertySettingCollection { LinkedObjectId = "obj-1" };

			Assert.AreEqual("obj-1", collection.LinkedObjectId);
		}

		[TestMethod]
		public void PropertyCollection_InitScope_ScopeIsSet()
		{
			var collection = new PropertySettingCollection { Scope = "global" };

			Assert.AreEqual("global", collection.Scope);
		}

		[TestMethod]
		public void PropertyCollection_InitSubId_SubIdIsSet()
		{
			var collection = new PropertySettingCollection { SubId = "sub-1" };

			Assert.AreEqual("sub-1", collection.SubId);
		}

		[TestMethod]
		public void PropertyCollection_InitAllProperties_AllPropertiesAreSet()
		{
			var collection = new PropertySettingCollection
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
			var collection = new PropertySettingCollection();

			Assert.IsNull(collection.LinkedObjectId);
			Assert.IsNull(collection.Scope);
			Assert.IsNull(collection.SubId);
			Assert.AreEqual(0, collection.Count);
		}

		[TestMethod]
		public void ICollection_IsReadOnly_ReturnsFalse()
		{
			var collection = new PropertySettingCollection();

			Assert.IsFalse(collection.IsReadOnly);
		}

		[TestMethod]
		public void ICollection_Add_CustomPropertySetting_IncreasesCount()
		{
			var collection = new PropertySettingCollection();
			collection.Add(new CustomPropertySetting("myProp"));

			Assert.AreEqual(1, collection.Count);
			Assert.AreEqual(1, collection.CustomSettings.Count);
		}

		[TestMethod]
		public void ICollection_Add_StringPropertySetting_IncreasesCount()
		{
			var collection = new PropertySettingCollection();
			collection.Add(new StringPropertySetting(new StringProperty()));

			Assert.AreEqual(1, collection.Count);
			Assert.AreEqual(1, collection.StringSettings.Count);
		}

		[TestMethod]
		public void ICollection_Add_BooleanPropertySetting_IncreasesCount()
		{
			var collection = new PropertySettingCollection();
			collection.Add(new BooleanPropertySetting(new BooleanProperty()));

			Assert.AreEqual(1, collection.Count);
			Assert.AreEqual(1, collection.BooleanSettings.Count);
		}

		[TestMethod]
		public void ICollection_Add_DiscretePropertySetting_IncreasesCount()
		{
			var collection = new PropertySettingCollection();
			collection.Add(new DiscretePropertySetting(new DiscreteProperty()));

			Assert.AreEqual(1, collection.Count);
			Assert.AreEqual(1, collection.DiscreteSettings.Count);
		}

		[TestMethod]
		public void ICollection_Add_Null_Throws()
		{
			var collection = new PropertySettingCollection();

			Assert.ThrowsException<ArgumentNullException>(() => collection.Add(null));
		}

		[TestMethod]
		public void ICollection_Add_UnsupportedPropertySettingType_Throws()
		{
			var collection = new PropertySettingCollection();
			var unsupportedSetting = new UnsupportedPropertySetting();

			var exception = Assert.ThrowsException<ArgumentException>(() => collection.Add(unsupportedSetting));
			StringAssert.Contains(exception.Message, "Unsupported property setting type");
		}

		[TestMethod]
		public void ICollection_Remove_CustomPropertySetting_DecreasesCount()
		{
			var item = new CustomPropertySetting("myProp");
			var collection = new PropertySettingCollection();
			collection.Add(item);

			var removed = collection.Remove(item);

			Assert.IsTrue(removed);
			Assert.AreEqual(0, collection.Count);
		}

		[TestMethod]
		public void ICollection_Remove_Null_ReturnsFalse()
		{
			var collection = new PropertySettingCollection();

			Assert.IsFalse(collection.Remove(null));
		}

		[TestMethod]
		public void ICollection_Contains_ExistingItem_ReturnsTrue()
		{
			var item = new CustomPropertySetting("myProp");
			var collection = new PropertySettingCollection();
			collection.Add(item);

			Assert.IsTrue(collection.Contains(item));
		}

		[TestMethod]
		public void ICollection_Contains_MissingItem_ReturnsFalse()
		{
			var collection = new PropertySettingCollection();

			Assert.IsFalse(collection.Contains(new CustomPropertySetting("myProp")));
		}

		[TestMethod]
		public void ICollection_Contains_Null_ReturnsFalse()
		{
			var collection = new PropertySettingCollection();

			Assert.IsFalse(collection.Contains(null));
		}

		[TestMethod]
		public void ICollection_Clear_RemovesAllItems()
		{
			var collection = new PropertySettingCollection();
			collection.Add(new CustomPropertySetting("myProp"));
			collection.Add(new StringPropertySetting(new StringProperty()));

			collection.Clear();

			Assert.AreEqual(0, collection.Count);
		}

		[TestMethod]
		public void ICollection_GetEnumerator_ReturnsAllItems()
		{
			var custom = new CustomPropertySetting("myProp");
			var str = new StringPropertySetting(new StringProperty());
			var collection = new PropertySettingCollection();
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
			var item = new CustomPropertySetting("myProp");
			var collection = new PropertySettingCollection();
			collection.Add(item);

			var array = new PropertySettingBase[1];
			collection.CopyTo(array, 0);

			Assert.AreEqual(item, array[0]);
		}

		private sealed class UnsupportedPropertySetting : PropertySettingBase
		{
			public UnsupportedPropertySetting()
				: base(true)
			{
			}
		}
	}
}
