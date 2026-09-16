namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a node is removed from a running job that is already in its post-roll.
	/// </summary>
	public sealed class JobNodeRemovedInPostRollNotAllowedError : JobNodeGraphChangedInPostRollNotAllowedError
	{
		/// <summary>
		/// Gets the unique identifier of the job node that is being removed.
		/// </summary>
		public string NodeId { get; internal set; }
	}
}
