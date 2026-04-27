namespace RT_MediaOps.Plan.Generic.DataReferences
{
	using System;
	using System.Collections.Generic;

	using ApiDataReference = Skyline.DataMiner.Solutions.MediaOps.Plan.API.DataReference;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using StorageDataReference = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.DataReference;

	[TestClass]
	public sealed class DataReferenceTests
	{
		[TestMethod]
		public void ResourceNameReference_Constructor_SetsType()
		{
			var reference = new ResourceNameReference();

			Assert.AreEqual(DataReferenceType.ResourceName, reference.Type);
		}

		[TestMethod]
		public void ResourcePropertyReference_Constructor_SetsProperties()
		{
			var guid = new Guid("12345678-1234-1234-1234-123456789012");
			var reference = new ResourcePropertyReference(guid);

			Assert.AreEqual(DataReferenceType.ResourceProperty, reference.Type);
			Assert.AreEqual(guid, reference.ResourcePropertyId);
		}

		[TestMethod]
		public void ResourcePropertyReference_ToStorage_ReturnsCorrectStorageObject()
		{
			var guid = new Guid("12345678-1234-1234-1234-123456789012");
			var reference = new ResourcePropertyReference(guid);

			var storage = reference.ToStorage();

			Assert.IsNotNull(storage);
			Assert.AreEqual("ResourceProperty", storage.ReferenceType);
			Assert.IsNotNull(storage.ReferenceData);
			Assert.AreEqual(guid.ToString(), storage.ReferenceData["ResourcePropertyId"]);
		}

		[TestMethod]
		public void ResourceNameReference_ToStorage_HasNoReferenceData()
		{
			var reference = new ResourceNameReference();

			var storage = reference.ToStorage();

			Assert.IsNotNull(storage);
			Assert.AreEqual("ResourceName", storage.ReferenceType);
			Assert.IsNull(storage.ReferenceData);
		}

		[TestMethod]
		public void FromStorage_NullInput_ReturnsNull()
		{
			var result = ApiDataReference.FromStorage(null);

			Assert.IsNull(result);
		}

		[TestMethod]
		public void FromStorage_InvalidReferenceType_ReturnsNull()
		{
			var storage = new StorageDataReference
			{
				ReferenceType = "NonExistentType",
			};

			var result = ApiDataReference.FromStorage(storage);

			Assert.IsNull(result);
		}

		[TestMethod]
		public void FromStorage_ResourceLinkedObjectId_ReturnsResourceLinkedObjectIdReference()
		{
			var storage = new StorageDataReference
			{
				ReferenceType = "ResourceLinkedObjectID",
			};

			var result = ApiDataReference.FromStorage(storage);

			Assert.IsNotNull(result);
			Assert.IsInstanceOfType(result, typeof(ResourceLinkedObjectIdReference));
			Assert.AreEqual(DataReferenceType.ResourceLinkedObjectID, result.Type);
		}

		[TestMethod]
		public void FromStorage_NodeConfigurationParameter_ReturnsNodeConfigurationParameterReference()
		{
			var guid = new Guid("12345678-1234-1234-1234-123456789012");
			var storage = new StorageDataReference
			{
				ReferenceType = "ConfigurationParameter",
				ReferenceData = new Dictionary<string, string> { ["ParameterId"] = guid.ToString() },
			};

			var result = ApiDataReference.FromStorage(storage);

			Assert.IsNotNull(result);
			var configRef = result as ConfigurationParameterReference;
			Assert.IsNotNull(configRef);
			Assert.AreEqual(DataReferenceType.ConfigurationParameter, result.Type);
			Assert.AreEqual(guid, configRef.ParameterId);
		}

		[TestMethod]
		public void FromStorage_ResourceProperty_ReturnsResourcePropertyReference()
		{
			var guid = new Guid("12345678-1234-1234-1234-123456789012");
			var storage = new StorageDataReference
			{
				ReferenceType = "ResourceProperty",
				ReferenceData = new Dictionary<string, string> { ["ResourcePropertyId"] = guid.ToString() },
			};

			var result = ApiDataReference.FromStorage(storage);

			Assert.IsNotNull(result);
			var resourcePropertyRef = result as ResourcePropertyReference;
			Assert.IsNotNull(resourcePropertyRef);
			Assert.AreEqual(DataReferenceType.ResourceProperty, result.Type);
			Assert.AreEqual(guid, resourcePropertyRef.ResourcePropertyId);
		}

		[TestMethod]
		public void NodeConfigurationParameterReference_ToStorage_ThenFromStorage_PreservesParameterId()
		{
			var guid = Guid.NewGuid();
			var original = new ConfigurationParameterReference(guid);

			var result = ApiDataReference.FromStorage(original.ToStorage()) as ConfigurationParameterReference;

			Assert.IsNotNull(result);
			Assert.AreEqual(original.ParameterId, result.ParameterId);
		}

		[TestMethod]
		public void ResourcePropertyReference_ToStorage_ThenFromStorage_PreservesResourcePropertyId()
		{
			var guid = Guid.NewGuid();
			var original = new ResourcePropertyReference(guid);

			var result = ApiDataReference.FromStorage(original.ToStorage()) as ResourcePropertyReference;

			Assert.IsNotNull(result);
			Assert.AreEqual(original.ResourcePropertyId, result.ResourcePropertyId);
		}
	}
}
