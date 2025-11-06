namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using Skyline.DataMiner.MediaOps.Plan.Exceptions;

    internal class ApiObjectValidator<T>
    {
        private readonly Dictionary<T, MediaOpsTraceData> traceDataPerItem = new Dictionary<T, MediaOpsTraceData>();
        private readonly HashSet<T> successfulIItems = new HashSet<T>();
        private readonly HashSet<T> unsuccessfulItems = new HashSet<T>();

        internal IReadOnlyDictionary<T, MediaOpsTraceData> TraceDataPerItem => traceDataPerItem;

        internal IReadOnlyCollection<T> SuccessfulItems => successfulIItems;

        internal IReadOnlyCollection<T> UnsuccessfulItems => unsuccessfulItems;

        internal void PassTraceData(T key, MediaOpsTraceData traceData)
        {
            if (traceDataPerItem.ContainsKey(key)) return;
            traceDataPerItem.Add(key, traceData);
        }

        internal void PassTraceData(ApiObjectValidator<T> internalValidator)
        {
            if (internalValidator == null) throw new ArgumentNullException(nameof(internalValidator));

            // Pass items in error state
            foreach (var itemId in internalValidator.TraceDataPerItem.Keys)
            {
                foreach (var traceData in internalValidator.TraceDataPerItem[itemId].ErrorData)
                {
                    AddValidationError(itemId, traceData);
                }
            }

            // Pass successful items
            foreach (var itemId in internalValidator.SuccessfulItems)
            {
                ReportSuccess(itemId);
            }
        }

        internal void AddValidationError(T key, MediaOpsErrorData error)
        {
            if (Object.Equals(key, default))
            {
                throw new ArgumentException($"Key cannot have default value {default}.", nameof(key));
            }

            if (error == null)
            {
                throw new ArgumentNullException(nameof(error));
            }

            if (!traceDataPerItem.TryGetValue(key, out var mediaOpsTraceData))
            {
                mediaOpsTraceData = new MediaOpsTraceData();
                traceDataPerItem.Add(key, mediaOpsTraceData);
            }

            mediaOpsTraceData.Add(error);
        }

        protected void ReportError(T key, MediaOpsErrorData error)
        {
            AddValidationError(key, error);
            ReportError(key);
        }

        protected void ReportError(T key)
        {
            if (Object.Equals(key, default))
            {
                throw new ArgumentException($"Key cannot have default value {default}.", nameof(key));
            }

            if (successfulIItems.Contains(key))
            {
                throw new InvalidOperationException($"An item cannot be marked as both successful and unsuccessful");
            }

            unsuccessfulItems.Add(key);
        }

        protected void ReportError(IEnumerable<T> keys)
        {
            foreach (var key in keys)
            {
                ReportError(key);
            }
        }

        protected void ReportSuccess(T key)
        {
            if (Object.Equals(key, default))
            {
                throw new ArgumentException($"Key cannot have default value {default}.", nameof(key));
            }

            if (unsuccessfulItems.Contains(key))
            {
                throw new InvalidOperationException($"An item cannot be marked as both successful and unsuccessful");
            }

            successfulIItems.Add(key);
        }

        protected void ReportSuccess(IEnumerable<T> keys)
        {
            foreach (var key in keys)
            {
                ReportSuccess(key);
            }
        }
    }
}
