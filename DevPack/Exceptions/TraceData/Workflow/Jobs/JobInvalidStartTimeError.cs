namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when a job configuration specifies an invalid start time.
	/// </summary>
	public sealed class JobInvalidStartTimeError : JobError
	{
		/// <summary>
		/// Gets the start time of the job.
		/// </summary>
		public DateTimeOffset Start { get; internal set; }
	}
}
