namespace Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Thrown when a MediaOps bulk operation failed.
    /// </summary>
    public class MediaOpsBulkException<K> : MediaOpsException
        where K : IEquatable<K>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MediaOpsBulkException{K}"/> class with the specified bulk operation result.
        /// </summary>
        /// <param name="result">The result of the bulk operation that caused the exception.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is <c>null</c>.</exception>
        public MediaOpsBulkException(IBulkOperationResult<K> result)
            : base(BuildTraceData(result))
        {
            Result = result ?? throw new ArgumentNullException(nameof(result));
        }

        /// <summary>
        /// Gets the result of the bulk operation that caused the exception.
        /// </summary>
        public IBulkOperationResult<K> Result { get; }

        private static MediaOpsTraceData BuildTraceData(IBulkOperationResult<K> result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            var trace = new MediaOpsTraceData();

            foreach (var id in result.UnsuccessfulIds)
            {
                if (!result.TraceDataPerItem.TryGetValue(id, out var itemTrace) || itemTrace == null)
                {
                    continue;
                }

                foreach (var error in itemTrace.ErrorData)
                {
                    // You can later introduce a MediaOpsErrorData-derived type that includes the ID if needed.
                    trace.Add(error);
                }
            }

            if (trace.ErrorData.Count == 0)
            {
                trace.Add(new MediaOpsErrorData
                {
                    ErrorMessage = "Bulk operation failed. See Result for per-item details.",
                });
            }

            return trace;
        }

        /// <summary>
        /// Returns a string that represents the current exception, including details about the bulk operation result.
        /// </summary>
        /// <returns>A string representation of the exception.</returns>
        public override string ToString()
        {
            const int maxSuccessfulIds = 10;
            var successfulIds = string.Join(", ", Result.SuccessfulIds.Take(maxSuccessfulIds));
            if (Result.SuccessfulIds.Count > maxSuccessfulIds)
            {
                successfulIds += $", ... ({Result.SuccessfulIds.Count - maxSuccessfulIds} more)";
            }

            var preLines = new List<string>(3 + Result.UnsuccessfulIds.Count)
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

                var traceDataLines =
                    traceData?.ToString().Split(new[] { Environment.NewLine, "\n" }, StringSplitOptions.None)
                    ?? Array.Empty<string>();

                preLines.Add($"  - {id}:");
                preLines.AddRange(traceDataLines.Select(x => $"    {x}"));
            }

            var lines = preLines.Concat(new[] { base.ToString() });
            return string.Join(Environment.NewLine, lines);
        }
    }
}
