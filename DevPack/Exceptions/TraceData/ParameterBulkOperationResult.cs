namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Profiles;

	internal class ParameterBulkOperationResult : BulkOperationResult<Parameter>
	{
		public ParameterBulkOperationResult(IReadOnlyCollection<Parameter> successItems, IReadOnlyCollection<Guid> unsuccessfulIds, IReadOnlyDictionary<Guid, MediaOpsTraceData> traceDataPerItem) : base(successItems, GetSuccessfulIds(successItems), unsuccessfulIds, traceDataPerItem)
		{
		}

		private static IReadOnlyCollection<Guid> GetSuccessfulIds(IReadOnlyCollection<Parameter> successItems)
		{
			return successItems.Select(item => item.ID).ToList();
		}
	}
}
