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
			if (value == null)
			{
				throw new ArgumentNullException(nameof(value));
			}

			if (string.IsNullOrEmpty(value.Name))
			{
				throw new ArgumentException("Custom property name cannot be null or empty.", nameof(value));
			}

			if (CustomValuesList.Any(x => string.Equals(x.Name, value.Name, StringComparison.Ordinal)))
			{
				throw new InvalidOperationException($"A custom property value with name '{value.Name}' already exists.");
			}

			CustomValuesList.Add(value);
		}

		internal void SetCustomProperty(CustomPropertyValue value)
		{
			if (value == null)
			{
				throw new ArgumentNullException(nameof(value));
			}

			if (string.IsNullOrEmpty(value.Name))
			{
				throw new ArgumentException("Custom property name cannot be null or empty.", nameof(value));
			}

			CustomValuesList.RemoveAll(x => string.Equals(x.Name, value.Name, StringComparison.Ordinal));
			CustomValuesList.Add(value);
		}

		internal bool RemoveCustomProperty(string name)
		{
			if (name == null)
			{
				throw new ArgumentNullException(nameof(name));
			}

			return CustomValuesList.RemoveAll(x => string.Equals(x.Name, name, StringComparison.Ordinal)) > 0;
		}

		internal void AddProperty(PropertyValue value)
		{
			if (value == null)
			{
				throw new ArgumentNullException(nameof(value));
			}

			if (PropertyValuesList.Any(x => x.Id == value.Id))
			{
				throw new InvalidOperationException($"A property value for property '{value.Id}' already exists.");
			}

			PropertyValuesList.Add(value);
		}

		internal void SetProperty(PropertyValue value)
		{
			if (value == null)
			{
				throw new ArgumentNullException(nameof(value));
			}

			PropertyValuesList.RemoveAll(x => x.Id == value.Id);
			PropertyValuesList.Add(value);
		}

		internal bool RemoveProperty(Guid propertyId)
		{
			return PropertyValuesList.RemoveAll(x => x.Id == propertyId) > 0;
		}
	}
}
