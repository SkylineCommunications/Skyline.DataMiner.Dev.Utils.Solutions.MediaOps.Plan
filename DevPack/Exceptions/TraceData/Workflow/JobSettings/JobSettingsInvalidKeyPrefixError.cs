namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a job settings configuration specifies an invalid key prefix.
	/// </summary>
	public sealed class JobSettingsInvalidKeyPrefixError : JobSettingsError
	{
		/// <summary>
		/// Gets the key prefix of the job settings.
		/// </summary>
		public string KeyPrefix { get; internal set; }
	}
}
