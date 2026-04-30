namespace RT_MediaOps.Plan.Automation.ScriptExecution
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;

	[TestClass]
	public sealed class DataReferenceInSettingsTests
	{
		private static readonly Guid TestResourcePropertyId = new Guid("12345678-1234-1234-1234-123456789012");

		private static ProfileParameterValue CreateProfileParameterValue(DataReferenceStorage reference)
		{
			return new ProfileParameterValue
			{
				ProfileParameterId = Guid.NewGuid(),
				Reference = reference,
			};
		}

		private static DataReferenceStorage CreateStorageReference()
		{
			return new DataReferenceStorage
			{
				ReferenceType = nameof(DataReferenceType.ResourceProperty),
				ReferenceData = new Dictionary<string, string> { ["ResourcePropertyId"] = TestResourcePropertyId.ToString() },
			};
		}

		[TestMethod]
		public void StorageCapabilitySetting_WithReference_ParsesReference()
		{
			var ppv = CreateProfileParameterValue(CreateStorageReference());

			var setting = new StorageCapabilitySetting(ppv);

			Assert.IsNotNull(setting.Reference);
			Assert.AreEqual(DataReferenceType.ResourceProperty, setting.Reference.Type);
			var resourcePropertyRef = setting.Reference as ResourcePropertyReference;
			Assert.IsNotNull(resourcePropertyRef);
			Assert.AreEqual(TestResourcePropertyId, resourcePropertyRef.ResourcePropertyId);
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
			var resourcePropertyRef = setting.Reference as ResourcePropertyReference;
			Assert.IsNotNull(resourcePropertyRef);
			Assert.AreEqual(TestResourcePropertyId, resourcePropertyRef.ResourcePropertyId);
		}

		[TestMethod]
		public void StorageRangeCapacitySetting_WithReference_ParsesReference()
		{
			var ppv = CreateProfileParameterValue(CreateStorageReference());

			var setting = new StorageRangeCapacitySetting(ppv);

			Assert.IsNotNull(setting.Reference);
			Assert.AreEqual(DataReferenceType.ResourceProperty, setting.Reference.Type);
			var resourcePropertyRef = setting.Reference as ResourcePropertyReference;
			Assert.IsNotNull(resourcePropertyRef);
			Assert.AreEqual(TestResourcePropertyId, resourcePropertyRef.ResourcePropertyId);
		}

		[TestMethod]
		public void StorageTextConfigurationSetting_WithReference_ParsesReference()
		{
			var ppv = CreateProfileParameterValue(CreateStorageReference());

			var setting = new StorageTextConfigurationSetting(ppv);

			Assert.IsNotNull(setting.Reference);
			Assert.AreEqual(DataReferenceType.ResourceProperty, setting.Reference.Type);
			var resourcePropertyRef = setting.Reference as ResourcePropertyReference;
			Assert.IsNotNull(resourcePropertyRef);
			Assert.AreEqual(TestResourcePropertyId, resourcePropertyRef.ResourcePropertyId);
		}

		[TestMethod]
		public void StorageNumberConfigurationSetting_WithReference_ParsesReference()
		{
			var ppv = CreateProfileParameterValue(CreateStorageReference());

			var setting = new StorageNumberConfigurationSetting(ppv);

			Assert.IsNotNull(setting.Reference);
			Assert.AreEqual(DataReferenceType.ResourceProperty, setting.Reference.Type);
			var resourcePropertyRef = setting.Reference as ResourcePropertyReference;
			Assert.IsNotNull(resourcePropertyRef);
			Assert.AreEqual(TestResourcePropertyId, resourcePropertyRef.ResourcePropertyId);
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
			var resourcePropertyRef = setting.Reference as ResourcePropertyReference;
			Assert.IsNotNull(resourcePropertyRef);
			Assert.AreEqual(TestResourcePropertyId, resourcePropertyRef.ResourcePropertyId);
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
			var resourcePropertyRef = setting.Reference as ResourcePropertyReference;
			Assert.IsNotNull(resourcePropertyRef);
			Assert.AreEqual(TestResourcePropertyId, resourcePropertyRef.ResourcePropertyId);
		}

		[TestMethod]
		public void GetProfileParameterValueWithChanges_WithReference_RoundTripsReference()
		{
			var ppv = CreateProfileParameterValue(CreateStorageReference());
			var setting = new StorageNumberCapacitySetting(ppv);

			var result = setting.GetProfileParameterValueWithChanges();

			Assert.IsNotNull(result.Reference);
			Assert.AreEqual(nameof(DataReferenceType.ResourceProperty), result.Reference.ReferenceType);
			Assert.IsNotNull(result.Reference.ReferenceData);
			Assert.AreEqual(TestResourcePropertyId.ToString(), result.Reference.ReferenceData["ResourcePropertyId"]);
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
