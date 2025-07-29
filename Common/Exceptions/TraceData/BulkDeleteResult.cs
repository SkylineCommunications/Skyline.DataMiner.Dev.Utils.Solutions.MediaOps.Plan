namespace Skyline.DataMiner.MediaOps.Plan.Exceptions
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Contains the successfully deleted items and the <see cref="MediaOpsTraceData"/> per item.
    /// </summary>
    /// <typeparam name="K">The ID of an item.</typeparam>
    public class BulkDeleteResult<K> : IBulkOperationResult<K>
    where K : IEquatable<K>
    {
        internal BulkDeleteResult(IReadOnlyCollection<K> successfulIds, IReadOnlyCollection<K> unsuccessfulIds, IReadOnlyDictionary<K, MediaOpsTraceData> traceDataPerItem)
        {
            SuccessfulIds = successfulIds ?? throw new ArgumentNullException(nameof(successfulIds));
            UnsuccessfulIds = unsuccessfulIds ?? throw new ArgumentNullException(nameof(unsuccessfulIds));
            TraceDataPerItem = traceDataPerItem ?? throw new ArgumentNullException(nameof(traceDataPerItem));
        }

        /// <summary>
        /// Gets a list of IDs of items that were successfully deleted.
        /// </summary>
        public IReadOnlyCollection<K> SuccessfulIds { get; }

        /// <summary>
        /// Gets a the <see cref="MediaOpsTraceData"/> per deleted item.
        /// </summary>
        public IReadOnlyDictionary<K, MediaOpsTraceData> TraceDataPerItem { get; }

        /// <summary>
        /// Gets a list of IDs of the items that could not get created or updated.
        /// </summary>
        public IReadOnlyCollection<K> UnsuccessfulIds { get; }
    }
}
