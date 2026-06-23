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
	public abstract class LevelBasedConnectionConfiguration : ConnectionConfiguration
	{
		private protected LevelBasedConnectionConfiguration()
		{
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
