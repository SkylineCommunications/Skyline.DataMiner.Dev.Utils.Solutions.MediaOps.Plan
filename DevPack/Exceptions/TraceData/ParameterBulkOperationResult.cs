namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.Net.Profiles;

    internal class ParameterBulkOperationResult : BulkOperationResult<Parameter>
    {
        public ParameterBulkOperationResult(IReadOnlyCollection<Parameter> successItems, IReadOnlyCollection<Guid> unsuccessfulIds, IReadOnlyDictionary<Guid, MediaOpsTraceData> traceDataPerItem) : base(successItems, unsuccessfulIds, traceDataPerItem)
        {
        }

        public override IReadOnlyCollection<Guid> SuccessfulIds => SuccessfulItems.Select(item => item.ID).ToList();
    }
}
