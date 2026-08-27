namespace RT_MediaOps.Plan.Generic.DataReferences
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;

	/// <summary>
	/// Tests how a <see cref="DataReference"/> is persisted: its <see cref="DataReferenceStorage"/> shape, the
	/// round-trip back into a reference and the JSON serialization used to store it on a DOM field.
	/// </summary>
	[TestClass]
	public sealed class DataReferenceStorageTests
	{
		private static readonly Guid PayloadId = new Guid("12345678-1234-1234-1234-123456789012");

		public static IEnumerable<object[]> AllTypes => DataReferenceFactory.AllTypes;

		public static IEnumerable<object[]> TypesWithPayload => DataReferenceFactory.TypesWithPayload;

		[DataTestMethod]
		[DynamicData(nameof(AllTypes))]
		public void DataReferenceStorageTests_ToStorage_StoresTheTypeAndThePayload(DataReferenceType type)
		{
			var storage = DataReferenceFactory.Create(type, PayloadId).ToStorage();
			var payloadKey = DataReferenceFactory.GetPayloadKey(type);

			Assert.AreEqual(type.ToString(), storage.ReferenceType);

			if (payloadKey == null)
			{
				Assert.IsNull(storage.ReferenceData, $"'{type}' carries no payload, so no reference data must be written.");
			}
			else
			{
				Assert.IsNotNull(storage.ReferenceData);
				Assert.AreEqual(PayloadId.ToString(), storage.ReferenceData[payloadKey]);
				Assert.IsFalse(storage.ReferenceData.ContainsKey("NodeId"));
			}
		}

		[DataTestMethod]
		[DynamicData(nameof(AllTypes))]
		public void DataReferenceStorageTests_ToStorage_WithNodeId_StoresTheNodeId(DataReferenceType type)
		{
			var storage = DataReferenceFactory.Create(type, PayloadId, "node-1").ToStorage();

			Assert.IsNotNull(storage.ReferenceData);
			Assert.AreEqual("node-1", storage.ReferenceData["NodeId"]);
		}

		[DataTestMethod]
		[DynamicData(nameof(AllTypes))]
		public void DataReferenceStorageTests_RoundTrip_WithoutNodeId_PreservesTheReference(DataReferenceType type)
		{
			var original = DataReferenceFactory.Create(type, PayloadId);

			var result = DataReference.FromStorage(original.ToStorage());

			Assert.IsNotNull(result);
			Assert.IsInstanceOfType(result, original.GetType());
			Assert.AreEqual(original, result);
			Assert.IsNull(result.NodeId);
		}

		[DataTestMethod]
		[DynamicData(nameof(AllTypes))]
		public void DataReferenceStorageTests_RoundTrip_WithNodeId_PreservesTheReference(DataReferenceType type)
		{
			var original = DataReferenceFactory.Create(type, PayloadId, "node-1");

			var result = DataReference.FromStorage(original.ToStorage());

			Assert.IsNotNull(result);
			Assert.AreEqual(original, result);
			Assert.AreEqual("node-1", result.NodeId);
		}

		[DataTestMethod]
		[DynamicData(nameof(AllTypes))]
		public void DataReferenceStorageTests_JsonRoundTrip_PreservesTheReference(DataReferenceType type)
		{
			var original = DataReferenceFactory.Create(type, PayloadId, "node-1");

			var json = original.ToStorage().Serialize();

			Assert.IsTrue(DataReferenceStorage.TryDeserialize(json, out var storage));
			Assert.AreEqual(original, DataReference.FromStorage(storage));
		}

		[TestMethod]
		public void DataReferenceStorageTests_FromStorage_Null_ReturnsNull()
		{
			Assert.IsNull(DataReference.FromStorage(null));
		}

		[TestMethod]
		public void DataReferenceStorageTests_FromStorage_UnknownReferenceType_ReturnsNull()
		{
			var storage = new DataReferenceStorage { ReferenceType = "NonExistentType" };

			Assert.IsNull(DataReference.FromStorage(storage));
		}

		[DataTestMethod]
		[DynamicData(nameof(TypesWithPayload))]
		public void DataReferenceStorageTests_FromStorage_MissingPayload_ReturnsNull(DataReferenceType type)
		{
			var storage = new DataReferenceStorage { ReferenceType = type.ToString() };

			Assert.IsNull(DataReference.FromStorage(storage), $"'{type}' must not be restored without its identifier.");
		}

		[DataTestMethod]
		[DynamicData(nameof(TypesWithPayload))]
		public void DataReferenceStorageTests_FromStorage_InvalidPayload_ReturnsNull(DataReferenceType type)
		{
			var storage = new DataReferenceStorage
			{
				ReferenceType = type.ToString(),
				ReferenceData = new Dictionary<string, string> { [DataReferenceFactory.GetPayloadKey(type)!] = "not-a-guid" },
			};

			Assert.IsNull(DataReference.FromStorage(storage), $"'{type}' must not be restored from an invalid identifier.");
		}

		[TestMethod]
		public void DataReferenceStorageTests_FromStorage_EmptyNodeId_RestoresReferenceWithoutNodeId()
		{
			var storage = new DataReferenceStorage
			{
				ReferenceType = nameof(DataReferenceType.ResourceName),
				ReferenceData = new Dictionary<string, string> { ["NodeId"] = String.Empty },
			};

			var result = DataReference.FromStorage(storage);

			Assert.IsInstanceOfType(result, typeof(ResourceNameReference));
			Assert.IsNull(result.NodeId);
		}

		[TestMethod]
		public void DataReferenceStorageTests_TryDeserialize_InvalidJson_ReturnsFalse()
		{
			Assert.IsFalse(DataReferenceStorage.TryDeserialize("not json", out var result));
			Assert.IsNull(result);
		}

		[TestMethod]
		public void DataReferenceStorageTests_TryDeserialize_NullOrEmpty_ReturnsFalse()
		{
			Assert.IsFalse(DataReferenceStorage.TryDeserialize(null, out _));
			Assert.IsFalse(DataReferenceStorage.TryDeserialize(String.Empty, out _));
		}

		[DataTestMethod]
		[DynamicData(nameof(AllTypes))]
		public void DataReferenceStorageTests_Equality_SameReference_IsEqual(DataReferenceType type)
		{
			var first = DataReferenceFactory.Create(type, PayloadId, "node-1").ToStorage();
			var second = DataReferenceFactory.Create(type, PayloadId, "node-1").ToStorage();

			Assert.IsTrue(first.Equals(second));
			Assert.AreEqual(first.GetHashCode(), second.GetHashCode());
		}

		[DataTestMethod]
		[DynamicData(nameof(AllTypes))]
		public void DataReferenceStorageTests_Equality_DifferentNodeId_IsNotEqual(DataReferenceType type)
		{
			var first = DataReferenceFactory.Create(type, PayloadId, "node-1").ToStorage();
			var second = DataReferenceFactory.Create(type, PayloadId, "node-2").ToStorage();

			Assert.IsFalse(first.Equals(second));
		}

		[DataTestMethod]
		[DynamicData(nameof(TypesWithPayload))]
		public void DataReferenceStorageTests_Equality_DifferentPayload_IsNotEqual(DataReferenceType type)
		{
			var first = DataReferenceFactory.Create(type, Guid.NewGuid()).ToStorage();
			var second = DataReferenceFactory.Create(type, Guid.NewGuid()).ToStorage();

			Assert.IsFalse(first.Equals(second));
		}

		[TestMethod]
		public void DataReferenceStorageTests_Equality_Null_IsNotEqual()
		{
			var storage = new ResourceNameReference().ToStorage();

			Assert.IsFalse(storage.Equals(null));
		}
	}
}
