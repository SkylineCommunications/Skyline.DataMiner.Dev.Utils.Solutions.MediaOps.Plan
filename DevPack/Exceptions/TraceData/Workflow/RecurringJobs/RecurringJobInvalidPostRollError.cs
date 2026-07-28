namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when a recurring job configuration specifies an invalid post-roll.
	/// </summary>
	public sealed class RecurringJobInvalidPostRollError : RecurringJobError
	{
		/// <summary>
		/// Gets the post-roll duration of the recurring job.
		/// </summary>
		public TimeSpan PostRollDuration { get; internal set; }
	}
}
