namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when a recurring job configuration specifies an invalid start time.
	/// </summary>
	public sealed class RecurringJobInvalidStartTimeError : RecurringJobError
	{
		/// <summary>
		/// Gets the start time of the recurring job.
		/// </summary>
		public DateTimeOffset Start { get; internal set; }
	}
}
