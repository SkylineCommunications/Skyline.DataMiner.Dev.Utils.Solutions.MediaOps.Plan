namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;
	using Skyline.DataMiner.Utils.DOM.Extensions;

	using DomLink = Storage.DOM.SlcRelationships.LinksInstance;

	internal class DomRelationshipHandler : DomInstanceApiObjectValidator<DomLink>
	{
		private readonly MediaOpsPlanApi planApi;

		private DomRelationshipHandler(MediaOpsPlanApi planApi)
		{
			this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
		}

		internal static bool TryCreateOrUpdate(MediaOpsPlanApi planApi, ICollection<Relationship> apiRelationships, out DomInstanceBulkOperationResult<DomLink> result)
		{
			var handler = new DomRelationshipHandler(planApi);
			handler.CreateOrUpdate(apiRelationships);

			result = new DomInstanceBulkOperationResult<DomLink>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		internal static bool TryDelete(MediaOpsPlanApi planApi, ICollection<Relationship> apiRelationships, out DomInstanceBulkOperationResult<DomLink> result)
		{
			var handler = new DomRelationshipHandler(planApi);
			handler.Delete(apiRelationships);

			result = new DomInstanceBulkOperationResult<DomLink>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		private void CreateOrUpdate(ICollection<Relationship> apiRelationships)
		{
			if (apiRelationships == null)
			{
				throw new ArgumentNullException(nameof(apiRelationships));
			}

			if (apiRelationships.Count == 0)
			{
				return;
			}

			var toCreate = apiRelationships.Where(x => x.IsNew).ToList();

			ValidateIdsNotInUse(toCreate);
			ValidateEndpoints(apiRelationships);

			var validRelationships = apiRelationships.Where(IsValid).ToList();
			if (validRelationships.Count == 0)
			{
				return;
			}

			// The referenced object types are locked first, so they cannot be removed while the relationships are written.
			// Object type locks are always taken before relationship locks to prevent deadlocks.
			var objectTypeLocks = validRelationships
				.SelectMany(x => new[] { x.Parent.ObjectTypeId, x.Child.ObjectTypeId })
				.Distinct()
				.Select(x => new RelationshipObjectType(x))
				.ToList();

			var objectTypeLockResult = planApi.LockManager.LockAllAndExecute(objectTypeLocks, () =>
			{
				ValidateObjectTypesExist(validRelationships);

				var lockResult = planApi.LockManager.LockAndExecute(validRelationships.Where(IsValid).ToList(), CreateOrUpdateLocked);
				ReportError(lockResult);
			});

			if (objectTypeLockResult.AllLocksGranted)
			{
				return;
			}

			foreach (var relationship in validRelationships.Where(IsValid))
			{
				ReportError(relationship.Id, new MediaOpsErrorData { ErrorMessage = $"Failed to lock relationship object type(s) '{string.Join("', '", objectTypeLockResult.FailedToLockObjects.Select(x => x.Id))}'." });
			}
		}

		private void CreateOrUpdateLocked(ICollection<Relationship> apiRelationships)
		{
			if (apiRelationships == null)
			{
				throw new ArgumentNullException(nameof(apiRelationships));
			}

			if (apiRelationships.Count == 0)
			{
				return;
			}

			if (apiRelationships.Any(x => !IsValid(x)))
			{
				throw new ArgumentException("Not all provided relationships are valid", nameof(apiRelationships));
			}

			var toCreate = apiRelationships.Where(x => x.IsNew).ToList();
			var toUpdate = apiRelationships.Except(toCreate).ToList();

			var changeResults = GetRelationshipsWithChanges(toUpdate);

			var toCreateDomInstances = toCreate
				.Where(IsValid)
				.Select(x => x.GetInstanceWithChanges())
				.ToList();
			var toUpdateDomInstances = changeResults
				.Where(IsValid)
				.Select(x => new DomLink(x.Instance))
				.ToList();
			CreateOrUpdateDomLinks(toCreateDomInstances.Concat(toUpdateDomInstances).ToList());
		}

		private void CreateOrUpdateDomLinks(ICollection<DomLink> domLinks)
		{
			if (domLinks == null)
			{
				throw new ArgumentNullException(nameof(domLinks));
			}

			if (domLinks.Count == 0)
			{
				return;
			}

			planApi.DomHelpers.SlcRelationshipsHelper.DomHelper.DomInstances.TryCreateOrUpdateInBatches(domLinks.Select(x => x.ToInstance()), out var domResult);

			foreach (var id in domResult.UnsuccessfulIds)
			{
				ReportError(id.Id);

				if (domResult.TraceDataPerItem.TryGetValue(id, out var traceData))
				{
					var mediaOpsTraceData = new MediaOpsTraceData();
					mediaOpsTraceData.Add(new MediaOpsErrorData { ErrorMessage = traceData.ToString() });

					PassTraceData(id.Id, mediaOpsTraceData);
				}
			}

			ReportSuccess(domResult.SuccessfulItems.Select(x => new DomLink(x)));
		}

		private void Delete(ICollection<Relationship> apiRelationships)
		{
			if (apiRelationships == null)
			{
				throw new ArgumentNullException(nameof(apiRelationships));
			}

			if (apiRelationships.Count == 0)
			{
				return;
			}

			ValidateStateForDeleteAction(apiRelationships);

			var lockResult = planApi.LockManager.LockAndExecute(apiRelationships.Where(IsValid).ToList(), DeleteLocked);
			ReportError(lockResult);
		}

		private void DeleteLocked(ICollection<Relationship> apiRelationships)
		{
			if (apiRelationships == null)
			{
				throw new ArgumentNullException(nameof(apiRelationships));
			}

			if (apiRelationships.Count == 0)
			{
				return;
			}

			if (apiRelationships.Any(x => !IsValid(x)))
			{
				throw new ArgumentException("Not all provided relationships are valid", nameof(apiRelationships));
			}

			var toDelete = apiRelationships.Select(x => x.OriginalInstance.ToInstance()).ToList();
			planApi.DomHelpers.SlcRelationshipsHelper.DomHelper.DomInstances.TryDeleteInBatches(toDelete, out var domResult);

			foreach (var id in domResult.UnsuccessfulIds)
			{
				ReportError(id.Id);

				if (domResult.TraceDataPerItem.TryGetValue(id, out var traceData))
				{
					var mediaOpsTraceData = new MediaOpsTraceData();
					mediaOpsTraceData.Add(new MediaOpsErrorData { ErrorMessage = traceData.ToString() });

					PassTraceData(id.Id, mediaOpsTraceData);
				}
			}

			ReportSuccess(toDelete.Where(x => domResult.SuccessfulIds.Contains(x.ID)).Select(x => new DomLink(x)));
		}

		private void ValidateIdsNotInUse(ICollection<Relationship> apiRelationships)
		{
			if (apiRelationships == null)
			{
				throw new ArgumentNullException(nameof(apiRelationships));
			}

			if (apiRelationships.Count == 0)
			{
				return;
			}

			var objectsRequiringValidation = apiRelationships.Where(x => x.IsNew && x.HasUserDefinedId).ToList();
			if (objectsRequiringValidation.Count == 0)
			{
				return;
			}

			var objectsWithDuplicateIds = objectsRequiringValidation
				.GroupBy(o => o.Id)
				.Where(g => g.Count() > 1)
				.SelectMany(g => g)
				.ToList();

			foreach (var relationship in objectsWithDuplicateIds)
			{
				var error = new RelationshipDuplicateIdError
				{
					ErrorMessage = $"Relationship '{relationship.Id}' has a duplicate ID.",
					Id = relationship.Id,
				};

				ReportError(relationship.Id, error);

				objectsRequiringValidation.Remove(relationship);
			}

			foreach (var foundInstance in planApi.DomHelpers.SlcRelationshipsHelper.GetRelationshipsInstances(objectsRequiringValidation.Select(x => x.Id)))
			{
				planApi.Logger.Information(this, "ID is already in use by a Relationships instance.", [foundInstance.ID.Id]);

				var error = new RelationshipIdInUseError
				{
					ErrorMessage = "ID is already in use.",
					Id = foundInstance.ID.Id,
				};

				ReportError(foundInstance.ID.Id, error);
			}
		}

		private void ValidateEndpoints(ICollection<Relationship> apiRelationships)
		{
			if (apiRelationships == null)
			{
				throw new ArgumentNullException(nameof(apiRelationships));
			}

			if (apiRelationships.Count == 0)
			{
				return;
			}

			foreach (var relationship in apiRelationships)
			{
				var message = GetEndpointError("Parent", relationship.Parent) ?? GetEndpointError("Child", relationship.Child);
				if (message == null)
				{
					continue;
				}

				var error = new RelationshipInvalidEndpointError
				{
					ErrorMessage = message,
					Id = relationship.Id,
				};

				ReportError(relationship.Id, error);
			}
		}

		private void ValidateObjectTypesExist(ICollection<Relationship> apiRelationships)
		{
			if (apiRelationships == null)
			{
				throw new ArgumentNullException(nameof(apiRelationships));
			}

			if (apiRelationships.Count == 0)
			{
				return;
			}

			var objectTypeIds = apiRelationships
				.SelectMany(x => new[] { x.Parent.ObjectTypeId, x.Child.ObjectTypeId })
				.Distinct()
				.ToList();

			var existingIds = new HashSet<Guid>(planApi.DomHelpers.SlcRelationshipsHelper.GetObjectTypes(objectTypeIds).Select(x => x.ID.Id));

			foreach (var relationship in apiRelationships)
			{
				var missingIds = new[] { relationship.Parent.ObjectTypeId, relationship.Child.ObjectTypeId }
					.Distinct()
					.Where(x => !existingIds.Contains(x))
					.ToList();

				if (missingIds.Count == 0)
				{
					continue;
				}

				var error = new RelationshipInvalidObjectTypeError
				{
					ErrorMessage = $"Relationship object type(s) '{string.Join("', '", missingIds)}' do not exist.",
					Id = relationship.Id,
				};

				ReportError(relationship.Id, error);
			}
		}

		private void ValidateStateForDeleteAction(ICollection<Relationship> apiRelationships)
		{
			foreach (var relationship in apiRelationships.Where(x => x.IsNew))
			{
				var error = new RelationshipNotFoundError
				{
					ErrorMessage = "A relationship that was not saved cannot be removed.",
					Id = relationship.Id,
				};

				ReportError(relationship.Id, error);
			}
		}

		private ICollection<DomChangeResults> GetRelationshipsWithChanges(ICollection<Relationship> apiRelationships)
		{
			return GetItemsWithChanges<Relationship, DomLink>(
				apiRelationships,
				r => r.OriginalInstance,
				r => r.GetInstanceWithChanges(),
				ids => planApi.DomHelpers.SlcRelationshipsHelper.GetLinks(ids),
				r => new RelationshipNotFoundError { ErrorMessage = $"Relationship with ID '{r.Id}' no longer exists.", Id = r.Id },
				(r, msg) => new RelationshipValueAlreadyChangedError { ErrorMessage = msg, Id = r.Id })
				.ToList();
		}

		private static string GetEndpointError(string side, RelationshipEndpoint endpoint)
		{
			if (endpoint == null)
			{
				return $"{side} must be filled out.";
			}

			if (endpoint.ObjectTypeId == Guid.Empty)
			{
				return $"{side} object type must be filled out.";
			}

			if (!InputValidator.IsNonEmptyText(endpoint.ObjectId))
			{
				return $"{side} object ID must be filled out.";
			}

			return null;
		}
	}
}
