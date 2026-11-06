namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Represents the concurrency, capacity and capability usage of a resource during a time range.
	/// </summary>
	public sealed class ResourceUsage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ResourceUsage"/> class.
		/// </summary>
		/// <param name="concurrencyConsumption">The number of concurrency slots consumed.</param>
		/// <param name="remainingConcurrency">The number of concurrency slots remaining.</param>
		/// <param name="capacityUsages">The capacity usage details.</param>
		/// <param name="capabilityUsages">The capability usage details.</param>
		public ResourceUsage(int concurrencyConsumption, int remainingConcurrency, IEnumerable<CapacityUsage> capacityUsages, IEnumerable<CapabilityUsage> capabilityUsages)
		{
			ConcurrencyConsumption = concurrencyConsumption;
			RemainingConcurrency = remainingConcurrency;
			CapacityUsages = (capacityUsages ?? Enumerable.Empty<CapacityUsage>()).ToList().AsReadOnly();
			CapabilityUsages = (capabilityUsages ?? Enumerable.Empty<CapabilityUsage>()).ToList().AsReadOnly();
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

		/// <summary>
		/// Gets the capability usage details.
		/// </summary>
		public IReadOnlyCollection<CapabilityUsage> CapabilityUsages { get; }
	}
}