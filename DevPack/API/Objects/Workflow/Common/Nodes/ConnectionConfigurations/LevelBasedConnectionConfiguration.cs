namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Represents a level-based <see cref="ConnectionConfiguration"/>.
	/// </summary>
	/// <remarks>
	/// A level-based connection maps the levels of the source node onto the levels of the destination node. The concrete
	/// subtype (for example <see cref="AllLevelBasedConnectionConfiguration"/> or
	/// <see cref="ShuffleLevelBasedConnectionConfiguration"/>) determines how the levels are matched.
	/// </remarks>
	/// <seealso cref="AllLevelBasedConnectionConfiguration"/>
	/// <seealso cref="ShuffleLevelBasedConnectionConfiguration"/>
	public abstract class LevelBasedConnectionConfiguration : ConnectionConfiguration
	{
		private protected LevelBasedConnectionConfiguration()
		{
		}

		private protected LevelBasedConnectionConfiguration(LevelBasedConnectionConfiguration levelBasedConnectionConfiguration)
			: base(levelBasedConnectionConfiguration)
		{
		}

		/// <summary>
		/// Determines whether this configuration matches all levels and, if so, returns it as an <see cref="AllLevelBasedConnectionConfiguration"/>.
		/// </summary>
		/// <param name="configuration">When this method returns, contains the current configuration as an <see cref="AllLevelBasedConnectionConfiguration"/> when it is one; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this configuration is an <see cref="AllLevelBasedConnectionConfiguration"/>; otherwise, <c>false</c>.</returns>
		public bool IsAllLevelBasedConnectionConfiguration(out AllLevelBasedConnectionConfiguration configuration)
		{
			configuration = this as AllLevelBasedConnectionConfiguration;
			return configuration != null;
		}

		/// <summary>
		/// Determines whether this configuration shuffles levels and, if so, returns it as a <see cref="ShuffleLevelBasedConnectionConfiguration"/>.
		/// </summary>
		/// <param name="configuration">When this method returns, contains the current configuration as a <see cref="ShuffleLevelBasedConnectionConfiguration"/> when it is one; otherwise, <c>null</c>.</param>
		/// <returns><c>true</c> when this configuration is a <see cref="ShuffleLevelBasedConnectionConfiguration"/>; otherwise, <c>false</c>.</returns>
		public bool IsShuffleLevelBasedConnectionConfiguration(out ShuffleLevelBasedConnectionConfiguration configuration)
		{
			configuration = this as ShuffleLevelBasedConnectionConfiguration;
			return configuration != null;
		}

		internal static new ConnectionConfiguration FromSection(StorageWorkflow.ConnectionsSection section)
		{
			if (section == null)
			{
				throw new ArgumentNullException(nameof(section));
			}

			var subtype = section.ConnectionSubtype ?? StorageWorkflow.SlcWorkflowIds.Enums.Connectionsubtype.All;

			switch (subtype)
			{
				case StorageWorkflow.SlcWorkflowIds.Enums.Connectionsubtype.All:
					return new AllLevelBasedConnectionConfiguration();
				default:
					return ShuffleLevelBasedConnectionConfiguration.FromSection(section);
			}
		}
	}
}
