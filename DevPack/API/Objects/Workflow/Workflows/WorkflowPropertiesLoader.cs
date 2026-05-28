namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	/// <summary>
	/// Handles batch lazy loading of property values for a workflow and all its nodes in a single query.
	/// </summary>
	internal sealed class WorkflowPropertiesLoader
	{
		internal const string MediaOpsScope = "MediaOps";

		private static readonly IReadOnlyCollection<CustomPropertyValue> EmptyCustomValues = [];
		private static readonly IReadOnlyCollection<PropertyValue> EmptyPropertyValues = [];

		private readonly Lazy<Dictionary<string, (IReadOnlyCollection<CustomPropertyValue> customValues, IReadOnlyCollection<PropertyValue> propertyValues)>> _lazy;

		internal WorkflowPropertiesLoader(MediaOpsPlanApi planApi, Guid workflowId, IEnumerable<string> nodeIds)
		{
			var capturedNodeIds = nodeIds?.ToList() ?? new List<string>();

			_lazy = new Lazy<Dictionary<string, (IReadOnlyCollection<CustomPropertyValue>, IReadOnlyCollection<PropertyValue>)>>(
				() => Load(planApi, workflowId, capturedNodeIds));
		}

		internal IReadOnlyCollection<CustomPropertyValue> GetCustomPropertyValues(string objectId)
		{
			if (objectId == null)
			{
				return EmptyCustomValues;
			}

			if (_lazy.Value.TryGetValue(objectId, out var data))
			{
				return data.customValues;
			}

			return EmptyCustomValues;
		}

		internal IReadOnlyCollection<PropertyValue> GetPropertyValues(string objectId)
		{
			if (objectId == null)
			{
				return EmptyPropertyValues;
			}

			if (_lazy.Value.TryGetValue(objectId, out var data))
			{
				return data.propertyValues;
			}

			return EmptyPropertyValues;
		}

		private static Dictionary<string, (IReadOnlyCollection<CustomPropertyValue>, IReadOnlyCollection<PropertyValue>)> Load(
			MediaOpsPlanApi planApi,
			Guid workflowId,
			List<string> nodeIds)
		{
			var result = new Dictionary<string, (IReadOnlyCollection<CustomPropertyValue>, IReadOnlyCollection<PropertyValue>)>(
				StringComparer.OrdinalIgnoreCase);

			var workflowIdString = workflowId.ToString();
			var allIds = new List<string> { workflowIdString };
			allIds.AddRange(nodeIds);

			// Property value collections for both the workflow itself and all of its nodes share the same
			// LinkedObjectId (the workflow ID) and use the 'MediaOps' scope. The workflow's own collection has
			// an empty SubId while each node's collection uses the node ID as SubId. A single filter on
			// LinkedObjectId and Scope therefore fetches the data for the workflow and all of its nodes at once.
			var filter = new ANDFilterElement<PropertyValueCollection>(
				PropertyValueCollectionExposers.LinkedObjectId.Equal(workflowIdString),
				PropertyValueCollectionExposers.Scope.Equal(MediaOpsScope));
			var collections = planApi.PropertyValueCollections.Read(filter);

			var groupedCustom = new Dictionary<string, List<CustomPropertyValue>>(StringComparer.OrdinalIgnoreCase);
			var groupedProperty = new Dictionary<string, List<PropertyValue>>(StringComparer.OrdinalIgnoreCase);

			foreach (var collection in collections)
			{
				if (collection.LinkedObjectId == null)
				{
					continue;
				}

				var key = string.IsNullOrEmpty(collection.SubId) ? workflowIdString : collection.SubId;

				if (!groupedCustom.TryGetValue(key, out var customList))
				{
					customList = [];
					groupedCustom[key] = customList;
				}

				customList.AddRange(collection.CustomValues);

				if (!groupedProperty.TryGetValue(key, out var propertyList))
				{
					propertyList = [];
					groupedProperty[key] = propertyList;
				}

				propertyList.AddRange(collection.PropertyValues);
			}

			foreach (var id in allIds)
			{
				var customValues = groupedCustom.TryGetValue(id, out var cv)
					? (IReadOnlyCollection<CustomPropertyValue>)cv
					: EmptyCustomValues;
				var propertyValues = groupedProperty.TryGetValue(id, out var pv)
					? (IReadOnlyCollection<PropertyValue>)pv
					: EmptyPropertyValues;

				result[id] = (customValues, propertyValues);
			}

			return result;
		}
	}
}
