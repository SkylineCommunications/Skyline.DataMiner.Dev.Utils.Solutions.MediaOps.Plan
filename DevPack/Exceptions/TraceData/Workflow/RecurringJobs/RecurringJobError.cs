namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;

	/// <summary>
	/// Represents an error that occurs when creating or updating a recurring job with invalid configuration.
	/// </summary>
	public class RecurringJobError : MediaOpsErrorData
	{
		/// <summary>
		/// Gets the unique identifier for the recurring job.
		/// </summary>
		public Guid Id { get; internal set; }
	}
}
