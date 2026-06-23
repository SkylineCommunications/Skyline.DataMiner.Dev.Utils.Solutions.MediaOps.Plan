namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Tools;

	using StorageConnectionConfiguration = Storage.DOM.ConnectionConfiguration;
	using StorageLevelInfo = Storage.DOM.LevelInfo;
	using StorageLevelMappingInfo = Storage.DOM.LevelMappingInfo;
	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Represents a level-based connection configuration that maps individual levels of the source node onto individual levels of the destination node.
	/// </summary>
	/// <remarks>
	/// Each destination level can be fed by at most one source level, while a single source level can feed multiple
	/// destination levels. The mapping is therefore keyed by destination level. An empty mapping is allowed.
	/// </remarks>
	public sealed class ShuffleLevelBasedConnectionConfiguration : LevelBasedConnectionConfiguration
	{
		private readonly Dictionary<long, long> levelMappings = new Dictionary<long, long>();

		/// <summary>
		/// Initializes a new instance of the <see cref="ShuffleLevelBasedConnectionConfiguration"/> class.
		/// </summary>
		public ShuffleLevelBasedConnectionConfiguration()
		{
		}

		/// <summary>
		/// Gets the level mappings of this configuration, keyed by destination level with the assigned source level as value.
		/// </summary>
		public IReadOnlyDictionary<long, long> LevelMappings => levelMappings;

		/// <summary>
		/// Assigns the specified source level to the specified destination level.
		/// </summary>
		/// <remarks>
		/// A destination level can only be fed by a single source level, so assigning a source level to a destination
		/// level that is already mapped replaces the previous assignment.
		/// </remarks>
		/// <param name="destinationLevel">The destination level that is fed.</param>
		/// <param name="sourceLevel">The source level that feeds the destination level.</param>
		/// <returns>The current <see cref="ShuffleLevelBasedConnectionConfiguration"/> instance.</returns>
		public ShuffleLevelBasedConnectionConfiguration AddLevelMapping(long destinationLevel, long sourceLevel)
		{
			levelMappings[destinationLevel] = sourceLevel;
			return this;
		}

		/// <summary>
		/// Removes the mapping for the specified destination level.
		/// </summary>
		/// <param name="destinationLevel">The destination level whose mapping should be removed.</param>
		/// <returns>The current <see cref="ShuffleLevelBasedConnectionConfiguration"/> instance.</returns>
		public ShuffleLevelBasedConnectionConfiguration RemoveLevelMapping(long destinationLevel)
		{
			levelMappings.Remove(destinationLevel);
			return this;
		}

		/// <summary>
		/// Removes all level mappings from this configuration.
		/// </summary>
		/// <returns>The current <see cref="ShuffleLevelBasedConnectionConfiguration"/> instance.</returns>
		public ShuffleLevelBasedConnectionConfiguration ClearLevelMappings()
		{
			levelMappings.Clear();
			return this;
		}

		internal static new ShuffleLevelBasedConnectionConfiguration FromSection(StorageWorkflow.ConnectionsSection section)
		{
			if (section == null)
			{
				throw new ArgumentNullException(nameof(section));
			}

			var configuration = new ShuffleLevelBasedConnectionConfiguration();

			if (StorageConnectionConfiguration.TryDeserialize(section.ConnectionDetails, out var storageConfiguration))
			{
				foreach (var levelMapping in storageConfiguration.LevelMappings)
				{
					if (levelMapping?.From == null || levelMapping.To == null)
					{
						continue;
					}

					configuration.levelMappings[levelMapping.To.Number] = levelMapping.From.Number;
				}
			}

			return configuration;
		}

		internal override void WriteTo(StorageWorkflow.ConnectionsSection section)
		{
			section.ConnectionType = StorageWorkflow.SlcWorkflowIds.Enums.Connectiontype.LevelBased;
			section.ConnectionSubtype = StorageWorkflow.SlcWorkflowIds.Enums.Connectionsubtype.Shuffle;

			var storageConfiguration = new StorageConnectionConfiguration
			{
				LevelMappings = levelMappings
					.Select(levelMapping => new StorageLevelMappingInfo(
						new StorageLevelInfo { Number = levelMapping.Value },
						new StorageLevelInfo { Number = levelMapping.Key }))
					.ToList(),
			};

			section.ConnectionDetails = storageConfiguration.Serialize();
		}

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			return obj is ShuffleLevelBasedConnectionConfiguration other
				&& DictionaryComparer<long, long>.Default.Equals(levelMappings, other.levelMappings);
		}

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			unchecked
			{
				var hash = typeof(ShuffleLevelBasedConnectionConfiguration).GetHashCode();
				hash = (hash * 23) + DictionaryComparer<long, long>.Default.GetHashCode(levelMappings);

				return hash;
			}
		}
	}
}
