namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when the combination of minimum and maximum value are invalid.
	/// </summary>
	public sealed class CapacityInvalidRangeError : CapacityError
	{
		/// <summary>
		/// Gets or sets the minimum allowable range value.
		/// </summary>
		public decimal RangeMin { get; internal set; }

		/// <summary>
		/// Gets or sets the maximum allowable range value.
		/// </summary>
		public decimal RangeMax { get; internal set; }
	}
}
