namespace RT_MediaOps.Plan.Properties.Definitions
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	public sealed class BooleanPropertyTests
	{
		[TestMethod]
		public void DefaultConstructor_GeneratesId()
		{
			var property = new BooleanProperty();

			Assert.AreNotEqual(Guid.Empty, property.Id);
		}

		[TestMethod]
		public void DefaultConstructor_DefaultValueIsFalse()
		{
			var property = new BooleanProperty();

			Assert.IsFalse(property.DefaultValue);
		}

		[TestMethod]
		public void GuidConstructor_AssignsProvidedId()
		{
			var id = Guid.NewGuid();

			var property = new BooleanProperty(id);

			Assert.AreEqual(id, property.Id);
		}

		[TestMethod]
		public void GuidConstructor_EmptyGuidThrows()
		{
			Assert.ThrowsException<ArgumentException>(() => new BooleanProperty(Guid.Empty));
		}

		[TestMethod]
		public void SetDefaultValue_True_ValueIsSet()
		{
			var property = new BooleanProperty { DefaultValue = true };

			Assert.IsTrue(property.DefaultValue);
		}

		[TestMethod]
		public void SetInheritedProperties_ValuesAreSet()
		{
			var property = new BooleanProperty
			{
				Name = "MyBool",
				Scope = "global",
				SectionName = "General",
				Order = 5,
				DefaultValue = true,
			};

			Assert.AreEqual("MyBool", property.Name);
			Assert.AreEqual("global", property.Scope);
			Assert.AreEqual("General", property.SectionName);
			Assert.AreEqual(5, property.Order);
			Assert.IsTrue(property.DefaultValue);
		}

		[TestMethod]
		public void Equals_SameValues_ReturnsTrue()
		{
			var id = Guid.NewGuid();
			var first = new BooleanProperty(id) { Name = "B", Scope = "s", SectionName = "sec", Order = 1, DefaultValue = true };
			var second = new BooleanProperty(id) { Name = "B", Scope = "s", SectionName = "sec", Order = 1, DefaultValue = true };

			Assert.IsTrue(first.Equals(second));
			Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
		}

		[TestMethod]
		public void Equals_DifferentDefaultValue_ReturnsFalse()
		{
			var id = Guid.NewGuid();
			var first = new BooleanProperty(id) { DefaultValue = true };
			var second = new BooleanProperty(id) { DefaultValue = false };

			Assert.IsFalse(first.Equals(second));
		}

		[TestMethod]
		public void Equals_DifferentName_ReturnsFalse()
		{
			var id = Guid.NewGuid();
			var first = new BooleanProperty(id) { Name = "A" };
			var second = new BooleanProperty(id) { Name = "B" };

			Assert.IsFalse(first.Equals(second));
		}

		[TestMethod]
		public void Equals_DifferentId_ReturnsFalse()
		{
			var first = new BooleanProperty(Guid.NewGuid());
			var second = new BooleanProperty(Guid.NewGuid());

			Assert.IsFalse(first.Equals(second));
		}

		[TestMethod]
		public void Equals_OtherPropertyType_ReturnsFalse()
		{
			var id = Guid.NewGuid();
			var booleanProperty = new BooleanProperty(id);
			var stringProperty = new StringProperty(id);

			Assert.IsFalse(object.Equals(booleanProperty, stringProperty));
		}

		[TestMethod]
		public void Equals_Null_ReturnsFalse()
		{
			var property = new BooleanProperty();

			Assert.IsFalse(property == null);
		}
	}
}
