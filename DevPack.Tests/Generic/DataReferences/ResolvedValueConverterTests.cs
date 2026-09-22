namespace RT_MediaOps.Plan.Generic.DataReferences
{
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	public sealed class ResolvedValueConverterTests
	{
		[TestMethod]
		public void ResolvedValueConverter_TryConvert_TextTargetKeepsValue()
		{
			var target = new TextConfiguration { Name = "Text" };

			Assert.IsTrue(ResolvedValueConverter.TryConvert(new StringResolvedValue("Brussels"), target, out var converted));
			Assert.AreEqual("Brussels", converted.GetRawValue());
		}

		[TestMethod]
		public void ResolvedValueConverter_TryConvert_CapabilityMatchesOption()
		{
			var target = new Capability { Name = "Location" }.SetDiscretes(["Brussels", "Paris"]);

			Assert.IsTrue(ResolvedValueConverter.TryConvert(new StringResolvedValue("Brussels"), target, out var converted));
			Assert.AreEqual("Brussels", converted.GetRawValue());
		}

		[TestMethod]
		public void ResolvedValueConverter_TryConvert_CapabilityIsCaseSensitive()
		{
			var target = new Capability { Name = "Location" }.SetDiscretes(["Brussels", "Paris"]);

			Assert.IsFalse(ResolvedValueConverter.TryConvert(new StringResolvedValue("brussels"), target, out var converted));
			Assert.IsNull(converted);
		}

		[TestMethod]
		public void ResolvedValueConverter_TryConvert_CapabilityWithoutMatchHasNoValue()
		{
			var target = new Capability { Name = "Location" }.SetDiscretes(["Brussels", "Paris"]);

			Assert.IsFalse(ResolvedValueConverter.TryConvert(new StringResolvedValue("London"), target, out var converted));
			Assert.IsNull(converted);
		}

		[TestMethod]
		public void ResolvedValueConverter_TryConvert_DiscreteTextMatchesDisplayValueAndReturnsRawValue()
		{
			var target = new DiscreteTextConfiguration { Name = "Location" }
				.AddDiscrete(new TextDiscreet("BRU", "Brussels"))
				.AddDiscrete(new TextDiscreet("PAR", "Paris"));

			Assert.IsTrue(ResolvedValueConverter.TryConvert(new StringResolvedValue("Brussels"), target, out var converted));
			Assert.AreEqual("BRU", converted.GetRawValue());
			Assert.AreEqual("Brussels", converted.DisplayValue);
		}

		[TestMethod]
		public void ResolvedValueConverter_TryConvert_DiscreteTextMatchesRawValue()
		{
			var target = new DiscreteTextConfiguration { Name = "Location" }
				.AddDiscrete(new TextDiscreet("BRU", "Brussels"));

			Assert.IsTrue(ResolvedValueConverter.TryConvert(new StringResolvedValue("BRU"), target, out var converted));
			Assert.AreEqual("BRU", converted.GetRawValue());
		}

		[TestMethod]
		public void ResolvedValueConverter_TryConvert_DiscreteTextUsesDisplayValueOfSource()
		{
			var target = new DiscreteTextConfiguration { Name = "Location" }
				.AddDiscrete(new TextDiscreet("BRU", "Brussels"));

			// The source is a dropdown holding raw value "1" shown as "Brussels".
			Assert.IsTrue(ResolvedValueConverter.TryConvert(new StringResolvedValue("1", "Brussels"), target, out var converted));
			Assert.AreEqual("BRU", converted.GetRawValue());
		}

		[TestMethod]
		public void ResolvedValueConverter_TryConvert_DiscreteNumberMatchesDisplayValueAndReturnsRawValue()
		{
			var target = new DiscreteNumberConfiguration { Name = "Channels" }
				.AddDiscrete(new NumberDiscreet(7, "Seven"));

			Assert.IsTrue(ResolvedValueConverter.TryConvert(new StringResolvedValue("Seven"), target, out var converted));
			Assert.AreEqual(7m, converted.GetRawValue());
		}

		[TestMethod]
		public void ResolvedValueConverter_TryConvert_DiscreteNumberWithoutMatchHasNoValue()
		{
			var target = new DiscreteNumberConfiguration { Name = "Channels" }
				.AddDiscrete(new NumberDiscreet(7, "Seven"));

			Assert.IsFalse(ResolvedValueConverter.TryConvert(new StringResolvedValue("Eight"), target, out var converted));
			Assert.IsNull(converted);
		}

		[TestMethod]
		public void ResolvedValueConverter_TryConvert_DropdownWithoutValueHasNoValue()
		{
			var target = new Capability { Name = "Location" }.SetDiscretes(["Brussels"]);

			Assert.IsFalse(ResolvedValueConverter.TryConvert(new NullResolvedValue(), target, out var converted));
			Assert.IsNull(converted);
		}

		[TestMethod]
		public void ResolvedValueConverter_TryConvert_UnknownTargetKeepsValue()
		{
			Assert.IsTrue(ResolvedValueConverter.TryConvert(new StringResolvedValue("Brussels"), (Parameter)null, out var converted));
			Assert.AreEqual("Brussels", converted.GetRawValue());
		}

		[TestMethod]
		public void ResolvedValueConverter_TryConvert_NumberTargetTakesNumericText()
		{
			var target = new NumberConfiguration { Name = "Channels" };

			Assert.IsTrue(ResolvedValueConverter.TryConvert(new StringResolvedValue("7"), target, out var converted));
			Assert.AreEqual(7m, converted.GetRawValue());
		}

		[TestMethod]
		public void ResolvedValueConverter_TryConvert_NumberTargetRejectsNonNumericText()
		{
			var target = new NumberConfiguration { Name = "Channels" };

			Assert.IsFalse(ResolvedValueConverter.TryConvert(new StringResolvedValue("seven"), target, out var converted));
			Assert.IsNull(converted);
		}

		[TestMethod]
		public void ResolvedValueConverter_TryConvert_NumberTargetRejectsThousandsSeparator()
		{
			var target = new NumberConfiguration { Name = "Channels" };

			// '1,5' must not silently become 15.
			Assert.IsFalse(ResolvedValueConverter.TryConvert(new StringResolvedValue("1,5"), target, out _));
		}

		[TestMethod]
		public void ResolvedValueConverter_TryConvert_NumberTargetTakesDecimalText()
		{
			var target = new NumberConfiguration { Name = "Gain" };

			Assert.IsTrue(ResolvedValueConverter.TryConvert(new StringResolvedValue("1.5"), target, out var converted));
			Assert.AreEqual(1.5m, converted.GetRawValue());
		}

		[TestMethod]
		public void ResolvedValueConverter_TryConvert_NumberTargetRejectsValueOutsideRange()
		{
			var target = new NumberConfiguration { Name = "Channels", RangeMin = 1, RangeMax = 8 };

			Assert.IsTrue(ResolvedValueConverter.TryConvert(new StringResolvedValue("8"), target, out _));
			Assert.IsFalse(ResolvedValueConverter.TryConvert(new StringResolvedValue("9"), target, out _));
			Assert.IsFalse(ResolvedValueConverter.TryConvert(new StringResolvedValue("0"), target, out _));
		}

		[TestMethod]
		public void ResolvedValueConverter_TryConvert_NumberCapacityTakesNumericText()
		{
			var target = new NumberCapacity { Name = "Bandwidth", RangeMin = 0 };

			Assert.IsTrue(ResolvedValueConverter.TryConvert(new StringResolvedValue("100"), target, out var converted));
			Assert.AreEqual(100m, converted.GetRawValue());
		}

		[TestMethod]
		public void ResolvedValueConverter_TryConvert_RangeCapacityKeepsValue()
		{
			// A range needs a minimum and a maximum, so a single value is left untouched for it.
			var target = new RangeCapacity { Name = "Window" };

			Assert.IsTrue(ResolvedValueConverter.TryConvert(new StringResolvedValue("5"), target, out var converted));
			Assert.AreEqual("5", converted.GetRawValue());
		}
	}
}
