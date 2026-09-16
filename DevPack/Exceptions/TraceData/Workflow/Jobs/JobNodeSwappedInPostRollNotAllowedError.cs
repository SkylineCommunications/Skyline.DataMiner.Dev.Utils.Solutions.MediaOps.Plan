namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a node is swapped on a running job that is already in its post-roll.
	/// </summary>
	public sealed class JobNodeSwappedInPostRollNotAllowedError : JobNodeGraphChangedInPostRollNotAllowedError
	{
		/// <summary>
		/// Gets the unique identifier of the job node that is being swapped out.
		/// </summary>
		public string NodeId { get; internal set; }

		/// <summary>
		/// Gets the unique identifier of the node that is being swapped to.
		/// </summary>
		public string TargetNodeId { get; internal set; }
	}
}
