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
		/// <summary>
		/// Initializes a new instance of the <see cref="RangeCapacityUsage"/> class.
		/// </summary>
		/// <param name="capacityId">The capacity identifier.</param>
		/// <param name="currentConsumption">The ranges currently consumed.</param>
		/// <param name="remaining">The ranges remaining.</param>
		public RangeCapacityUsage(Guid capacityId, IEnumerable<CapacityRange> currentConsumption, IEnumerable<CapacityRange> remaining)
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