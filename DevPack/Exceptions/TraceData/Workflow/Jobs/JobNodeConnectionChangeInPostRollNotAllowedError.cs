namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a connection of a running job that is already in its post-roll is added,
	/// removed or reconfigured.
	/// </summary>
	public sealed class JobNodeConnectionChangeInPostRollNotAllowedError : JobNodeGraphChangeInPostRollNotAllowedError
	{
		/// <summary>
		/// Gets the unique identifier of the job connection.
		/// </summary>
		public string ConnectionId { get; internal set; }
	}
}
