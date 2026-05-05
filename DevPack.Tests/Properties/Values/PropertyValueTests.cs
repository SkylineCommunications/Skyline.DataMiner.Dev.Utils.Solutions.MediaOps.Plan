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
			var value = new CustomPropertyValue("MyCustom");

			Assert.AreEqual("MyCustom", value.Name);
		}

		[TestMethod]
		public void CustomPropertyValue_NullName_Throws()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new CustomPropertyValue((string)null));
		}

		[TestMethod]
		public void CustomPropertyValue_SetValue_ValueIsSet()
		{
			var value = new CustomPropertyValue("MyCustom") { Value = "abc" };

			Assert.AreEqual("abc", value.Value);
		}

		[TestMethod]
		public void StringPropertyValue_Constructor_SetsPropertyId()
		{
			var id = Guid.NewGuid();
			var property = new StringProperty(id);

			var value = new StringPropertyValue(property);

			Assert.AreEqual(id, value.PropertyId);
		}

		[TestMethod]
		public void StringPropertyValue_NullProperty_Throws()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new StringPropertyValue((StringProperty)null));
		}

		[TestMethod]
		public void StringPropertyValue_SetValue_ValueIsSet()
		{
			var value = new StringPropertyValue(new StringProperty()) { Value = "hello" };

			Assert.AreEqual("hello", value.Value);
		}

		[TestMethod]
		public void BooleanPropertyValue_Constructor_SetsPropertyId()
		{
			var id = Guid.NewGuid();
			var property = new BooleanProperty(id);

			var value = new BooleanPropertyValue(property);

			Assert.AreEqual(id, value.PropertyId);
		}

		[TestMethod]
		public void BooleanPropertyValue_NullProperty_Throws()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new BooleanPropertyValue((BooleanProperty)null));
		}

		[TestMethod]
		public void BooleanPropertyValue_SetValue_ValueIsSet()
		{
			var value = new BooleanPropertyValue(new BooleanProperty()) { Value = true };

			Assert.IsTrue(value.Value);
		}

		[TestMethod]
		public void DiscretePropertyValue_Constructor_SetsPropertyId()
		{
			var id = Guid.NewGuid();
			var property = new DiscreteProperty(id);

			var value = new DiscretePropertyValue(property);

			Assert.AreEqual(id, value.PropertyId);
		}

		[TestMethod]
		public void DiscretePropertyValue_NullProperty_Throws()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new DiscretePropertyValue((DiscreteProperty)null));
		}

		[TestMethod]
		public void DiscretePropertyValue_SetValue_ValueIsSet()
		{
			var value = new DiscretePropertyValue(new DiscreteProperty()) { Value = "Option1" };

			Assert.AreEqual("Option1", value.Value);
		}
	}
}
