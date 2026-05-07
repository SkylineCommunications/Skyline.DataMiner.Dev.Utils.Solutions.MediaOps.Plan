namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcProperties;

	internal static class DomPropertyValueCollectionHandler
	{
		internal static bool TryCreateOrUpdate(MediaOpsPlanApi planApi, ICollection<PropertyValueCollection> apiObjects, out DomInstanceBulkOperationResult<PropertyValuesInstance> result)
		{
			if (apiObjects == null)
			{
				throw new ArgumentNullException(nameof(apiObjects));
			}

			var duplicateCollections = GetCollectionsWithDuplicateLinkedObjectAndSubId(apiObjects);
			if (duplicateCollections.Count > 0)
			{
				var duplicateCollectionIds = duplicateCollections.Select(x => x.Id).Distinct().ToList();
				var traceDataPerItem = duplicateCollectionIds.ToDictionary(
					id => id,
					id =>
					{
						var traceData = new MediaOpsTraceData();
						traceData.Add(new MediaOpsErrorData
						{
							ErrorMessage = "The combination of LinkedObjectId and SubId must be unique.",
						});
						return traceData;
					});

				result = new DomInstanceBulkOperationResult<PropertyValuesInstance>(Array.Empty<PropertyValuesInstance>(), duplicateCollectionIds, traceDataPerItem);
				return false;
			}

			throw new NotImplementedException();
		}

		internal static bool TryDelete(MediaOpsPlanApi planApi, ICollection<PropertyValueCollection> apiObjects, out DomInstanceBulkOperationResult<PropertyValuesInstance> result)
		{
			throw new NotImplementedException();
		}

		internal static IReadOnlyCollection<PropertyValueCollection> GetCollectionsWithDuplicateLinkedObjectAndSubId(ICollection<PropertyValueCollection> apiObjects)
		{
			if (apiObjects == null)
			{
				throw new ArgumentNullException(nameof(apiObjects));
			}

			return apiObjects
				.Where(x => InputValidator.IsNonEmptyText(x.LinkedObjectId))
				.GroupBy(x => (x.LinkedObjectId, x.SubId))
				.Where(g => g.Count() > 1)
				.SelectMany(g => g)
				.ToList();
		}
	}
}
