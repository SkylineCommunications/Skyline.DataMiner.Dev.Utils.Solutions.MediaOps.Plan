namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Represents usage of a range capacity.
	/// </summary>
	public sealed class RangeCapacityUsage : CapacityUsage
	{
		internal RangeCapacityUsage(Guid capacityId, IEnumerable<CapacityRange> currentConsumption, IEnumerable<CapacityRange> remaining)
			: base(capacityId)
		{
			CurrentConsumption = currentConsumption.ToList().AsReadOnly();
			Remaining = remaining.ToList().AsReadOnly();
		}

		/// <summary>
		/// Gets the ranges currently consumed.
		/// </summary>
		public IReadOnlyCollection<CapacityRange> CurrentConsumption { get; }

		/// <summary>
		/// Gets the ranges remaining.
		/// </summary>
		public IReadOnlyCollection<CapacityRange> Remaining { get; }
	}
}