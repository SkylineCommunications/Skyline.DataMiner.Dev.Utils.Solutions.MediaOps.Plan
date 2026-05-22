namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents a node associated with a resource within a resource pool.
	/// </summary>
	public interface IResourceNode : INode
	{
		/// <summary>
		/// Gets the unique identifier of the resource pool associated with this node.
		/// </summary>
		Guid ResourcePoolId { get; }

		/// <summary>
		/// Gets the unique identifier of the resource associated with this node.
		/// </summary>
		Guid ResourceId { get; }
	}
}
