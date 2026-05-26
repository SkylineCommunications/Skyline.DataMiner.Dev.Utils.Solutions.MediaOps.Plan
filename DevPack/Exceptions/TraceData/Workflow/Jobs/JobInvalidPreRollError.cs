namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when a job configuration specifies an invalid pre-roll duration.
	/// </summary>
	public sealed class JobInvalidPreRollError : JobError
	{
		/// <summary>
		/// Gets the pre-roll duration of the job.
		/// </summary>
		public TimeSpan PreRoll { get; internal set; }
	}
}
