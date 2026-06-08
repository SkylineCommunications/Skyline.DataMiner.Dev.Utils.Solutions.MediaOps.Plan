namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when a job configuration specifies an invalid pre-roll.
	/// </summary>
	public sealed class JobInvalidPreRollError : JobError
	{
		/// <summary>
		/// Gets the pre-roll start time of the job.
		/// </summary>
		public DateTimeOffset PreRollStart { get; internal set; }

		/// <summary>
		/// Gets the start time of the job.
		/// </summary>
		public DateTimeOffset Start { get; internal set; }
	}
}
