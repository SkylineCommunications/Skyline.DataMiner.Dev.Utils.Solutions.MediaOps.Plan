namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when a recurring job configuration specifies an invalid duration.
	/// </summary>
	public sealed class RecurringJobInvalidDurationError : RecurringJobError
	{
		/// <summary>
		/// Gets the duration of the recurring job.
		/// </summary>
		public TimeSpan Duration { get; internal set; }
	}
}
