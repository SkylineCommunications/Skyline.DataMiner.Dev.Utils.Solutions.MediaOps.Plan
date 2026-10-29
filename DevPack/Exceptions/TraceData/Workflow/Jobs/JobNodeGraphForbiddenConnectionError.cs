namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.API;

	/// <summary>
	/// Represents an error that occurs when a connection in the node graph of a job is forbidden by flow rules.
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
		/// Gets the type of the object of the node the connection starts from, whose flow rules were evaluated.
		/// </summary>
		public FlowRuleObjectType FromObjectType { get; internal set; }

		/// <summary>
		/// Gets the unique identifier of the object of the node the connection starts from, whose flow rules were evaluated.
		/// </summary>
		public Guid FromObjectId { get; internal set; }

		/// <summary>
		/// Gets the type of the object of the node the connection goes to, whose flow rules were evaluated.
		/// </summary>
		public FlowRuleObjectType ToObjectType { get; internal set; }

		/// <summary>
		/// Gets the unique identifier of the object of the node the connection goes to, whose flow rules were evaluated.
		/// </summary>
		public Guid ToObjectId { get; internal set; }
	}
}
