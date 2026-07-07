namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when a job is transitioned to completed before its post-roll end time has passed.
	/// </summary>
	public sealed class JobPostRollEndNotReachedError : JobError
	{
		/// <summary>
		/// Gets the post-roll end time of the job.
		/// </summary>
		public DateTimeOffset PostRollEnd { get; internal set; }
	}
}
