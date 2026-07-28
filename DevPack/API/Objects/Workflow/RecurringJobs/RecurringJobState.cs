namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Specifies the state of a recurring job.
	/// </summary>
	public enum RecurringJobState
	{
		/// <summary>
		/// The recurring job is active.
		/// </summary>
		Active = 0,

		/// <summary>
		/// The recurring job has been cancelled.
		///	</summary>
		Cancelled = 1,

		/// <summary>
		/// The recurring job has been completed.
		/// </summary>
		Completed = 2,
	}
}
