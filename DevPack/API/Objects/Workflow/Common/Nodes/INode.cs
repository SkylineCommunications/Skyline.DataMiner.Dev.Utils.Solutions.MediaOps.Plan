namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Represents a node.
	/// </summary>
	public interface INode
	{
		/// <summary>
		/// Gets the unique identifier of the node, which is assigned by the system and cannot be modified by users.
		/// </summary>
		string Id { get; }
	}
}
