namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents the base error for a change to the node graph of a running job that is already in its post-roll.
	/// The concrete type indicates which part of the node graph was affected.
	/// </summary>
	/// <seealso cref="JobNodeAddedInPostRollNotAllowedError"/>
	/// <seealso cref="JobNodeConnectionChangeInPostRollNotAllowedError"/>
	/// <seealso cref="JobNodeGroupChangeInPostRollNotAllowedError"/>
	/// <seealso cref="JobNodeLinkChangeInPostRollNotAllowedError"/>
	/// <seealso cref="JobNodeRemovedInPostRollNotAllowedError"/>
	/// <seealso cref="JobNodeSwapInPostRollNotAllowedError"/>
	public abstract class JobNodeGraphChangeInPostRollNotAllowedError : JobNodeGraphError
	{
	}
}
