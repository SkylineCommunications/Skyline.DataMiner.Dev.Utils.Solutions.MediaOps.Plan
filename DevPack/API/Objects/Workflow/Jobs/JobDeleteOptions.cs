namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Represents options for deleting a job.
	/// </summary>
	public class JobDeleteOptions
	{
		/// <summary>
		/// Gets or sets a value indicating whether to force delete the job.
		/// When set to <c>true</c>, the job is deleted regardless of its state; otherwise, only jobs in the Draft, Canceled or Completed state can be deleted.
		/// </summary>
		public bool ForceDelete { get; set; } = false;

		internal static JobDeleteOptions GetDefaults()
		{
			return new JobDeleteOptions
			{
				ForceDelete = false,
			};
		}
	}
}
