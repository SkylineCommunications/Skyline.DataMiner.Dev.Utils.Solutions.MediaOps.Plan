namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;

	/// <summary>
	/// Owner-scoped context that lazily loads every <see cref="PropertySettingCollection"/> belonging to
	/// an owning object (e.g. a workflow, job or recurring job) and its sub-objects (e.g. nodes) in a
	/// single backend call, and that hands out per-owner <see cref="PropertySettingsScope"/> instances
	/// hiding the storage details (<c>LinkedObjectId</c>, <c>Scope</c>, <c>SubId</c>) from the user.
	/// </summary>
	internal sealed class PropertySettingsContext
	{
		internal const string MediaOpsScope = "MediaOps";

		private static readonly IReadOnlyCollection<CustomPropertySetting> EmptyCustomSettings = [];
		private static readonly IReadOnlyCollection<PropertySetting> EmptyPropertySettings = [];

		private readonly Guid ownerId;
		private readonly Lazy<Dictionary<string, LoadedEntry>> lazy;

		internal PropertySettingsContext(MediaOpsPlanApi planApi, Guid ownerId, IEnumerable<string> subIds)
		{
			this.ownerId = ownerId;

			var capturedSubIds = subIds?.ToList() ?? new List<string>();
			lazy = new Lazy<Dictionary<string, LoadedEntry>>(() => Load(planApi, ownerId, capturedSubIds));
		}

		/// <summary>
		/// Gets the linked object id shared by the owner and all of its sub-objects.
		/// </summary>
		internal string LinkedObjectId => ownerId.ToString();

		/// <summary>
		/// Creates a <see cref="PropertySettingsScope"/> for the owner itself (empty <c>SubId</c>).
		/// </summary>
		internal PropertySettingsScope CreateOwnerScope() => new PropertySettingsScope(() => this, subId: string.Empty);

		/// <summary>
		/// Creates a <see cref="PropertySettingsScope"/> for a sub-object identified by <paramref name="subId"/>.
		/// </summary>
		internal PropertySettingsScope CreateSubScope(string subId) => new PropertySettingsScope(() => this, subId ?? string.Empty);

		/// <summary>
		/// Returns every original <see cref="PropertySettingCollection"/> already loaded by the context,
		/// or <c>null</c> when no property access has triggered the lazy load yet. Callers can use this
		/// to avoid forcing a load when none is required (e.g. before a delete).
		/// </summary>
		internal IReadOnlyCollection<PropertySettingCollection> TryGetCachedOriginalCollections()
		{
			if (!lazy.IsValueCreated)
			{
				return null;
			}

			return lazy.Value.Values
				.Select(x => x.OriginalCollection)
				.Where(x => x != null)
				.ToList();
		}

		internal IReadOnlyCollection<CustomPropertySetting> GetInitialCustomSettings(string key)
			=> lazy.Value.TryGetValue(NormalizeKey(key), out var entry) ? entry.CustomSettings : EmptyCustomSettings;

		internal IReadOnlyCollection<PropertySetting> GetInitialPropertySettings(string key)
			=> lazy.Value.TryGetValue(NormalizeKey(key), out var entry) ? entry.PropertySettings : EmptyPropertySettings;

		internal PropertySettingCollection GetOriginalCollection(string key)
			=> lazy.Value.TryGetValue(NormalizeKey(key), out var entry) ? entry.OriginalCollection : null;

		private string NormalizeKey(string subId) => string.IsNullOrEmpty(subId) ? ownerId.ToString() : subId;

		private static Dictionary<string, LoadedEntry> Load(
			MediaOpsPlanApi planApi,
			Guid ownerId,
			List<string> subIds)
		{
			var result = new Dictionary<string, LoadedEntry>(StringComparer.OrdinalIgnoreCase);

			var ownerIdString = ownerId.ToString();
			var allKeys = new List<string> { ownerIdString };
			allKeys.AddRange(subIds);

			if (planApi == null)
			{
				// New/unsaved owner: nothing has ever been persisted yet, so every entry is empty.
				foreach (var key in allKeys)
				{
					result[key] = new LoadedEntry(null, EmptyCustomSettings, EmptyPropertySettings);
				}

				return result;
			}

			// Owner and sub-objects share the same LinkedObjectId and the 'MediaOps' scope, so a single
			// filter retrieves the entire tree at once. The owner uses an empty SubId; sub-objects use
			// their own id as SubId.
			var filter = new ANDFilterElement<PropertySettingCollection>(
				PropertySettingCollectionExposers.LinkedObjectId.Equal(ownerIdString),
				PropertySettingCollectionExposers.Scope.Equal(MediaOpsScope));
			var collections = planApi.PropertySettingCollections.Read(filter);

			var byKey = new Dictionary<string, PropertySettingCollection>(StringComparer.OrdinalIgnoreCase);
			foreach (var collection in collections)
			{
				if (collection.LinkedObjectId == null)
				{
					continue;
				}

				var key = string.IsNullOrEmpty(collection.SubId) ? ownerIdString : collection.SubId;
				byKey[key] = collection;
			}

			foreach (var key in allKeys)
			{
				if (byKey.TryGetValue(key, out var collection))
				{
					result[key] = new LoadedEntry(
						collection,
						collection.CustomSettings.ToList(),
						collection.PropertySettings.ToList());
				}
				else
				{
					result[key] = new LoadedEntry(null, EmptyCustomSettings, EmptyPropertySettings);
				}
			}

			return result;
		}

		private sealed class LoadedEntry
		{
			internal LoadedEntry(
				PropertySettingCollection originalCollection,
				IReadOnlyCollection<CustomPropertySetting> customSettings,
				IReadOnlyCollection<PropertySetting> propertySettings)
			{
				OriginalCollection = originalCollection;
				CustomSettings = customSettings;
				PropertySettings = propertySettings;
			}

			internal PropertySettingCollection OriginalCollection { get; }

			internal IReadOnlyCollection<CustomPropertySetting> CustomSettings { get; }

			internal IReadOnlyCollection<PropertySetting> PropertySettings { get; }
		}
	}
}
