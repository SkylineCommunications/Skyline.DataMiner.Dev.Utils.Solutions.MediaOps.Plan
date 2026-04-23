namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	/// <summary>
	/// Represents an error that occurs when the specified maximum value for a capacity configuration range is invalid.
	/// </summary>
	public sealed class CapacityInvalidRangeMaxError : CapacityError
	{
		/// <summary>
		/// Gets or sets the maximum allowable range value.
		/// </summary>
		public decimal RangeMax { get; internal set; }
	}
}
