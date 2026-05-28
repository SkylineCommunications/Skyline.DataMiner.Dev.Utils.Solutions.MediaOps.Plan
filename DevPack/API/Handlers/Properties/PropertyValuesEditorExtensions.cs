namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// Helpers shared by handlers that own <see cref="PropertyValueCollection"/> objects
	/// (workflows, jobs, recurring jobs, ...). Keeps the editor-to-persistence-action
	/// aggregation in one place while leaving each handler in charge of dispatching the
	/// resulting batches and reporting failures in its own error vocabulary.
	/// </summary>
	internal static class PropertyValuesEditorExtensions
	{
		/// <summary>
		/// Walks the supplied (ownerId, editor) pairs, asks every editor for its
		/// <see cref="PropertyValuesPersistenceAction"/> and splits the results into a
		/// create/update batch and a delete batch. The returned map associates each produced
		/// <see cref="PropertyValueCollection"/> Id with the owner it belongs to so callers
		/// can translate handler failures back to their own object.
		/// </summary>
		internal static (List<PropertyValueCollection> ToCreateOrUpdate, List<PropertyValueCollection> ToDelete, Dictionary<Guid, Guid> OwnerIdByCollectionId)
			BuildPersistenceActions(this IEnumerable<KeyValuePair<Guid, PropertyValuesEditor>> ownerEditors)
		{
			var toCreateOrUpdate = new List<PropertyValueCollection>();
			var toDelete = new List<PropertyValueCollection>();
			var ownerIdByCollectionId = new Dictionary<Guid, Guid>();

			if (ownerEditors == null)
			{
				return (toCreateOrUpdate, toDelete, ownerIdByCollectionId);
			}

			foreach (var pair in ownerEditors)
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
