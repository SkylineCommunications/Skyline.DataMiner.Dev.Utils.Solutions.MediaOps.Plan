namespace RT_MediaOps.Plan.Generic.Exceptions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	using DomResource = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.ResourceInstance;

	[TestClass]
	public sealed class BulkOperationResultTests
	{
		[TestMethod]
		public void ThrowSingleException_WithTraceDataForKey_ThrowsReportedTraceData()
		{
			var id = Guid.NewGuid();
			var traceData = new MediaOpsTraceData();
			traceData.Add(new ResourceNotFoundError { Id = id, ErrorMessage = "The linked CORE resource no longer exists." });

			var result = CreateResult(new[] { id }, new Dictionary<Guid, MediaOpsTraceData> { [id] = traceData });

			var exception = Assert.ThrowsException<MediaOpsException>(() => result.ThrowSingleException(id));
			Assert.AreSame(traceData, exception.TraceData);
		}

		[TestMethod]
		public void ThrowSingleException_WithTraceDataUnderAnotherKey_ThrowsReportedErrors()
		{
			// An item can be marked unsuccessful while its trace data is dropped, for example when the CORE or DOM
			// bulk operation reports a failure without per-item trace data.
			var requestedId = Guid.NewGuid();
			var otherId = Guid.NewGuid();

			var traceData = new MediaOpsTraceData();
			traceData.Add(new MediaOpsErrorData { ErrorMessage = "Something went wrong." });

			var result = CreateResult(new[] { requestedId, otherId }, new Dictionary<Guid, MediaOpsTraceData> { [otherId] = traceData });

			var exception = Assert.ThrowsException<MediaOpsException>(() => result.ThrowSingleException(requestedId));
			Assert.AreEqual("Something went wrong.", exception.Message);
		}

		[TestMethod]
		public void ThrowSingleException_WithoutAnyTraceData_ThrowsMediaOpsException()
		{
			var id = Guid.NewGuid();

			var result = CreateResult(new[] { id }, new Dictionary<Guid, MediaOpsTraceData>());

			var exception = Assert.ThrowsException<MediaOpsException>(() => result.ThrowSingleException(id));
			Assert.AreEqual(1, exception.TraceData.ErrorData.Count);
			StringAssert.Contains(exception.Message, id.ToString());
		}

		private static DomInstanceBulkOperationResult<DomResource> CreateResult(IReadOnlyCollection<Guid> unsuccessfulIds, IReadOnlyDictionary<Guid, MediaOpsTraceData> traceDataPerItem)
		{
			return new DomInstanceBulkOperationResult<DomResource>(Array.Empty<DomResource>(), unsuccessfulIds.ToList(), traceDataPerItem);
		}
	}
}
