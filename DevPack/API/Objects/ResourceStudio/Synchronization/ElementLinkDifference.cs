namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Represents a difference between the element a resource is linked to in DOM and the one it is linked to in CORE.
	/// </summary>
	public sealed class ElementLinkDifference : SynchronizationDifference
	{
		internal ElementLinkDifference(int domAgentId, int domElementId, int coreAgentId, int coreElementId)
			: base(SynchronizationDifferenceKind.ValueMismatch)
		{
			DomAgentId = domAgentId;
			DomElementId = domElementId;
			CoreAgentId = coreAgentId;
			CoreElementId = coreElementId;
		}

		/// <summary>
		/// Gets the identifier of the DataMiner Agent configured in DOM.
		/// </summary>
		public int DomAgentId { get; }

		/// <summary>
		/// Gets the identifier of the element configured in DOM.
		/// </summary>
		public int DomElementId { get; }

		/// <summary>
		/// Gets the identifier of the DataMiner Agent configured in CORE.
		/// </summary>
		public int CoreAgentId { get; }

		/// <summary>
		/// Gets the identifier of the element configured in CORE.
		/// </summary>
		public int CoreElementId { get; }
	}
}
