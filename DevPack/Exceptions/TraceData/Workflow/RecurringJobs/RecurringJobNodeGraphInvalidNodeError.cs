namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a node in the node graph of a recurring job is invalid.
	/// </summary>
	public class RecurringJobNodeGraphInvalidNodeError : RecurringJobNodeGraphError
	{
		/// <summary>
		/// Gets the unique identifier of the recurring job node.
		/// </summary>
		public string NodeId { get; internal set; }
	}
}
