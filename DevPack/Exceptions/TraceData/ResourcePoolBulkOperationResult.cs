namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.Net.Messages;

    internal class ResourcePoolBulkOperationResult : BulkOperationResult<ResourcePool>
    {
        public ResourcePoolBulkOperationResult(IReadOnlyCollection<ResourcePool> successItems, IReadOnlyCollection<Guid> unsuccessfulIds, IReadOnlyDictionary<Guid, MediaOpsTraceData> traceDataPerItem) : base(successItems, unsuccessfulIds, traceDataPerItem)
        {
        }

        protected override IReadOnlyCollection<Guid> GetSuccessfulIds(IReadOnlyCollection<ResourcePool> successItems)
        {
            return successItems.Select(rp => rp.ID).ToList();
        }
    }
}
