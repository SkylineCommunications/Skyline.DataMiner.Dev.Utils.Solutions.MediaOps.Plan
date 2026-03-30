namespace RT_MediaOps.Plan.Automation.ScriptExecution
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;

	using ApiDataReference = Skyline.DataMiner.Solutions.MediaOps.Plan.API.DataReference;
	using StorageDataReference = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.DataReference;

	[TestClass]
	public sealed class DataReferenceInSettingsTests
	{
		private static ProfileParameterValue CreateProfileParameterValue(StorageDataReference reference)
		{
			return new ProfileParameterValue
			{
				ProfileParameterId = Guid.NewGuid(),
				Reference = reference,
			};
		}

		private static StorageDataReference CreateStorageReference()
		{
			return new StorageDataReference
			{
				ReferenceType = nameof(DataReferenceType.ResourceProperty),
				ReferenceId = "setting-ref-id",
			};
		}

		[TestMethod]
		public void StorageCapabilitySetting_WithReference_ParsesReference()
		{
			var ppv = CreateProfileParameterValue(CreateStorageReference());

			var setting = new StorageCapabilitySetting(ppv);

			Assert.IsNotNull(setting.Reference);
			Assert.AreEqual(DataReferenceType.ResourceProperty, setting.Reference.Type);
			Assert.AreEqual("setting-ref-id", setting.Reference.ReferenceId);
		}

		[TestMethod]
		public void StorageCapabilitySetting_NullReference_ReferenceIsNull()
		{
			var ppv = CreateProfileParameterValue(null);

			var setting = new StorageCapabilitySetting(ppv);

			Assert.IsNull(setting.Reference);
		}

		[TestMethod]
		public void StorageNumberCapacitySetting_WithReference_ParsesReference()
		{
			var ppv = CreateProfileParameterValue(CreateStorageReference());

			var setting = new StorageNumberCapacitySetting(ppv);

			Assert.IsNotNull(setting.Reference);
			Assert.AreEqual(DataReferenceType.ResourceProperty, setting.Reference.Type);
			Assert.AreEqual("setting-ref-id", setting.Reference.ReferenceId);
		}

		[TestMethod]
		public void StorageRangeCapacitySetting_WithReference_ParsesReference()
		{
			var ppv = CreateProfileParameterValue(CreateStorageReference());

			var setting = new StorageRangeCapacitySetting(ppv);

			Assert.IsNotNull(setting.Reference);
			Assert.AreEqual(DataReferenceType.ResourceProperty, setting.Reference.Type);
			Assert.AreEqual("setting-ref-id", setting.Reference.ReferenceId);
		}

		[TestMethod]
		public void StorageTextConfigurationSetting_WithReference_ParsesReference()
		{
			var ppv = CreateProfileParameterValue(CreateStorageReference());

			var setting = new StorageTextConfigurationSetting(ppv);

			Assert.IsNotNull(setting.Reference);
			Assert.AreEqual(DataReferenceType.ResourceProperty, setting.Reference.Type);
			Assert.AreEqual("setting-ref-id", setting.Reference.ReferenceId);
		}

		[TestMethod]
		public void StorageNumberConfigurationSetting_WithReference_ParsesReference()
		{
			var ppv = CreateProfileParameterValue(CreateStorageReference());

			var setting = new StorageNumberConfigurationSetting(ppv);

			Assert.IsNotNull(setting.Reference);
			Assert.AreEqual(DataReferenceType.ResourceProperty, setting.Reference.Type);
			Assert.AreEqual("setting-ref-id", setting.Reference.ReferenceId);
		}

		[TestMethod]
		public void StorageDiscreteTextConfigurationSetting_WithReference_ParsesReference()
		{
			var configuration = new DiscreteTextConfiguration()
				.AddDiscrete(new TextDiscreet("val1", "Value 1"));
			var ppv = CreateProfileParameterValue(CreateStorageReference());

			var setting = new StorageDiscreteTextConfigurationSetting(configuration, ppv);

			Assert.IsNotNull(setting.Reference);
			Assert.AreEqual(DataReferenceType.ResourceProperty, setting.Reference.Type);
			Assert.AreEqual("setting-ref-id", setting.Reference.ReferenceId);
		}

		[TestMethod]
		public void StorageDiscreteNumberConfigurationSetting_WithReference_ParsesReference()
		{
			var configuration = new DiscreteNumberConfiguration()
				.AddDiscrete(new NumberDiscreet(1m, "One"));
			var ppv = CreateProfileParameterValue(CreateStorageReference());

			var setting = new StorageDiscreteNumberConfigurationSetting(configuration, ppv);

			Assert.IsNotNull(setting.Reference);
			Assert.AreEqual(DataReferenceType.ResourceProperty, setting.Reference.Type);
			Assert.AreEqual("setting-ref-id", setting.Reference.ReferenceId);
		}

		[TestMethod]
		public void GetProfileParameterValueWithChanges_WithReference_RoundTripsReference()
		{
			var ppv = CreateProfileParameterValue(CreateStorageReference());
			var setting = new StorageNumberCapacitySetting(ppv);

			var result = setting.GetProfileParameterValueWithChanges();

			Assert.IsNotNull(result.Reference);
			Assert.AreEqual(nameof(DataReferenceType.ResourceProperty), result.Reference.ReferenceType);
			Assert.AreEqual("setting-ref-id", result.Reference.ReferenceId);
		}

		[TestMethod]
		public void GetProfileParameterValueWithChanges_NullReference_ReferenceIsNull()
		{
			var ppv = CreateProfileParameterValue(null);
			var setting = new StorageNumberCapacitySetting(ppv);

			var result = setting.GetProfileParameterValueWithChanges();

			Assert.IsNull(result.Reference);
		}
	}
}
