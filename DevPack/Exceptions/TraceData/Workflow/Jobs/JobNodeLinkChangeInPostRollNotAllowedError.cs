namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a parent-child link of a running job that is already in its post-roll is
	/// added or removed.
	/// </summary>
	public sealed class JobNodeLinkChangeInPostRollNotAllowedError : JobNodeGraphChangeInPostRollNotAllowedError
	{
		/// <summary>
		/// Gets the unique identifier of the parent job node.
		/// </summary>
		public string ParentNodeId { get; internal set; }

		/// <summary>
		/// Gets the unique identifier of the child job node.
		/// </summary>
		public string ChildNodeId { get; internal set; }
	}
}
