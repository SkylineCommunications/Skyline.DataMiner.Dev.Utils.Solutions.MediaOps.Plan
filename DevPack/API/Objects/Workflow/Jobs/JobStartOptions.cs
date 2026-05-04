namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Represents options for starting a job.
	/// </summary>
	public class JobStartOptions
	{
		/// <summary>
		/// Gets or sets a value indicating whether the job's start time should be preserved.
		/// This is only relevant when a pre-roll is configured.
		/// </summary>
		public bool KeepJobStartTime { get; set; } = true;

		internal static JobStartOptions GetDefaults()
		{
			return new JobStartOptions
			{
				KeepJobStartTime = true,
			};
		}
	}
}
