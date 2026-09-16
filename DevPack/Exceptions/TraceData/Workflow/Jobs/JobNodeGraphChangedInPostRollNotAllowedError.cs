namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents the base error for a change to the node graph of a running job that is already in its post-roll.
	/// The concrete type indicates which part of the node graph was affected.
	/// </summary>
	/// <seealso cref="JobNodeAddedInPostRollNotAllowedError"/>
	/// <seealso cref="JobNodeConnectionChangedInPostRollNotAllowedError"/>
	/// <seealso cref="JobNodeGroupChangedInPostRollNotAllowedError"/>
	/// <seealso cref="JobNodeLinkChangedInPostRollNotAllowedError"/>
	/// <seealso cref="JobNodeRemovedInPostRollNotAllowedError"/>
	/// <seealso cref="JobNodeSwappedInPostRollNotAllowedError"/>
	public abstract class JobNodeGraphChangedInPostRollNotAllowedError : JobNodeGraphError
	{
	}
}
