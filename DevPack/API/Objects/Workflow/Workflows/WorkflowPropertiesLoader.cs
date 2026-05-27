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

			var allIds = new List<string> { workflowId.ToString() };
			allIds.AddRange(nodeIds);

			var filterElements = allIds
				.Select(id => PropertyValueCollectionExposers.LinkedObjectId.Equal(id))
				.ToArray();

			var filter = new ORFilterElement<PropertyValueCollection>(filterElements);
			var collections = planApi.PropertyValueCollections.Read(filter);

			var groupedCustom = new Dictionary<string, List<CustomPropertyValue>>(StringComparer.OrdinalIgnoreCase);
			var groupedProperty = new Dictionary<string, List<PropertyValue>>(StringComparer.OrdinalIgnoreCase);

			foreach (var collection in collections)
			{
				var id = collection.LinkedObjectId;
				if (id == null)
				{
					continue;
				}

				if (!groupedCustom.TryGetValue(id, out var customList))
				{
					customList = [];
					groupedCustom[id] = customList;
				}

				customList.AddRange(collection.CustomValues);

				if (!groupedProperty.TryGetValue(id, out var propertyList))
				{
					propertyList = [];
					groupedProperty[id] = propertyList;
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
