namespace RT_MediaOps.Plan.Generic.Exceptions
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	[TestClass]
	public sealed class BulkOperationResultTests
	{
		[TestMethod]
		public void ThrowSingleException_WhenRequestedIdHasTraceData_ThrowsThatTraceData()
		{
			var requestedId = Guid.NewGuid();
			var expectedMessage = "Concurrency must be greater than or equal to 1.";

			var result = new TestBulkOperationResult(
				successItems: new[] { "ok" },
				successfulIds: new[] { Guid.NewGuid() },
				unsuccessfulIds: new[] { requestedId },
				traceDataPerItem: new Dictionary<Guid, MediaOpsTraceData>
				{
					[requestedId] = CreateTraceData(expectedMessage),
				});

			var exception = Assert.ThrowsException<MediaOpsException>(() => result.ThrowSingleException(requestedId));
			Assert.AreEqual(expectedMessage, exception.Message);
		}

		[TestMethod]
		public void ThrowSingleException_WhenRequestedIdHasNoTraceData_FallsBackToAvailableTraceData()
		{
			var requestedId = Guid.NewGuid();
			var availableId = Guid.NewGuid();
			var expectedMessage = "Concurrency must be greater than or equal to 1.";

			var result = new TestBulkOperationResult(
				successItems: Array.Empty<string>(),
				successfulIds: Array.Empty<Guid>(),
				unsuccessfulIds: new[] { requestedId },
				traceDataPerItem: new Dictionary<Guid, MediaOpsTraceData>
				{
					[availableId] = CreateTraceData(expectedMessage),
				});

			var exception = Assert.ThrowsException<MediaOpsException>(() => result.ThrowSingleException(requestedId));
			Assert.AreEqual(expectedMessage, exception.Message);
		}

		[TestMethod]
		public void ThrowSingleException_WhenNoTraceDataAvailable_ThrowsGenericMessage()
		{
			var requestedId = Guid.NewGuid();
			var result = new TestBulkOperationResult(
				successItems: Array.Empty<string>(),
				successfulIds: Array.Empty<Guid>(),
				unsuccessfulIds: new[] { requestedId },
				traceDataPerItem: new Dictionary<Guid, MediaOpsTraceData>());

			var exception = Assert.ThrowsException<MediaOpsException>(() => result.ThrowSingleException(requestedId));
			Assert.AreEqual($"Operation failed for item with ID '{requestedId}', but no detailed error information is available.", exception.Message);
		}

		private static MediaOpsTraceData CreateTraceData(string message)
		{
			var traceData = new MediaOpsTraceData();
			traceData.Add(new MediaOpsErrorData { ErrorMessage = message });
			return traceData;
		}

		private sealed class TestBulkOperationResult : BulkOperationResult<string>
		{
			public TestBulkOperationResult(
				IReadOnlyCollection<string> successItems,
				IReadOnlyCollection<Guid> successfulIds,
				IReadOnlyCollection<Guid> unsuccessfulIds,
				IReadOnlyDictionary<Guid, MediaOpsTraceData> traceDataPerItem)
				: base(successItems, successfulIds, unsuccessfulIds, traceDataPerItem)
			{
			}
		}
	}
}
