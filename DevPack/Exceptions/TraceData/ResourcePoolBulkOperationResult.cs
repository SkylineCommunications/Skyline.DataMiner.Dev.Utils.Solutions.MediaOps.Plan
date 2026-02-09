namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    using System;
    using System.Collections.Generic;

    using Skyline.DataMiner.Net.Messages;

    internal class ResourcePoolBulkOperationResult : LinkableObjectBulkOperationResult<ResourcePool>
    {
        public ResourcePoolBulkOperationResult(IReadOnlyCollection<ResourcePool> successItems, IReadOnlyCollection<Guid> unsuccessfulIds, IReadOnlyDictionary<Guid, MediaOpsTraceData> traceDataPerItem) : base(successItems, unsuccessfulIds, traceDataPerItem)
        {
        }
    }
}
