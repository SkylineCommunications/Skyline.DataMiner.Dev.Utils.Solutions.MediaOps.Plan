namespace RT_MediaOps.Plan.Generic.Settings
{
	using System;

	using Skyline.DataMiner.Core.DataMinerSystem.Common;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	[TestClass]
	public sealed class SettingValueReferenceTests
	{
		[TestMethod]
		public void Setting_ValueSet_ClearsReference()
		{
			AssertValueClearsReference(new CapabilitySetting(Guid.NewGuid()), setting => setting.Value = "value");
			AssertValueClearsReference(new NumberCapacitySetting(Guid.NewGuid()), setting => setting.Value = 1);
			AssertValueClearsReference(new RangeCapacitySetting(Guid.NewGuid()), setting => setting.MinValue = 1);
			AssertValueClearsReference(new RangeCapacitySetting(Guid.NewGuid()), setting => setting.MaxValue = 2);
			AssertValueClearsReference(new NumberConfigurationSetting(Guid.NewGuid()), setting => setting.Value = 1);
			AssertValueClearsReference(new TextConfigurationSetting(Guid.NewGuid()), setting => setting.Value = "value");
			AssertValueClearsReference(new DiscreteNumberConfigurationSetting(Guid.NewGuid()), setting => setting.Value = new NumberDiscreet(1, "one"));
			AssertValueClearsReference(new DiscreteTextConfigurationSetting(Guid.NewGuid()), setting => setting.Value = new TextDiscreet("one", "one"));
		}

		[TestMethod]
		public void Setting_ReferenceSet_ClearsValue()
		{
			AssertReferenceClearsValue(new CapabilitySetting(Guid.NewGuid()) { Value = "value" });
			AssertReferenceClearsValue(new NumberCapacitySetting(Guid.NewGuid()) { Value = 1 });
			AssertReferenceClearsValue(new NumberConfigurationSetting(Guid.NewGuid()) { Value = 1 });
			AssertReferenceClearsValue(new TextConfigurationSetting(Guid.NewGuid()) { Value = "value" });
			AssertReferenceClearsValue(new DiscreteNumberConfigurationSetting(Guid.NewGuid()) { Value = new NumberDiscreet(1, "one") });
			AssertReferenceClearsValue(new DiscreteTextConfigurationSetting(Guid.NewGuid()) { Value = new TextDiscreet("one", "one") });

			var rangeSetting = new RangeCapacitySetting(Guid.NewGuid()) { MinValue = 1, MaxValue = 2 };
			AssertReferenceClearsValue(rangeSetting);
			Assert.IsNull(rangeSetting.MinValue);
			Assert.IsNull(rangeSetting.MaxValue);
		}

		[TestMethod]
		public void Setting_NullValueSet_DoesNotClearReference()
		{
			AssertNullValuePreservesReference(new CapabilitySetting(Guid.NewGuid()), setting => setting.Value = null);
			AssertNullValuePreservesReference(new NumberCapacitySetting(Guid.NewGuid()), setting => setting.Value = null);
			AssertNullValuePreservesReference(new RangeCapacitySetting(Guid.NewGuid()), setting => setting.MinValue = null);
			AssertNullValuePreservesReference(new RangeCapacitySetting(Guid.NewGuid()), setting => setting.MaxValue = null);
			AssertNullValuePreservesReference(new NumberConfigurationSetting(Guid.NewGuid()), setting => setting.Value = null);
			AssertNullValuePreservesReference(new TextConfigurationSetting(Guid.NewGuid()), setting => setting.Value = null);
			AssertNullValuePreservesReference(new DiscreteNumberConfigurationSetting(Guid.NewGuid()), setting => setting.Value = null);
			AssertNullValuePreservesReference(new DiscreteTextConfigurationSetting(Guid.NewGuid()), setting => setting.Value = null);
		}

		[TestMethod]
		public void ScriptParameterSetting_ValueSet_ClearsReference()
		{
			var setting = new ScriptParameterSetting("parameter") { Reference = new ResourceNameReference() };

			setting.Value = "value";

			Assert.IsNull(setting.Reference);
		}

		[TestMethod]
		public void ScriptParameterSetting_ReferenceSet_ClearsValue()
		{
			var setting = new ScriptParameterSetting("parameter") { Value = "value" };

			setting.Reference = new ResourceNameReference();

			Assert.IsNull(setting.Value);
		}

		[TestMethod]
		public void ScriptElementSetting_ValueSet_ClearsReference()
		{
			var setting = new ScriptElementSetting("dummy") { Reference = new ResourceNameReference() };
			setting.DmsElementId = new DmsElementId(1, 2);
			Assert.IsNull(setting.Reference);

			setting.Reference = new ResourceNameReference();
			setting.ElementName = "element";
			Assert.IsNull(setting.Reference);
		}

		[TestMethod]
		public void ScriptElementSetting_ReferenceSet_ClearsValues()
		{
			var setting = new ScriptElementSetting("dummy")
			{
				DmsElementId = new DmsElementId(1, 2),
				ElementName = "element",
			};

			setting.Reference = new ResourceNameReference();

			Assert.AreEqual(default, setting.DmsElementId);
			Assert.IsNull(setting.ElementName);
		}

		[TestMethod]
		public void ScriptSettings_NullValueSet_DoesNotClearReference()
		{
			var parameterSetting = new ScriptParameterSetting("parameter") { Reference = new ResourceNameReference() };
			parameterSetting.Value = null;
			Assert.IsNotNull(parameterSetting.Reference);

			var elementSetting = new ScriptElementSetting("dummy") { Reference = new ResourceNameReference() };
			elementSetting.DmsElementId = default;
			elementSetting.ElementName = null;
			Assert.IsNotNull(elementSetting.Reference);
		}

		private static void AssertValueClearsReference<TSetting>(TSetting setting, Action<TSetting> setValue)
			where TSetting : Setting
		{
			setting.Reference = new ResourceNameReference();

			setValue(setting);

			Assert.IsNull(setting.Reference, setting.GetType().Name);
		}

		private static void AssertReferenceClearsValue(Setting setting)
		{
			setting.Reference = new ResourceNameReference();

			Assert.IsFalse(setting.HasValue, setting.GetType().Name);
		}

		private static void AssertNullValuePreservesReference<TSetting>(TSetting setting, Action<TSetting> setValue)
			where TSetting : Setting
		{
			setting.Reference = new ResourceNameReference();

			setValue(setting);

			Assert.IsNotNull(setting.Reference, setting.GetType().Name);
		}
	}
}
