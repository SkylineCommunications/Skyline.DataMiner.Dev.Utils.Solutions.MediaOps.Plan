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

	using DomPropertySettingCollection = Storage.DOM.SlcProperties.PropertyValuesInstance;

	internal class DomPropertySettingCollectionHandler : DomInstanceApiObjectValidator<DomPropertySettingCollection>
	{
		private readonly MediaOpsPlanApi planApi;
		private PropertyLookup propertyLookup;

		private DomPropertySettingCollectionHandler(MediaOpsPlanApi planApi)
		{
			this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
		}

		internal static bool TryCreateOrUpdate(MediaOpsPlanApi planApi, ICollection<PropertySettingCollection> apiSettingCollections, out DomInstanceBulkOperationResult<DomPropertySettingCollection> result)
		{
			var handler = new DomPropertySettingCollectionHandler(planApi);
			handler.CreateOrUpdate(apiSettingCollections);

			result = new DomInstanceBulkOperationResult<DomPropertySettingCollection>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		internal static bool TryDelete(MediaOpsPlanApi planApi, ICollection<PropertySettingCollection> apiSettingCollections, out DomInstanceBulkOperationResult<DomPropertySettingCollection> result)
		{
			var handler = new DomPropertySettingCollectionHandler(planApi);
			handler.Delete(apiSettingCollections);

			result = new DomInstanceBulkOperationResult<DomPropertySettingCollection>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		private void CreateOrUpdate(ICollection<PropertySettingCollection> apiSettingCollections)
		{
			if (apiSettingCollections == null)
			{
				throw new ArgumentNullException(nameof(apiSettingCollections));
			}

			if (apiSettingCollections.Count == 0)
			{
				return;
			}

			var toCreate = apiSettingCollections.Where(x => x.IsNew).ToList();

			ValidateIdsNotInUse(toCreate);
			ValidateLinkedObjectIds(toCreate);
			ValidateScopes(toCreate);
			ValidateLinkedObjectIdAndSubIdAreUnique(toCreate.Where(IsValid).ToList());

			propertyLookup = new PropertyLookup(planApi, apiSettingCollections);
			ValidateCustomProperties(apiSettingCollections);
			ValidatePropertyDefinitionsAndValues(apiSettingCollections);

			var lockResult = planApi.LockManager.LockAndExecute(apiSettingCollections.Where(IsValid).ToList(), CreateOrUpdateLocked);
			ReportError(lockResult);
		}

		private void CreateOrUpdateLocked(ICollection<PropertySettingCollection> apiSettingCollections)
		{
			if (apiSettingCollections == null)
			{
				throw new ArgumentNullException(nameof(apiSettingCollections));
			}

			if (apiSettingCollections.Count == 0)
			{
				return;
			}

			if (apiSettingCollections.Any(x => !IsValid(x)))
			{
				throw new ArgumentException($"Not all provided property value collections are valid", nameof(apiSettingCollections));
			}

			var toCreate = apiSettingCollections.Where(x => x.IsNew).ToList();
			var toUpdate = apiSettingCollections.Except(toCreate).ToList();

			var changeResults = GetPropertySettingCollectionsWithChanges(toUpdate);

			var toCreateDomInstances = toCreate
				.Where(IsValid)
				.Select(x => x.GetInstanceWithChanges())
				.ToList();
			var toUpdateDomInstances = changeResults
				.Where(IsValid)
				.Select(x => new DomPropertySettingCollection(x.Instance))
				.ToList();
			var domInstancesToPersist = toCreateDomInstances.Concat(toUpdateDomInstances).ToList();

			// The file content is attached to the DOM instance, so it can only be stored once that instance exists. The
			// names of the files that still have to be uploaded are therefore kept out of this first write.
			DeferPendingFileNames(apiSettingCollections, domInstancesToPersist);

			var persistedInstances = CreateOrUpdateDomPropertySettingCollections(domInstancesToPersist);
			var storedInstances = SyncAttachments(apiSettingCollections.Where(IsValid).ToList(), persistedInstances);

			// Reported last, because a collection whose files could not be stored must not be reported as successful.
			ReportSuccess(storedInstances.Where(x => !UnsuccessfulItems.Contains(x.ID.Id)));
		}

		// A file name may only be stored once its content is uploaded, so a name that is still pending keeps the value that
		// is stored today. That value only refers to content that is available on the collection.
		private static void DeferPendingFileNames(ICollection<PropertySettingCollection> apiSettingCollections, ICollection<DomPropertySettingCollection> domInstances)
		{
			var domInstancesById = domInstances.ToDictionary(x => x.ID.Id);

			foreach (var settingCollection in apiSettingCollections)
			{
				if (!domInstancesById.TryGetValue(settingCollection.Id, out var domInstance))
				{
					continue;
				}

				foreach (var setting in settingCollection.FileSettings.Where(x => x.FilesToUpload.Count > 0))
				{
					SetFileNames(domInstance, setting, setting.Files.Where(setting.IsStored));
				}
			}
		}

		private static void SetFileNames(DomPropertySettingCollection domInstance, FilePropertySetting setting, IEnumerable<string> fileNames)
		{
			var section = domInstance.PropertyValue.FirstOrDefault(x => x.PropertyID == setting.Id);
			if (section == null)
			{
				return;
			}

			section.Value = string.Join(FilePropertySetting.FileSeparator.ToString(), fileNames);
		}

		private ICollection<DomPropertySettingCollection> SyncAttachments(ICollection<PropertySettingCollection> apiSettingCollections, ICollection<DomPropertySettingCollection> persistedInstances)
		{
			var persistedInstancesById = persistedInstances.ToDictionary(x => x.ID.Id);
			var uploadedSettingsPerCollection = new Dictionary<Guid, ICollection<FilePropertySetting>>();

			foreach (var settingCollection in apiSettingCollections)
			{
				if (!persistedInstancesById.TryGetValue(settingCollection.Id, out var persistedInstance))
				{
					// The collection itself is not stored, so there is nothing to attach content to.
					continue;
				}

				var uploadedSettings = UploadPendingFiles(settingCollection);
				if (uploadedSettings.Count == 0)
				{
					continue;
				}

				foreach (var setting in uploadedSettings)
				{
					SetFileNames(persistedInstance, setting, setting.Files);
				}

				uploadedSettingsPerCollection[settingCollection.Id] = uploadedSettings;
			}

			// The names of the uploaded files are only stored now, so a stored name always refers to content that exists.
			var committedInstances = CreateOrUpdateDomPropertySettingCollections(uploadedSettingsPerCollection.Keys.Select(x => persistedInstancesById[x]).ToList())
				.ToDictionary(x => x.ID.Id);

			foreach (var settingCollection in apiSettingCollections.Where(x => persistedInstancesById.ContainsKey(x.Id)))
			{
				if (committedInstances.ContainsKey(settingCollection.Id) && uploadedSettingsPerCollection.TryGetValue(settingCollection.Id, out var uploadedSettings))
				{
					foreach (var setting in uploadedSettings)
					{
						setting.ClearPendingFileChanges();
					}
				}

				// A setting without pending uploads is stored as it is, so its removals are final as well.
				foreach (var setting in settingCollection.FileSettings.Where(x => x.FilesToUpload.Count == 0))
				{
					setting.ClearPendingFileChanges();
				}

				DeleteOrphanedAttachments(settingCollection);

				settingCollection.ClearRemovedFileSettings();
			}

			return persistedInstances.Select(x => committedInstances.TryGetValue(x.ID.Id, out var committed) ? committed : x).ToList();
		}

		private ICollection<FilePropertySetting> UploadPendingFiles(PropertySettingCollection settingCollection)
		{
			var instanceId = new DomInstanceId(settingCollection.Id);
			var uploadedSettings = new List<FilePropertySetting>();

			foreach (var setting in settingCollection.FileSettings)
			{
				// The collection exists from here on, so the content of its files can be read on demand.
				setting.SetStorageContext(planApi, settingCollection.Id);

				if (setting.FilesToUpload.Count == 0)
				{
					continue;
				}

				try
				{
					foreach (var fileToUpload in setting.FilesToUpload)
					{
						planApi.PropertyAttachments.Add(instanceId, FilePropertySetting.GetAttachmentName(setting.Id, fileToUpload.Key), fileToUpload.Value);
					}

					uploadedSettings.Add(setting);
				}
				catch (Exception ex)
				{
					ReportError(settingCollection.Id, new PropertySettingCollectionInvalidPropertySettingsError
					{
						ErrorMessage = $"The files of the property could not be stored: {ex.Message}",
						PropertyId = setting.Id,
						Id = settingCollection.Id,
					});
				}
			}

			return uploadedSettings;
		}

		private void DeleteOrphanedAttachments(PropertySettingCollection settingCollection)
		{
			// A collection that never held a file has no attachments, so the attachment API is not contacted for it.
			if (settingCollection.FileSettings.Count == 0 && settingCollection.RemovedFileSettings.Count == 0)
			{
				return;
			}

			var instanceId = new DomInstanceId(settingCollection.Id);

			// Only the attachments that nothing refers to anymore are removed. Reconciling against the full stored
			// attachment list catches orphans from removed file settings, settings that were never loaded and content
			// that was uploaded for a file name that could not be stored.
			var expectedAttachments = new HashSet<string>(
				settingCollection.FileSettings.SelectMany(x => x.Files.Where(x.IsStored).Select(f => FilePropertySetting.GetAttachmentName(x.Id, f))),
				StringComparer.OrdinalIgnoreCase);

			foreach (var attachmentName in planApi.PropertyAttachments.GetNames(instanceId).Where(x => !expectedAttachments.Contains(x)).ToList())
			{
				try
				{
					planApi.PropertyAttachments.Delete(instanceId, attachmentName);
				}
				catch (Exception ex)
				{
					planApi.Logger.Error(this, $"Failed to delete orphaned attachment '{attachmentName}': {ex}");
				}
			}
		}

		private ICollection<DomPropertySettingCollection> CreateOrUpdateDomPropertySettingCollections(ICollection<DomPropertySettingCollection> domValueCollections)
		{
			if (domValueCollections == null)
			{
				throw new ArgumentNullException(nameof(domValueCollections));
			}

			if (domValueCollections.Count == 0)
			{
				return new List<DomPropertySettingCollection>();
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

			return domResult.SuccessfulItems.Select(x => new DomPropertySettingCollection(x)).ToList();
		}

		private void Delete(ICollection<PropertySettingCollection> apiSettingCollections)
		{
			if (apiSettingCollections == null)
			{
				throw new ArgumentNullException(nameof(apiSettingCollections));
			}

			if (apiSettingCollections.Count == 0)
			{
				return;
			}

			ValidateStateForDeleteAction(apiSettingCollections);

			var lockResult = planApi.LockManager.LockAndExecute(apiSettingCollections.Where(IsValid).ToList(), DeleteLocked);
			ReportError(lockResult);
		}

		private void DeleteLocked(ICollection<PropertySettingCollection> apiSettingCollections)
		{
			if (apiSettingCollections == null)
			{
				throw new ArgumentNullException(nameof(apiSettingCollections));
			}

			if (apiSettingCollections.Count == 0)
			{
				return;
			}

			if (apiSettingCollections.Any(x => !IsValid(x)))
			{
				throw new ArgumentException($"Not all provided property value collections are valid", nameof(apiSettingCollections));
			}

			// The server removes the attachments together with the instance, so they need no separate cleanup here.
			var toDelete = apiSettingCollections.Select(x => x.OriginalInstance.ToInstance());
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

			ReportSuccess(toDelete.Where(x => domResult.SuccessfulIds.Contains(x.ID)).Select(x => new DomPropertySettingCollection(x)));
		}

		private void ValidateIdsNotInUse(ICollection<PropertySettingCollection> apiSettingCollections)
		{
			if (apiSettingCollections == null)
			{
				throw new ArgumentNullException(nameof(apiSettingCollections));
			}

			if (apiSettingCollections.Count == 0)
			{
				return;
			}

			var objectsRequiringValidation = apiSettingCollections.Where(x => x.IsNew && x.HasUserDefinedId).ToList();
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
				var error = new PropertySettingCollectionDuplicateIdError
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

				var error = new PropertySettingCollectionIdInUseError
				{
					ErrorMessage = "ID is already in use.",
					Id = foundInstance.ID.Id,
				};

				ReportError(foundInstance.ID.Id, error);
			}
		}

		private void ValidateLinkedObjectIds(ICollection<PropertySettingCollection> apiSettingCollections)
		{
			if (apiSettingCollections == null)
			{
				throw new ArgumentNullException(nameof(apiSettingCollections));
			}

			if (apiSettingCollections.Count == 0)
			{
				return;
			}

			var objectsRequiringValidation = apiSettingCollections.ToList();

			foreach (var valueCollection in objectsRequiringValidation.Where(x => !InputValidator.IsNonEmptyText(x.LinkedObjectId)).ToArray())
			{
				var error = new PropertySettingCollectionInvalidLinkedObjectIdError
				{
					ErrorMessage = "Linked object ID cannot be empty.",
					Id = valueCollection.Id,
				};

				ReportError(valueCollection.Id, error);

				objectsRequiringValidation.Remove(valueCollection);
			}

			foreach (var valueCollection in objectsRequiringValidation.Where(x => !InputValidator.HasValidTextLength(x.LinkedObjectId)).ToArray())
			{
				var error = new PropertySettingCollectionInvalidLinkedObjectIdError
				{
					ErrorMessage = $"Linked object ID exceeds maximum length of {InputValidator.DefaultMaxTextLength} characters.",
					Id = valueCollection.Id,
					LinkedObjectId = valueCollection.LinkedObjectId,
				};

				ReportError(valueCollection.Id, error);
			}
		}

		private void ValidateScopes(ICollection<PropertySettingCollection> apiSettingCollections)
		{
			if (apiSettingCollections == null)
			{
				throw new ArgumentNullException(nameof(apiSettingCollections));
			}

			if (apiSettingCollections.Count == 0)
			{
				return;
			}

			var objectsRequiringValidation = apiSettingCollections.ToList();

			foreach (var valueCollection in objectsRequiringValidation.Where(x => !InputValidator.IsNonEmptyText(x.Scope)).ToArray())
			{
				var error = new PropertySettingCollectionInvalidScopeError
				{
					ErrorMessage = "Scope cannot be empty.",
					Id = valueCollection.Id,
				};

				ReportError(valueCollection.Id, error);

				objectsRequiringValidation.Remove(valueCollection);
			}

			foreach (var valueCollection in objectsRequiringValidation.Where(x => !InputValidator.HasValidTextLength(x.Scope)).ToArray())
			{
				var error = new PropertySettingCollectionInvalidScopeError
				{
					ErrorMessage = $"Scope exceeds maximum length of {InputValidator.DefaultMaxTextLength} characters.",
					Id = valueCollection.Id,
					Scope = valueCollection.Scope,
				};

				ReportError(valueCollection.Id, error);
			}
		}

		private void ValidateLinkedObjectIdAndSubIdAreUnique(ICollection<PropertySettingCollection> apiSettingCollections)
		{
			if (apiSettingCollections == null)
			{
				throw new ArgumentNullException(nameof(apiSettingCollections));
			}

			if (apiSettingCollections.Count == 0)
			{
				return;
			}

			var objectsRequiringValidation = apiSettingCollections.ToList();

			var duplicateGroups = objectsRequiringValidation
				.GroupBy(x => (x.LinkedObjectId, x.SubId))
				.Where(g => g.Count() > 1)
				.ToList();

			foreach (var group in duplicateGroups)
			{
				foreach (var valueCollection in group.ToArray())
				{
					var error = new PropertySettingCollectionDuplicateLinkedObjectIdAndSubIdError
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

				var error = new PropertySettingCollectionDuplicateLinkedObjectIdAndSubIdError
				{
					ErrorMessage = $"A property value collection with LinkedObjectId '{valueCollection.LinkedObjectId}' and SubId '{valueCollection.SubId}' already exists.",
					Id = valueCollection.Id,
					LinkedObjectId = valueCollection.LinkedObjectId,
					SubId = valueCollection.SubId,
				};

				ReportError(valueCollection.Id, error);
			}
		}

		private void ValidateCustomProperties(ICollection<PropertySettingCollection> apiSettingCollections)
		{
			if (apiSettingCollections == null)
			{
				throw new ArgumentNullException(nameof(apiSettingCollections));
			}

			if (apiSettingCollections.Count == 0)
			{
				return;
			}

			foreach (var valueCollection in apiSettingCollections)
			{
				if (valueCollection.CustomSettings.Any(x => !InputValidator.IsNonEmptyText(x.Name)))
				{
					var error = new PropertySettingCollectionInvalidCustomSettingsError
					{
						ErrorMessage = "Collection contains empty names.",
						Id = valueCollection.Id,
					};

					ReportError(valueCollection.Id, error);
					continue;
				}

				foreach (var customValue in valueCollection.CustomSettings.Where(x => !InputValidator.HasValidTextLength(x.Name)).ToArray())
				{
					var error = new PropertySettingCollectionInvalidCustomSettingsError
					{
						ErrorMessage = $"Name '{customValue.Name}' exceeds maximum length of {InputValidator.DefaultMaxTextLength} characters.",
						Id = valueCollection.Id,
						Name = customValue.Name,
					};

					ReportError(valueCollection.Id, error);
				}

				var duplicates = valueCollection.CustomSettings
					.GroupBy(x => x.Name)
					.Where(g => g.Count() > 1)
					.ToDictionary(x => x.Key, x => x.Count());
				foreach (var kvp in duplicates)
				{
					var error = new PropertySettingCollectionInvalidCustomSettingsError
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

				var requiringValidation = valueCollection.CustomSettings.ToList();

				var collectionPropertyNames = propertyLookup.PropertiesByScope.TryGetValue(valueCollection.Scope, out var propertiesForScope)
					? propertiesForScope.Select(p => p.Name).ToHashSet()
					: new HashSet<string>();
				foreach (var customValue in requiringValidation.Where(x => collectionPropertyNames.Contains(x.Name)).ToArray())
				{
					var error = new PropertySettingCollectionInvalidCustomSettingsError
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
					var error = new PropertySettingCollectionInvalidCustomSettingsError
					{
						ErrorMessage = $"Value for name '{customValue.Name}' exceeds maximum length of {InputValidator.DefaultMaxTextLength} characters.",
						Id = valueCollection.Id,
						Name = customValue.Name,
					};
					ReportError(valueCollection.Id, error);
				}
			}	
		}

		private void ValidatePropertyDefinitionsAndValues(ICollection<PropertySettingCollection> apiSettingCollections)
		{
			if (apiSettingCollections == null)
			{
				throw new ArgumentNullException(nameof(apiSettingCollections));
			}

			if (apiSettingCollections.Count == 0)
			{
				return;
			}

			foreach (var valueCollection in apiSettingCollections)
			{
				var duplicates = valueCollection.PropertySettings
					.GroupBy(x => x.Id)
					.Where(g => g.Count() > 1)
					.ToDictionary(x => x.Key, x => x.Count());

				foreach (var kvp in duplicates)
				{
					var error = new PropertySettingCollectionInvalidPropertySettingsError
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

				foreach (var propertySetting in valueCollection.PropertySettings)
				{
					if (propertySetting.Id == Guid.Empty)
					{
						var error = new PropertySettingCollectionInvalidPropertySettingsError
						{
							ErrorMessage = "Property ID cannot be empty.",
							Id = valueCollection.Id,
						};

						ReportError(valueCollection.Id, error);
						continue;
					}

					if (!propertyLookup.PropertyById.TryGetValue(propertySetting.Id, out var property))
					{
						var error = new PropertySettingCollectionInvalidPropertySettingsError
						{
							Id = valueCollection.Id,
							PropertyId = propertySetting.Id,
							ErrorMessage = $"Property with ID '{propertySetting.Id}' not found.",
						};

						ReportError(valueCollection.Id, error);
						continue;
					}

					if (property.Scope != valueCollection.Scope)
					{
						var error = new PropertySettingCollectionInvalidPropertySettingsError
						{
							Id = valueCollection.Id,
							PropertyId = propertySetting.Id,
							ErrorMessage = $"Property scope '{property.Scope}' does not match property value collection scope '{valueCollection.Scope}'.",
						};

						ReportError(valueCollection.Id, error);
						continue;
					}

					PassTraceData(PropertySettingValidator.Validate(valueCollection.Id, property, propertySetting, propertySetting.HasValue));
				}
			}
		}

		private void ValidateStateForDeleteAction(ICollection<PropertySettingCollection> apiSettingCollections)
		{
			if (apiSettingCollections == null)
			{
				throw new ArgumentNullException(nameof(apiSettingCollections));
			}

			if (apiSettingCollections.Count == 0)
			{
				return;
			}

			foreach (var valueCollection in apiSettingCollections.Where(x => x.IsNew))
			{
				var error = new PropertySettingCollectionInvalidStateError
				{
					ErrorMessage = $"A property value collection that was not saved cannot be removed.",
					Id = valueCollection.Id,
				};

				ReportError(valueCollection.Id, error);
			}
		}

		private ICollection<DomChangeResults> GetPropertySettingCollectionsWithChanges(ICollection<PropertySettingCollection> apiSettingCollections)
		{
			return GetItemsWithChanges<PropertySettingCollection, DomPropertySettingCollection>(
				apiSettingCollections,
				p => p.OriginalInstance,
				p => p.GetInstanceWithChanges(),
				ids => planApi.DomHelpers.SlcPropertiesHelper.GetPropertyValues(ids),
				p => new PropertySettingCollectionNotFoundError { ErrorMessage = $"Property setting collection with ID '{p.Id}' no longer exists.", Id = p.Id },
				(p, msg) => new PropertySettingCollectionValueAlreadyChangedError { ErrorMessage = msg, Id = p.Id })
				.ToList();
		}

		private sealed class PropertyLookup
		{
			private readonly MediaOpsPlanApi planApi;

			public PropertyLookup(MediaOpsPlanApi planApi, ICollection<PropertySettingCollection> apiSettingCollections)
			{
				this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));

				 LoadPropertiesByScope(apiSettingCollections);
				 LoadRemainingPropertiesById(apiSettingCollections);
			}

			public IReadOnlyDictionary<string, IReadOnlyCollection<Property>> PropertiesByScope { get; } = new Dictionary<string, IReadOnlyCollection<Property>>();

			public IReadOnlyDictionary<Guid, Property> PropertyById { get; } = new Dictionary<Guid, Property>();

			private void LoadPropertiesByScope(ICollection<PropertySettingCollection> apiSettingCollections)
			{
				var scopes = apiSettingCollections
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

			private void LoadRemainingPropertiesById(ICollection<PropertySettingCollection> apiSettingCollections)
			{
				var propertyIds = apiSettingCollections
					.SelectMany(x => x.PropertySettings)
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
