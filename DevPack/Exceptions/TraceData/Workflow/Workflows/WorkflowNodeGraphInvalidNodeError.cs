namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a node in the node graph of a workflow is invalid.
	/// </summary>
	/// <seealso cref="WorkflowNodeGraphDuplicateNodeIdError"/>
	/// <seealso cref="WorkflowNodeGraphEmptyNodeIdError"/>
	/// <seealso cref="WorkflowNodeGraphInvalidNodeAliasError"/>
	/// <seealso cref="WorkflowNodeGraphInvalidResourceNodeError"/>
	/// <seealso cref="WorkflowNodeGraphInvalidResourcePoolNodeError"/>
	/// <seealso cref="WorkflowNodeSwapNotAllowedError"/>
	public class WorkflowNodeGraphInvalidNodeError : WorkflowNodeGraphError
	{
		/// <summary>
		/// Gets the unique identifier of the workflow node.
		/// </summary>
		public string NodeId { get; internal set; }
	}
}
