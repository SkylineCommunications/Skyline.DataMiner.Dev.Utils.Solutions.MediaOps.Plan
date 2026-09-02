namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	/// <summary>
	/// Holds the outcome of a DOM to CORE comparison, keyed on the DOM instance identifier.
	/// </summary>
	internal sealed class SynchronizationDetectionResult
	{
		public Dictionary<Guid, List<SynchronizationDifference>> DifferencesPerItem { get; } = new Dictionary<Guid, List<SynchronizationDifference>>();

		public Dictionary<Guid, MediaOpsTraceData> BlockersPerItem { get; } = new Dictionary<Guid, MediaOpsTraceData>();

		public bool IsSynchronized => DifferencesPerItem.Count == 0 && BlockersPerItem.Count == 0;

		public bool IsSynchronizedItem(Guid id)
		{
			return !DifferencesPerItem.ContainsKey(id) && !BlockersPerItem.ContainsKey(id);
		}

		public IEnumerable<Guid> GetOutOfSyncIds()
		{
			return DifferencesPerItem.Keys.Concat(BlockersPerItem.Keys).Distinct();
		}
	}
}
