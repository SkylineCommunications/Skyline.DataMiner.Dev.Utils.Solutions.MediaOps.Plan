namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a parent-child link in the node graph of a job is invalid.
	/// </summary>
	public sealed class JobNodeGraphInvalidLinkError : JobNodeGraphError
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
