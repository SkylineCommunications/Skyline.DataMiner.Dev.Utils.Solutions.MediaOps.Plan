namespace Skyline.DataMiner.Solutions.MediaOps.Plan.UnitTesting.Simulation
{
	using Skyline.DataMiner.Net.Messages;

	/// <summary>
	/// Represents a minimal in-memory simulated element, sufficient for element existence and
	/// information lookups by the MediaOps Plan API.
	/// </summary>
	public sealed class SimulatedElement
	{
		internal SimulatedElement(int dmaId, int elementId, string name, string protocolName, string protocolVersion)
		{
			DmaId = dmaId;
			ElementId = elementId;
			Name = name;
			ProtocolName = protocolName;
			ProtocolVersion = protocolVersion;
		}

		/// <summary>Gets the DataMiner Agent ID hosting this element.</summary>
		public int DmaId { get; }

		/// <summary>Gets the element ID.</summary>
		public int ElementId { get; }

		/// <summary>Gets the element name.</summary>
		public string Name { get; }

		/// <summary>Gets the protocol name.</summary>
		public string ProtocolName { get; }

		/// <summary>Gets the protocol version.</summary>
		public string ProtocolVersion { get; }

		/// <summary>Gets the element state.</summary>
		public ElementState State { get; } = ElementState.Active;

		internal LiteElementInfoEvent ToLiteElementInfo()
		{
			return new LiteElementInfoEvent
			{
				DataMinerID = DmaId,
				HostingAgentID = DmaId,
				ElementID = ElementId,
				Name = Name,
				Protocol = ProtocolName,
				ProtocolVersion = ProtocolVersion,
				State = State,
			};
		}

		internal ElementInfoEventMessage ToElementInfo()
		{
			return new ElementInfoEventMessage
			{
				DataMinerID = DmaId,
				HostingAgentID = DmaId,
				ElementID = ElementId,
				Name = Name,
				Protocol = ProtocolName,
				ProtocolVersion = ProtocolVersion,
				State = State,
			};
		}
	}
}
