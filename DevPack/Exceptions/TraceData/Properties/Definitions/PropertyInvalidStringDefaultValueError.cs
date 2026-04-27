namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a property's default string value is invalid.
	/// </summary>
	public sealed class PropertyInvalidStringDefaultValueError : PropertyError
	{
		/// <summary>
		/// Gets the configured default value that caused the error.
		/// </summary>
		public string DefaultValue { get; internal set; }
	}
}
