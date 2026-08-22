namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Represents a Resource Studio item that has no counterpart in CORE.
	/// </summary>
	public sealed class MissingCoreObjectDifference : SynchronizationDifference
	{
		internal MissingCoreObjectDifference()
			: base(SynchronizationDifferenceKind.Missing)
		{
		}
	}
}
