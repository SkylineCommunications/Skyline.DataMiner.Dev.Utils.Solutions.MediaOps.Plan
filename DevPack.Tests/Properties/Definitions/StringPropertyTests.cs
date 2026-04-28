namespace RT_MediaOps.Plan.Properties.Definitions
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	public sealed class StringPropertyTests
	{
		[TestMethod]
		public void DefaultConstructor_GeneratesId()
		{
			var property = new StringProperty();

			Assert.AreNotEqual(Guid.Empty, property.Id);
		}

		[TestMethod]
		public void DefaultConstructor_DefaultValuesAreSet()
		{
			var property = new StringProperty();

			Assert.IsNull(property.DefaultValue);
			Assert.AreEqual(250, property.SizeLimit);
			Assert.IsFalse(property.IsMultiLine);
		}

		[TestMethod]
		public void GuidConstructor_AssignsProvidedIdAndDefaults()
		{
			var id = Guid.NewGuid();

			var property = new StringProperty(id);

			Assert.AreEqual(id, property.Id);
			Assert.AreEqual(250, property.SizeLimit);
			Assert.IsFalse(property.IsMultiLine);
		}

		[TestMethod]
		public void GuidConstructor_EmptyGuidThrows()
		{
			Assert.ThrowsException<ArgumentException>(() => new StringProperty(Guid.Empty));
		}

		[TestMethod]
		public void SetProperties_ValuesAreSet()
		{
			var property = new StringProperty
			{
				Name = "MyString",
				Scope = "global",
				SectionName = "Section1",
				Order = 2,
				DefaultValue = "default",
				SizeLimit = 100,
				IsMultiLine = true,
			};

			Assert.AreEqual("MyString", property.Name);
			Assert.AreEqual("global", property.Scope);
			Assert.AreEqual("Section1", property.SectionName);
			Assert.AreEqual(2, property.Order);
			Assert.AreEqual("default", property.DefaultValue);
			Assert.AreEqual(100, property.SizeLimit);
			Assert.IsTrue(property.IsMultiLine);
		}

		[TestMethod]
		public void Equals_SameValues_ReturnsTrue()
		{
			var id = Guid.NewGuid();
			var first = new StringProperty(id) { Name = "S", DefaultValue = "x", SizeLimit = 50, IsMultiLine = true };
			var second = new StringProperty(id) { Name = "S", DefaultValue = "x", SizeLimit = 50, IsMultiLine = true };

			Assert.IsTrue(first.Equals(second));
			Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
		}

		[TestMethod]
		public void Equals_DifferentDefaultValue_ReturnsFalse()
		{
			var id = Guid.NewGuid();
			var first = new StringProperty(id) { DefaultValue = "a" };
			var second = new StringProperty(id) { DefaultValue = "b" };

			Assert.IsFalse(first.Equals(second));
		}

		[TestMethod]
		public void Equals_DifferentSizeLimit_ReturnsFalse()
		{
			var id = Guid.NewGuid();
			var first = new StringProperty(id) { SizeLimit = 10 };
			var second = new StringProperty(id) { SizeLimit = 20 };

			Assert.IsFalse(first.Equals(second));
		}

		[TestMethod]
		public void Equals_DifferentIsMultiLine_ReturnsFalse()
		{
			var id = Guid.NewGuid();
			var first = new StringProperty(id) { IsMultiLine = false };
			var second = new StringProperty(id) { IsMultiLine = true };

			Assert.IsFalse(first.Equals(second));
		}

		[TestMethod]
		public void Equals_OtherPropertyType_ReturnsFalse()
		{
			var id = Guid.NewGuid();
			var stringProperty = new StringProperty(id);
			var booleanProperty = new BooleanProperty(id);

			Assert.IsFalse(stringProperty.Equals(booleanProperty));
		}

		[TestMethod]
		public void Equals_Null_ReturnsFalse()
		{
			var property = new StringProperty();

			Assert.IsFalse(property.Equals(null));
		}
	}
}
