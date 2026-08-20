namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Represents a difference between the resource type derived from the DOM configuration and the resource type capability in CORE.
	/// </summary>
	public sealed class ResourceTypeDifference : SynchronizationDifference
	{
		internal ResourceTypeDifference(SynchronizationDifferenceKind kind, string domValue, string coreValue)
			: base(kind)
		{
			DomValue = domValue;
			CoreValue = coreValue;
		}

		/// <summary>
		/// Gets the resource type derived from the DOM configuration.
		/// </summary>
		public string DomValue { get; }

		/// <summary>
		/// Gets the resource type configured in CORE, or <see langword="null"/> when no resource type is configured in CORE.
		/// </summary>
		public string CoreValue { get; }
	}
}
