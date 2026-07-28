namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when the recurring pattern of a recurring job is invalid.
	/// </summary>
	public sealed class RecurringJobInvalidPatternError : RecurringJobError
	{
		/// <summary>
		/// Gets the reason why the recurring pattern is invalid.
		/// </summary>
		public string Reason { get; internal set; }
	}
}
