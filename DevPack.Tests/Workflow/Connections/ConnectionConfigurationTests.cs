namespace RT_MediaOps.Plan.Workflow.Connections
{
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	using StorageWorkflow = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcWorkflow;

	[TestClass]
	public sealed class ConnectionConfigurationTests
	{
		[TestMethod]
		public void Shuffle_AddLevelMapping_ExposedAsReadOnly()
		{
			var configuration = new ShuffleLevelBasedConnectionConfiguration()
				.AddLevelMapping(destinationLevel: 1, sourceLevel: 10)
				.AddLevelMapping(destinationLevel: 2, sourceLevel: 10);

			Assert.AreEqual(2, configuration.LevelMappings.Count);
			Assert.AreEqual(10L, configuration.LevelMappings[1L]);
			Assert.AreEqual(10L, configuration.LevelMappings[2L]);
		}

		[TestMethod]
		public void Shuffle_AddLevelMapping_SameDestination_ReplacesSource()
		{
			var configuration = new ShuffleLevelBasedConnectionConfiguration()
				.AddLevelMapping(destinationLevel: 1, sourceLevel: 10)
				.AddLevelMapping(destinationLevel: 1, sourceLevel: 20);

			Assert.AreEqual(1, configuration.LevelMappings.Count);
			Assert.AreEqual(20L, configuration.LevelMappings[1L]);
		}

		[TestMethod]
		public void Shuffle_RemoveAndClearLevelMappings_UpdateCollection()
		{
			var configuration = new ShuffleLevelBasedConnectionConfiguration()
				.AddLevelMapping(destinationLevel: 1, sourceLevel: 10)
				.AddLevelMapping(destinationLevel: 2, sourceLevel: 20);

			configuration.RemoveLevelMapping(1);
			Assert.AreEqual(1, configuration.LevelMappings.Count);
			Assert.IsFalse(configuration.LevelMappings.ContainsKey(1L));

			configuration.ClearLevelMappings();
			Assert.AreEqual(0, configuration.LevelMappings.Count);
		}

		[TestMethod]
		public void All_WriteTo_SetsLevelBasedAllAndClearsDetails()
		{
			var section = new StorageWorkflow.ConnectionsSection
			{
				ConnectionDetails = "stale-payload",
			};

			ConnectionConfiguration configuration = new AllLevelBasedConnectionConfiguration();
			configuration.WriteTo(section);

			Assert.AreEqual(StorageWorkflow.SlcWorkflowIds.Enums.Connectiontype.LevelBased, section.ConnectionType);
			Assert.AreEqual(StorageWorkflow.SlcWorkflowIds.Enums.Connectionsubtype.All, section.ConnectionSubtype);
			Assert.IsNull(section.ConnectionDetails);
		}

		[TestMethod]
		public void Shuffle_WriteTo_SetsLevelBasedShuffleAndWritesDetails()
		{
			var section = new StorageWorkflow.ConnectionsSection();

			ConnectionConfiguration configuration = new ShuffleLevelBasedConnectionConfiguration()
				.AddLevelMapping(destinationLevel: 1, sourceLevel: 10);
			configuration.WriteTo(section);

			Assert.AreEqual(StorageWorkflow.SlcWorkflowIds.Enums.Connectiontype.LevelBased, section.ConnectionType);
			Assert.AreEqual(StorageWorkflow.SlcWorkflowIds.Enums.Connectionsubtype.Shuffle, section.ConnectionSubtype);
			Assert.IsNotNull(section.ConnectionDetails);
		}

		[TestMethod]
		public void Shuffle_WriteTo_EmptyMappings_PersistsShuffleSubtype()
		{
			var section = new StorageWorkflow.ConnectionsSection();

			// An empty shuffle blocks the mapping and must remain a shuffle (not collapse into an all-level mapping).
			ConnectionConfiguration configuration = new ShuffleLevelBasedConnectionConfiguration();
			configuration.WriteTo(section);

			Assert.AreEqual(StorageWorkflow.SlcWorkflowIds.Enums.Connectiontype.LevelBased, section.ConnectionType);
			Assert.AreEqual(StorageWorkflow.SlcWorkflowIds.Enums.Connectionsubtype.Shuffle, section.ConnectionSubtype);
		}

		[TestMethod]
		public void FromSection_All_ReturnsAllConfiguration()
		{
			var section = new StorageWorkflow.ConnectionsSection
			{
				ConnectionType = StorageWorkflow.SlcWorkflowIds.Enums.Connectiontype.LevelBased,
				ConnectionSubtype = StorageWorkflow.SlcWorkflowIds.Enums.Connectionsubtype.All,
			};

			var configuration = ConnectionConfiguration.FromSection(section);

			Assert.IsInstanceOfType(configuration, typeof(AllLevelBasedConnectionConfiguration));
		}

		[TestMethod]
		public void FromSection_MissingType_DefaultsToLevelBasedAll()
		{
			var section = new StorageWorkflow.ConnectionsSection();

			var configuration = ConnectionConfiguration.FromSection(section);

			Assert.IsInstanceOfType(configuration, typeof(AllLevelBasedConnectionConfiguration));
		}

		[TestMethod]
		public void RoundTrip_Shuffle_PreservesLevelMappings()
		{
			var original = new ShuffleLevelBasedConnectionConfiguration()
				.AddLevelMapping(destinationLevel: 1, sourceLevel: 10)
				.AddLevelMapping(destinationLevel: 2, sourceLevel: 10)
				.AddLevelMapping(destinationLevel: 3, sourceLevel: 20);

			var section = new StorageWorkflow.ConnectionsSection();
			((ConnectionConfiguration)original).WriteTo(section);

			var restored = ConnectionConfiguration.FromSection(section) as ShuffleLevelBasedConnectionConfiguration;

			Assert.IsNotNull(restored);
			Assert.AreEqual(3, restored.LevelMappings.Count);
			Assert.AreEqual(10L, restored.LevelMappings[1L]);
			Assert.AreEqual(10L, restored.LevelMappings[2L]);
			Assert.AreEqual(20L, restored.LevelMappings[3L]);
		}

		[TestMethod]
		public void RoundTrip_ShuffleWithoutMappings_ReturnsEmptyShuffle()
		{
			var original = new ShuffleLevelBasedConnectionConfiguration();

			var section = new StorageWorkflow.ConnectionsSection();
			((ConnectionConfiguration)original).WriteTo(section);

			var restored = ConnectionConfiguration.FromSection(section) as ShuffleLevelBasedConnectionConfiguration;

			Assert.IsNotNull(restored);
			Assert.IsFalse(restored.LevelMappings.Any());
		}

		[TestMethod]
		public void Equals_AllConfigurations_AreEqual()
		{
			var a = new AllLevelBasedConnectionConfiguration();
			var b = new AllLevelBasedConnectionConfiguration();

			Assert.AreEqual(a, b);
			Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
		}

		[TestMethod]
		public void Equals_ShuffleWithSameMappings_AreEqual()
		{
			var a = new ShuffleLevelBasedConnectionConfiguration()
				.AddLevelMapping(destinationLevel: 1, sourceLevel: 10)
				.AddLevelMapping(destinationLevel: 2, sourceLevel: 20);

			var b = new ShuffleLevelBasedConnectionConfiguration()
				.AddLevelMapping(destinationLevel: 2, sourceLevel: 20)
				.AddLevelMapping(destinationLevel: 1, sourceLevel: 10);

			Assert.AreEqual(a, b);
			Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
		}

		[TestMethod]
		public void Equals_ShuffleWithDifferentMappings_AreNotEqual()
		{
			var a = new ShuffleLevelBasedConnectionConfiguration()
				.AddLevelMapping(destinationLevel: 1, sourceLevel: 10);

			var b = new ShuffleLevelBasedConnectionConfiguration()
				.AddLevelMapping(destinationLevel: 1, sourceLevel: 20);

			Assert.AreNotEqual(a, b);
		}

		[TestMethod]
		public void Equals_AllAndShuffle_AreNotEqual()
		{
			var all = new AllLevelBasedConnectionConfiguration();
			var shuffle = new ShuffleLevelBasedConnectionConfiguration();

			Assert.AreNotEqual<ConnectionConfiguration>(all, shuffle);
		}
	}
}
