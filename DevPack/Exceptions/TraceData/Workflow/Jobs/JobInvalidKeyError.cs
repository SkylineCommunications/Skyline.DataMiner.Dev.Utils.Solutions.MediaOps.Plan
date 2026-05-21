namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a job configuration specifies an invalid key.
	/// </summary>
	public sealed class JobInvalidKeyError : JobError
	{
		/// <summary>
		/// Gets the key of the job.
		/// </summary>
		public string Key { get; internal set; }
	}
}
