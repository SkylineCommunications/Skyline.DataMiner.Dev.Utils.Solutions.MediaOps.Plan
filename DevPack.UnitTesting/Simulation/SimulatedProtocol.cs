namespace Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Simulation
{
	using System;

	using Skyline.DataMiner.Net.Messages;

	/// <summary>
	/// Represents a connector (protocol) version installed on the simulated DataMiner System.
	/// </summary>
	internal sealed class SimulatedProtocol
	{
		public SimulatedProtocol(string name, string version, ProtocolType type, string connectionType)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Version = version ?? throw new ArgumentNullException(nameof(version));
			Type = type;
			ConnectionType = connectionType ?? throw new ArgumentNullException(nameof(connectionType));
		}

		public string Name { get; }

		public string Version { get; }

		public ProtocolType Type { get; }

		/// <summary>
		/// Gets the connection type string (for example <c>HTTP</c> or <c>Virtual</c>) that the
		/// connector exposes as its main port, used when loading the protocol connection info.
		/// </summary>
		public string ConnectionType { get; }
	}
}
