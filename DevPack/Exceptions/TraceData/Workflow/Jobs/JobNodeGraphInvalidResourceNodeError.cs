namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when a resource node in the node graph of a job is invalid.
	/// </summary>
	public sealed class JobNodeGraphInvalidResourceNodeError : JobNodeGraphInvalidNodeError
	{
		/// <summary>
		/// Gets the unique identifier of the resource pool.
		/// </summary>
		public Guid ResourcePoolId { get; internal set; }

		/// <summary>
		/// Gets the unique identifier of the resource.
		/// </summary>
		public Guid ResourceId { get; internal set; }
	}
}
