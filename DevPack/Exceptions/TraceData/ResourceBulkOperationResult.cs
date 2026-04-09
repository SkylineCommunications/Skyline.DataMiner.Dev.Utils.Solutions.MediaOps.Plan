namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.Messages;

	internal class ResourceBulkOperationResult : LinkableObjectBulkOperationResult<Resource>
	{
		public ResourceBulkOperationResult(IReadOnlyCollection<Resource> successItems, IReadOnlyCollection<Guid> unsuccessfulIds, IReadOnlyDictionary<Guid, MediaOpsTraceData> traceDataPerItem) : base(successItems, unsuccessfulIds, traceDataPerItem)
		{
		}
	}
}
