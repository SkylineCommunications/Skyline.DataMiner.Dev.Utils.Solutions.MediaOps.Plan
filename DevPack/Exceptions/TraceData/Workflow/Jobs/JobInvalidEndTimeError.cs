namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when a job configuration specifies an invalid end time.
	/// </summary>
	public sealed class JobInvalidEndTimeError : JobError
	{
		/// <summary>
		/// Gets the end time of the job.
		/// </summary>
		public DateTimeOffset End { get; internal set; }
	}
}
