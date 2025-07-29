namespace Skyline.DataMiner.MediaOps.Plan.Exceptions
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Contains the IDs of the items that got processed and the <see cref="MediaOpsTraceData"/> per item.
    /// </summary>
    /// <typeparam name="K">The type of the ID of an item.</typeparam>
    public interface IBulkOperationResult<K> where K : IEquatable<K>
    {
        /// <summary>
        /// Gets a collection of IDs of items that were successfully processed.
        /// </summary>
        IReadOnlyCollection<K> SuccessfulIds { get; }

        /// <summary>
        /// Gets a the <see cref="MediaOpsTraceData"/> per processed item.
        /// </summary>
        IReadOnlyDictionary<K, MediaOpsTraceData> TraceDataPerItem { get; }

        /// <summary>
        /// Gets a collection of IDs of the items that could not get processed.
        /// </summary>
        IReadOnlyCollection<K> UnsuccessfulIds { get; }
    }
}
