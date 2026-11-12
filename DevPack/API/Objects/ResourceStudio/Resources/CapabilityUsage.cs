namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Represents how a capability of a resource is booked during a time range.
	/// </summary>
	public sealed class CapabilityUsage
	{
		internal CapabilityUsage(Guid capabilityId, bool isTimeDependent, IEnumerable<CapabilityBooking> bookings)
		{
			CapabilityId = capabilityId;
			IsTimeDependent = isTimeDependent;
			Bookings = (bookings ?? Enumerable.Empty<CapabilityBooking>()).ToList().AsReadOnly();
		}

		/// <summary>
		/// Gets the capability identifier.
		/// </summary>
		public Guid CapabilityId { get; }

		/// <summary>
		/// Gets a value indicating whether the capability is time-dependent. A capability that is not time-dependent is
		/// available regardless of other bookings.
		/// </summary>
		public bool IsTimeDependent { get; }

		/// <summary>
		/// Gets how the capability is booked by other bookings that overlap with the time range. Only filled in for a
		/// time-dependent capability.
		/// </summary>
		public IReadOnlyCollection<CapabilityBooking> Bookings { get; }
	}
}
