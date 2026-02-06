namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.Net.Profiles;

    internal abstract class LinkableObjectBulkOperationResult<T> : BulkOperationResult<LinkableObject> where T : LinkableObject
    {
        protected LinkableObjectBulkOperationResult(IReadOnlyCollection<T> successItems, IReadOnlyCollection<Guid> unsuccessfulIds, IReadOnlyDictionary<Guid, MediaOpsTraceData> traceDataPerItem) : base(successItems, successItems.Select(x => x.ID).ToList(), unsuccessfulIds, traceDataPerItem)
        {
        }
    }
}
