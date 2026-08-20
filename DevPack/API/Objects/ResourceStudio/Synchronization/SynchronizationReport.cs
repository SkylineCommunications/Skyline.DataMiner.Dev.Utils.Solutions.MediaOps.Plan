namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Represents the Resource Studio items whose DOM configuration is not in sync with their CORE counterpart.
	/// </summary>
	public sealed class SynchronizationReport
	{
		private SynchronizationReport(IEnumerable<ResourcePoolSynchronizationItem> resourcePools, IEnumerable<ResourceSynchronizationItem> resources)
		{
			ResourcePools = new List<ResourcePoolSynchronizationItem>(resourcePools ?? []).AsReadOnly();
			Resources = new List<ResourceSynchronizationItem>(resources ?? []).AsReadOnly();
		}

		/// <summary>
		/// Gets the resource pools that are out of sync.
		/// </summary>
		public IReadOnlyCollection<ResourcePoolSynchronizationItem> ResourcePools { get; }

		/// <summary>
		/// Gets the resources that are out of sync.
		/// </summary>
		public IReadOnlyCollection<ResourceSynchronizationItem> Resources { get; }

		/// <summary>
		/// Gets a value indicating whether everything within the inspected scope is in sync.
		/// </summary>
		public bool IsSynchronized => ResourcePools.Count == 0 && Resources.Count == 0;

		/// <summary>
		/// Gets all out of sync items, resource pools first.
		/// </summary>
		/// <returns>The resource pools followed by the resources.</returns>
		public IEnumerable<SynchronizationItem> GetAllItems()
		{
			return ResourcePools.Cast<SynchronizationItem>().Concat(Resources);
		}

		internal static SynchronizationReport Create(IEnumerable<ResourcePoolSynchronizationItem> resourcePools, IEnumerable<ResourceSynchronizationItem> resources)
		{
			return new SynchronizationReport(resourcePools, resources);
		}
	}
}
