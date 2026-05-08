namespace RT_MediaOps.Plan.Properties.Values
{
	using System;
	using System.Collections.Generic;
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
			Assert.IsNotNull(collection.LinkedValues);

			Assert.AreEqual(0, collection.CustomValues.Count);
			Assert.AreEqual(0, collection.StringValues.Count);
			Assert.AreEqual(0, collection.BooleanValues.Count);
			Assert.AreEqual(0, collection.DiscreteValues.Count);
			Assert.AreEqual(0, collection.LinkedValues.Count());
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
		public void Add_NullItem_ThrowsArgumentNullException()
		{
			var collection = new PropertyValueCollection();

			Assert.ThrowsException<ArgumentNullException>(() => collection.Add(null));
		}

		[TestMethod]
		public void Add_UnsupportedType_ThrowsArgumentException()
		{
			var collection = new PropertyValueCollection();

			Assert.ThrowsException<ArgumentException>(() => collection.Add(new UnsupportedPropertyValue()));
		}

		[TestMethod]
		public void Add_StringPropertyValue_AppearsInStringValuesAndIncreasesCount()
		{
			var collection = new PropertyValueCollection();
			var value = new StringPropertyValue(new StringProperty());

			collection.Add(value);

			Assert.AreEqual(1, collection.StringValues.Count);
			Assert.AreEqual(1, collection.Count);
			Assert.IsTrue(collection.StringValues.Contains(value));
		}

		[TestMethod]
		public void Add_BooleanPropertyValue_AppearsInBooleanValuesAndIncreasesCount()
		{
			var collection = new PropertyValueCollection();
			var value = new BooleanPropertyValue(new BooleanProperty());

			collection.Add(value);

			Assert.AreEqual(1, collection.BooleanValues.Count);
			Assert.AreEqual(1, collection.Count);
			Assert.IsTrue(collection.BooleanValues.Contains(value));
		}

		[TestMethod]
		public void Add_DiscretePropertyValue_AppearsInDiscreteValuesAndIncreasesCount()
		{
			var collection = new PropertyValueCollection();
			var value = new DiscretePropertyValue(new DiscreteProperty());

			collection.Add(value);

			Assert.AreEqual(1, collection.DiscreteValues.Count);
			Assert.AreEqual(1, collection.Count);
			Assert.IsTrue(collection.DiscreteValues.Contains(value));
		}

		[TestMethod]
		public void Add_CustomPropertyValue_AppearsInCustomValuesAndIncreasesCount()
		{
			var collection = new PropertyValueCollection();
			var value = new CustomPropertyValue("MyCustom");

			collection.Add(value);

			Assert.AreEqual(1, collection.CustomValues.Count);
			Assert.AreEqual(1, collection.Count);
			Assert.IsTrue(collection.CustomValues.Contains(value));
		}

		[TestMethod]
		public void Add_DuplicateStringPropertyValue_ThrowsArgumentException()
		{
			var property = new StringProperty();
			var collection = new PropertyValueCollection();
			collection.Add(new StringPropertyValue(property));

			Assert.ThrowsException<ArgumentException>(() => collection.Add(new StringPropertyValue(property)));
		}

		[TestMethod]
		public void Add_DuplicateBooleanPropertyValue_ThrowsArgumentException()
		{
			var property = new BooleanProperty();
			var collection = new PropertyValueCollection();
			collection.Add(new BooleanPropertyValue(property));

			Assert.ThrowsException<ArgumentException>(() => collection.Add(new BooleanPropertyValue(property)));
		}

		[TestMethod]
		public void Add_DuplicateDiscretePropertyValue_ThrowsArgumentException()
		{
			var property = new DiscreteProperty();
			var collection = new PropertyValueCollection();
			collection.Add(new DiscretePropertyValue(property));

			Assert.ThrowsException<ArgumentException>(() => collection.Add(new DiscretePropertyValue(property)));
		}

		[TestMethod]
		public void Add_DuplicateCustomPropertyValue_ThrowsArgumentException()
		{
			var collection = new PropertyValueCollection();
			collection.Add(new CustomPropertyValue("MyCustom"));

			Assert.ThrowsException<ArgumentException>(() => collection.Add(new CustomPropertyValue("MyCustom")));
		}

		[TestMethod]
		public void Add_SameStringPropertyValueInstance_ThrowsArgumentException()
		{
			var value = new StringPropertyValue(new StringProperty());
			var collection = new PropertyValueCollection();
			collection.Add(value);

			Assert.ThrowsException<ArgumentException>(() => collection.Add(value));
		}

		[TestMethod]
		public void Add_SameBooleanPropertyValueInstance_ThrowsArgumentException()
		{
			var value = new BooleanPropertyValue(new BooleanProperty());
			var collection = new PropertyValueCollection();
			collection.Add(value);

			Assert.ThrowsException<ArgumentException>(() => collection.Add(value));
		}

		[TestMethod]
		public void Add_SameDiscretePropertyValueInstance_ThrowsArgumentException()
		{
			var value = new DiscretePropertyValue(new DiscreteProperty());
			var collection = new PropertyValueCollection();
			collection.Add(value);

			Assert.ThrowsException<ArgumentException>(() => collection.Add(value));
		}

		[TestMethod]
		public void Add_SameCustomPropertyValueInstance_ThrowsArgumentException()
		{
			var value = new CustomPropertyValue("MyCustom");
			var collection = new PropertyValueCollection();
			collection.Add(value);

			Assert.ThrowsException<ArgumentException>(() => collection.Add(value));
		}

		[TestMethod]
		public void Remove_ExistingStringPropertyValue_ReturnsTrueAndDecreasesCount()
		{
			var collection = new PropertyValueCollection();
			var value = new StringPropertyValue(new StringProperty());
			collection.Add(value);

			var result = collection.Remove(value);

			Assert.IsTrue(result);
			Assert.AreEqual(0, collection.Count);
		}

		[TestMethod]
		public void Remove_ExistingBooleanPropertyValue_ReturnsTrueAndDecreasesCount()
		{
			var collection = new PropertyValueCollection();
			var value = new BooleanPropertyValue(new BooleanProperty());
			collection.Add(value);

			var result = collection.Remove(value);

			Assert.IsTrue(result);
			Assert.AreEqual(0, collection.Count);
		}

		[TestMethod]
		public void Remove_ExistingDiscretePropertyValue_ReturnsTrueAndDecreasesCount()
		{
			var collection = new PropertyValueCollection();
			var value = new DiscretePropertyValue(new DiscreteProperty());
			collection.Add(value);

			var result = collection.Remove(value);

			Assert.IsTrue(result);
			Assert.AreEqual(0, collection.Count);
		}

		[TestMethod]
		public void Remove_ExistingCustomPropertyValue_ReturnsTrueAndDecreasesCount()
		{
			var collection = new PropertyValueCollection();
			var value = new CustomPropertyValue("MyCustom");
			collection.Add(value);

			var result = collection.Remove(value);

			Assert.IsTrue(result);
			Assert.AreEqual(0, collection.Count);
		}

		[TestMethod]
		public void Remove_NonExistingItem_ReturnsFalse()
		{
			var collection = new PropertyValueCollection();

			var result = collection.Remove(new StringPropertyValue(new StringProperty()));

			Assert.IsFalse(result);
		}

		[TestMethod]
		public void Remove_NullItem_ReturnsFalse()
		{
			var collection = new PropertyValueCollection();

			var result = collection.Remove(null);

			Assert.IsFalse(result);
		}

		[TestMethod]
		public void Remove_AllowsReAddingAfterRemoval()
		{
			var property = new StringProperty();
			var collection = new PropertyValueCollection();
			var value = new StringPropertyValue(property);
			collection.Add(value);
			collection.Remove(value);

			collection.Add(new StringPropertyValue(property));

			Assert.AreEqual(1, collection.Count);
		}

		[TestMethod]
		public void Contains_ExistingItem_ReturnsTrue()
		{
			var collection = new PropertyValueCollection();
			var value = new StringPropertyValue(new StringProperty());
			collection.Add(value);

			Assert.IsTrue(collection.Contains(value));
		}

		[TestMethod]
		public void Contains_NonExistingItem_ReturnsFalse()
		{
			var collection = new PropertyValueCollection();

			Assert.IsFalse(collection.Contains(new StringPropertyValue(new StringProperty())));
		}

		[TestMethod]
		public void Contains_NullItem_ReturnsFalse()
		{
			var collection = new PropertyValueCollection();

			Assert.IsFalse(collection.Contains(null));
		}

		[TestMethod]
		public void Clear_RemovesAllItemsAndResetsCount()
		{
			var collection = new PropertyValueCollection();
			collection.Add(new StringPropertyValue(new StringProperty()));
			collection.Add(new BooleanPropertyValue(new BooleanProperty()));
			collection.Add(new DiscretePropertyValue(new DiscreteProperty()));
			collection.Add(new CustomPropertyValue("MyCustom"));

			collection.Clear();

			Assert.AreEqual(0, collection.Count);
			Assert.AreEqual(0, collection.StringValues.Count);
			Assert.AreEqual(0, collection.BooleanValues.Count);
			Assert.AreEqual(0, collection.DiscreteValues.Count);
			Assert.AreEqual(0, collection.CustomValues.Count);
		}

		[TestMethod]
		public void CopyTo_ValidArray_CopiesAllItems()
		{
			var collection = new PropertyValueCollection();
			var strVal = new StringPropertyValue(new StringProperty());
			var boolVal = new BooleanPropertyValue(new BooleanProperty());
			collection.Add(strVal);
			collection.Add(boolVal);

			var array = new PropertyValueBase[2];
			collection.CopyTo(array, 0);

			CollectionAssert.Contains(array, strVal);
			CollectionAssert.Contains(array, boolVal);
		}

		[TestMethod]
		public void CopyTo_NullArray_ThrowsArgumentNullException()
		{
			var collection = new PropertyValueCollection();

			Assert.ThrowsException<ArgumentNullException>(() => collection.CopyTo(null, 0));
		}

		[TestMethod]
		public void CopyTo_NegativeIndex_ThrowsArgumentOutOfRangeException()
		{
			var collection = new PropertyValueCollection();

			Assert.ThrowsException<ArgumentOutOfRangeException>(() => collection.CopyTo(new PropertyValueBase[1], -1));
		}

		[TestMethod]
		public void CopyTo_ArrayTooSmall_ThrowsArgumentException()
		{
			var collection = new PropertyValueCollection();
			collection.Add(new StringPropertyValue(new StringProperty()));
			collection.Add(new BooleanPropertyValue(new BooleanProperty()));

			Assert.ThrowsException<ArgumentException>(() => collection.CopyTo(new PropertyValueBase[1], 0));
		}

		[TestMethod]
		public void GetEnumerator_ReturnsAllAddedItems()
		{
			var collection = new PropertyValueCollection();
			var strVal = new StringPropertyValue(new StringProperty());
			var boolVal = new BooleanPropertyValue(new BooleanProperty());
			var discVal = new DiscretePropertyValue(new DiscreteProperty());
			var custVal = new CustomPropertyValue("custom");
			collection.Add(strVal);
			collection.Add(boolVal);
			collection.Add(discVal);
			collection.Add(custVal);

			var items = new List<PropertyValueBase>(collection);

			Assert.AreEqual(4, items.Count);
			CollectionAssert.Contains(items, strVal);
			CollectionAssert.Contains(items, boolVal);
			CollectionAssert.Contains(items, discVal);
			CollectionAssert.Contains(items, custVal);
		}

		[TestMethod]
		public void LinkedValues_ContainsStringBooleanAndDiscreteValues()
		{
			var collection = new PropertyValueCollection();
			var strVal = new StringPropertyValue(new StringProperty());
			var boolVal = new BooleanPropertyValue(new BooleanProperty());
			var discVal = new DiscretePropertyValue(new DiscreteProperty());
			collection.Add(strVal);
			collection.Add(boolVal);
			collection.Add(discVal);

			var linked = collection.LinkedValues.ToList();

			Assert.AreEqual(3, linked.Count);
			CollectionAssert.Contains(linked, strVal);
			CollectionAssert.Contains(linked, boolVal);
			CollectionAssert.Contains(linked, discVal);
		}

		private sealed class UnsupportedPropertyValue : PropertyValueBase
		{
		}
	}
}
