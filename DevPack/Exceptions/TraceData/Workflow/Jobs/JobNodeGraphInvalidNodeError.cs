namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a node in the node graph of a job is invalid.
	/// </summary>
	/// <seealso cref="JobNodeGraphDuplicateNodeIdError"/>
	/// <seealso cref="JobNodeGraphEmptyNodeIdError"/>
	/// <seealso cref="JobNodeGraphInvalidNodeAliasError"/>
	/// <seealso cref="JobNodeGraphInvalidResourceNodeError"/>
	/// <seealso cref="JobNodeGraphInvalidResourcePoolNodeError"/>
	/// <seealso cref="JobNodeSwapNotAllowedError"/>
	public class JobNodeGraphInvalidNodeError : JobNodeGraphError
	{
		/// <summary>
		/// Gets the unique identifier of the job node.
		/// </summary>
		public string NodeId { get; internal set; }
	}
}
