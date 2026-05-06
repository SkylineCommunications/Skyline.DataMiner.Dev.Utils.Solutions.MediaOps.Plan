namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Linq;

	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Live.API.Objects.Orchestration;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcProperties;
	using Skyline.DataMiner.Utils.DOM.Extensions;

	using DomPropertyValueCollection = Storage.DOM.SlcProperties.PropertyValuesInstance;

	internal class DomPropertyValueCollectionHandler : DomInstanceApiObjectValidator<DomPropertyValueCollection>
	{
		private readonly MediaOpsPlanApi planApi;

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
					ErrorMessage = $"Property value collection '{valueCollection.Name}' has a duplicate ID.",
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

			var propertyIds = apiValueCollections
				.SelectMany(x => x.PropertyValues)
				.Select(x => x.Id)
				.Distinct()
				.ToList();
			var propertiesById = planApi.Properties.Read(propertyIds).ToDictionary(x => x.Id);

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

					if (!propertiesById.TryGetValue(propertyValue.Id, out var property))
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
	}
}
