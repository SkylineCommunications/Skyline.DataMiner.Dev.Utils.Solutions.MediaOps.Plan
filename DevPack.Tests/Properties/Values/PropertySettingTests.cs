namespace RT_MediaOps.Plan.Properties.Values
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	public sealed class PropertySettingTests
	{
		[TestMethod]
		public void CustomPropertySetting_Constructor_SetsName()
		{
			var setting = new CustomPropertySetting("MyCustom");

			Assert.AreEqual("MyCustom", setting.Name);
		}

		[TestMethod]
		public void CustomPropertySetting_NullName_Throws()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new CustomPropertySetting((string)null!));
		}

		[TestMethod]
		public void CustomPropertySetting_SetValue_ValueIsSet()
		{
			var setting = new CustomPropertySetting("MyCustom") { Value = "abc" };

			Assert.AreEqual("abc", setting.Value);
		}

		[TestMethod]
		public void StringPropertySetting_Constructor_SetsPropertyId()
		{
			var id = Guid.NewGuid();
			var property = new StringProperty(id);

			var setting = new StringPropertySetting(property);

			Assert.AreEqual(id, setting.Id);
		}

		[TestMethod]
		public void StringPropertySetting_NullProperty_Throws()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new StringPropertySetting((StringProperty)null!));
		}

		[TestMethod]
		public void StringPropertySetting_SetValue_ValueIsSet()
		{
			var setting = new StringPropertySetting(new StringProperty()) { Value = "hello" };

			Assert.AreEqual("hello", setting.Value);
		}

		[TestMethod]
		public void BooleanPropertySetting_Constructor_SetsPropertyId()
		{
			var id = Guid.NewGuid();
			var property = new BooleanProperty(id);

			var setting = new BooleanPropertySetting(property);

			Assert.AreEqual(id, setting.Id);
		}

		[TestMethod]
		public void BooleanPropertySetting_NullProperty_Throws()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new BooleanPropertySetting((BooleanProperty)null!));
		}

		[TestMethod]
		public void BooleanPropertySetting_SetValue_ValueIsSet()
		{
			var setting = new BooleanPropertySetting(new BooleanProperty()) { Value = true };

			Assert.IsTrue(setting.Value);
		}

		[TestMethod]
		public void DiscretePropertySetting_Constructor_SetsPropertyId()
		{
			var id = Guid.NewGuid();
			var property = new DiscreteProperty(id);

			var setting = new DiscretePropertySetting(property);

			Assert.AreEqual(id, setting.Id);
		}

		[TestMethod]
		public void DiscretePropertySetting_NullProperty_Throws()
		{
			Assert.ThrowsException<ArgumentNullException>(() => new DiscretePropertySetting((DiscreteProperty)null!));
		}

		[TestMethod]
		public void DiscretePropertySetting_SetValue_ValueIsSet()
		{
			var setting = new DiscretePropertySetting(new DiscreteProperty()) { Value = "Option1" };

			Assert.AreEqual("Option1", setting.Value);
		}
	}
}
