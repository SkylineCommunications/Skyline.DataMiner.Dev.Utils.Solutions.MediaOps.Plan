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
				.AddDiscrete(new TextDiscrete("BRU", "Brussels"))
				.AddDiscrete(new TextDiscrete("PAR", "Paris"));

			Assert.IsTrue(ResolvedValueConverter.TryConvert(new StringResolvedValue("Brussels"), target, out var converted));
			Assert.AreEqual("BRU", converted.GetRawValue());
			Assert.AreEqual("Brussels", converted.DisplayValue);
		}

		[TestMethod]
		public void ResolvedValueConverter_TryConvert_DiscreteTextMatchesRawValue()
		{
			var target = new DiscreteTextConfiguration { Name = "Location" }
				.AddDiscrete(new TextDiscrete("BRU", "Brussels"));

			Assert.IsTrue(ResolvedValueConverter.TryConvert(new StringResolvedValue("BRU"), target, out var converted));
			Assert.AreEqual("BRU", converted.GetRawValue());
		}

		[TestMethod]
		public void ResolvedValueConverter_TryConvert_DiscreteTextUsesDisplayValueOfSource()
		{
			var target = new DiscreteTextConfiguration { Name = "Location" }
				.AddDiscrete(new TextDiscrete("BRU", "Brussels"));

			// The source is a dropdown holding raw value "1" shown as "Brussels".
			Assert.IsTrue(ResolvedValueConverter.TryConvert(new StringResolvedValue("1", "Brussels"), target, out var converted));
			Assert.AreEqual("BRU", converted.GetRawValue());
		}

		[TestMethod]
		public void ResolvedValueConverter_TryConvert_DiscreteNumberMatchesDisplayValueAndReturnsRawValue()
		{
			var target = new DiscreteNumberConfiguration { Name = "Channels" }
				.AddDiscrete(new NumberDiscrete(7, "Seven"));

			Assert.IsTrue(ResolvedValueConverter.TryConvert(new StringResolvedValue("Seven"), target, out var converted));
			Assert.AreEqual(7m, converted.GetRawValue());
		}

		[TestMethod]
		public void ResolvedValueConverter_TryConvert_DiscreteNumberWithoutMatchHasNoValue()
		{
			var target = new DiscreteNumberConfiguration { Name = "Channels" }
				.AddDiscrete(new NumberDiscrete(7, "Seven"));

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
	}
}
