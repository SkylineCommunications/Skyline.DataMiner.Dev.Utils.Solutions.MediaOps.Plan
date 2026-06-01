namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Per-owner mutable view over a <see cref="PropertySettingsContext"/>. Exposes a flat split API
	/// (custom values vs. system-defined property values) to the caller and translates the local
	/// state into a <see cref="PropertyValuesPersistenceAction"/> when it is time to persist.
	/// </summary>
	internal sealed class PropertySettingsScope
	{
		private readonly Func<PropertySettingsContext> getContext;
		private readonly string subId;

		private List<CustomPropertySetting> customSettings;
		private List<PropertySetting> propertySettings;
		private bool isDirty;

		internal PropertySettingsScope(Func<PropertySettingsContext> getContext, string subId)
		{
			this.getContext = getContext;
			this.subId = subId ?? string.Empty;
		}

		internal bool IsDirty => isDirty;

		internal IReadOnlyCollection<CustomPropertySetting> CustomPropertySettings => CustomSettingsList;

		internal IReadOnlyCollection<PropertySetting> PropertySettings => PropertySettingsList;

		private PropertySettingsContext Context => getContext?.Invoke();

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

		private List<CustomPropertySetting> CustomSettingsList
			=> customSettings ??= (Context?.GetInitialCustomSettings(Key) ?? []).ToList();

		private List<PropertySetting> PropertySettingsList
			=> propertySettings ??= (Context?.GetInitialPropertySettings(Key) ?? []).ToList();

		internal void AddCustomProperty(CustomPropertySetting setting)
		{
			CustomSettingsList.Add(setting);
			isDirty = true;
		}

		internal void SetCustomProperties(IEnumerable<CustomPropertySetting> settings)
		{
			customSettings = settings?.ToList() ?? [];
			isDirty = true;
		}

		internal void RemoveCustomProperty(CustomPropertySetting setting)
		{
			CustomSettingsList.Remove(setting);
			isDirty = true;
		}

		internal void AddProperty(PropertySetting setting)
		{
			PropertySettingsList.Add(setting);
			isDirty = true;
		}

		internal void SetProperties(IEnumerable<PropertySetting> settings)
		{
			propertySettings = settings?.ToList() ?? [];
			isDirty = true;
		}

		internal void RemoveProperty(PropertySetting setting)
		{
			PropertySettingsList.Remove(setting);
			isDirty = true;
		}

		/// <summary>
		/// Produces the persistence action that the property setting collection handler should apply, or
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
			var hasContent = (customSettings != null && customSettings.Count > 0)
				|| (propertySettings != null && propertySettings.Count > 0);

			if (!hasContent)
			{
				return original != null ? PropertyValuesPersistenceAction.Delete(original) : null;
			}

			PropertySettingCollection target;
			if (original != null)
			{
				target = original;
				target.Clear();
			}
			else
			{
				target = new PropertySettingCollection
				{
					LinkedObjectId = context?.LinkedObjectId,
					Scope = PropertySettingsContext.MediaOpsScope,
					SubId = subId,
				};
			}

			if (customSettings != null)
			{
				foreach (var setting in customSettings)
				{
					target.Add(setting);
				}
			}

			if (propertySettings != null)
			{
				foreach (var setting in propertySettings)
				{
					target.Add(setting);
				}
			}

			return PropertyValuesPersistenceAction.CreateOrUpdate(target);
		}
	}

	internal sealed class PropertyValuesPersistenceAction
	{
		private PropertyValuesPersistenceAction(PropertySettingCollection collection, bool delete)
		{
			Collection = collection;
			IsDelete = delete;
		}

		internal PropertySettingCollection Collection { get; }

		internal bool IsDelete { get; }

		internal static PropertyValuesPersistenceAction CreateOrUpdate(PropertySettingCollection collection)
			=> new PropertyValuesPersistenceAction(collection, delete: false);

		internal static PropertyValuesPersistenceAction Delete(PropertySettingCollection collection)
			=> new PropertyValuesPersistenceAction(collection, delete: true);
	}
}
