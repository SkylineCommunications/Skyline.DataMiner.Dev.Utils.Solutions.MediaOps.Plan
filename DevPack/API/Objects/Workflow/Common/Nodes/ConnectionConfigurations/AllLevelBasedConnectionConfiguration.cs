namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Represents a level-based connection configuration that connects all levels of the source node to all levels of the destination node.
	/// </summary>
	/// <remarks>
	/// This is the default configuration for a new connection. It does not require any additional information.
	/// </remarks>
	public sealed class AllLevelBasedConnectionConfiguration : LevelBasedConnectionConfiguration
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AllLevelBasedConnectionConfiguration"/> class.
		/// </summary>
		public AllLevelBasedConnectionConfiguration()
		{
		}

		internal AllLevelBasedConnectionConfiguration(AllLevelBasedConnectionConfiguration allLevelBasedConnectionConfiguration)
			: base(allLevelBasedConnectionConfiguration)
		{
		}

		internal override void WriteTo(StorageWorkflow.ConnectionsSection section)
		{
			section.ConnectionType = StorageWorkflow.SlcWorkflowIds.Enums.Connectiontype.LevelBased;
			section.ConnectionSubtype = StorageWorkflow.SlcWorkflowIds.Enums.Connectionsubtype.All;
			section.ConnectionDetails = null;
		}

		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			return obj is AllLevelBasedConnectionConfiguration;
		}

		/// <inheritdoc/>
		public override int GetHashCode()
		{
			return typeof(AllLevelBasedConnectionConfiguration).GetHashCode();
		}
	}
}
