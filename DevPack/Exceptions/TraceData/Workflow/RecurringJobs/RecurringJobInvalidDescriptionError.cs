namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a recurring job configuration specifies an invalid description.
	/// </summary>
	public sealed class RecurringJobInvalidDescriptionError : RecurringJobError
	{
		/// <summary>
		/// Gets the description of the recurring job.
		/// </summary>
		public string Description { get; internal set; }
	}
}
