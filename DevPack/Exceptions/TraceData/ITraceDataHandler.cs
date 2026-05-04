namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Net;

	internal interface ITraceDataHandler<T> where T : ErrorData
	{
		IReadOnlyDictionary<Guid, MediaOpsTraceData> Translate(ICollection<T> coreErrorData);
	}
}
