namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when a configuration configuration specifies an invalid number of decimal places.
	/// </summary>
	public class ConfigurationInvalidDecimalsError : ConfigurationError
	{
		/// <summary>
		/// Gets or sets the number of decimal places to use for numeric values.
		/// </summary>
		public int Decimals { get; set; }
	}
}
