namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Provides constant values for job lifecycle event names.
	/// </summary>
	public static class JobEvent
	{
		/// <summary>
		/// Name of the event that will be executed at the start of a job.
		/// </summary>
		public const string Start = "START";

		/// <summary>
		/// Name of the event that will be executed at the end of a job.
		/// </summary>
		public const string End = "STOP";
	}
}
