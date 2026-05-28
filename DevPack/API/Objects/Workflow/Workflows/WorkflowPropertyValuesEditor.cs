namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Maintains a mutable, lazily initialized view of the custom property values and property values associated
	/// with a workflow or a workflow node. The initial values are pulled from the shared <see cref="WorkflowPropertiesLoader"/>
	/// the first time they are accessed; from that point on all reads and mutations operate on the local copy.
	/// </summary>
	internal sealed class WorkflowPropertyValuesEditor
	{
		private readonly Func<IReadOnlyCollection<CustomPropertyValue>> getInitialCustomValues;
		private readonly Func<IReadOnlyCollection<PropertyValue>> getInitialPropertyValues;

		private List<CustomPropertyValue> customValues;
		private List<PropertyValue> propertyValues;

		internal WorkflowPropertyValuesEditor(
			Func<IReadOnlyCollection<CustomPropertyValue>> getInitialCustomValues,
			Func<IReadOnlyCollection<PropertyValue>> getInitialPropertyValues)
		{
			this.getInitialCustomValues = getInitialCustomValues;
			this.getInitialPropertyValues = getInitialPropertyValues;
		}

		internal IReadOnlyCollection<CustomPropertyValue> CustomPropertyValues => CustomValuesList;

		internal IReadOnlyCollection<PropertyValue> PropertyValues => PropertyValuesList;

		private List<CustomPropertyValue> CustomValuesList
			=> customValues ??= getInitialCustomValues?.Invoke()?.ToList() ?? [];

		private List<PropertyValue> PropertyValuesList
			=> propertyValues ??= getInitialPropertyValues?.Invoke()?.ToList() ?? [];

		internal void AddCustomProperty(CustomPropertyValue value)
		{
			CustomValuesList.Add(value);
		}

		internal void SetCustomProperties(IEnumerable<CustomPropertyValue> values)
		{
			customValues = values?.ToList() ?? [];
		}

		internal void RemoveCustomProperty(CustomPropertyValue value)
		{
			CustomValuesList.Remove(value);
		}

		internal void AddProperty(PropertyValue value)
		{
			PropertyValuesList.Add(value);
		}

		internal void SetProperties(IEnumerable<PropertyValue> values)
		{
			propertyValues = values?.ToList() ?? [];
		}

		internal void RemoveProperty(PropertyValue value)
		{
			PropertyValuesList.Remove(value);
		}
	}
}
