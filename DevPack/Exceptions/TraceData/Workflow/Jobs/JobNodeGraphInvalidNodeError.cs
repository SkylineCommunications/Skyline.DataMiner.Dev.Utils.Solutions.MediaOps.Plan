namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a node in the node graph of a job is invalid.
	/// </summary>
	public class JobNodeGraphInvalidNodeError : JobNodeGraphError
	{
		/// <summary>
		/// Gets the unique identifier of the job node.
		/// </summary>
		public string NodeId { get; internal set; }
	}
}
