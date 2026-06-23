namespace RT_MediaOps.Plan.Workflow.Connections
{
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;

	[TestClass]
	public sealed class ConnectionConfigurationStorageTests
	{
		[TestMethod]
		public void TryDeserialize_EmptyString_ReturnsFalse()
		{
			Assert.IsFalse(ConnectionConfiguration.TryDeserialize(string.Empty, out var result));
			Assert.IsNull(result);
		}

		[TestMethod]
		public void TryDeserialize_NullString_ReturnsFalse()
		{
			Assert.IsFalse(ConnectionConfiguration.TryDeserialize(null, out var result));
			Assert.IsNull(result);
		}

		[TestMethod]
		public void TryDeserialize_InvalidJson_ReturnsFalse()
		{
			Assert.IsFalse(ConnectionConfiguration.TryDeserialize("not-valid-json", out _));
		}

		[TestMethod]
		public void RoundTrip_LevelMappings_PreservesAllData()
		{
			var original = new ConnectionConfiguration
			{
				LevelMappings =
				{
					new LevelMappingInfo(new LevelInfo { Number = 10L }, new LevelInfo { Number = 1L }),
					new LevelMappingInfo(new LevelInfo { Number = 10L }, new LevelInfo { Number = 2L }),
					new LevelMappingInfo(new LevelInfo { Number = 20L }, new LevelInfo { Number = 3L }),
				},
			};

			var success = ConnectionConfiguration.TryDeserialize(original.Serialize(), out var restored);

			Assert.IsTrue(success);
			Assert.IsNotNull(restored);
			var mappings = restored.LevelMappings.ToList();
			Assert.AreEqual(3, mappings.Count);
			Assert.AreEqual(10L, mappings[0].From.Number);
			Assert.AreEqual(1L, mappings[0].To.Number);
			Assert.AreEqual(10L, mappings[1].From.Number);
			Assert.AreEqual(2L, mappings[1].To.Number);
			Assert.AreEqual(20L, mappings[2].From.Number);
			Assert.AreEqual(3L, mappings[2].To.Number);
		}

		[TestMethod]
		public void TryDeserialize_LegacyJson_ParsesFromAndToLevels()
		{
			const string legacyJson = "{\"LevelMappings\":[{\"From\":{\"Number\":0,\"Name\":\"L 001\"},\"To\":{\"Number\":0,\"Name\":\"L 001\"}},{\"From\":{\"Number\":0,\"Name\":\"L 001\"},\"To\":{\"Number\":1,\"Name\":\"L 002\"}}]}";

			var success = ConnectionConfiguration.TryDeserialize(legacyJson, out var restored);

			Assert.IsTrue(success);
			Assert.IsNotNull(restored);
			var mappings = restored.LevelMappings.ToList();
			Assert.AreEqual(2, mappings.Count);
			Assert.AreEqual(0L, mappings[0].From.Number);
			Assert.AreEqual(0L, mappings[0].To.Number);
			Assert.AreEqual(0L, mappings[1].From.Number);
			Assert.AreEqual(1L, mappings[1].To.Number);
		}

		[TestMethod]
		public void RoundTrip_EmptyLevelMappings_NoEntries()
		{
			var original = new ConnectionConfiguration();

			var success = ConnectionConfiguration.TryDeserialize(original.Serialize(), out var restored);

			Assert.IsTrue(success);
			Assert.IsNotNull(restored);
			Assert.AreEqual(0, restored.LevelMappings.Count);
		}
	}
}
