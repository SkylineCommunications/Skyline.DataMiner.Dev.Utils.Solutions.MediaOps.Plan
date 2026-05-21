namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a connection in the node graph of a job is invalid.
	/// </summary>
	public class JobNodeGraphInvalidConnectionError : JobNodeGraphError
	{
		/// <summary>
		/// Gets the unique identifier of the job connection.
		/// </summary>
		public string ConnectionId { get; internal set; }
	}
}
