namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Contains the successfully handled items and the <see cref="MediaOpsTraceData"/> per item.
    /// </summary>
    /// <typeparam name="T">The type of objects that were processed.</typeparam>
    internal abstract class BulkOperationResult<T> : IBulkOperationResult<Guid>
        where T : class
    {
        private protected BulkOperationResult(IReadOnlyCollection<T> successItems, IReadOnlyCollection<Guid> successfulIds, IReadOnlyCollection<Guid> unsuccessfulIds, IReadOnlyDictionary<Guid, MediaOpsTraceData> traceDataPerItem)
        {
            SuccessfulItems = successItems ?? throw new ArgumentNullException(nameof(successItems));
            SuccessfulIds = successfulIds ?? throw new ArgumentNullException(nameof(successfulIds));
            UnsuccessfulIds = unsuccessfulIds ?? throw new ArgumentNullException(nameof(unsuccessfulIds));
            TraceDataPerItem = traceDataPerItem ?? throw new ArgumentNullException(nameof(traceDataPerItem));
        }

        /// <summary>
        /// Gets a list of IDs of successfully handled items.
        /// </summary>
        public IReadOnlyCollection<Guid> SuccessfulIds { get; }

        public IReadOnlyCollection<T> SuccessfulItems { get; }

        /// <summary>
        /// Gets the <see cref="MediaOpsTraceData"/> per successfully handled item.
        /// </summary>
        public IReadOnlyDictionary<Guid, MediaOpsTraceData> TraceDataPerItem { get; }

        /// <summary>
        /// Gets a list of IDs of the items that could not get handled.
        /// </summary>
        public IReadOnlyCollection<Guid> UnsuccessfulIds { get; }

        internal bool HasFailures
        {
            get
            {
                return UnsuccessfulIds.Count > 0;
            }
        }

        internal void ThrowBulkException()
        {
            throw new MediaOpsBulkException<Guid>(this);
        }

        internal void ThrowSingleException(Guid key)
        {
            throw new MediaOpsException(TraceDataPerItem[key]);
        }
    }
}
