namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Represents the concurrency and capacity usage of a resource during a time range.
	/// </summary>
	public sealed class ResourceUsage
	{
		internal ResourceUsage(int concurrencyConsumption, int remainingConcurrency, IEnumerable<CapacityUsage> capacityUsages)
		{
			ConcurrencyConsumption = concurrencyConsumption;
			RemainingConcurrency = remainingConcurrency;
			CapacityUsages = capacityUsages.ToList().AsReadOnly();
		}

		/// <summary>
		/// Gets the number of concurrency slots currently consumed.
		/// </summary>
		public int ConcurrencyConsumption { get; }

		/// <summary>
		/// Gets the number of concurrency slots remaining.
		/// </summary>
		public int RemainingConcurrency { get; }

		/// <summary>
		/// Gets the capacity usage details.
		/// </summary>
		public IReadOnlyCollection<CapacityUsage> CapacityUsages { get; }
	}
}