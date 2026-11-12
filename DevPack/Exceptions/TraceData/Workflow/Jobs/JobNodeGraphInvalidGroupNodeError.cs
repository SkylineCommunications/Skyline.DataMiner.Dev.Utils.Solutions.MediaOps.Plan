namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when a node group of a job references a node that is not part of the node graph.
	/// </summary>
	public sealed class JobNodeGraphInvalidGroupNodeError : JobNodeGraphError
	{
		/// <summary>
		/// Gets the unique identifier of the node group.
		/// </summary>
		public Guid GroupId { get; internal set; }

		/// <summary>
		/// Gets the name of the node group.
		/// </summary>
		public string GroupName { get; internal set; }

		/// <summary>
		/// Gets the unique identifier of the job node.
		/// </summary>
		public string NodeId { get; internal set; }
	}
}
