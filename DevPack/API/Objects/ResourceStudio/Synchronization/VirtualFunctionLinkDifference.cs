namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents a difference between the virtual function a resource is linked to in DOM and the one it is linked to in CORE.
	/// </summary>
	public sealed class VirtualFunctionLinkDifference : SynchronizationDifference
	{
		internal VirtualFunctionLinkDifference()
			: base(SynchronizationDifferenceKind.ValueMismatch)
		{
		}

		/// <summary>
		/// Gets the identifier of the function definition configured in DOM.
		/// </summary>
		public Guid DomFunctionId { get; internal set; }

		/// <summary>
		/// Gets the identifier of the function definition configured in CORE.
		/// </summary>
		public Guid CoreFunctionId { get; internal set; }

		/// <summary>
		/// Gets the identifier of the DataMiner Agent hosting the main DVE element configured in DOM.
		/// </summary>
		public int DomAgentId { get; internal set; }

		/// <summary>
		/// Gets the identifier of the main DVE element configured in DOM.
		/// </summary>
		public int DomElementId { get; internal set; }

		/// <summary>
		/// Gets the identifier of the DataMiner Agent hosting the main DVE element configured in CORE.
		/// </summary>
		public int CoreAgentId { get; internal set; }

		/// <summary>
		/// Gets the identifier of the main DVE element configured in CORE.
		/// </summary>
		public int CoreElementId { get; internal set; }

		/// <summary>
		/// Gets the function table index configured in DOM.
		/// </summary>
		public string DomFunctionTableIndex { get; internal set; }

		/// <summary>
		/// Gets the function table index configured in CORE, or <see langword="null"/> when no table index is configured in CORE.
		/// </summary>
		public string CoreFunctionTableIndex { get; internal set; }
	}
}
