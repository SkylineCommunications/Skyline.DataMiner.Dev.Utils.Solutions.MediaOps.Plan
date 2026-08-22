namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Specifies the nature of a difference between the DOM configuration and the CORE configuration.
	/// </summary>
	public enum SynchronizationDifferenceKind
	{
		/// <summary>
		/// The item is configured in DOM but does not exist in CORE.
		/// </summary>
		Missing,

		/// <summary>
		/// The item exists in CORE but is no longer configured in DOM.
		/// </summary>
		Obsolete,

		/// <summary>
		/// The item exists on both sides but the configured values differ.
		/// </summary>
		ValueMismatch,
	}
}
