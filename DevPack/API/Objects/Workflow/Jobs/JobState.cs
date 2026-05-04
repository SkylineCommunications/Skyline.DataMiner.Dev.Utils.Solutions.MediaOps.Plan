namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Represents the state of a job.
	/// </summary>
	public enum JobState
	{
		/// <summary>
		/// The job is in draft state.
		/// </summary>
		Draft = 0,

		/// <summary>
		/// The job is in tentative state.
		/// </summary>
		Tentative = 1,

		/// <summary>
		/// The job is in confirmed state.
		/// </summary>
		Confirmed = 2,

		/// <summary>
		///  The job is in running state.
		/// </summary>
		Running = 3,

		/// <summary>
		/// The job is in completed state.
		/// </summary>
		Completed = 4,

		//ReadyForInvoice = 5,

		/// <summary>
		/// The job is in canceled state.
		/// </summary>
		Canceled = 6,

		//Invoiced = 7
	}
}
