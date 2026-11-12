namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents the value a time-dependent capability is booked with during a time range.
	/// </summary>
	public sealed class CapabilityBooking
	{
		internal CapabilityBooking(DateTimeOffset start, DateTimeOffset end, string value)
		{
			Start = start;
			End = end;
			Value = value;
		}

		/// <summary>
		/// Gets the start of the booked time range.
		/// </summary>
		public DateTimeOffset Start { get; }

		/// <summary>
		/// Gets the end of the booked time range.
		/// </summary>
		public DateTimeOffset End { get; }

		/// <summary>
		/// Gets the booked value.
		/// </summary>
		public string Value { get; }
	}
}
