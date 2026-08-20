namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Represents a single difference between the DOM configuration and the CORE configuration of a Resource Studio item.
	/// </summary>
	/// <remarks>
	/// Instances only carry data. It is up to the consumer to turn them into a human readable description.
	/// </remarks>
	public abstract class SynchronizationDifference
	{
		private protected SynchronizationDifference(SynchronizationDifferenceKind kind)
		{
			Kind = kind;
		}

		/// <summary>
		/// Gets the nature of the difference.
		/// </summary>
		public SynchronizationDifferenceKind Kind { get; }
	}
}
