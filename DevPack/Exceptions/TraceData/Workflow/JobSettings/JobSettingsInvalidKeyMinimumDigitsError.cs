namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a job settings configuration specifies an invalid key minimum digits.
	/// </summary>
	public sealed class JobSettingsInvalidKeyMinimumDigitsError : JobSettingsError
	{
		/// <summary>
		/// Gets the minimum number of digits required for the key value.
		/// </summary>
		public int KeyMinimumDigits { get; internal set; }
	}
}
