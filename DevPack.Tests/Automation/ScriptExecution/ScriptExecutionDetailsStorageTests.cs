namespace RT_MediaOps.Plan.Automation.ScriptExecution
{
	using System;

	using StorageDOM = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;

	[TestClass]
	public sealed class ScriptExecutionDetailsStorageTests
	{
		[TestMethod]
		public void TryDeserialize_EmptyString_ReturnsFalse()
		{
			Assert.IsFalse(StorageDOM.ScriptExecutionDetails.TryDeserialize(string.Empty, out var result));
			Assert.IsNull(result);
		}

		[TestMethod]
		public void TryDeserialize_NullString_ReturnsFalse()
		{
			Assert.IsFalse(StorageDOM.ScriptExecutionDetails.TryDeserialize(null, out var result));
			Assert.IsNull(result);
		}

		[TestMethod]
		public void TryDeserialize_InvalidJson_ReturnsFalse()
		{
			Assert.IsFalse(StorageDOM.ScriptExecutionDetails.TryDeserialize("not-valid-json", out _));
		}

		[TestMethod]
		public void RoundTrip_DummyReference_PreservesAllData()
		{
			var original = new StorageDOM.ScriptExecutionDetails { ScriptName = "TestScript" };
			original.DummyReferences.Add("dummy1", new StorageDOM.DataReference
			{
				ReferenceType = "ResourceName",
				ReferenceId = "res-123",
			});

			var success = StorageDOM.ScriptExecutionDetails.TryDeserialize(original.Serialize(), out var restored);

			Assert.IsTrue(success);
			Assert.IsNotNull(restored);
			Assert.AreEqual(original.ScriptName, restored.ScriptName);
			Assert.IsTrue(restored.DummyReferences.ContainsKey("dummy1"));
			Assert.AreEqual("ResourceName", restored.DummyReferences["dummy1"].ReferenceType);
			Assert.AreEqual("res-123", restored.DummyReferences["dummy1"].ReferenceId);
		}

		[TestMethod]
		public void RoundTrip_ParameterReference_PreservesAllData()
		{
			var original = new StorageDOM.ScriptExecutionDetails { ScriptName = "TestScript" };
			original.ParameterReferences.Add("param1", new StorageDOM.DataReference
			{
				ReferenceType = "ResourceProperty",
				ReferenceId = "prop-456",
			});

			var success = StorageDOM.ScriptExecutionDetails.TryDeserialize(original.Serialize(), out var restored);

			Assert.IsTrue(success);
			Assert.IsNotNull(restored);
			Assert.AreEqual(original.ScriptName, restored.ScriptName);
			Assert.IsTrue(restored.ParameterReferences.ContainsKey("param1"));
			Assert.AreEqual("ResourceProperty", restored.ParameterReferences["param1"].ReferenceType);
			Assert.AreEqual("prop-456", restored.ParameterReferences["param1"].ReferenceId);
		}

		[TestMethod]
		public void RoundTrip_MixedReferences_PreservesAllData()
		{
			var original = new StorageDOM.ScriptExecutionDetails { ScriptName = "MixedScript" };
			original.DummyReferences.Add("dummy1", new StorageDOM.DataReference
			{
				ReferenceType = "ResourceLinkedObjectID",
				ReferenceId = "obj-789",
			});
			original.ParameterReferences.Add("param1", new StorageDOM.DataReference
			{
				ReferenceType = "SchedulingConfigurationParameter",
				ReferenceId = "sched-012",
			});
			original.Dummies.Add("dummy2", "101/1234");
			original.Parameters.Add("param2", "hardcoded-value");

			var success = StorageDOM.ScriptExecutionDetails.TryDeserialize(original.Serialize(), out var restored);

			Assert.IsTrue(success);
			Assert.IsNotNull(restored);
			Assert.AreEqual(original.ScriptName, restored.ScriptName);
			Assert.AreEqual("ResourceLinkedObjectID", restored.DummyReferences["dummy1"].ReferenceType);
			Assert.AreEqual("obj-789", restored.DummyReferences["dummy1"].ReferenceId);
			Assert.AreEqual("SchedulingConfigurationParameter", restored.ParameterReferences["param1"].ReferenceType);
			Assert.AreEqual("sched-012", restored.ParameterReferences["param1"].ReferenceId);
			Assert.AreEqual("101/1234", restored.Dummies["dummy2"]);
			Assert.AreEqual("hardcoded-value", restored.Parameters["param2"]);
		}

		[TestMethod]
		public void RoundTrip_EmptyReferences_NoEntries()
		{
			var original = new StorageDOM.ScriptExecutionDetails { ScriptName = "NoRefScript" };

			var success = StorageDOM.ScriptExecutionDetails.TryDeserialize(original.Serialize(), out var restored);

			Assert.IsTrue(success);
			Assert.IsNotNull(restored);
			Assert.AreEqual(0, restored.DummyReferences.Count);
			Assert.AreEqual(0, restored.ParameterReferences.Count);
		}

		[TestMethod]
		public void RoundTrip_ProfileParameterValue_WithReference_PreservesAllData()
		{
			var id = Guid.NewGuid();
			var original = new StorageDOM.ScriptExecutionDetails { ScriptName = "PpvScript" };
			original.ProfileParameterValues.Add(new StorageDOM.ProfileParameterValue
			{
				ProfileParameterId = id,
				StringValue = "some-value",
				Reference = new StorageDOM.DataReference
				{
					ReferenceType = "ResourceName",
					ReferenceId = "ppv-ref-id",
				},
			});

			var success = StorageDOM.ScriptExecutionDetails.TryDeserialize(original.Serialize(), out var restored);

			Assert.IsTrue(success);
			Assert.IsNotNull(restored);
			Assert.AreEqual(1, restored.ProfileParameterValues.Count);
			var ppv = restored.ProfileParameterValues[0];
			Assert.AreEqual(id, ppv.ProfileParameterId);
			Assert.AreEqual("some-value", ppv.StringValue);
			Assert.IsNotNull(ppv.Reference);
			Assert.AreEqual("ResourceName", ppv.Reference.ReferenceType);
			Assert.AreEqual("ppv-ref-id", ppv.Reference.ReferenceId);
		}

		[TestMethod]
		public void Equals_SameContent_ReturnsTrue()
		{
			var a = CreateFullDetails();
			var b = CreateFullDetails();

			Assert.IsTrue(a.Equals(b));
		}

		[TestMethod]
		public void Equals_Null_ReturnsFalse()
		{
			var a = CreateFullDetails();

			Assert.IsFalse(a.Equals(null));
		}

		[TestMethod]
		public void Equals_DifferentScriptName_ReturnsFalse()
		{
			var a = CreateFullDetails();
			var b = CreateFullDetails();
			b.ScriptName = "OtherScript";

			Assert.IsFalse(a.Equals(b));
		}

		[TestMethod]
		public void Equals_DifferentParameters_ReturnsFalse()
		{
			var a = CreateFullDetails();
			var b = CreateFullDetails();
			b.Parameters["p1"] = "different-value";

			Assert.IsFalse(a.Equals(b));
		}

		[TestMethod]
		public void Equals_DifferentDummies_ReturnsFalse()
		{
			var a = CreateFullDetails();
			var b = CreateFullDetails();
			b.Dummies["d1"] = "different-element";

			Assert.IsFalse(a.Equals(b));
		}

		[TestMethod]
		public void Equals_DifferentParameterReferences_ReturnsFalse()
		{
			var a = CreateFullDetails();
			var b = CreateFullDetails();
			b.ParameterReferences["p1"] = new StorageDOM.DataReference { ReferenceType = "ResourceName", ReferenceId = "other-id" };

			Assert.IsFalse(a.Equals(b));
		}

		[TestMethod]
		public void Equals_DifferentDummyReferences_ReturnsFalse()
		{
			var a = CreateFullDetails();
			var b = CreateFullDetails();
			b.DummyReferences["d1"] = new StorageDOM.DataReference { ReferenceType = "ResourceName", ReferenceId = "other-id" };

			Assert.IsFalse(a.Equals(b));
		}

		private static StorageDOM.ScriptExecutionDetails CreateFullDetails()
		{
			var details = new StorageDOM.ScriptExecutionDetails { ScriptName = "MyScript" };
			details.Parameters.Add("p1", "val1");
			details.Dummies.Add("d1", "101/1");
			details.ParameterReferences.Add("p1", new StorageDOM.DataReference { ReferenceType = "ResourceProperty", ReferenceId = "ref-1" });
			details.DummyReferences.Add("d1", new StorageDOM.DataReference { ReferenceType = "ResourceName", ReferenceId = "ref-2" });
			return details;
		}
	}
}
