namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a job settings configuration specifies an invalid key increment.
	/// </summary>
	public sealed class JobSettingsInvalidKeyIncrementError : JobSettingsError
	{
		/// <summary>
		/// Gets the increment value used for key generation.
		/// </summary>
		public int KeyIncrement { get; internal set; }
	}
}
