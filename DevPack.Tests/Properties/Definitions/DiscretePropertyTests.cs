namespace RT_MediaOps.Plan.Properties.Definitions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	public sealed class DiscretePropertyTests
	{
		[TestMethod]
		public void DefaultConstructor_GeneratesId()
		{
			var property = new DiscreteProperty();

			Assert.AreNotEqual(Guid.Empty, property.Id);
		}

		[TestMethod]
		public void DefaultConstructor_DiscretesIsEmpty()
		{
			var property = new DiscreteProperty();

			Assert.IsNotNull(property.Discretes);
			Assert.AreEqual(0, property.Discretes.Count);
		}

		[TestMethod]
		public void DefaultConstructor_DefaultValueIsNull()
		{
			var property = new DiscreteProperty();

			Assert.IsNull(property.DefaultValue);
		}

		[TestMethod]
		public void GuidConstructor_AssignsProvidedId()
		{
			var id = Guid.NewGuid();

			var property = new DiscreteProperty(id);

			Assert.AreEqual(id, property.Id);
		}

		[TestMethod]
		public void GuidConstructor_EmptyGuidThrows()
		{
			Assert.ThrowsException<ArgumentException>(() => new DiscreteProperty(Guid.Empty));
		}

		[TestMethod]
		public void SetDefaultValue_ValueIsSet()
		{
			var property = new DiscreteProperty { DefaultValue = "A" };

			Assert.AreEqual("A", property.DefaultValue);
		}

		[TestMethod]
		public void AddDiscrete_AppendsValue()
		{
			var property = new DiscreteProperty();

			property.AddDiscrete("A");
			property.AddDiscrete("B");

			CollectionAssert.AreEqual(new[] { "A", "B" }, property.Discretes.ToList());
		}

		[TestMethod]
		public void AddDiscrete_ReturnsSameInstance()
		{
			var property = new DiscreteProperty();

			var result = property.AddDiscrete("A");

			Assert.AreSame(property, result);
		}

		[TestMethod]
		public void AddDiscrete_AllowsDuplicates()
		{
			var property = new DiscreteProperty();

			property.AddDiscrete("A").AddDiscrete("A");

			Assert.AreEqual(2, property.Discretes.Count);
		}

		[TestMethod]
		public void AddDiscrete_NullThrowsArgumentNullException()
		{
			var property = new DiscreteProperty();

			Assert.ThrowsException<ArgumentNullException>(() => property.AddDiscrete(null));
		}

		[TestMethod]
		public void RemoveDiscrete_RemovesFirstMatch()
		{
			var property = new DiscreteProperty();
			property.AddDiscrete("A").AddDiscrete("B").AddDiscrete("A");

			property.RemoveDiscrete("A");

			CollectionAssert.AreEqual(new[] { "B", "A" }, property.Discretes.ToList());
		}

		[TestMethod]
		public void RemoveDiscrete_ReturnsSameInstance()
		{
			var property = new DiscreteProperty();
			property.AddDiscrete("A");

			var result = property.RemoveDiscrete("A");

			Assert.AreSame(property, result);
		}

		[TestMethod]
		public void RemoveDiscrete_NotPresent_NoChange()
		{
			var property = new DiscreteProperty();
			property.AddDiscrete("A");

			property.RemoveDiscrete("B");

			CollectionAssert.AreEqual(new[] { "A" }, property.Discretes.ToList());
		}

		[TestMethod]
		public void RemoveDiscrete_NullThrowsArgumentNullException()
		{
			var property = new DiscreteProperty();

			Assert.ThrowsException<ArgumentNullException>(() => property.RemoveDiscrete(null));
		}

		[TestMethod]
		public void SetDiscretes_ReplacesAllValues()
		{
			var property = new DiscreteProperty();
			property.AddDiscrete("A").AddDiscrete("B");

			property.SetDiscretes(new[] { "X", "Y", "Z" });

			CollectionAssert.AreEqual(new[] { "X", "Y", "Z" }, property.Discretes.ToList());
		}

		[TestMethod]
		public void SetDiscretes_ReturnsSameInstance()
		{
			var property = new DiscreteProperty();

			var result = property.SetDiscretes(new[] { "A" });

			Assert.AreSame(property, result);
		}

		[TestMethod]
		public void SetDiscretes_EquivalentValues_DoesNotReplace()
		{
			var property = new DiscreteProperty();
			property.AddDiscrete("A").AddDiscrete("B");

			// Same set in different order should be a no-op (ScrambledEquals).
			property.SetDiscretes(new[] { "B", "A" });

			CollectionAssert.AreEqual(new[] { "A", "B" }, property.Discretes.ToList());
		}

		[TestMethod]
		public void SetDiscretes_NullThrowsArgumentNullException()
		{
			var property = new DiscreteProperty();

			Assert.ThrowsException<ArgumentNullException>(() => property.SetDiscretes(null));
		}

		[TestMethod]
		public void SetDiscretes_NullElement_Throws()
		{
			var property = new DiscreteProperty();

			Assert.ThrowsException<ArgumentException>(() => property.SetDiscretes(new[] { "A", null, "B" }));
		}

		[TestMethod]
		public void Discretes_IsReadOnlyView()
		{
			var property = new DiscreteProperty();
			property.AddDiscrete("A");

			Assert.IsInstanceOfType<IReadOnlyCollection<string>>(property.Discretes);
		}

		[TestMethod]
		public void Equals_SameValuesAndDiscretes_ReturnsTrue()
		{
			var id = Guid.NewGuid();
			var first = new DiscreteProperty(id) { Name = "D", DefaultValue = "A" };
			first.SetDiscretes(new[] { "A", "B", "C" });

			var second = new DiscreteProperty(id) { Name = "D", DefaultValue = "A" };
			second.SetDiscretes(new[] { "A", "B", "C" });

			Assert.IsTrue(first.Equals(second));
			Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
		}

		[TestMethod]
		public void Equals_DiscretesInDifferentOrder_ReturnsTrue()
		{
			var id = Guid.NewGuid();
			var first = new DiscreteProperty(id);
			first.SetDiscretes(new[] { "A", "B", "C" });

			var second = new DiscreteProperty(id);
			second.SetDiscretes(new[] { "C", "B", "A" });

			Assert.IsTrue(first.Equals(second));
			Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
		}

		[TestMethod]
		public void Equals_DifferentDiscretes_ReturnsFalse()
		{
			var id = Guid.NewGuid();
			var first = new DiscreteProperty(id);
			first.SetDiscretes(new[] { "A", "B" });

			var second = new DiscreteProperty(id);
			second.SetDiscretes(new[] { "A", "B", "C" });

			Assert.IsFalse(first.Equals(second));
		}

		[TestMethod]
		public void Equals_DifferentDefaultValue_ReturnsFalse()
		{
			var id = Guid.NewGuid();
			var first = new DiscreteProperty(id) { DefaultValue = "A" };
			var second = new DiscreteProperty(id) { DefaultValue = "B" };

			Assert.IsFalse(first.Equals(second));
		}

		[TestMethod]
		public void Equals_OtherPropertyType_ReturnsFalse()
		{
			var id = Guid.NewGuid();
			var discrete = new DiscreteProperty(id);
			var stringProperty = new StringProperty(id);

			Assert.IsFalse(discrete.Equals(stringProperty));
		}

		[TestMethod]
		public void Equals_Null_ReturnsFalse()
		{
			var property = new DiscreteProperty();

			Assert.IsFalse(property.Equals(null));
		}
	}
}
