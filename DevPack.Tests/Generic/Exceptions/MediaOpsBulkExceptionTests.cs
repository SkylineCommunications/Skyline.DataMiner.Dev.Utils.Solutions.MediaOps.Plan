namespace RT_MediaOps.Plan.Generic.Exceptions
{
	using System;
	using System.Collections.Generic;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

	using DomResource = Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio.ResourceInstance;

	[TestClass]
	public sealed class MediaOpsBulkExceptionTests
	{
		[TestMethod]
		public void Message_WithSingleFailure_ReturnsErrorMessage()
		{
			var id = Guid.NewGuid();
			var exception = CreateException((id, "The linked CORE resource no longer exists."));

			Assert.AreEqual("The linked CORE resource no longer exists.", exception.Message);
		}

		[TestMethod]
		public void Message_WithMultipleFailures_ListsEveryFailure()
		{
			var firstId = Guid.NewGuid();
			var secondId = Guid.NewGuid();

			var exception = CreateException((firstId, "First failure."), (secondId, "Second failure."));

			var message = exception.Message;
			StringAssert.Contains(message, "0 succeeded, 2 failed");
			StringAssert.Contains(message, firstId.ToString());
			StringAssert.Contains(message, "First failure.");
			StringAssert.Contains(message, secondId.ToString());
			StringAssert.Contains(message, "Second failure.");
		}

		[TestMethod]
		public void ToString_WithMultipleFailures_ContainsFailureSummary()
		{
			var firstId = Guid.NewGuid();
			var secondId = Guid.NewGuid();

			var exception = CreateException((firstId, "First failure."), (secondId, "Second failure."));

			var text = exception.ToString();
			StringAssert.Contains(text, nameof(MediaOpsBulkException<Guid>).Split('`')[0]);
			StringAssert.Contains(text, "0 succeeded, 2 failed");
			StringAssert.Contains(text, "First failure.");
			StringAssert.Contains(text, "Second failure.");
		}

		private static MediaOpsBulkException<Guid> CreateException(params (Guid Id, string ErrorMessage)[] failures)
		{
			var unsuccessfulIds = new List<Guid>();
			var traceDataPerItem = new Dictionary<Guid, MediaOpsTraceData>();

			foreach (var failure in failures)
			{
				var traceData = new MediaOpsTraceData();
				traceData.Add(new ResourceNotFoundError { Id = failure.Id, ErrorMessage = failure.ErrorMessage });

				unsuccessfulIds.Add(failure.Id);
				traceDataPerItem.Add(failure.Id, traceData);
			}

			var result = new DomInstanceBulkOperationResult<DomResource>(Array.Empty<DomResource>(), unsuccessfulIds, traceDataPerItem);

			// A stack overflow here means Message and ToString() are recursing into each other again.
			return Assert.ThrowsException<MediaOpsBulkException<Guid>>(() => result.ThrowBulkException());
		}
	}
}
