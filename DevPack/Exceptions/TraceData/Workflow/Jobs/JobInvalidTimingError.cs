namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when a job configuration specifies invalid timing.
	/// </summary>
	public sealed class JobInvalidTimingError : JobError
	{
		/// <summary>
		/// Gets the start time of the job.
		/// </summary>
		public DateTimeOffset Start { get; internal set; }

		/// <summary>
		/// Gets the end time of the job.
		/// </summary>
		public DateTimeOffset End { get; internal set; }
	}
}
