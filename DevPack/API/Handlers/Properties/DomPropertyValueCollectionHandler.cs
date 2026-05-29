namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcProperties;
	using Skyline.DataMiner.Utils.DOM.Extensions;

	using DomPropertyValueCollection = Storage.DOM.SlcProperties.PropertyValuesInstance;

	internal class DomPropertyValueCollectionHandler : DomInstanceApiObjectValidator<DomPropertyValueCollection>
	{
		private readonly MediaOpsPlanApi planApi;
		private PropertyLookup propertyLookup;

		private DomPropertyValueCollectionHandler(MediaOpsPlanApi planApi)
		{
			this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
		}

		internal static bool TryCreateOrUpdate(MediaOpsPlanApi planApi, ICollection<PropertyValueCollection> apiValueCollections, out DomInstanceBulkOperationResult<DomPropertyValueCollection> result)
		{
			var handler = new DomPropertyValueCollectionHandler(planApi);
			handler.CreateOrUpdate(apiValueCollections);

			result = new DomInstanceBulkOperationResult<DomPropertyValueCollection>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		internal static bool TryDelete(MediaOpsPlanApi planApi, ICollection<PropertyValueCollection> apiValueCollections, out DomInstanceBulkOperationResult<DomPropertyValueCollection> result)
		{
			var handler = new DomPropertyValueCollectionHandler(planApi);
			handler.Delete(apiValueCollections);

			result = new DomInstanceBulkOperationResult<DomPropertyValueCollection>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		private void CreateOrUpdate(ICollection<PropertyValueCollection> apiValueCollections)
		{
			if (apiValueCollections == null)
			{
				throw new ArgumentNullException(nameof(apiValueCollections));
			}

			if (apiValueCollections.Count == 0)
			{
				return;
			}

			var toCreate = apiValueCollections.Where(x => x.IsNew).ToList();

			ValidateIdsNotInUse(toCreate);
			ValidateLinkedObjectIds(toCreate);
			ValidateScopes(toCreate);
			ValidateLinkedObjectIdAndSubIdAreUnique(toCreate.Where(IsValid).ToList());

			propertyLookup = new PropertyLookup(planApi, apiValueCollections);
			ValidateCustomProperties(apiValueCollections);
			ValidatePropertyDefinitionsAndValues(apiValueCollections);

			var lockResult = planApi.LockManager.LockAndExecute(apiValueCollections.Where(IsValid).ToList(), CreateOrUpdateLocked);
			ReportError(lockResult);
		}

		private void CreateOrUpdateLocked(ICollection<PropertyValueCollection> apiValueCollections)
		{
			if (apiValueCollections == null)
			{
				throw new ArgumentNullException(nameof(apiValueCollections));
			}

			if (apiValueCollections.Count == 0)
			{
				return;
			}

			if (apiValueCollections.Any(x => !IsValid(x)))
			{
				throw new ArgumentException($"Not all provided property value collections are valid", nameof(apiValueCollections));
			}

			var toCreate = apiValueCollections.Where(x => x.IsNew).ToList();
			var toUpdate = apiValueCollections.Except(toCreate).ToList();

			var changeResults = GetPropertyValueCollectionsWithChanges(toUpdate);

			var toCreateDomInstances = toCreate
				.Where(IsValid)
				.Select(x => x.GetInstanceWithChanges())
				.ToList();
			var toUpdateDomInstances = changeResults
				.Where(IsValid)
				.Select(x => new DomPropertyValueCollection(x.Instance))
				.ToList();
			CreateOrUpdateDomPropertyValueCollections(toCreateDomInstances.Concat(toUpdateDomInstances).ToList());
		}

		private void CreateOrUpdateDomPropertyValueCollections(ICollection<DomPropertyValueCollection> domValueCollections)
		{
			if (domValueCollections == null)
			{
				throw new ArgumentNullException(nameof(domValueCollections));
			}

			if (domValueCollections.Count == 0)
			{
				return;
			}

			planApi.DomHelpers.SlcPropertiesHelper.DomHelper.DomInstances.TryCreateOrUpdateInBatches(domValueCollections.Select(x => x.ToInstance()), out var domResult);

			foreach (var id in domResult.UnsuccessfulIds)
			{
				ReportError(id.Id);

				if (domResult.TraceDataPerItem.TryGetValue(id, out var traceData))
				{
					var mediaOpsTraceData = new MediaOpsTraceData();
					mediaOpsTraceData.Add(new MediaOpsErrorData() { ErrorMessage = traceData.ToString() });

					PassTraceData(id.Id, mediaOpsTraceData);
				}
			}

			ReportSuccess(domResult.SuccessfulItems.Select(x => new DomPropertyValueCollection(x)));
		}

		private void Delete(ICollection<PropertyValueCollection> apiValueCollections)
		{
			if (apiValueCollections == null)
			{
				throw new ArgumentNullException(nameof(apiValueCollections));
			}

			if (apiValueCollections.Count == 0)
			{
				return;
			}

			ValidateStateForDeleteAction(apiValueCollections);

			var lockResult = planApi.LockManager.LockAndExecute(apiValueCollections.Where(IsValid).ToList(), DeleteLocked);
			ReportError(lockResult);
		}

		private void DeleteLocked(ICollection<PropertyValueCollection> apiValueCollections)
		{
			if (apiValueCollections == null)
			{
				throw new ArgumentNullException(nameof(apiValueCollections));
			}

			if (apiValueCollections.Count == 0)
			{
				return;
			}

			if (apiValueCollections.Any(x => !IsValid(x)))
			{
				throw new ArgumentException($"Not all provided property value collections are valid", nameof(apiValueCollections));
			}

			var toDelete = apiValueCollections.Select(x => x.OriginalInstance.ToInstance());
			planApi.DomHelpers.SlcPropertiesHelper.DomHelper.DomInstances.TryDeleteInBatches(toDelete, out var domResult);

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

			ReportSuccess(toDelete.Where(x => domResult.SuccessfulIds.Contains(x.ID)).Select(x => new DomPropertyValueCollection(x)));
		}

		private void ValidateIdsNotInUse(ICollection<PropertyValueCollection> apiValueCollections)
		{
			if (apiValueCollections == null)
			{
				throw new ArgumentNullException(nameof(apiValueCollections));
			}

			if (apiValueCollections.Count == 0)
			{
				return;
			}

			var objectsRequiringValidation = apiValueCollections.Where(x => x.IsNew && x.HasUserDefinedId).ToList();
			if (objectsRequiringValidation.Count == 0)
			{
				return;
			}

			var objectsWithDuplicateIds = objectsRequiringValidation
				.GroupBy(o => o.Id)
				.Where(g => g.Count() > 1)
				.SelectMany(g => g)
				.ToList();

			foreach (var valueCollection in objectsWithDuplicateIds)
			{
				var error = new PropertyValueCollectionDuplicateIdError
				{
					ErrorMessage = $"Property value collection has a duplicate ID.",
					Id = valueCollection.Id,
				};

				ReportError(valueCollection.Id, error);

				objectsRequiringValidation.Remove(valueCollection);
			}

			foreach (var foundInstance in planApi.DomHelpers.SlcPropertiesHelper.GetPropertiesInstances(objectsRequiringValidation.Select(x => x.Id)))
			{
				planApi.Logger.Information(this, $"ID is already in use by a Properties instance.", [foundInstance.ID.Id]);

				var error = new PropertyValueCollectionIdInUseError
				{
					ErrorMessage = "ID is already in use.",
					Id = foundInstance.ID.Id,
				};

				ReportError(foundInstance.ID.Id, error);
			}
		}

		private void ValidateLinkedObjectIds(ICollection<PropertyValueCollection> apiValueCollections)
		{
			if (apiValueCollections == null)
			{
				throw new ArgumentNullException(nameof(apiValueCollections));
			}

			if (apiValueCollections.Count == 0)
			{
				return;
			}

			var objectsRequiringValidation = apiValueCollections.ToList();

			foreach (var valueCollection in objectsRequiringValidation.Where(x => !InputValidator.IsNonEmptyText(x.LinkedObjectId)).ToArray())
			{
				var error = new PropertyValueCollectionInvalidLinkedObjectIdError
				{
					ErrorMessage = "Linked object ID cannot be empty.",
					Id = valueCollection.Id,
				};

				ReportError(valueCollection.Id, error);

				objectsRequiringValidation.Remove(valueCollection);
			}

			foreach (var valueCollection in objectsRequiringValidation.Where(x => !InputValidator.HasValidTextLength(x.LinkedObjectId)).ToArray())
			{
				var error = new PropertyValueCollectionInvalidLinkedObjectIdError
				{
					ErrorMessage = $"Linked object ID exceeds maximum length of {InputValidator.DefaultMaxTextLength} characters.",
					Id = valueCollection.Id,
					LinkedObjectId = valueCollection.LinkedObjectId,
				};

				ReportError(valueCollection.Id, error);
			}
		}

		private void ValidateScopes(ICollection<PropertyValueCollection> apiValueCollections)
		{
			if (apiValueCollections == null)
			{
				throw new ArgumentNullException(nameof(apiValueCollections));
			}

			if (apiValueCollections.Count == 0)
			{
				return;
			}

			var objectsRequiringValidation = apiValueCollections.ToList();

			foreach (var valueCollection in objectsRequiringValidation.Where(x => !InputValidator.IsNonEmptyText(x.Scope)).ToArray())
			{
				var error = new PropertyValueCollectionInvalidScopeError
				{
					ErrorMessage = "Scope cannot be empty.",
					Id = valueCollection.Id,
				};

				ReportError(valueCollection.Id, error);

				objectsRequiringValidation.Remove(valueCollection);
			}

			foreach (var valueCollection in objectsRequiringValidation.Where(x => !InputValidator.HasValidTextLength(x.Scope)).ToArray())
			{
				var error = new PropertyValueCollectionInvalidScopeError
				{
					ErrorMessage = $"Scope exceeds maximum length of {InputValidator.DefaultMaxTextLength} characters.",
					Id = valueCollection.Id,
					Scope = valueCollection.Scope,
				};

				ReportError(valueCollection.Id, error);
			}
		}

		private void ValidateLinkedObjectIdAndSubIdAreUnique(ICollection<PropertyValueCollection> apiValueCollections)
		{
			if (apiValueCollections == null)
			{
				throw new ArgumentNullException(nameof(apiValueCollections));
			}

			if (apiValueCollections.Count == 0)
			{
				return;
			}

			var objectsRequiringValidation = apiValueCollections.ToList();

			var duplicateGroups = objectsRequiringValidation
				.GroupBy(x => (x.LinkedObjectId, x.SubId))
				.Where(g => g.Count() > 1)
				.ToList();

			foreach (var group in duplicateGroups)
			{
				foreach (var valueCollection in group.ToArray())
				{
					var error = new PropertyValueCollectionDuplicateLinkedObjectIdAndSubIdError
					{
						ErrorMessage = $"Property value collection has a duplicate combination of LinkedObjectId '{group.Key.LinkedObjectId}' and SubId '{group.Key.SubId}'.",
						Id = valueCollection.Id,
						LinkedObjectId = group.Key.LinkedObjectId,
						SubId = group.Key.SubId,
					};

					ReportError(valueCollection.Id, error);

					objectsRequiringValidation.Remove(valueCollection);
				}
			}

			if (objectsRequiringValidation.Count == 0)
			{
				return;
			}

			var distinctLinkedObjectIds = objectsRequiringValidation
				.Select(x => x.LinkedObjectId)
				.Distinct()
				.ToList();

			var linkedObjectIdFilter = new ORFilterElement<DomInstance>(
				distinctLinkedObjectIds.Select(id => DomInstanceExposers.FieldValues.DomInstanceField(SlcPropertiesIds.Sections.PropertyValueInfo.LinkedObjectID).Equal(id)).ToArray());

			FilterElement<DomInstance> filter =
				DomInstanceExposers.DomDefinitionId.Equal(SlcPropertiesIds.Definitions.PropertyValues.Id)
				.AND(linkedObjectIdFilter);

			var existingByKey = planApi.DomHelpers.SlcPropertiesHelper.GetPropertyValues(filter)
				.GroupBy(x => (x.PropertyValueInfo.LinkedObjectID, x.PropertyValueInfo.SubID))
				.ToDictionary(g => g.Key, g => g.ToList());

			foreach (var valueCollection in objectsRequiringValidation)
			{
				if (!existingByKey.TryGetValue((valueCollection.LinkedObjectId, valueCollection.SubId), out var existingMatches))
				{
					continue;
				}

				var conflicts = existingMatches.Where(x => x.ID.Id != valueCollection.Id).ToList();
				if (conflicts.Count == 0)
				{
					continue;
				}

				planApi.Logger.Information(this, $"Combination of LinkedObjectId '{valueCollection.LinkedObjectId}' and SubId '{valueCollection.SubId}' is already in use by property value collection(s) with ID(s) {string.Join(", ", conflicts.Select(x => x.ID.Id))}.");

				var error = new PropertyValueCollectionDuplicateLinkedObjectIdAndSubIdError
				{
					ErrorMessage = $"A property value collection with LinkedObjectId '{valueCollection.LinkedObjectId}' and SubId '{valueCollection.SubId}' already exists.",
					Id = valueCollection.Id,
					LinkedObjectId = valueCollection.LinkedObjectId,
					SubId = valueCollection.SubId,
				};

				ReportError(valueCollection.Id, error);
			}
		}

		private void ValidateCustomProperties(ICollection<PropertyValueCollection> apiValueCollections)
		{
			if (apiValueCollections == null)
			{
				throw new ArgumentNullException(nameof(apiValueCollections));
			}

			if (apiValueCollections.Count == 0)
			{
				return;
			}

			foreach (var valueCollection in apiValueCollections)
			{
				if (valueCollection.CustomValues.Any(x => !InputValidator.IsNonEmptyText(x.Name)))
				{
					var error = new PropertyValueCollectionInvalidCustomSettingsError
					{
						ErrorMessage = "Collection contains empty names.",
						Id = valueCollection.Id,
					};

					ReportError(valueCollection.Id, error);
					continue;
				}

				foreach (var customValue in valueCollection.CustomValues.Where(x => !InputValidator.HasValidTextLength(x.Name)).ToArray())
				{
					var error = new PropertyValueCollectionInvalidCustomSettingsError
					{
						ErrorMessage = $"Name '{customValue.Name}' exceeds maximum length of {InputValidator.DefaultMaxTextLength} characters.",
						Id = valueCollection.Id,
						Name = customValue.Name,
					};

					ReportError(valueCollection.Id, error);
				}

				var duplicates = valueCollection.CustomValues
					.GroupBy(x => x.Name)
					.Where(g => g.Count() > 1)
					.ToDictionary(x => x.Key, x => x.Count());
				foreach (var kvp in duplicates)
				{
					var error = new PropertyValueCollectionInvalidCustomSettingsError
					{
						ErrorMessage = $"Name '{kvp.Key}' is defined {kvp.Value} times.",
						Id = valueCollection.Id,
						Name = kvp.Key,
					};

					ReportError(valueCollection.Id, error);
				}

				if (duplicates.Count > 0)
				{
					continue;
				}

				var requiringValidation = valueCollection.CustomValues.ToList();

				var collectionPropertyNames = propertyLookup.PropertiesByScope.TryGetValue(valueCollection.Scope, out var propertiesForScope)
					? propertiesForScope.Select(p => p.Name).ToHashSet()
					: new HashSet<string>();
				foreach (var customValue in requiringValidation.Where(x => collectionPropertyNames.Contains(x.Name)).ToArray())
				{
					var error = new PropertyValueCollectionInvalidCustomSettingsError
					{
						ErrorMessage = $"Name '{customValue.Name}' cannot be the same as a property name in the same scope.",
						Id = valueCollection.Id,
						Name = customValue.Name,
					};

					ReportError(valueCollection.Id, error);
					requiringValidation.Remove(customValue);
				}

				foreach (var customValue in requiringValidation.Where(x => x.HasValue && !InputValidator.HasValidTextLength(x.Value)).ToArray())
				{
					var error = new PropertyValueCollectionInvalidCustomSettingsError
					{
						ErrorMessage = $"Value for name '{customValue.Name}' exceeds maximum length of {InputValidator.DefaultMaxTextLength} characters.",
						Id = valueCollection.Id,
						Name = customValue.Name,
					};
					ReportError(valueCollection.Id, error);
				}
			}	
		}

		private void ValidatePropertyDefinitionsAndValues(ICollection<PropertyValueCollection> apiValueCollections)
		{
			if (apiValueCollections == null)
			{
				throw new ArgumentNullException(nameof(apiValueCollections));
			}

			if (apiValueCollections.Count == 0)
			{
				return;
			}

			foreach (var valueCollection in apiValueCollections)
			{
				var duplicates = valueCollection.PropertyValues
					.GroupBy(x => x.Id)
					.Where(g => g.Count() > 1)
					.ToDictionary(x => x.Key, x => x.Count());

				foreach (var kvp in duplicates)
				{
					var error = new PropertyValueCollectionInvalidPropertySettingsError
					{
						Id = valueCollection.Id,
						PropertyId = kvp.Key,
						ErrorMessage = $"Property value collection contains {kvp.Value} values with the same property ID '{kvp.Key}'.",
					};
					ReportError(valueCollection.Id, error);
				}

				if (duplicates.Count > 0)
				{
					continue;
				}

				foreach (var propertyValue in valueCollection.PropertyValues)
				{
					if (propertyValue.Id == Guid.Empty)
					{
						var error = new PropertyValueCollectionInvalidPropertySettingsError
						{
							ErrorMessage = "Property ID cannot be empty.",
							Id = valueCollection.Id,
						};

						ReportError(valueCollection.Id, error);
						continue;
					}

					if (!propertyLookup.PropertyById.TryGetValue(propertyValue.Id, out var property))
					{
						var error = new PropertyValueCollectionInvalidPropertySettingsError
						{
							Id = valueCollection.Id,
							PropertyId = propertyValue.Id,
							ErrorMessage = $"Property with ID '{propertyValue.Id}' not found.",
						};

						ReportError(valueCollection.Id, error);
						continue;
					}

					if (property.Scope != valueCollection.Scope)
					{
						var error = new PropertyValueCollectionInvalidPropertySettingsError
						{
							Id = valueCollection.Id,
							PropertyId = propertyValue.Id,
							ErrorMessage = $"Property scope '{property.Scope}' does not match property value collection scope '{valueCollection.Scope}'.",
						};

						ReportError(valueCollection.Id, error);
						continue;
					}

					PassTraceData(PropertySettingValidator.Validate(valueCollection.Id, property, propertyValue, propertyValue.HasValue));
				}
			}
		}

		private void ValidateStateForDeleteAction(ICollection<PropertyValueCollection> apiValueCollections)
		{
			if (apiValueCollections == null)
			{
				throw new ArgumentNullException(nameof(apiValueCollections));
			}

			if (apiValueCollections.Count == 0)
			{
				return;
			}

			foreach (var valueCollection in apiValueCollections.Where(x => x.IsNew))
			{
				var error = new PropertyValueCollectionInvalidStateError
				{
					ErrorMessage = $"A property value collection that was not saved cannot be removed.",
					Id = valueCollection.Id,
				};

				ReportError(valueCollection.Id, error);
			}
		}

		private ICollection<DomChangeResults> GetPropertyValueCollectionsWithChanges(ICollection<PropertyValueCollection> apiValueCollections)
		{
			return GetItemsWithChanges<PropertyValueCollection, DomPropertyValueCollection>(
				apiValueCollections,
				p => p.OriginalInstance,
				p => p.GetInstanceWithChanges(),
				ids => planApi.DomHelpers.SlcPropertiesHelper.GetPropertyValues(ids),
				p => new PropertyValueCollectionNotFoundError { ErrorMessage = $"Property value collection with ID '{p.Id}' no longer exists.", Id = p.Id },
				(p, msg) => new PropertyValueCollectionValueAlreadyChangedError { ErrorMessage = msg, Id = p.Id })
				.ToList();
		}

		private sealed class PropertyLookup
		{
			private readonly MediaOpsPlanApi planApi;

			public PropertyLookup(MediaOpsPlanApi planApi, ICollection<PropertyValueCollection> apiValueCollections)
			{
				this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));

				 LoadPropertiesByScope(apiValueCollections);
				 LoadRemainingPropertiesById(apiValueCollections);
			}

			public IReadOnlyDictionary<string, IReadOnlyCollection<Property>> PropertiesByScope { get; } = new Dictionary<string, IReadOnlyCollection<Property>>();

			public IReadOnlyDictionary<Guid, Property> PropertyById { get; } = new Dictionary<Guid, Property>();

			private void LoadPropertiesByScope(ICollection<PropertyValueCollection> apiValueCollections)
			{
				var scopes = apiValueCollections
					.Select(x => x.Scope)
					.Distinct()
					.ToList();
				var filter = new ORFilterElement<Property>(scopes.Select(x => PropertyExposers.Scope.Equal(x)).ToArray());
				foreach (var property in planApi.Properties.Read(filter))
				{
					if (!PropertiesByScope.TryGetValue(property.Scope, out var propertiesForScope))
					{
						propertiesForScope = new List<Property>();
						((Dictionary<string, IReadOnlyCollection<Property>>)PropertiesByScope).Add(property.Scope, propertiesForScope);
					}

					((List<Property>)propertiesForScope).Add(property);

					((Dictionary<Guid, Property>)PropertyById).Add(property.Id, property);
				}
			}

			private void LoadRemainingPropertiesById(ICollection<PropertyValueCollection> apiValueCollections)
			{
				var propertyIds = apiValueCollections
					.SelectMany(x => x.PropertyValues)
					.Select(x => x.Id)
					.Distinct()
					.Where(id => !PropertyById.ContainsKey(id))
					.ToList();
				if (propertyIds.Count == 0)
				{
					return;
				}

				foreach (var property in planApi.Properties.Read(propertyIds))
				{
					((Dictionary<Guid, Property>)PropertyById).Add(property.Id, property);

					if (string.IsNullOrEmpty(property.Scope))
					{
						continue;
					}

					if (!PropertiesByScope.TryGetValue(property.Scope, out var propertiesForScope))
					{
						propertiesForScope = new List<Property>();
						((Dictionary<string, IReadOnlyCollection<Property>>)PropertiesByScope).Add(property.Scope, propertiesForScope);
					}

					((List<Property>)propertiesForScope).Add(property);
				}
			}
		}
	}
}
