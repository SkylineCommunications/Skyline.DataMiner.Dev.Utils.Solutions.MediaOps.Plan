namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a connection in the node graph of a workflow is invalid.
	/// </summary>
	public class WorkflowNodeGraphInvalidConnectionError : WorkflowNodeGraphError
	{
		/// <summary>
		/// Gets the unique identifier of the workflow connection.
		/// </summary>
		public string ConnectionId { get; internal set; }
	}
}
