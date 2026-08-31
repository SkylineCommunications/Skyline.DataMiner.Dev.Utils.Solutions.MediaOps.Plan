namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcRelationships;
	using Skyline.DataMiner.Utils.DOM.Extensions;

	using DomObjectType = Storage.DOM.SlcRelationships.ObjectTypesInstance;

	internal class DomRelationshipObjectTypeHandler : DomInstanceApiObjectValidator<DomObjectType>
	{
		private readonly MediaOpsPlanApi planApi;

		private DomRelationshipObjectTypeHandler(MediaOpsPlanApi planApi)
		{
			this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
		}

		internal static bool TryCreateOrUpdate(MediaOpsPlanApi planApi, ICollection<RelationshipObjectType> apiObjectTypes, out DomInstanceBulkOperationResult<DomObjectType> result)
		{
			var handler = new DomRelationshipObjectTypeHandler(planApi);
			handler.CreateOrUpdate(apiObjectTypes);

			result = new DomInstanceBulkOperationResult<DomObjectType>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		internal static bool TryDelete(MediaOpsPlanApi planApi, ICollection<RelationshipObjectType> apiObjectTypes, out DomInstanceBulkOperationResult<DomObjectType> result)
		{
			var handler = new DomRelationshipObjectTypeHandler(planApi);
			handler.Delete(apiObjectTypes);

			result = new DomInstanceBulkOperationResult<DomObjectType>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		private void CreateOrUpdate(ICollection<RelationshipObjectType> apiObjectTypes)
		{
			if (apiObjectTypes == null)
			{
				throw new ArgumentNullException(nameof(apiObjectTypes));
			}

			if (apiObjectTypes.Count == 0)
			{
				return;
			}

			var toCreate = apiObjectTypes.Where(x => x.IsNew).ToList();

			ValidateIdsNotInUse(toCreate);
			ValidateNames(apiObjectTypes);

			var validObjectTypes = apiObjectTypes.Where(IsValid).ToList();
			if (validObjectTypes.Count == 0)
			{
				return;
			}

			// Names must be unique across all object types, so the name validation and the write are serialized on a
			// single lock instead of on the object type locks, which do not exclude each other for different IDs.
			var nameLockResult = planApi.LockManager.LockAllAndExecute(new[] { new ObjectTypeNamesSentinel() }, () =>
			{
				var lockResult = planApi.LockManager.LockAndExecute(validObjectTypes, CreateOrUpdateLocked);
				ReportError(lockResult);
			});

			if (nameLockResult.AllLocksGranted)
			{
				return;
			}

			foreach (var objectType in validObjectTypes.Where(IsValid))
			{
				ReportError(objectType.Id, new MediaOpsErrorData { ErrorMessage = "Failed to lock the relationship object type names." });
			}
		}

		private void CreateOrUpdateLocked(ICollection<RelationshipObjectType> apiObjectTypes)
		{
			if (apiObjectTypes == null)
			{
				throw new ArgumentNullException(nameof(apiObjectTypes));
			}

			if (apiObjectTypes.Count == 0)
			{
				return;
			}

			if (apiObjectTypes.Any(x => !IsValid(x)))
			{
				throw new ArgumentException("Not all provided relationship object types are valid", nameof(apiObjectTypes));
			}

			var toCreate = apiObjectTypes.Where(x => x.IsNew).ToList();
			var toUpdate = apiObjectTypes.Except(toCreate).ToList();

			var changeResults = GetObjectTypesWithChanges(toUpdate);

			var toUpdateNameValidation = toUpdate.Where(x => changeResults.Any(y => y.Instance.ID.Id == x.Id && y.ChangedFields.Select(z => z.FieldDescriptorId).Contains(SlcRelationshipsIds.Sections.ObjectTypeInfo.ObjectName.Id)));
			ValidateDomNames(toCreate.Concat(toUpdateNameValidation).ToList());

			var toCreateDomInstances = toCreate
				.Where(IsValid)
				.Select(x => x.GetInstanceWithChanges())
				.ToList();
			var toUpdateDomInstances = changeResults
				.Where(IsValid)
				.Select(x => new DomObjectType(x.Instance))
				.ToList();
			CreateOrUpdateDomObjectTypes(toCreateDomInstances.Concat(toUpdateDomInstances).ToList());
		}

		private void CreateOrUpdateDomObjectTypes(ICollection<DomObjectType> domObjectTypes)
		{
			if (domObjectTypes == null)
			{
				throw new ArgumentNullException(nameof(domObjectTypes));
			}

			if (domObjectTypes.Count == 0)
			{
				return;
			}

			planApi.DomHelpers.SlcRelationshipsHelper.DomHelper.DomInstances.TryCreateOrUpdateInBatches(domObjectTypes.Select(x => x.ToInstance()), out var domResult);

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

			ReportSuccess(domResult.SuccessfulItems.Select(x => new DomObjectType(x)));
		}

		private void Delete(ICollection<RelationshipObjectType> apiObjectTypes)
		{
			if (apiObjectTypes == null)
			{
				throw new ArgumentNullException(nameof(apiObjectTypes));
			}

			if (apiObjectTypes.Count == 0)
			{
				return;
			}

			ValidateStateForDeleteAction(apiObjectTypes);
			ValidateNamesAreNotReserved(apiObjectTypes.Where(IsValid).ToList());

			var lockResult = planApi.LockManager.LockAndExecute(apiObjectTypes.Where(IsValid).ToList(), DeleteLocked);
			ReportError(lockResult);
		}

		private void DeleteLocked(ICollection<RelationshipObjectType> apiObjectTypes)
		{
			if (apiObjectTypes == null)
			{
				throw new ArgumentNullException(nameof(apiObjectTypes));
			}

			if (apiObjectTypes.Count == 0)
			{
				return;
			}

			if (apiObjectTypes.Any(x => !IsValid(x)))
			{
				throw new ArgumentException("Not all provided relationship object types are valid", nameof(apiObjectTypes));
			}

			// The reference check runs while the object type locks are held: relationship writes lock the object types
			// they reference, so no relationship can be added for an object type that is being removed.
			ValidateObjectTypesAreNotInUse(apiObjectTypes);

			var toDelete = apiObjectTypes.Where(IsValid).Select(x => x.OriginalInstance.ToInstance()).ToList();
			if (toDelete.Count == 0)
			{
				return;
			}

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

			ReportSuccess(toDelete.Where(x => domResult.SuccessfulIds.Contains(x.ID)).Select(x => new DomObjectType(x)));
		}

		private void ValidateIdsNotInUse(ICollection<RelationshipObjectType> apiObjectTypes)
		{
			if (apiObjectTypes == null)
			{
				throw new ArgumentNullException(nameof(apiObjectTypes));
			}

			if (apiObjectTypes.Count == 0)
			{
				return;
			}

			var objectsRequiringValidation = apiObjectTypes.Where(x => x.IsNew && x.HasUserDefinedId).ToList();
			if (objectsRequiringValidation.Count == 0)
			{
				return;
			}

			var objectsWithDuplicateIds = objectsRequiringValidation
				.GroupBy(o => o.Id)
				.Where(g => g.Count() > 1)
				.SelectMany(g => g)
				.ToList();

			foreach (var objectType in objectsWithDuplicateIds)
			{
				var error = new RelationshipObjectTypeDuplicateIdError
				{
					ErrorMessage = $"Relationship object type '{objectType.Name}' has a duplicate ID.",
					Id = objectType.Id,
				};

				ReportError(objectType.Id, error);

				objectsRequiringValidation.Remove(objectType);
			}

			foreach (var foundInstance in planApi.DomHelpers.SlcRelationshipsHelper.GetObjectTypes(objectsRequiringValidation.Select(x => x.Id)))
			{
				planApi.Logger.Information(this, "ID is already in use by a relationship object type instance.", [foundInstance.ID.Id]);

				var error = new RelationshipObjectTypeIdInUseError
				{
					ErrorMessage = "ID is already in use.",
					Id = foundInstance.ID.Id,
				};

				ReportError(foundInstance.ID.Id, error);
			}
		}

		private void ValidateNames(ICollection<RelationshipObjectType> apiObjectTypes)
		{
			if (apiObjectTypes == null)
			{
				throw new ArgumentNullException(nameof(apiObjectTypes));
			}

			if (apiObjectTypes.Count == 0)
			{
				return;
			}

			var objectsRequiringValidation = apiObjectTypes.ToList();

			foreach (var objectType in objectsRequiringValidation.Where(x => !InputValidator.IsNonEmptyText(x.Name)).ToArray())
			{
				var error = new RelationshipObjectTypeInvalidNameError
				{
					ErrorMessage = "Name cannot be empty.",
					Id = objectType.Id,
				};

				ReportError(objectType.Id, error);

				objectsRequiringValidation.Remove(objectType);
			}

			foreach (var objectType in objectsRequiringValidation.Where(x => !InputValidator.HasValidTextLength(x.Name)).ToArray())
			{
				var error = new RelationshipObjectTypeInvalidNameError
				{
					ErrorMessage = $"Name exceeds maximum length of {InputValidator.DefaultMaxTextLength} characters.",
					Id = objectType.Id,
					Name = objectType.Name,
				};

				ReportError(objectType.Id, error);

				objectsRequiringValidation.Remove(objectType);
			}

			ValidateNamesAreNotReserved(objectsRequiringValidation);

			var objectsWithDuplicateNames = objectsRequiringValidation
				.GroupBy(objectType => objectType.Name)
				.Where(g => g.Count() > 1)
				.SelectMany(x => x)
				.ToList();

			foreach (var objectType in objectsWithDuplicateNames)
			{
				var error = new RelationshipObjectTypeDuplicateNameError
				{
					ErrorMessage = $"Relationship object type '{objectType.Name}' has a duplicate name.",
					Id = objectType.Id,
					Name = objectType.Name,
				};

				ReportError(objectType.Id, error);
			}
		}

		// The MediaOps solution owns the reserved object types, so consumers can neither create nor remove them.
		private void ValidateNamesAreNotReserved(ICollection<RelationshipObjectType> apiObjectTypes)
		{
			foreach (var objectType in apiObjectTypes.Where(x => RelationshipObjectType.ReservedNames.Contains(x.Name, StringComparer.OrdinalIgnoreCase)))
			{
				var error = new RelationshipObjectTypeReservedNameError
				{
					ErrorMessage = $"Name '{objectType.Name}' is reserved by the MediaOps solution.",
					Id = objectType.Id,
					Name = objectType.Name,
				};

				ReportError(objectType.Id, error);
			}
		}

		private void ValidateDomNames(ICollection<RelationshipObjectType> apiObjectTypes)
		{
			if (apiObjectTypes == null)
			{
				throw new ArgumentNullException(nameof(apiObjectTypes));
			}

			if (apiObjectTypes.Count == 0)
			{
				return;
			}

			FilterElement<DomInstance> Filter(string name) =>
				DomInstanceExposers.DomDefinitionId.Equal(SlcRelationshipsIds.Definitions.ObjectTypes.Id)
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcRelationshipsIds.Sections.ObjectTypeInfo.ObjectName).Equal(name));

			var domObjectTypesByName = planApi.DomHelpers.SlcRelationshipsHelper.GetObjectTypes(apiObjectTypes.Select(x => x.Name), Filter)
				.GroupBy(x => x.ObjectTypeInfo.ObjectName)
				.ToDictionary(x => x.Key, x => (IReadOnlyCollection<DomObjectType>)x.ToList());

			foreach (var objectType in apiObjectTypes)
			{
				if (!domObjectTypesByName.TryGetValue(objectType.Name, out var domObjectTypes))
				{
					continue;
				}

				var existing = domObjectTypes.Where(x => x.ID.Id != objectType.Id).ToList();
				if (existing.Count == 0)
				{
					continue;
				}

				planApi.Logger.Information(this, $"Name '{objectType.Name}' is already in use by relationship object type(s) with ID(s)", [existing.Select(x => x.ID.Id).ToArray()]);

				var error = new RelationshipObjectTypeNameExistsError
				{
					ErrorMessage = "Name is already in use.",
					Id = objectType.Id,
					Name = objectType.Name,
				};

				ReportError(objectType.Id, error);
			}
		}

		private void ValidateStateForDeleteAction(ICollection<RelationshipObjectType> apiObjectTypes)
		{
			foreach (var objectType in apiObjectTypes.Where(x => x.IsNew))
			{
				var error = new RelationshipObjectTypeNotFoundError
				{
					ErrorMessage = "A relationship object type that was not saved cannot be removed.",
					Id = objectType.Id,
				};

				ReportError(objectType.Id, error);
			}
		}

		private void ValidateObjectTypesAreNotInUse(ICollection<RelationshipObjectType> apiObjectTypes)
		{
			if (apiObjectTypes.Count == 0)
			{
				return;
			}

			var filter = new ORFilterElement<Relationship>(apiObjectTypes
				.SelectMany(x => new[] { RelationshipExposers.Parent.ObjectTypeId.Equal(x.Id), RelationshipExposers.Child.ObjectTypeId.Equal(x.Id) })
				.ToArray());

			var relationshipsByObjectTypeId = planApi.Relationships.Read(filter)
				.SelectMany(r => new[] { r.Parent?.ObjectTypeId ?? Guid.Empty, r.Child?.ObjectTypeId ?? Guid.Empty }.Distinct().Select(objectTypeId => new { Relationship = r, ObjectTypeId = objectTypeId }))
				.GroupBy(x => x.ObjectTypeId)
				.ToDictionary(g => g.Key, g => (IReadOnlyCollection<Relationship>)g.Select(x => x.Relationship).ToList());

			foreach (var objectType in apiObjectTypes)
			{
				if (!relationshipsByObjectTypeId.TryGetValue(objectType.Id, out var relationships))
				{
					continue;
				}

				var error = new RelationshipObjectTypeInUseError
				{
					ErrorMessage = $"Relationship object type '{objectType.Name}' is in use by {relationships.Count} relationship(s).",
					Id = objectType.Id,
					RelationshipIds = relationships.Select(x => x.Id).ToList(),
				};

				ReportError(objectType.Id, error);
			}
		}

		private ICollection<DomChangeResults> GetObjectTypesWithChanges(ICollection<RelationshipObjectType> apiObjectTypes)
		{
			return GetItemsWithChanges<RelationshipObjectType, DomObjectType>(
				apiObjectTypes,
				o => o.OriginalInstance,
				o => o.GetInstanceWithChanges(),
				ids => planApi.DomHelpers.SlcRelationshipsHelper.GetObjectTypes(ids),
				o => new RelationshipObjectTypeNotFoundError { ErrorMessage = $"Relationship object type with ID '{o.Id}' no longer exists.", Id = o.Id },
				(o, msg) => new RelationshipObjectTypeValueAlreadyChangedError { ErrorMessage = msg, Id = o.Id })
				.ToList();
		}

		// Used to serialize the name validation of relationship object types, which cannot be done with the locks of
		// the individual object types.
		private sealed class ObjectTypeNamesSentinel : ApiObject
		{
			private static readonly Guid SentinelId = new Guid("6d7fbd66-9c2b-4d0f-9ec6-2f3a0a5f0a6d");

			internal ObjectTypeNamesSentinel() : base(SentinelId)
			{
			}
		}
	}
}
