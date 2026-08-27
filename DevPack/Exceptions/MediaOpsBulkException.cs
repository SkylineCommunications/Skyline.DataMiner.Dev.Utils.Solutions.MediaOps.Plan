namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Thrown when a MediaOps bulk operation failed.
	/// </summary>
	/// <typeparam name="K">The type of the identifiers used in the bulk operation.</typeparam>
	public class MediaOpsBulkException<K> : MediaOpsBulkException
		where K : IEquatable<K>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MediaOpsBulkException{K}"/> class with the specified bulk operation result.
		/// </summary>
		/// <param name="result">The result of the bulk operation that caused the exception.</param>
		/// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is <c>null</c>.</exception>
		public MediaOpsBulkException(IBulkOperationResult<K> result)
		{
			Result = result ?? throw new ArgumentNullException(nameof(result));
		}

		/// <summary>
		/// Gets the result of the bulk operation that caused the exception.
		/// </summary>
		public IBulkOperationResult<K> Result { get; }

		/// <summary>
		/// Gets the error message that explains the reason for this <see cref="MediaOpsException" />.
		/// </summary>
		public override string Message
		{
			get
			{
				if (Result.TraceDataPerItem.Count == 1 && Result.TraceDataPerItem.First().Value.ErrorData.Count == 1)
				{
					return Result.TraceDataPerItem.First().Value.ErrorData[0].ErrorMessage;
				}

				return BuildFailureSummary();
			}
		}

		// Never call ToString() from here: Exception.ToString() reads the virtual Message, which would recurse until the stack overflows.
		private string BuildFailureSummary()
		{
			const int maxSuccessfulIds = 10;
			var successfulIds = string.Join(", ", Result.SuccessfulIds.Take(maxSuccessfulIds));
			if (Result.SuccessfulIds.Count > maxSuccessfulIds)
			{
				successfulIds += $", ... ({Result.SuccessfulIds.Count - maxSuccessfulIds} more)";
			}

			var lines = new List<string>(3 + Result.UnsuccessfulIds.Count)
									{
										$"Bulk CRUD operation: {Result.SuccessfulIds.Count} succeeded, {Result.UnsuccessfulIds.Count} failed",
										$" - IDs of the successful items: {successfulIds}",
										$" - Failures:",
									};

			var traces = Result.TraceDataPerItem;
			foreach (var id in Result.UnsuccessfulIds)
			{
				if (!traces.TryGetValue(id, out var traceData))
				{
					continue;
				}

				var traceDataLines = traceData?.ToString().Split(new[] { Environment.NewLine, "\n" }, StringSplitOptions.None) ?? Array.Empty<string>();
				lines.Add($"  - {id}:");
				lines.AddRange(traceDataLines.Select(x => $"    {x}"));
			}

			return string.Join(Environment.NewLine, lines);
		}
	}

	/// <summary>
	/// Represents an exception thrown when a MediaOps bulk operation fails.
	/// </summary>
	public class MediaOpsBulkException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MediaOpsBulkException"/> class.
		/// </summary>
		protected internal MediaOpsBulkException()
		{
		}
	}
}
