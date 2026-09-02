namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Represents a difference between the maximum concurrency configured in DOM and the one configured in CORE.
	/// </summary>
	public sealed class MaxConcurrencyDifference : SynchronizationDifference
	{
		internal MaxConcurrencyDifference(int domValue, int coreValue)
			: base(SynchronizationDifferenceKind.ValueMismatch)
		{
			DomValue = domValue;
			CoreValue = coreValue;
		}

		/// <summary>
		/// Gets the maximum concurrency configured in DOM.
		/// </summary>
		public int DomValue { get; }

		/// <summary>
		/// Gets the maximum concurrency configured in CORE.
		/// </summary>
		public int CoreValue { get; }
	}
}
