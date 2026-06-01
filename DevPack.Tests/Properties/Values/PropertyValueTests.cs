namespace RT_MediaOps.Plan.Properties.Values
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	public sealed class PropertyValueTests
	{
		[TestMethod]
		public void CustomPropertyValue_Constructor_SetsName()
		{
			var value = new CustomPropertySetting("MyCustom");

			Assert.AreEqual("MyCustom", value.Name);
		}

		[TestMethod]
		public void CustomPropertyValue_NullName_Throws()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new CustomPropertySetting((string)null!));
		}

		[TestMethod]
		public void CustomPropertyValue_SetValue_ValueIsSet()
		{
			var value = new CustomPropertySetting("MyCustom") { Value = "abc" };

			Assert.AreEqual("abc", value.Value);
		}

		[TestMethod]
		public void StringPropertyValue_Constructor_SetsPropertyId()
		{
			var id = Guid.NewGuid();
			var property = new StringProperty(id);

			var value = new StringPropertySetting(property);

			Assert.AreEqual(id, value.Id);
		}

		[TestMethod]
		public void StringPropertyValue_NullProperty_Throws()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new StringPropertySetting((StringProperty)null!));
		}

		[TestMethod]
		public void StringPropertyValue_SetValue_ValueIsSet()
		{
			var value = new StringPropertySetting(new StringProperty()) { Value = "hello" };

			Assert.AreEqual("hello", value.Value);
		}

		[TestMethod]
		public void BooleanPropertyValue_Constructor_SetsPropertyId()
		{
			var id = Guid.NewGuid();
			var property = new BooleanProperty(id);

			var value = new BooleanPropertySetting(property);

			Assert.AreEqual(id, value.Id);
		}

		[TestMethod]
		public void BooleanPropertyValue_NullProperty_Throws()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new BooleanPropertySetting((BooleanProperty)null!));
		}

		[TestMethod]
		public void BooleanPropertyValue_SetValue_ValueIsSet()
		{
			var value = new BooleanPropertySetting(new BooleanProperty()) { Value = true };

			Assert.IsTrue(value.Value);
		}

		[TestMethod]
		public void DiscretePropertyValue_Constructor_SetsPropertyId()
		{
			var id = Guid.NewGuid();
			var property = new DiscreteProperty(id);

			var value = new DiscretePropertySetting(property);

			Assert.AreEqual(id, value.Id);
		}

		[TestMethod]
		public void DiscretePropertyValue_NullProperty_Throws()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new DiscretePropertySetting((DiscreteProperty)null!));
		}

		[TestMethod]
		public void DiscretePropertyValue_SetValue_ValueIsSet()
		{
			var value = new DiscretePropertySetting(new DiscreteProperty()) { Value = "Option1" };

			Assert.AreEqual("Option1", value.Value);
		}
	}
}
