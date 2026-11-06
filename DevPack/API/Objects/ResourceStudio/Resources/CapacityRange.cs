namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Represents a continuous capacity range.
	/// </summary>
	public sealed class CapacityRange
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CapacityRange"/> class.
		/// </summary>
		/// <param name="start">The start of the range.</param>
		/// <param name="end">The end of the range.</param>
		public CapacityRange(decimal start, decimal end)
		{
			Start = start;
			End = end;
		}

		/// <summary>
		/// Gets the start of the range.
		/// </summary>
		public decimal Start { get; }

		/// <summary>
		/// Gets the end of the range.
		/// </summary>
		public decimal End { get; }
	}
}