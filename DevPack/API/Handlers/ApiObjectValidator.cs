namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Tools;

    internal class ApiObjectValidator
    {
        private readonly Dictionary<Guid, MediaOpsTraceData> traceDataPerItem = new Dictionary<Guid, MediaOpsTraceData>();
        private readonly HashSet<Guid> successfulIItems = new HashSet<Guid>();
        private readonly HashSet<Guid> unsuccessfulItems = new HashSet<Guid>();

        internal IReadOnlyDictionary<Guid, MediaOpsTraceData> TraceDataPerItem => traceDataPerItem;

        internal IReadOnlyCollection<Guid> SuccessfulItems => successfulIItems;

        internal IReadOnlyCollection<Guid> UnsuccessfulItems => unsuccessfulItems;

        internal void PassTraceData(Guid key, MediaOpsTraceData traceData)
        {
            if (traceDataPerItem.ContainsKey(key)) return;
            traceDataPerItem.Add(key, traceData);
        }

        internal void PassTraceData(ApiObjectValidator internalValidator)
        {
            if (internalValidator == null) throw new ArgumentNullException(nameof(internalValidator));

            // Pass items in error state
            foreach (var id in internalValidator.UnsuccessfulItems)
            {
                ReportError(id);
                if (internalValidator.TraceDataPerItem.TryGetValue(id, out var traceData))
                {
                    PassTraceData(id, traceData);
                }
            }

            // Pass successful items
            foreach (var itemId in internalValidator.SuccessfulItems)
            {
                ReportSuccess(itemId);
            }
        }

        internal void AddValidationError(Guid key, MediaOpsErrorData error)
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

        protected void ReportError(Guid key, MediaOpsErrorData error)
        {
            AddValidationError(key, error);
            ReportError(key);
        }

        protected void ReportError(Guid key)
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

        protected void ReportError(IEnumerable<Guid> keys)
        {
            foreach (var key in keys)
            {
                ReportError(key);
            }
        }

        protected void ReportError<T>(LockManager.LockResult<T> result) where T : ApiObject
        {
            foreach (var failedToLockObject in result.FailedToLockObjects)
            {
                ReportError(failedToLockObject.Id, new MediaOpsErrorData() { ErrorMessage = $"Failed to lock {typeof(T)}." });
            }
        }

        protected void ReportSuccess(Guid key)
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

        protected void ReportSuccess(IEnumerable<Guid> keys)
        {
            foreach (var key in keys)
            {
                ReportSuccess(key);
            }
        }

        protected bool IsValid(IIdentifiable identifiable)
        {
            return !TraceDataPerItem.Keys.Contains(identifiable.Id);
        }
    }
}
