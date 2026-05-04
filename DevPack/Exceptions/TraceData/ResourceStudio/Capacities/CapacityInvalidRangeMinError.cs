namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when the specified minimum value for a capacity configuration range is invalid.
	/// </summary>
	public sealed class CapacityInvalidRangeMinError : CapacityError
	{
		/// <summary>
		/// Gets or sets the minimum allowable range value.
		/// </summary>
		public decimal RangeMin { get; internal set; }
	}
}
