namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Maintains a mutable, lazily initialized view of the custom property values and property values associated
/// with a single owning object (e.g. a workflow, a workflow node, a job or a job node). The initial values are
/// pulled from the shared <see cref="PropertyValuesLoader"/> the first time they are accessed; from that point
/// on all reads and mutations operate on the local copy.
/// </summary>
internal sealed class PropertyValuesEditor
{
private readonly Func<string> getLinkedObjectId;
private readonly Func<string> getSubId;
private readonly Func<PropertyValueCollection> getOriginalCollection;
private readonly Func<IReadOnlyCollection<CustomPropertyValue>> getInitialCustomValues;
private readonly Func<IReadOnlyCollection<PropertyValue>> getInitialPropertyValues;

private List<CustomPropertyValue> customValues;
private List<PropertyValue> propertyValues;
private bool isDirty;

internal PropertyValuesEditor(
Func<string> getLinkedObjectId,
Func<string> getSubId,
Func<PropertyValueCollection> getOriginalCollection,
Func<IReadOnlyCollection<CustomPropertyValue>> getInitialCustomValues,
Func<IReadOnlyCollection<PropertyValue>> getInitialPropertyValues)
{
this.getLinkedObjectId = getLinkedObjectId;
this.getSubId = getSubId;
this.getOriginalCollection = getOriginalCollection;
this.getInitialCustomValues = getInitialCustomValues;
this.getInitialPropertyValues = getInitialPropertyValues;
}

internal string LinkedObjectId => getLinkedObjectId?.Invoke();

internal string SubId => getSubId?.Invoke() ?? string.Empty;

/// <summary>
/// Gets a value indicating whether the user ever mutated the editor's collections.
/// </summary>
internal bool IsDirty => isDirty;

internal IReadOnlyCollection<CustomPropertyValue> CustomPropertyValues => CustomValuesList;

internal IReadOnlyCollection<PropertyValue> PropertyValues => PropertyValuesList;

private List<CustomPropertyValue> CustomValuesList
=> customValues ??= getInitialCustomValues?.Invoke()?.ToList() ?? [];

private List<PropertyValue> PropertyValuesList
=> propertyValues ??= getInitialPropertyValues?.Invoke()?.ToList() ?? [];

internal void AddCustomProperty(CustomPropertyValue value)
{
CustomValuesList.Add(value);
isDirty = true;
}

internal void SetCustomProperties(IEnumerable<CustomPropertyValue> values)
{
customValues = values?.ToList() ?? [];
isDirty = true;
}

internal void RemoveCustomProperty(CustomPropertyValue value)
{
CustomValuesList.Remove(value);
isDirty = true;
}

internal void AddProperty(PropertyValue value)
{
PropertyValuesList.Add(value);
isDirty = true;
}

internal void SetProperties(IEnumerable<PropertyValue> values)
{
propertyValues = values?.ToList() ?? [];
isDirty = true;
}

internal void RemoveProperty(PropertyValue value)
{
PropertyValuesList.Remove(value);
isDirty = true;
}

/// <summary>
/// Produces the work item that the <see cref="PropertyValueCollection"/> handler should act on
/// based on the current editor state. Returns <c>null</c> when nothing needs to happen
/// (editor never mutated and no original collection to keep in sync).
/// </summary>
internal PropertyValuesPersistenceAction BuildPersistenceAction()
{
if (!isDirty)
{
return null;
}

var original = getOriginalCollection?.Invoke();
var hasContent = (customValues != null && customValues.Count > 0)
|| (propertyValues != null && propertyValues.Count > 0);

if (!hasContent)
{
return original != null
? PropertyValuesPersistenceAction.Delete(original)
: null;
}

PropertyValueCollection target;
if (original != null)
{
target = original;
target.Clear();
}
else
{
target = new PropertyValueCollection
{
LinkedObjectId = LinkedObjectId,
Scope = PropertyValuesLoader.MediaOpsScope,
SubId = SubId,
};
}

if (customValues != null)
{
foreach (var value in customValues)
{
target.Add(value);
}
}

if (propertyValues != null)
{
foreach (var value in propertyValues)
{
target.Add(value);
}
}

return PropertyValuesPersistenceAction.CreateOrUpdate(target);
}
}

internal sealed class PropertyValuesPersistenceAction
{
private PropertyValuesPersistenceAction(PropertyValueCollection collection, bool delete)
{
Collection = collection;
IsDelete = delete;
}

internal PropertyValueCollection Collection { get; }

internal bool IsDelete { get; }

internal static PropertyValuesPersistenceAction CreateOrUpdate(PropertyValueCollection collection)
=> new PropertyValuesPersistenceAction(collection, delete: false);

internal static PropertyValuesPersistenceAction Delete(PropertyValueCollection collection)
=> new PropertyValuesPersistenceAction(collection, delete: true);
}
}
