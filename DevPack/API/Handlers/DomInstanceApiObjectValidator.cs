namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;

	internal class DomInstanceApiObjectValidator<T> : ApiObjectValidator<T> where T : DomInstanceBase
	{
		private readonly List<Guid> successfulIds = new List<Guid>();

		internal override IReadOnlyCollection<Guid> SuccessfulIds => successfulIds;

		protected override void ReportSuccess(T item)
		{
			if (unsuccessfulItems.Contains(item.ID.Id))
			{
				throw new InvalidOperationException($"An item cannot be marked as both successful and unsuccessful");
			}

			successfulIds.Add(item.ID.Id);
			successfulItems.Add(item);
		}

		protected IEnumerable<DomChangeResults> GetItemsWithChanges<TApiObject, TDomInstance>(
			ICollection<TApiObject> apiObjects,
			Func<TApiObject, DomInstanceBase> getOriginalInstance,
			Func<TApiObject, DomInstanceBase> getUpdatedInstance,
			Func<IEnumerable<Guid>, IEnumerable<TDomInstance>> fetchStoredInstances,
			Func<TApiObject, MediaOpsErrorData> createNotFoundError,
			Func<TApiObject, string, MediaOpsErrorData> createValueChangedError)
			where TApiObject : ApiObject
			where TDomInstance : DomInstanceBase
		{
			if (apiObjects == null)
			{
				throw new ArgumentNullException(nameof(apiObjects));
			}

			if (apiObjects.Count == 0)
			{
				return [];
			}

			return GetItemsWithChangesIterator(apiObjects, getOriginalInstance, getUpdatedInstance, fetchStoredInstances, createNotFoundError, createValueChangedError);
		}

		private IEnumerable<DomChangeResults> GetItemsWithChangesIterator<TApiObject, TDomInstance>(
			ICollection<TApiObject> apiObjects,
			Func<TApiObject, DomInstanceBase> getOriginalInstance,
			Func<TApiObject, DomInstanceBase> getUpdatedInstance,
			Func<IEnumerable<Guid>, IEnumerable<TDomInstance>> fetchStoredInstances,
			Func<TApiObject, MediaOpsErrorData> createNotFoundError,
			Func<TApiObject, string, MediaOpsErrorData> createValueChangedError)
			where TApiObject : ApiObject
			where TDomInstance : DomInstanceBase
		{
			var itemsRequiringValidation = apiObjects.Where(x => !x.IsNew && x.HasChanges).ToList();
			if (itemsRequiringValidation.Count == 0)
			{
				yield break;
			}

			var storedInstancesById = fetchStoredInstances(itemsRequiringValidation.Select(x => x.Id)).ToDictionary(x => x.ID.Id);
			foreach (var item in itemsRequiringValidation)
			{
				if (!storedInstancesById.TryGetValue(item.Id, out var stored))
				{
					ReportError(item.Id, createNotFoundError(item));
					continue;
				}

				var changeResult = DomChangeHandler.HandleChanges(getOriginalInstance(item), getUpdatedInstance(item), stored);
				if (changeResult.HasErrors)
				{
					foreach (var errorDetails in changeResult.Errors)
					{
						ReportError(item.Id, createValueChangedError(item, errorDetails.Message));
					}
				}

				yield return changeResult;
			}
		}
	}
}
