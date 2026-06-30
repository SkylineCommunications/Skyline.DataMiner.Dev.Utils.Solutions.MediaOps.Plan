namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a resource pool node is present on a job that is in the Confirmed or
	/// Running state, where only concrete resource nodes are allowed.
	/// </summary>
	public sealed class JobResourcePoolNodeNotAllowedError : JobError
	{
		/// <summary>
		/// Gets the unique identifier of the resource pool node that is not allowed.
		/// </summary>
		public string NodeId { get; internal set; }
	}
}
