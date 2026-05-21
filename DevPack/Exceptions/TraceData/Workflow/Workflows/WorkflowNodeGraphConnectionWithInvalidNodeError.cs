namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a connection in the node graph of a workflow links to a node that is not valid.
	/// </summary>
	public sealed class WorkflowNodeGraphConnectionWithInvalidNodeError : WorkflowNodeGraphInvalidConnectionError
	{
		/// <summary>
		/// Gets the unique identifier of the workflow node.
		/// </summary>
		public string NodeId { get; internal set; }
	}
}
