namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents options for starting a job.
	/// </summary>
	public class JobStartOptions
	{
		/// <summary>
		/// Gets or sets the new start time of the job.
		/// When <see langword="null"/> (the default), the current start time is kept, even when a pre-roll is configured.
		/// When set, the provided value replaces the current start time and must lie in the future and not be later than the job's end time.
		/// </summary>
		public DateTimeOffset? NewStartTime { get; set; }
	}
}
