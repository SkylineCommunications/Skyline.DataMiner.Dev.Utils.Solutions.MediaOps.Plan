namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when a recurring job configuration specifies an invalid end date for its recurring pattern.
	/// </summary>
	public sealed class RecurringJobInvalidEndDateError : RecurringJobError
	{
		/// <summary>
		/// Gets the end date of the recurring pattern.
		/// </summary>
		public DateTimeOffset EndDate { get; internal set; }
	}
}
