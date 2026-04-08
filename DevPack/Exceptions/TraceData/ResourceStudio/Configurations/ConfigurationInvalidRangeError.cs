namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when the combination of minimum and maximum value are invalid.
	/// </summary>
	public class ConfigurationInvalidRangeError : ConfigurationError
	{
		/// <summary>
		/// Gets or sets the minimum allowable range value.
		/// </summary>
		public decimal RangeMin { get; set; }

		/// <summary>
		/// Gets or sets the maximum allowable range value.
		/// </summary>
		public decimal RangeMax { get; set; }
	}
}
