namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when a job configuration specifies an invalid post-roll duration.
	/// </summary>
	public sealed class JobInvalidPostRollError : JobError
	{
		/// <summary>
		/// Gets the post-roll duration of the job.
		/// </summary>
		public TimeSpan PostRoll { get; internal set; }
	}
}
