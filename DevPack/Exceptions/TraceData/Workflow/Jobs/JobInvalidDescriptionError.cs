namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a job configuration specifies an invalid description.
	/// </summary>
	public sealed class JobInvalidDescriptionError : JobError
	{
		/// <summary>
		/// Gets the description of the job.
		/// </summary>
		public string Description { get; internal set; }
	}
}
