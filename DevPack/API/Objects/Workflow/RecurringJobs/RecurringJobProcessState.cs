namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Represents the state of a recurring job process.
	/// </summary>
	public enum RecurringJobProcessState
	{
		/// <summary>
		/// The process state could not be retrieved.
		/// </summary>
		NA = 0,

		/// <summary>
		/// The process is updating the series of recurring jobs.
		/// </summary>
		UpdatingSeries = 1,

		/// <summary>
		/// The series of recurring jobs has been updated.
		/// </summary>
		SeriesUpdated = 2,

		/// <summary>
		/// The series of recurring jobs could not be updated.
		/// </summary>
		UpdateFailed = 3,

		/// <summary>
		/// The process is canceling the series of recurring jobs.
		/// </summary>
		CancelingSeries = 4
	}
}
