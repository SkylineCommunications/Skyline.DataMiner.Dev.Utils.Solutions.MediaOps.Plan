namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when a job configuration specifies an invalid post-roll.
	/// </summary>
	public sealed class JobInvalidPostRollError : JobError
	{
		/// <summary>
		/// Gets the post-roll end time of the job.
		/// </summary>
		public DateTimeOffset PostRollEnd { get; internal set; }

		/// <summary>
		/// Gets the end time of the job.
		/// </summary>
		public DateTimeOffset End { get; internal set; }
	}
}
