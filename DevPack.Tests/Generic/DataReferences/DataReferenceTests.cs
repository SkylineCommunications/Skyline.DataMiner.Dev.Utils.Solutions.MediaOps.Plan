namespace RT_MediaOps.Plan.Generic.DataReferences
{
	using ApiDataReference = Skyline.DataMiner.Solutions.MediaOps.Plan.API.DataReference;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using StorageDataReference = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.DataReference;

	[TestClass]
	public sealed class DataReferenceTests
	{
		[TestMethod]
		public void Constructor_TypeOnly_SetsTypeAndNullReferenceId()
		{
			var reference = new ApiDataReference(DataReferenceType.ResourceName);

			Assert.AreEqual(DataReferenceType.ResourceName, reference.Type);
			Assert.IsNull(reference.ReferenceId);
		}

		[TestMethod]
		public void Constructor_TypeAndReferenceId_SetsBothProperties()
		{
			var reference = new ApiDataReference(DataReferenceType.ResourceProperty, "my-id");

			Assert.AreEqual(DataReferenceType.ResourceProperty, reference.Type);
			Assert.AreEqual("my-id", reference.ReferenceId);
		}

		[TestMethod]
		public void ToStorage_ReturnsCorrectStorageObject()
		{
			var reference = new ApiDataReference(DataReferenceType.ResourceProperty, "my-id");

			var storage = reference.ToStorage();

			Assert.IsNotNull(storage);
			Assert.AreEqual("ResourceProperty", storage.ReferenceType);
			Assert.AreEqual("my-id", storage.ReferenceId);
		}

		[TestMethod]
		public void ToStorage_NullReferenceId_StorageReferenceIdIsNull()
		{
			var reference = new ApiDataReference(DataReferenceType.ResourceName);

			var storage = reference.ToStorage();

			Assert.IsNotNull(storage);
			Assert.AreEqual("ResourceName", storage.ReferenceType);
			Assert.IsNull(storage.ReferenceId);
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
				ReferenceId = "some-id",
			};

			var result = ApiDataReference.FromStorage(storage);

			Assert.IsNull(result);
		}

		[TestMethod]
		public void FromStorage_ValidInput_ReturnsMappedDataReference()
		{
			var storage = new StorageDataReference
			{
				ReferenceType = "ResourceLinkedObjectID",
				ReferenceId = "abc-123",
			};

			var result = ApiDataReference.FromStorage(storage);

			Assert.IsNotNull(result);
			Assert.AreEqual(DataReferenceType.ResourceLinkedObjectID, result.Type);
			Assert.AreEqual("abc-123", result.ReferenceId);
		}

		[TestMethod]
		public void FromStorage_ValidInputWithNullReferenceId_ReturnsMappedDataReference()
		{
			var storage = new StorageDataReference
			{
				ReferenceType = "SchedulingConfigurationParameter",
				ReferenceId = null,
			};

			var result = ApiDataReference.FromStorage(storage);

			Assert.IsNotNull(result);
			Assert.AreEqual(DataReferenceType.SchedulingConfigurationParameter, result.Type);
			Assert.IsNull(result.ReferenceId);
		}

		[TestMethod]
		public void ToStorage_ThenFromStorage_PreservesValues()
		{
			var original = new ApiDataReference(DataReferenceType.ResourceProperty, "round-trip-id");

			var result = ApiDataReference.FromStorage(original.ToStorage());

			Assert.IsNotNull(result);
			Assert.AreEqual(original.Type, result.Type);
			Assert.AreEqual(original.ReferenceId, result.ReferenceId);
		}
	}
}
