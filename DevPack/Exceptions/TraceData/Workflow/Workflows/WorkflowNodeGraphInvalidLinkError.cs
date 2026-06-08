namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a parent-child link in the node graph of a workflow is invalid.
	/// </summary>
	public sealed class WorkflowNodeGraphInvalidLinkError : WorkflowNodeGraphError
	{
		/// <summary>
		/// Gets the unique identifier of the parent workflow node.
		/// </summary>
		public string ParentNodeId { get; internal set; }

		/// <summary>
		/// Gets the unique identifier of the child workflow node.
		/// </summary>
		public string ChildNodeId { get; internal set; }
	}
}
