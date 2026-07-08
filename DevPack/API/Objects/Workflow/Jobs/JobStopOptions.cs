namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents options for stopping a running job.
	/// </summary>
	public class JobStopOptions
	{
		/// <summary>
		/// Gets or sets the new post-roll end time of the job.
		/// When <see langword="null"/> (the default), the current post-roll end time is kept for jobs that have a pre/post-roll configured, while jobs without a post-roll have their post-roll end aligned with the new end time.
		/// When set, the provided value replaces the current post-roll end time and must lie sufficiently in the future.
		/// </summary>
		public DateTimeOffset? NewPostRollEnd { get; set; }
	}
}
