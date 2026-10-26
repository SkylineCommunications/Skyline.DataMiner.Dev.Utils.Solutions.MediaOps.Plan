namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when a connection in the node graph of a job is forbidden by the flow rules of the connected resource pools.
	/// </summary>
	public sealed class JobNodeGraphForbiddenConnectionError : JobNodeGraphInvalidConnectionError
	{
		/// <summary>
		/// Gets the unique identifier of the node the connection starts from.
		/// </summary>
		public string FromNodeId { get; internal set; }

		/// <summary>
		/// Gets the unique identifier of the node the connection goes to.
		/// </summary>
		public string ToNodeId { get; internal set; }

		/// <summary>
		/// Gets the unique identifier of the resource pool the connection starts from.
		/// </summary>
		public Guid FromResourcePoolId { get; internal set; }

		/// <summary>
		/// Gets the unique identifier of the resource pool the connection goes to.
		/// </summary>
		public Guid ToResourcePoolId { get; internal set; }
	}
}
