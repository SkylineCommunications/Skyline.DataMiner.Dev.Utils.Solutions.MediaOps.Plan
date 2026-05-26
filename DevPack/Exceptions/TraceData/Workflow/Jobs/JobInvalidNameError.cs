namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a job configuration specifies an invalid name.
	/// </summary>
	public sealed class JobInvalidNameError : JobError
	{
		/// <summary>
		/// Gets the name of the job.
		/// </summary>
		public string Name { get; internal set; }
	}
}
