namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a connection in the node graph of a recurring job is invalid.
	/// </summary>
	public class RecurringJobNodeGraphInvalidConnectionError : RecurringJobNodeGraphError
	{
		/// <summary>
		/// Gets the unique identifier of the recurring job connection.
		/// </summary>
		public string ConnectionId { get; internal set; }
	}
}
