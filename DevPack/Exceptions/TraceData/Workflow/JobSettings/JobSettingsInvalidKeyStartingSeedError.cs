namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a job settings configuration specifies an invalid key starting seed.
	/// </summary>
	public sealed class JobSettingsInvalidKeyStartingSeedError : JobSettingsError
	{
		/// <summary>
		/// Gets the initial seed value used for key generation.
		/// </summary>
		public int KeyStartingSeed { get; internal set; }
	}
}
