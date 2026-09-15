namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a node is added to a running job that is already in its post-roll.
	/// </summary>
	public sealed class JobNodeAddedInPostRollNotAllowedError : JobNodeGraphChangeInPostRollNotAllowedError
	{
		/// <summary>
		/// Gets the unique identifier of the job node that is being added.
		/// </summary>
		public string NodeId { get; internal set; }
	}
}
