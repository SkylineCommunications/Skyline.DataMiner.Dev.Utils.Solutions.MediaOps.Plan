namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Tools;

	internal abstract class ApiObjectValidator<T> : ApiObjectValidator
	{
		protected readonly HashSet<T> successfulItems = new HashSet<T>();

		internal IReadOnlyCollection<T> SuccessfulItems => successfulItems;

		internal abstract IReadOnlyCollection<Guid> SuccessfulIds { get; }

		internal void PassTraceData(ApiObjectValidator<T> internalValidator)
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

		protected override void ReportError(Guid key)
		{
			if (SuccessfulIds.Contains(key))
			{
				throw new InvalidOperationException($"An item cannot be marked as both successful and unsuccessful");
			}

			unsuccessfulItems.Add(key);
		}

		protected void ReportError<K>(LockManager.LockResult<K> result) where K : ApiObject
		{
			foreach (var failedToLockObject in result.FailedToLockObjects)
			{
				ReportError(failedToLockObject.Id, new MediaOpsErrorData() { ErrorMessage = $"Failed to lock {typeof(T).Name} {failedToLockObject.Id}." });
			}
		}

		protected abstract void ReportSuccess(T item);

		protected void ReportSuccess(IEnumerable<T> items)
		{
			foreach (var item in items)
			{
				ReportSuccess(item);
			}
		}
	}

	internal class ApiObjectValidator
	{
		private readonly Dictionary<Guid, MediaOpsTraceData> traceDataPerItem = new Dictionary<Guid, MediaOpsTraceData>();
		protected readonly HashSet<Guid> unsuccessfulItems = new HashSet<Guid>();

		internal IReadOnlyDictionary<Guid, MediaOpsTraceData> TraceDataPerItem => traceDataPerItem;

		internal IReadOnlyCollection<Guid> UnsuccessfulItems => unsuccessfulItems;

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
		}

		internal void PassTraceData(Guid key, MediaOpsTraceData traceData)
		{
			if (!traceDataPerItem.TryGetValue(key, out var existingTraceData))
			{
				traceDataPerItem.Add(key, traceData);
			}
			else
			{
				foreach (var error in traceData.ErrorData)
				{
					existingTraceData.Add(error);
				}
			}
		}

		protected void ReportError(Guid key, MediaOpsErrorData error)
		{
			AddValidationError(key, error);
			ReportError(key);
		}

		protected virtual void ReportError(Guid key)
		{
			unsuccessfulItems.Add(key);
		}

		protected bool IsValid(IIdentifiable identifiable)
		{
			return !TraceDataPerItem.Keys.Contains(identifiable.Id);
		}

		private void AddValidationError(Guid key, MediaOpsErrorData error)
		{
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
	}
}
