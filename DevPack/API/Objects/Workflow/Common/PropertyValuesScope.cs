namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Per-owner mutable view over a <see cref="PropertyValuesContext"/>. Exposes a flat split API
	/// (custom values vs. system-defined property values) to the caller and translates the local
	/// state into a <see cref="PropertyValuesPersistenceAction"/> when it is time to persist.
	/// </summary>
	internal sealed class PropertyValuesScope
	{
		private readonly Func<PropertyValuesContext> getContext;
		private readonly string subId;

		private List<CustomPropertyValue> customValues;
		private List<PropertyValue> propertyValues;
		private bool isDirty;

		internal PropertyValuesScope(Func<PropertyValuesContext> getContext, string subId)
		{
			this.getContext = getContext;
			this.subId = subId ?? string.Empty;
		}

		internal bool IsDirty => isDirty;

		internal IReadOnlyCollection<CustomPropertyValue> CustomPropertyValues => CustomValuesList;

		internal IReadOnlyCollection<PropertyValue> PropertyValues => PropertyValuesList;

		private PropertyValuesContext Context => getContext?.Invoke();

		private string Key
		{
			get
			{
				if (!string.IsNullOrEmpty(subId))
				{
					return subId;
				}

				return Context?.LinkedObjectId ?? string.Empty;
			}
		}

		private List<CustomPropertyValue> CustomValuesList
			=> customValues ??= (Context?.GetInitialCustomValues(Key) ?? []).ToList();

		private List<PropertyValue> PropertyValuesList
			=> propertyValues ??= (Context?.GetInitialPropertyValues(Key) ?? []).ToList();

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
		/// Produces the persistence action that the property value collection handler should apply, or
		/// <c>null</c> when the scope was never mutated.
		/// </summary>
		internal PropertyValuesPersistenceAction BuildPersistenceAction()
		{
			if (!isDirty)
			{
				return null;
			}

			var context = Context;
			var original = context?.GetOriginalCollection(Key);
			var hasContent = (customValues != null && customValues.Count > 0)
				|| (propertyValues != null && propertyValues.Count > 0);

			if (!hasContent)
			{
				return original != null ? PropertyValuesPersistenceAction.Delete(original) : null;
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
					LinkedObjectId = context?.LinkedObjectId,
					Scope = PropertyValuesContext.MediaOpsScope,
					SubId = subId,
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
