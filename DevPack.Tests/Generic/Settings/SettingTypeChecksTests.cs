namespace RT_MediaOps.Plan.Generic.Settings
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	public sealed class SettingTypeChecksTests
	{
		[TestMethod]
		public void ConfigurationSetting_IsNumberConfiguration_ReturnsConcreteType()
		{
			ConfigurationSetting setting = new NumberConfigurationSetting(Guid.NewGuid());

			Assert.IsTrue(setting.IsNumberConfiguration(out NumberConfigurationSetting asNumber));
			Assert.AreSame(setting, asNumber);

			Assert.IsFalse(setting.IsDiscreteNumberConfiguration(out DiscreteNumberConfigurationSetting asDiscreteNumber));
			Assert.IsNull(asDiscreteNumber);
			Assert.IsFalse(setting.IsTextConfiguration(out TextConfigurationSetting asText));
			Assert.IsNull(asText);
			Assert.IsFalse(setting.IsDiscreteTextConfiguration(out DiscreteTextConfigurationSetting asDiscreteText));
			Assert.IsNull(asDiscreteText);
		}

		[TestMethod]
		public void ConfigurationSetting_IsDiscreteNumberConfiguration_ReturnsConcreteType()
		{
			ConfigurationSetting setting = new DiscreteNumberConfigurationSetting(Guid.NewGuid());

			Assert.IsTrue(setting.IsDiscreteNumberConfiguration(out DiscreteNumberConfigurationSetting asDiscreteNumber));
			Assert.AreSame(setting, asDiscreteNumber);

			Assert.IsFalse(setting.IsNumberConfiguration(out NumberConfigurationSetting asNumber));
			Assert.IsNull(asNumber);
			Assert.IsFalse(setting.IsTextConfiguration(out TextConfigurationSetting asText));
			Assert.IsNull(asText);
			Assert.IsFalse(setting.IsDiscreteTextConfiguration(out DiscreteTextConfigurationSetting asDiscreteText));
			Assert.IsNull(asDiscreteText);
		}

		[TestMethod]
		public void ConfigurationSetting_IsTextConfiguration_ReturnsConcreteType()
		{
			ConfigurationSetting setting = new TextConfigurationSetting(Guid.NewGuid());

			Assert.IsTrue(setting.IsTextConfiguration(out TextConfigurationSetting asText));
			Assert.AreSame(setting, asText);

			Assert.IsFalse(setting.IsNumberConfiguration(out NumberConfigurationSetting asNumber));
			Assert.IsNull(asNumber);
			Assert.IsFalse(setting.IsDiscreteNumberConfiguration(out DiscreteNumberConfigurationSetting asDiscreteNumber));
			Assert.IsNull(asDiscreteNumber);
			Assert.IsFalse(setting.IsDiscreteTextConfiguration(out DiscreteTextConfigurationSetting asDiscreteText));
			Assert.IsNull(asDiscreteText);
		}

		[TestMethod]
		public void ConfigurationSetting_IsDiscreteTextConfiguration_ReturnsConcreteType()
		{
			ConfigurationSetting setting = new DiscreteTextConfigurationSetting(Guid.NewGuid());

			Assert.IsTrue(setting.IsDiscreteTextConfiguration(out DiscreteTextConfigurationSetting asDiscreteText));
			Assert.AreSame(setting, asDiscreteText);

			Assert.IsFalse(setting.IsNumberConfiguration(out NumberConfigurationSetting asNumber));
			Assert.IsNull(asNumber);
			Assert.IsFalse(setting.IsDiscreteNumberConfiguration(out DiscreteNumberConfigurationSetting asDiscreteNumber));
			Assert.IsNull(asDiscreteNumber);
			Assert.IsFalse(setting.IsTextConfiguration(out TextConfigurationSetting asText));
			Assert.IsNull(asText);
		}

		[TestMethod]
		public void CapacitySetting_IsNumberCapacity_ReturnsConcreteType()
		{
			CapacitySetting setting = new NumberCapacitySetting(Guid.NewGuid());

			Assert.IsTrue(setting.IsNumberCapacity(out NumberCapacitySetting asNumber));
			Assert.AreSame(setting, asNumber);

			Assert.IsFalse(setting.IsRangeCapacity(out RangeCapacitySetting asRange));
			Assert.IsNull(asRange);
		}

		[TestMethod]
		public void CapacitySetting_IsRangeCapacity_ReturnsConcreteType()
		{
			CapacitySetting setting = new RangeCapacitySetting(Guid.NewGuid());

			Assert.IsTrue(setting.IsRangeCapacity(out RangeCapacitySetting asRange));
			Assert.AreSame(setting, asRange);

			Assert.IsFalse(setting.IsNumberCapacity(out NumberCapacitySetting asNumber));
			Assert.IsNull(asNumber);
		}
	}
}
