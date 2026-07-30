namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when a recurring job configuration specifies an invalid pre-roll.
	/// </summary>
	public sealed class RecurringJobInvalidPreRollError : RecurringJobError
	{
		/// <summary>
		/// Gets the pre-roll duration of the recurring job.
		/// </summary>
		public TimeSpan PreRollDuration { get; internal set; }
	}
}
