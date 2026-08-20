namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Represents a difference between the name configured in DOM and the name configured in CORE.
	/// </summary>
	public sealed class NameDifference : SynchronizationDifference
	{
		internal NameDifference(string domValue, string coreValue)
			: base(SynchronizationDifferenceKind.ValueMismatch)
		{
			DomValue = domValue;
			CoreValue = coreValue;
		}

		/// <summary>
		/// Gets the name configured in DOM.
		/// </summary>
		public string DomValue { get; }

		/// <summary>
		/// Gets the name configured in CORE.
		/// </summary>
		public string CoreValue { get; }
	}
}
