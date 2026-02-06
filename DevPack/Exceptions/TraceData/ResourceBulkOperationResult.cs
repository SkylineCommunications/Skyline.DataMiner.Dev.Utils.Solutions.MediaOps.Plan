namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.Net.Messages;

    internal class ResourceBulkOperationResult : BulkOperationResult<Resource>
    {
        public ResourceBulkOperationResult(IReadOnlyCollection<Resource> successItems, IReadOnlyCollection<Guid> unsuccessfulIds, IReadOnlyDictionary<Guid, MediaOpsTraceData> traceDataPerItem) : base(successItems, unsuccessfulIds, traceDataPerItem)
        {
        }

        protected override IReadOnlyCollection<Guid> GetSuccessfulIds(IReadOnlyCollection<Resource> successItems)
        {
            return successItems.Select(r => r.ID).ToList();
        }
    }
}
