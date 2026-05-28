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

private readonly Lazy<Dictionary<string, LoadedEntry>> _lazy;

internal PropertyValuesLoader(MediaOpsPlanApi planApi, Guid ownerId, IEnumerable<string> subIds)
{
var capturedSubIds = subIds?.ToList() ?? new List<string>();

_lazy = new Lazy<Dictionary<string, LoadedEntry>>(
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
return data.CustomValues;
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
return data.PropertyValues;
}

return EmptyPropertyValues;
}

internal PropertyValueCollection GetOriginalCollection(string objectId)
{
if (objectId == null)
{
return null;
}

return _lazy.Value.TryGetValue(objectId, out var data) ? data.OriginalCollection : null;
}

/// <summary>
/// Returns every original <see cref="PropertyValueCollection"/> that was loaded for the owner
/// and its sub-objects, or <c>null</c> when the loader has not yet been materialized. Callers
/// that have not triggered loading can use this to avoid forcing an otherwise unneeded query.
/// </summary>
internal IReadOnlyCollection<PropertyValueCollection> TryGetCachedOriginalCollections()
{
if (!_lazy.IsValueCreated)
{
return null;
}

return _lazy.Value.Values
.Select(x => x.OriginalCollection)
.Where(x => x != null)
.ToList();
}

private static Dictionary<string, LoadedEntry> Load(
MediaOpsPlanApi planApi,
Guid ownerId,
List<string> subIds)
{
var result = new Dictionary<string, LoadedEntry>(StringComparer.OrdinalIgnoreCase);

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

var byKey = new Dictionary<string, PropertyValueCollection>(StringComparer.OrdinalIgnoreCase);

foreach (var collection in collections)
{
if (collection.LinkedObjectId == null)
{
continue;
}

var key = string.IsNullOrEmpty(collection.SubId) ? ownerIdString : collection.SubId;
byKey[key] = collection;
}

foreach (var id in allIds)
{
if (byKey.TryGetValue(id, out var collection))
{
result[id] = new LoadedEntry(
collection,
collection.CustomValues.ToList(),
collection.PropertyValues.ToList());
}
else
{
result[id] = new LoadedEntry(null, EmptyCustomValues, EmptyPropertyValues);
}
}

return result;
}

private sealed class LoadedEntry
{
internal LoadedEntry(
PropertyValueCollection originalCollection,
IReadOnlyCollection<CustomPropertyValue> customValues,
IReadOnlyCollection<PropertyValue> propertyValues)
{
OriginalCollection = originalCollection;
CustomValues = customValues;
PropertyValues = propertyValues;
}

internal PropertyValueCollection OriginalCollection { get; }

internal IReadOnlyCollection<CustomPropertyValue> CustomValues { get; }

internal IReadOnlyCollection<PropertyValue> PropertyValues { get; }
}
}
}
