namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Specifies the repeat type of a recurring job.
	/// </summary>
	public enum RepeatType
	{
		/// <summary>
		/// The recurring job instance will not repeat.
		/// </summary>
		Never = 0, // TODO: does it make sense to expose this if we don't allow creating a recurring job that never repeats? Maybe we should remove this value from the enum.

		/// <summary>
		/// The recurring job instance will repeat every day.
		/// </summary>
		Daily = 1,

		/// <summary>
		/// The recurring job instance will repeat every week.
		/// </summary>
		Weekly = 2,

		/// <summary>
		/// The recurring job instance will repeat every month.
		/// </summary>
		Monthly = 3,

		/// <summary>
		/// The recurring job instance will repeat every year.
		/// </summary>
		Yearly = 4,
	}

}

