namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a file property is configured with an invalid size limit, such as a value that is not positive or a value that exceeds the maximum file size allowed by DataMiner.
	/// </summary>
	public sealed class PropertyInvalidFileSizeLimitError : PropertyError
	{
		/// <summary>
		/// Gets the configured size limit, in MB, that caused the error.
		/// </summary>
		public long SizeLimit { get; internal set; }
	}
}
