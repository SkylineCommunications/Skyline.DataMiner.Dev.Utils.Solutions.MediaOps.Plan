namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a recurring job configuration specifies an invalid name.
	/// </summary>
	public sealed class RecurringJobInvalidNameError : RecurringJobError
	{
		/// <summary>
		/// Gets the name of the recurring job.
		/// </summary>
		public string Name { get; internal set; }
	}
}
