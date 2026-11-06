namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;

	/// <summary>
	/// Represents usage of a numeric capacity.
	/// </summary>
	public sealed class NumberCapacityUsage : CapacityUsage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NumberCapacityUsage"/> class.
		/// </summary>
		/// <param name="capacityId">The capacity identifier.</param>
		/// <param name="currentConsumption">The capacity currently consumed.</param>
		/// <param name="remaining">The capacity remaining.</param>
		public NumberCapacityUsage(Guid capacityId, decimal currentConsumption, decimal remaining)
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