namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a connection in the node graph of a recurring job links to a node that is not valid.
	/// </summary>
	public sealed class RecurringJobNodeGraphConnectionWithInvalidNodeError : RecurringJobNodeGraphInvalidConnectionError
	{
		/// <summary>
		/// Gets the unique identifier of the recurring job node.
		/// </summary>
		public string NodeId { get; internal set; }
	}
}
