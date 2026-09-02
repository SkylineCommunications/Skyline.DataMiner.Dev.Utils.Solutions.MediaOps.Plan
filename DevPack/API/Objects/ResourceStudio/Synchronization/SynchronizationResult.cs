namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	/// <summary>
	/// Represents the outcome of synchronizing a selection of Resource Studio items with CORE.
	/// </summary>
	public sealed class SynchronizationResult
	{
		private SynchronizationResult(IEnumerable<Guid> synchronizedResourcePoolIds, IEnumerable<Guid> synchronizedResourceIds, IDictionary<Guid, MediaOpsTraceData> failures)
		{
			SynchronizedResourcePoolIds = new List<Guid>(synchronizedResourcePoolIds ?? []).AsReadOnly();
			SynchronizedResourceIds = new List<Guid>(synchronizedResourceIds ?? []).AsReadOnly();
			Failures = new Dictionary<Guid, MediaOpsTraceData>(failures ?? new Dictionary<Guid, MediaOpsTraceData>());
		}

		/// <summary>
		/// Gets the identifiers of the DOM resource pools that were synchronized.
		/// </summary>
		public IReadOnlyCollection<Guid> SynchronizedResourcePoolIds { get; }

		/// <summary>
		/// Gets the identifiers of the DOM resources that were synchronized.
		/// </summary>
		public IReadOnlyCollection<Guid> SynchronizedResourceIds { get; }

		/// <summary>
		/// Gets the problems per DOM item that could not be synchronized.
		/// </summary>
		public IReadOnlyDictionary<Guid, MediaOpsTraceData> Failures { get; }

		/// <summary>
		/// Gets a value indicating whether one or more of the selected items could not be synchronized.
		/// </summary>
		public bool HasFailures => Failures.Count > 0;

		internal static SynchronizationResult Create(IEnumerable<Guid> synchronizedResourcePoolIds, IEnumerable<Guid> synchronizedResourceIds, IDictionary<Guid, MediaOpsTraceData> failures)
		{
			return new SynchronizationResult(synchronizedResourcePoolIds, synchronizedResourceIds, failures);
		}
	}
}
