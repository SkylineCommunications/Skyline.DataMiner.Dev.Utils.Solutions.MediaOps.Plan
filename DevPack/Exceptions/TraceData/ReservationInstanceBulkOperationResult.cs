namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net.ResourceManager.Objects;

	internal class ReservationInstanceBulkOperationResult : LinkableObjectBulkOperationResult<ReservationInstance>
	{
		public ReservationInstanceBulkOperationResult(IReadOnlyCollection<ReservationInstance> successItems, IReadOnlyCollection<Guid> unsuccessfulIds, IReadOnlyDictionary<Guid, MediaOpsTraceData> traceDataPerItem)
			: base(successItems, unsuccessfulIds, traceDataPerItem)
		{
		}
	}
}
