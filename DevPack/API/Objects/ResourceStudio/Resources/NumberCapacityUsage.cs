namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents usage of a numeric capacity.
	/// </summary>
	public sealed class NumberCapacityUsage : CapacityUsage
	{
		internal NumberCapacityUsage(Guid capacityId, decimal currentConsumption, decimal remaining)
			: base(capacityId)
		{
			CurrentConsumption = currentConsumption;
			Remaining = remaining;
		}

		/// <summary>
		/// Gets the capacity currently consumed.
		/// </summary>
		public decimal CurrentConsumption { get; }

		/// <summary>
		/// Gets the capacity remaining.
		/// </summary>
		public decimal Remaining { get; }
	}
}