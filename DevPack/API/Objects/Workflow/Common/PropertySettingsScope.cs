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

		private PropertySettingCollection settings;
		private bool isDirty;

		internal PropertySettingsScope(Func<PropertySettingsContext> getContext, string subId)
		{
			this.getContext = getContext;
			this.subId = subId ?? string.Empty;
		}

		internal bool IsDirty => isDirty;

		internal IReadOnlyCollection<CustomPropertySetting> CustomPropertySettings => Settings.CustomSettings;

		internal IReadOnlyCollection<PropertySetting> PropertySettings => Settings.PropertySettings;

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

		/// <summary>
		/// Gets the backing collection for this scope. It is seeded lazily from the context, and because
		/// <see cref="PropertySettingCollection.Add"/> wraps every setting in a fresh inner instance, the
		/// scope never shares a reference with the caller (or with the owner a setting originated from).
		/// </summary>
		private PropertySettingCollection Settings => settings ??= BuildInitialSettings();

		private PropertySettingCollection BuildInitialSettings()
		{
			var collection = new PropertySettingCollection();
			var context = Context;
			var key = Key;

			foreach (var setting in context?.GetInitialCustomSettings(key) ?? [])
			{
				collection.Add(setting);
			}

			foreach (var setting in context?.GetInitialPropertySettings(key) ?? [])
			{
				collection.Add(setting);
			}

			return collection;
		}

		internal void AddCustomProperty(CustomPropertySetting setting)
		{
			if (setting == null)
			{
				throw new ArgumentNullException(nameof(setting));
			}

			Settings.Add(setting);
			isDirty = true;
		}

		internal void SetCustomProperties(IEnumerable<CustomPropertySetting> settings)
		{
			if (settings == null)
			{
				throw new ArgumentNullException(nameof(settings));
			}

			Settings.SetCustomSettings(settings.Where(s => s != null));
			isDirty = true;
		}

		internal void RemoveCustomProperty(CustomPropertySetting setting)
		{
			if (setting == null)
			{
				throw new ArgumentNullException(nameof(setting));
			}

			Settings.Remove(setting);
			isDirty = true;
		}

		internal void AddProperty(PropertySetting setting)
		{
			if (setting == null)
			{
				throw new ArgumentNullException(nameof(setting));
			}

			Settings.Add(setting);
			isDirty = true;
		}

		internal void SetProperties(IEnumerable<PropertySetting> settings)
		{
			if (settings == null)
			{
				throw new ArgumentNullException(nameof(settings));
			}

			Settings.SetPropertySettings(settings.Where(s => s != null));
			isDirty = true;
		}

		internal void RemoveProperty(PropertySetting setting)
		{
			if (setting == null)
			{
				throw new ArgumentNullException(nameof(setting));
			}

			Settings.Remove(setting);
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
			var current = Settings;

			if (current.Count == 0)
			{
				return original != null ? PropertyValuesPersistenceAction.Delete(original) : null;
			}

			// Dirty content must always carry owner metadata. The context is the single source of truth
			// for LinkedObjectId; without it the collection would be persisted orphaned (null link).
			if (context == null)
			{
				throw new InvalidOperationException(
					"Cannot persist property settings because the owning context has not been wired. " +
					"Ensure the owner's context is created (e.g. via EnsureContext) before saving.");
			}

			if (original != null)
			{
				// Existing owner: reuse the loaded collection so the handler performs an update with the
				// correct identity and DOM tracking, replacing its content with the current state.
				original.Clear();

				foreach (var setting in current.CustomSettings)
				{
					original.Add(setting);
				}

				foreach (var setting in current.PropertySettings)
				{
					original.Add(setting);
				}

				return PropertyValuesPersistenceAction.CreateOrUpdate(original);
			}

			// New owner: build the persistence collection with its owner metadata set once via the init
			// properties, then copy the current in-memory state into it.
			var target = new PropertySettingCollection
			{
				LinkedObjectId = context.LinkedObjectId,
				Scope = PropertySettingsContext.MediaOpsScope,
				SubId = subId,
			};

			foreach (var setting in current.CustomSettings)
			{
				target.Add(setting);
			}

			foreach (var setting in current.PropertySettings)
			{
				target.Add(setting);
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
