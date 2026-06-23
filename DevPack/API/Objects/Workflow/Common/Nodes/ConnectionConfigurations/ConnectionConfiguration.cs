namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	using StorageWorkflow = Storage.DOM.SlcWorkflow;

	/// <summary>
	/// Represents the configuration of a <see cref="NodeConnection{TNode}"/>.
	/// </summary>
	/// <remarks>
	/// This is the base type of a closed hierarchy of connection configurations. Level-based is currently the only
	/// supported connection type; the concrete subtype determines how the connection is persisted (its storage
	/// connection subtype and any additional details). Consumers interact with the configuration through the typed
	/// subclasses (for example <see cref="AllLevelBasedConnectionConfiguration"/> or
	/// <see cref="ShuffleLevelBasedConnectionConfiguration"/>) and never with the underlying storage enums.
	/// </remarks>
	public abstract class ConnectionConfiguration
	{
		private protected ConnectionConfiguration()
		{
		}

		private protected ConnectionConfiguration(ConnectionConfiguration connectionConfiguration)
		{
		}

		/// <summary>
		/// Writes the connection type, subtype and any additional details of this configuration to the specified storage section.
		/// </summary>
		/// <param name="section">The storage section to write to.</param>
		internal abstract void WriteTo(StorageWorkflow.ConnectionsSection section);

		/// <summary>
		/// Reconstructs the concrete <see cref="ConnectionConfiguration"/> represented by the specified storage section.
		/// </summary>
		/// <remarks>
		/// Level-based is currently the only supported connection type, so every section is reconstructed as a
		/// <see cref="LevelBasedConnectionConfiguration"/>.
		/// </remarks>
		/// <param name="section">The storage section to read from.</param>
		/// <returns>The reconstructed configuration.</returns>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="section"/> is null.</exception>
		internal static ConnectionConfiguration FromSection(StorageWorkflow.ConnectionsSection section)
		{
			if (section == null)
			{
				throw new ArgumentNullException(nameof(section));
			}

			return LevelBasedConnectionConfiguration.FromSection(section);
		}
	}
}
