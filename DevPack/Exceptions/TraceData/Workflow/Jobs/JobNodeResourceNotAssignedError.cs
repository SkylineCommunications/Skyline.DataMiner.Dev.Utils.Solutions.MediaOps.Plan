namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a job cannot be confirmed because one of its nodes does not have a
	/// concrete resource assigned.
	/// </summary>
	public sealed class JobNodeResourceNotAssignedError : JobError
	{
		/// <summary>
		/// Gets the unique identifier of the job node that does not have a concrete resource assigned.
		/// </summary>
		public string NodeId { get; internal set; }
	}
}
