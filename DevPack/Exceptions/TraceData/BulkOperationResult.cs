namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Contains the successfully created or updated items and the <see cref="MediaOpsTraceData"/> per item.
    /// </summary>
    /// <typeparam name="K">The ID of an item.</typeparam>
    public class BulkOperationResult<K> : IBulkOperationResult<K>
        where K : IEquatable<K>
    {
        internal BulkOperationResult(IReadOnlyCollection<K> successfulIds, IReadOnlyCollection<K> unsuccessfulIds, IReadOnlyDictionary<K, MediaOpsTraceData> traceDataPerItem)
        {
            SuccessfulIds = successfulIds ?? throw new ArgumentNullException(nameof(successfulIds));
            UnsuccessfulIds = unsuccessfulIds ?? throw new ArgumentNullException(nameof(unsuccessfulIds));
            TraceDataPerItem = traceDataPerItem ?? throw new ArgumentNullException(nameof(traceDataPerItem));
        }

        /// <summary>
        /// Gets a list of IDs of successfully created or updated items.
        /// </summary>
        public IReadOnlyCollection<K> SuccessfulIds { get; }

        /// <summary>
        /// Gets the <see cref="MediaOpsTraceData"/> per created or updated item.
        /// </summary>
        public IReadOnlyDictionary<K, MediaOpsTraceData> TraceDataPerItem { get; }

        /// <summary>
        /// Gets a list of IDs of the items that could not get created or updated.
        /// </summary>
        public IReadOnlyCollection<K> UnsuccessfulIds { get; }

        internal bool HasFailures
        {
            get
            {
                return UnsuccessfulIds.Count > 0;
            }
        }

        internal void ThrowBulkException()
        {
            throw new MediaOpsBulkException<K>(this);
        }

        internal void ThrowSingleException(K key)
        {
            throw new MediaOpsException(TraceDataPerItem[key]);
        }
    }
}
