namespace RT_MediaOps.Plan.Properties.Definitions
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	public sealed class FilePropertyTests
	{
		[TestMethod]
		public void DefaultConstructor_GeneratesId()
		{
			var property = new FileProperty();

			Assert.AreNotEqual(Guid.Empty, property.Id);
		}

		[TestMethod]
		public void DefaultConstructor_DefaultValuesAreSet()
		{
			var property = new FileProperty();

			Assert.IsFalse(property.HasSizeLimit);
			Assert.AreEqual(0, property.SizeLimit);
			Assert.IsFalse(property.AllowMultiple);
		}

		[TestMethod]
		public void GuidConstructor_AssignsProvidedId()
		{
			var id = Guid.NewGuid();

			var property = new FileProperty(id);

			Assert.AreEqual(id, property.Id);
		}

		[TestMethod]
		public void GuidConstructor_EmptyGuidThrows()
		{
			Assert.ThrowsException<ArgumentException>(() => new FileProperty(Guid.Empty));
		}

		[TestMethod]
		public void SetProperties_ValuesAreSet()
		{
			var property = new FileProperty
			{
				Name = "MyFile",
				Scope = "global",
				SectionName = "Files",
				Order = 3,
				HasSizeLimit = true,
				SizeLimit = 50,
				AllowMultiple = true,
			};

			Assert.AreEqual("MyFile", property.Name);
			Assert.AreEqual("global", property.Scope);
			Assert.AreEqual("Files", property.SectionName);
			Assert.AreEqual(3, property.Order);
			Assert.IsTrue(property.HasSizeLimit);
			Assert.AreEqual(50, property.SizeLimit);
			Assert.IsTrue(property.AllowMultiple);
		}

		[TestMethod]
		public void Equals_SameValues_ReturnsTrue()
		{
			var id = Guid.NewGuid();
			var first = new FileProperty(id) { Name = "F", SizeLimit = 100, AllowMultiple = true };
			var second = new FileProperty(id) { Name = "F", SizeLimit = 100, AllowMultiple = true };

			Assert.IsTrue(first.Equals(second));
			Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
		}

		[TestMethod]
		public void Equals_DifferentSizeLimit_ReturnsFalse()
		{
			var id = Guid.NewGuid();
			var first = new FileProperty(id) { SizeLimit = 10 };
			var second = new FileProperty(id) { SizeLimit = 20 };

			Assert.IsFalse(first.Equals(second));
		}

		[TestMethod]
		public void Equals_DifferentAllowMultiple_ReturnsFalse()
		{
			var id = Guid.NewGuid();
			var first = new FileProperty(id) { AllowMultiple = false };
			var second = new FileProperty(id) { AllowMultiple = true };

			Assert.IsFalse(first.Equals(second));
		}

		[TestMethod]
		public void Equals_DifferentId_ReturnsFalse()
		{
			var first = new FileProperty(Guid.NewGuid());
			var second = new FileProperty(Guid.NewGuid());

			Assert.IsFalse(first.Equals(second));
		}

		[TestMethod]
		public void Equals_OtherPropertyType_ReturnsFalse()
		{
			var id = Guid.NewGuid();
			var fileProperty = new FileProperty(id);
			var stringProperty = new StringProperty(id);

			Assert.IsFalse(object.Equals(fileProperty, stringProperty));
		}

		[TestMethod]
		public void Equals_Null_ReturnsFalse()
		{
			var property = new FileProperty();

			Assert.IsFalse(property == null);
		}
	}
}
