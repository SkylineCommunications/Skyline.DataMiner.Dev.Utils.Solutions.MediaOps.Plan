namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents a node associated with a resource pool.
	/// </summary>
	public interface IResourcePoolNode
	{
		/// <summary>
		/// Gets the unique identifier of the resource pool associated with this node.
		/// </summary>
		Guid ResourcePoolId { get; }
	}
}
