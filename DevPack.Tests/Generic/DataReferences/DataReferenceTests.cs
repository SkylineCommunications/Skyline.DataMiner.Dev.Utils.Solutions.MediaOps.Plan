namespace RT_MediaOps.Plan.Generic.DataReferences
{
	using System;
	using System.Collections.Generic;

	using ApiDataReference = Skyline.DataMiner.Solutions.MediaOps.Plan.API.DataReference;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;

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
			var storage = new DataReferenceStorage
			{
				ReferenceType = "NonExistentType",
			};

			var result = ApiDataReference.FromStorage(storage);

			Assert.IsNull(result);
		}

		[TestMethod]
		public void FromStorage_ResourceLinkedObjectId_ReturnsResourceLinkedObjectIdReference()
		{
			var storage = new DataReferenceStorage
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
			var storage = new DataReferenceStorage
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
			var storage = new DataReferenceStorage
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

		[TestMethod]
		public void JobPropertyReference_Constructor_SetsProperties()
		{
			var guid = Guid.NewGuid();
			var reference = new JobPropertyReference(guid);

			Assert.AreEqual(DataReferenceType.JobProperty, reference.Type);
			Assert.AreEqual(guid, reference.JobPropertyId);
			Assert.IsNull(reference.NodeId);
		}

		[TestMethod]
		public void JobPropertyReference_ToStorage_ThenFromStorage_PreservesData()
		{
			var guid = Guid.NewGuid();
			var original = new JobPropertyReference(guid);

			var result = ApiDataReference.FromStorage(original.ToStorage()) as JobPropertyReference;

			Assert.IsNotNull(result);
			Assert.AreEqual(original.JobPropertyId, result.JobPropertyId);
		}

		[TestMethod]
		public void WorkflowPropertyReference_Constructor_SetsProperties()
		{
			var guid = Guid.NewGuid();
			var reference = new WorkflowPropertyReference(guid);

			Assert.AreEqual(DataReferenceType.WorkflowProperty, reference.Type);
			Assert.AreEqual(guid, reference.WorkflowPropertyId);
			Assert.IsNull(reference.NodeId);
		}

		[TestMethod]
		public void WorkflowPropertyReference_ToStorage_ThenFromStorage_PreservesData()
		{
			var guid = Guid.NewGuid();
			var original = new WorkflowPropertyReference(guid);

			var result = ApiDataReference.FromStorage(original.ToStorage()) as WorkflowPropertyReference;

			Assert.IsNotNull(result);
			Assert.AreEqual(original.WorkflowPropertyId, result.WorkflowPropertyId);
		}

		[TestMethod]
		public void CapabilityParameterReference_Constructor_SetsProperties()
		{
			var guid = Guid.NewGuid();
			var reference = new CapabilityParameterReference(guid);

			Assert.AreEqual(DataReferenceType.CapabilityParameter, reference.Type);
			Assert.AreEqual(guid, reference.ParameterId);
		}

		[TestMethod]
		public void CapabilityParameterReference_ToStorage_ThenFromStorage_PreservesData()
		{
			var guid = Guid.NewGuid();
			var original = new CapabilityParameterReference(guid);

			var result = ApiDataReference.FromStorage(original.ToStorage()) as CapabilityParameterReference;

			Assert.IsNotNull(result);
			Assert.AreEqual(original.ParameterId, result.ParameterId);
		}

		[TestMethod]
		public void CapacityParameterReference_Constructor_SetsProperties()
		{
			var guid = Guid.NewGuid();
			var reference = new CapacityParameterReference(guid);

			Assert.AreEqual(DataReferenceType.CapacityParameter, reference.Type);
			Assert.AreEqual(guid, reference.ParameterId);
		}

		[TestMethod]
		public void CapacityParameterReference_ToStorage_ThenFromStorage_PreservesData()
		{
			var guid = Guid.NewGuid();
			var original = new CapacityParameterReference(guid);

			var result = ApiDataReference.FromStorage(original.ToStorage()) as CapacityParameterReference;

			Assert.IsNotNull(result);
			Assert.AreEqual(original.ParameterId, result.ParameterId);
		}

		[TestMethod]
		public void WorkflowNameReference_Constructor_SetsType()
		{
			var reference = new WorkflowNameReference();

			Assert.AreEqual(DataReferenceType.WorkflowName, reference.Type);
			Assert.IsNull(reference.NodeId);
		}

		[TestMethod]
		public void JobNameReference_Constructor_SetsType()
		{
			var reference = new JobNameReference();

			Assert.AreEqual(DataReferenceType.JobName, reference.Type);
			Assert.IsNull(reference.NodeId);
		}

		[TestMethod]
		public void FromStorage_JobName_ReturnsJobNameReference()
		{
			var storage = new DataReferenceStorage
			{
				ReferenceType = "JobName",
			};

			var result = ApiDataReference.FromStorage(storage);

			Assert.IsNotNull(result);
			Assert.IsInstanceOfType(result, typeof(JobNameReference));
			Assert.AreEqual(DataReferenceType.JobName, result.Type);
		}

		[TestMethod]
		public void FromStorage_WorkflowName_ReturnsWorkflowNameReference()
		{
			var storage = new DataReferenceStorage
			{
				ReferenceType = "WorkflowName",
			};

			var result = ApiDataReference.FromStorage(storage);

			Assert.IsNotNull(result);
			Assert.IsInstanceOfType(result, typeof(WorkflowNameReference));
			Assert.AreEqual(DataReferenceType.WorkflowName, result.Type);
		}

		[TestMethod]
		public void DataReference_WithNodeId_PreservesNodeIdThroughRoundTrip()
		{
			var guid = Guid.NewGuid();
			var nodeId = "node-42";
			var original = new ResourcePropertyReference(guid, nodeId);

			Assert.AreEqual(nodeId, original.NodeId);

			var result = ApiDataReference.FromStorage(original.ToStorage()) as ResourcePropertyReference;

			Assert.IsNotNull(result);
			Assert.AreEqual(nodeId, result.NodeId);
			Assert.AreEqual(guid, result.ResourcePropertyId);
		}

		[TestMethod]
		public void DataReference_Equals_SameTypeAndData_ReturnsTrue()
		{
			var guid = Guid.NewGuid();
			var ref1 = new ResourcePropertyReference(guid);
			var ref2 = new ResourcePropertyReference(guid);

			Assert.IsTrue(ref1.Equals(ref2));
			Assert.IsTrue(ref2.Equals(ref1));
		}

		[TestMethod]
		public void DataReference_Equals_DifferentData_ReturnsFalse()
		{
			var ref1 = new ResourcePropertyReference(Guid.NewGuid());
			var ref2 = new ResourcePropertyReference(Guid.NewGuid());

			Assert.IsFalse(ref1.Equals(ref2));
		}

		[TestMethod]
		public void DataReference_Equals_DifferentType_ReturnsFalse()
		{
			var ref1 = new ResourceNameReference();
			var ref2 = new WorkflowNameReference();

			Assert.IsFalse(ref1.Equals((ApiDataReference)ref2));
		}

		[TestMethod]
		public void DataReference_Equals_DifferentNodeId_ReturnsFalse()
		{
			var guid = Guid.NewGuid();
			var ref1 = new ResourcePropertyReference(guid, "node-1");
			var ref2 = new ResourcePropertyReference(guid, "node-2");

			Assert.IsFalse(ref1.Equals(ref2));
		}

		[TestMethod]
		public void DataReference_GetHashCode_EqualObjects_SameHashCode()
		{
			var guid = Guid.NewGuid();
			var ref1 = new ResourcePropertyReference(guid);
			var ref2 = new ResourcePropertyReference(guid);

			Assert.AreEqual(ref1.GetHashCode(), ref2.GetHashCode());
		}

		[TestMethod]
		public void DataReference_ToString_WithoutNodeId_ReturnsType()
		{
			var reference = new ResourceNameReference();

			Assert.AreEqual("ResourceName", reference.ToString());
		}

		[TestMethod]
		public void DataReference_ToString_WithNodeId_IncludesNodeId()
		{
			var reference = new ResourceNameReference("node-5");

			Assert.AreEqual("ResourceName (NodeId: node-5)", reference.ToString());
		}

		[TestMethod]
		public void IsNodeScoped_ResourceName_ReturnsTrue()
		{
			Assert.IsTrue(DataReferenceType.ResourceName.IsNodeScoped());
		}

		[TestMethod]
		public void IsNodeScoped_ResourceProperty_ReturnsTrue()
		{
			Assert.IsTrue(DataReferenceType.ResourceProperty.IsNodeScoped());
		}

		[TestMethod]
		public void IsNodeScoped_ConfigurationParameter_ReturnsTrue()
		{
			Assert.IsTrue(DataReferenceType.ConfigurationParameter.IsNodeScoped());
		}

		[TestMethod]
		public void IsNodeScoped_CapabilityParameter_ReturnsTrue()
		{
			Assert.IsTrue(DataReferenceType.CapabilityParameter.IsNodeScoped());
		}

		[TestMethod]
		public void IsNodeScoped_CapacityParameter_ReturnsTrue()
		{
			Assert.IsTrue(DataReferenceType.CapacityParameter.IsNodeScoped());
		}

		[TestMethod]
		public void IsNodeScoped_JobProperty_ReturnsFalse()
		{
			Assert.IsFalse(DataReferenceType.JobProperty.IsNodeScoped());
		}

		[TestMethod]
		public void IsNodeScoped_WorkflowProperty_ReturnsFalse()
		{
			Assert.IsFalse(DataReferenceType.WorkflowProperty.IsNodeScoped());
		}

		[TestMethod]
		public void IsNodeScoped_JobName_ReturnsFalse()
		{
			Assert.IsFalse(DataReferenceType.JobName.IsNodeScoped());
		}

		[TestMethod]
		public void IsNodeScoped_WorkflowName_ReturnsFalse()
		{
			Assert.IsFalse(DataReferenceType.WorkflowName.IsNodeScoped());
		}

		[TestMethod]
		public void DataReferenceStorage_Serialize_ThenDeserialize_RoundTrips()
		{
			var guid = Guid.NewGuid();
			var original = new ResourcePropertyReference(guid, "node-1");
			var storage = original.ToStorage();

			var json = storage.Serialize();
			Assert.IsTrue(DataReferenceStorage.TryDeserialize(json, out var deserialized));

			var result = ApiDataReference.FromStorage(deserialized) as ResourcePropertyReference;
			Assert.IsNotNull(result);
			Assert.AreEqual(guid, result.ResourcePropertyId);
			Assert.AreEqual("node-1", result.NodeId);
		}

		[TestMethod]
		public void DataReferenceStorage_TryDeserialize_InvalidJson_ReturnsFalse()
		{
			Assert.IsFalse(DataReferenceStorage.TryDeserialize("not json", out var result));
			Assert.IsNull(result);
		}

		[TestMethod]
		public void DataReferenceStorage_TryDeserialize_NullOrEmpty_ReturnsFalse()
		{
			Assert.IsFalse(DataReferenceStorage.TryDeserialize(null, out _));
			Assert.IsFalse(DataReferenceStorage.TryDeserialize(string.Empty, out _));
		}

		[TestMethod]
		public void DataReferenceStorage_Equals_SameContent_ReturnsTrue()
		{
			var guid = Guid.NewGuid();
			var ref1 = new ResourcePropertyReference(guid);
			var ref2 = new ResourcePropertyReference(guid);

			Assert.IsTrue(ref1.ToStorage().Equals(ref2.ToStorage()));
		}

		[TestMethod]
		public void DataReferenceStorage_Equals_DifferentContent_ReturnsFalse()
		{
			var ref1 = new ResourcePropertyReference(Guid.NewGuid());
			var ref2 = new ResourcePropertyReference(Guid.NewGuid());

			Assert.IsFalse(ref1.ToStorage().Equals(ref2.ToStorage()));
		}
	}
}
