namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a node group of a running job that is already in its post-roll is added,
	/// removed or has its membership changed.
	/// </summary>
	public sealed class JobNodeGroupChangedInPostRollNotAllowedError : JobNodeGraphChangedInPostRollNotAllowedError
	{
		/// <summary>
		/// Gets the name of the node group.
		/// </summary>
		public string GroupName { get; internal set; }
	}
}
