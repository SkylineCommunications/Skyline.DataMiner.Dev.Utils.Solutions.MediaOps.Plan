namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when a job is transitioned to running before its pre-roll start time has passed.
	/// </summary>
	public sealed class JobPreRollStartNotReachedError : JobError
	{
		/// <summary>
		/// Gets the pre-roll start time of the job.
		/// </summary>
		public DateTimeOffset PreRollStart { get; internal set; }
	}
}
