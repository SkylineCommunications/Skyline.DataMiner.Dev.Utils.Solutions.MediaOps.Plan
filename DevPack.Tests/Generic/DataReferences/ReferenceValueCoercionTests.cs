namespace RT_MediaOps.Plan.Generic.DataReferences
{
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	public sealed class ReferenceValueCoercionTests
	{
		[TestMethod]
		public void ReferenceValueCoercion_TryCoerce_TextTargetKeepsValue()
		{
			var target = new TextConfiguration { Name = "Text" };

			Assert.IsTrue(ReferenceValueCoercion.TryCoerce(new StringResolvedValue("Brussels"), target, out var coerced));
			Assert.AreEqual("Brussels", coerced.GetRawValue());
		}

		[TestMethod]
		public void ReferenceValueCoercion_TryCoerce_CapabilityMatchesOption()
		{
			var target = new Capability { Name = "Location" }.SetDiscretes(["Brussels", "Paris"]);

			Assert.IsTrue(ReferenceValueCoercion.TryCoerce(new StringResolvedValue("Brussels"), target, out var coerced));
			Assert.AreEqual("Brussels", coerced.GetRawValue());
		}

		[TestMethod]
		public void ReferenceValueCoercion_TryCoerce_CapabilityIsCaseSensitive()
		{
			var target = new Capability { Name = "Location" }.SetDiscretes(["Brussels", "Paris"]);

			Assert.IsFalse(ReferenceValueCoercion.TryCoerce(new StringResolvedValue("brussels"), target, out var coerced));
			Assert.IsNull(coerced);
		}

		[TestMethod]
		public void ReferenceValueCoercion_TryCoerce_CapabilityWithoutMatchHasNoValue()
		{
			var target = new Capability { Name = "Location" }.SetDiscretes(["Brussels", "Paris"]);

			Assert.IsFalse(ReferenceValueCoercion.TryCoerce(new StringResolvedValue("London"), target, out var coerced));
			Assert.IsNull(coerced);
		}

		[TestMethod]
		public void ReferenceValueCoercion_TryCoerce_DiscreteTextMatchesDisplayValueAndReturnsRawValue()
		{
			var target = new DiscreteTextConfiguration { Name = "Location" }
				.AddDiscrete(new TextDiscreet("BRU", "Brussels"))
				.AddDiscrete(new TextDiscreet("PAR", "Paris"));

			Assert.IsTrue(ReferenceValueCoercion.TryCoerce(new StringResolvedValue("Brussels"), target, out var coerced));
			Assert.AreEqual("BRU", coerced.GetRawValue());
			Assert.AreEqual("Brussels", coerced.DisplayValue);
		}

		[TestMethod]
		public void ReferenceValueCoercion_TryCoerce_DiscreteTextMatchesRawValue()
		{
			var target = new DiscreteTextConfiguration { Name = "Location" }
				.AddDiscrete(new TextDiscreet("BRU", "Brussels"));

			Assert.IsTrue(ReferenceValueCoercion.TryCoerce(new StringResolvedValue("BRU"), target, out var coerced));
			Assert.AreEqual("BRU", coerced.GetRawValue());
		}

		[TestMethod]
		public void ReferenceValueCoercion_TryCoerce_DiscreteTextUsesDisplayValueOfSource()
		{
			var target = new DiscreteTextConfiguration { Name = "Location" }
				.AddDiscrete(new TextDiscreet("BRU", "Brussels"));

			// The source is a dropdown holding raw value "1" shown as "Brussels".
			Assert.IsTrue(ReferenceValueCoercion.TryCoerce(new StringResolvedValue("1", "Brussels"), target, out var coerced));
			Assert.AreEqual("BRU", coerced.GetRawValue());
		}

		[TestMethod]
		public void ReferenceValueCoercion_TryCoerce_DiscreteNumberMatchesDisplayValueAndReturnsRawValue()
		{
			var target = new DiscreteNumberConfiguration { Name = "Channels" }
				.AddDiscrete(new NumberDiscreet(7, "Seven"));

			Assert.IsTrue(ReferenceValueCoercion.TryCoerce(new StringResolvedValue("Seven"), target, out var coerced));
			Assert.AreEqual(7m, coerced.GetRawValue());
		}

		[TestMethod]
		public void ReferenceValueCoercion_TryCoerce_DiscreteNumberWithoutMatchHasNoValue()
		{
			var target = new DiscreteNumberConfiguration { Name = "Channels" }
				.AddDiscrete(new NumberDiscreet(7, "Seven"));

			Assert.IsFalse(ReferenceValueCoercion.TryCoerce(new StringResolvedValue("Eight"), target, out var coerced));
			Assert.IsNull(coerced);
		}

		[TestMethod]
		public void ReferenceValueCoercion_TryCoerce_DropdownWithoutValueHasNoValue()
		{
			var target = new Capability { Name = "Location" }.SetDiscretes(["Brussels"]);

			Assert.IsFalse(ReferenceValueCoercion.TryCoerce(new NullResolvedValue(), target, out var coerced));
			Assert.IsNull(coerced);
		}

		[TestMethod]
		public void ReferenceValueCoercion_TryCoerce_UnknownTargetKeepsValue()
		{
			Assert.IsTrue(ReferenceValueCoercion.TryCoerce(new StringResolvedValue("Brussels"), (Parameter)null, out var coerced));
			Assert.AreEqual("Brussels", coerced.GetRawValue());
		}
	}
}
