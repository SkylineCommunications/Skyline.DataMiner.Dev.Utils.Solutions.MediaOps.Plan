namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a string property is configured with an invalid size limit, such as a negative value or a value that exceeds the maximum allowed limit.
	/// </summary>
	public sealed class PropertyInvalidStringSizeLimitError : PropertyError
	{
		/// <summary>
		/// Gets the configured size limit that caused the error.
		/// </summary>
		public int SizeLimit { get; internal set; }
	}
}
