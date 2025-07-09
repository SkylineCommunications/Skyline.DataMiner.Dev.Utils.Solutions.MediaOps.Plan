namespace Skyline.DataMiner.MediaOps.Plan.Exceptions
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Contains the successfully created or updated items and the <see cref="MediaOpsTraceData"/> per item.
    /// </summary>
    /// <typeparam name="K">The ID of an item.</typeparam>
    public class BulkCreateOrUpdateResult<K> : IBulkOperationResult<K>
        where K : IEquatable<K>
    {
		internal BulkCreateOrUpdateResult(List<K> successfulIds, List<K> unsuccessfulIds, Dictionary<K, MediaOpsTraceData> traceDataPerItem)
		{
			SuccessfulIds = successfulIds ?? throw new ArgumentNullException(nameof(successfulIds));
			UnsuccessfulIds = unsuccessfulIds ?? throw new ArgumentNullException(nameof(unsuccessfulIds));
			TraceDataPerItem = traceDataPerItem ?? throw new ArgumentNullException(nameof(traceDataPerItem));
		}

        /// <summary>
        /// Gets a list of IDs of successfully created or updated items.
        /// </summary>
        public List<K> SuccessfulIds { get; }

        /// <summary>
        /// Gets the <see cref="MediaOpsTraceData"/> per created or updated item.
        /// </summary>
        public Dictionary<K, MediaOpsTraceData> TraceDataPerItem { get; }

        /// <summary>
        /// Gets a list of IDs of the items that could not get created or updated.
        /// </summary>
        public List<K> UnsuccessfulIds { get; }
    }
}
