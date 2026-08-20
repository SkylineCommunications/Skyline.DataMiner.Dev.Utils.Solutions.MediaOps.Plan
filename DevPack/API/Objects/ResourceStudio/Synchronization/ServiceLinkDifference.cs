namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Represents a difference between the service a resource is linked to in DOM and the one it is linked to in CORE.
	/// </summary>
	public sealed class ServiceLinkDifference : SynchronizationDifference
	{
		internal ServiceLinkDifference(SynchronizationDifferenceKind kind, string domValue, string coreValue)
			: base(kind)
		{
			DomValue = domValue;
			CoreValue = coreValue;
		}

		/// <summary>
		/// Gets the service link configured in DOM.
		/// </summary>
		public string DomValue { get; }

		/// <summary>
		/// Gets the service link configured in CORE, or <see langword="null"/> when no service link is configured in CORE.
		/// </summary>
		public string CoreValue { get; }
	}
}
