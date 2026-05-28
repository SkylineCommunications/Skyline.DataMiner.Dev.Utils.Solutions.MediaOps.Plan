namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
using System;
using System.Collections.Generic;
using System.Linq;

using Skyline.DataMiner.Net.Messages.SLDataGateway;

/// <summary>
/// Handles batch lazy loading of property values for an owning object (such as a workflow or job)
/// and all of its sub-objects (such as nodes) in a single query. The owner shares its
/// <c>LinkedObjectId</c> with all its sub-objects; the owner uses an empty <c>SubId</c> while each
/// sub-object uses its own id as <c>SubId</c>.
/// </summary>
internal sealed class PropertyValuesLoader
{
internal const string MediaOpsScope = "MediaOps";

private static readonly IReadOnlyCollection<CustomPropertyValue> EmptyCustomValues = [];
private static readonly IReadOnlyCollection<PropertyValue> EmptyPropertyValues = [];

private readonly Lazy<Dictionary<string, (IReadOnlyCollection<CustomPropertyValue> customValues, IReadOnlyCollection<PropertyValue> propertyValues)>> _lazy;

internal PropertyValuesLoader(MediaOpsPlanApi planApi, Guid ownerId, IEnumerable<string> subIds)
{
var capturedSubIds = subIds?.ToList() ?? new List<string>();

_lazy = new Lazy<Dictionary<string, (IReadOnlyCollection<CustomPropertyValue>, IReadOnlyCollection<PropertyValue>)>>(
() => Load(planApi, ownerId, capturedSubIds));
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
Guid ownerId,
List<string> subIds)
{
var result = new Dictionary<string, (IReadOnlyCollection<CustomPropertyValue>, IReadOnlyCollection<PropertyValue>)>(
StringComparer.OrdinalIgnoreCase);

var ownerIdString = ownerId.ToString();
var allIds = new List<string> { ownerIdString };
allIds.AddRange(subIds);

// Property value collections for both the owner itself and all of its sub-objects share the same
// LinkedObjectId (the owner ID) and use the 'MediaOps' scope. The owner's own collection has
// an empty SubId while each sub-object's collection uses the sub-object ID as SubId. A single
// filter on LinkedObjectId and Scope therefore fetches the data for the owner and all of its
// sub-objects at once.
var filter = new ANDFilterElement<PropertyValueCollection>(
PropertyValueCollectionExposers.LinkedObjectId.Equal(ownerIdString),
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

var key = string.IsNullOrEmpty(collection.SubId) ? ownerIdString : collection.SubId;

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
