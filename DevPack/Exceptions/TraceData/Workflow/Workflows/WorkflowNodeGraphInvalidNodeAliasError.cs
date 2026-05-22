namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a node in the node graph of a workflow has an invalid alias.
	/// </summary>
	public sealed class WorkflowNodeGraphInvalidNodeAliasError : WorkflowNodeGraphInvalidNodeError
	{
		/// <summary>
		/// Gets the alias of the workflow node.
		/// </summary>
		public string Alias { get; internal set; }
	}
}
