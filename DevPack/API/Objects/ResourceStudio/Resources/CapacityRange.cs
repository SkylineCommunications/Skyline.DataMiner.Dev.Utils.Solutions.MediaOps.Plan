namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	/// <summary>
	/// Represents a continuous capacity range.
	/// </summary>
	public sealed class CapacityRange
	{
		internal CapacityRange(decimal start, decimal end)
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