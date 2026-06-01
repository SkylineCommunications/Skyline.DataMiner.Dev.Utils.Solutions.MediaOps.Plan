namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Helpers shared by handlers that own <see cref="PropertySettingCollection"/> objects (workflows,
	/// jobs, recurring jobs, ...). Aggregates the persistence actions produced by a set of owner
	/// <see cref="PropertySettingsScope"/>s while leaving each handler in charge of dispatching the
	/// resulting batches and translating handler failures into its own error vocabulary.
	/// </summary>
	internal static class PropertyValuesScopeExtensions
	{
		/// <summary>
		/// Walks the supplied (ownerId, scope) pairs, asks every scope for its
		/// <see cref="PropertyValuesPersistenceAction"/> and splits the results into a create/update
		/// batch and a delete batch. The returned map associates each produced collection id with the
		/// owner it belongs to so callers can map failures back to the right owner object.
		/// </summary>
		internal static (List<PropertySettingCollection> ToCreateOrUpdate, List<PropertySettingCollection> ToDelete, Dictionary<Guid, Guid> OwnerIdByCollectionId)
			BuildPersistenceActions(this IEnumerable<KeyValuePair<Guid, PropertySettingsScope>> ownerScopes)
		{
			var toCreateOrUpdate = new List<PropertySettingCollection>();
			var toDelete = new List<PropertySettingCollection>();
			var ownerIdByCollectionId = new Dictionary<Guid, Guid>();

			if (ownerScopes == null)
			{
				return (toCreateOrUpdate, toDelete, ownerIdByCollectionId);
			}

			foreach (var pair in ownerScopes)
			{
				var action = pair.Value?.BuildPersistenceAction();
				if (action == null)
				{
					continue;
				}

				ownerIdByCollectionId[action.Collection.Id] = pair.Key;

				if (action.IsDelete)
				{
					toDelete.Add(action.Collection);
				}
				else
				{
					toCreateOrUpdate.Add(action.Collection);
				}
			}

			return (toCreateOrUpdate, toDelete, ownerIdByCollectionId);
		}
	}
}
