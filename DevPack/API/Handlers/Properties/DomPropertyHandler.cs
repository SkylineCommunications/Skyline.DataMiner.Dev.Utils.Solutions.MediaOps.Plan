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

	using DomProperty = Storage.DOM.SlcProperties.PropertyInstance;

	internal class DomPropertyHandler : DomInstanceApiObjectValidator<DomProperty>
	{
		private readonly MediaOpsPlanApi planApi;

		private DomPropertyHandler(MediaOpsPlanApi planApi)
		{
			this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
		}

		internal static bool TryCreateOrUpdate(MediaOpsPlanApi planApi, ICollection<Property> apiProperties, out DomInstanceBulkOperationResult<DomProperty> result)
		{
			var handler = new DomPropertyHandler(planApi);
			handler.CreateOrUpdate(apiProperties);

			result = new DomInstanceBulkOperationResult<DomProperty>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		internal static bool TryDelete(MediaOpsPlanApi planApi, ICollection<Property> apiProperties, out DomInstanceBulkOperationResult<DomProperty> result, PropertyDeleteOptions options = null)
		{
			var handler = new DomPropertyHandler(planApi);
			handler.Delete(apiProperties, options ?? PropertyDeleteOptions.GetDefaults());

			result = new DomInstanceBulkOperationResult<DomProperty>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		private void CreateOrUpdate(ICollection<Property> apiProperties)
		{
			if (apiProperties == null)
			{
				throw new ArgumentNullException(nameof(apiProperties));
			}

			if (apiProperties.Count == 0)
			{
				return;
			}

			var toCreate = apiProperties.Where(x => x.IsNew).ToList();

			ValidateIdsNotInUse(toCreate);
			ValidateNames(apiProperties);
			ValidateScopes(apiProperties);
			ValidateSectionNames(apiProperties);

			ValidateStringProperties(apiProperties.OfType<StringProperty>().ToList());
			ValidateDiscreteProperties(apiProperties.OfType<DiscreteProperty>().ToList());
			ValidateFileProperties(apiProperties.OfType<FileProperty>().ToList());

			var lockResult = planApi.LockManager.LockAndExecute(apiProperties.Where(IsValid).ToList(), CreateOrUpdateLocked);
			ReportError(lockResult);
		}

		private void CreateOrUpdateLocked(ICollection<Property> apiProperties)
		{
			if (apiProperties == null)
			{
				throw new ArgumentNullException(nameof(apiProperties));
			}

			if (apiProperties.Count == 0)
			{
				return;
			}

			if (apiProperties.Any(x => !IsValid(x)))
			{
				throw new ArgumentException($"Not all provided properties are valid", nameof(apiProperties));
			}

			var toCreate = apiProperties.Where(x => x.IsNew).ToList();
			var toUpdate = apiProperties.Except(toCreate).ToList();

			var changeResults = GetPropertiesWithChanges(toUpdate);

			var toUpdateNameValidation = toUpdate.Where(x => changeResults.Any(y => y.Instance.ID.Id == x.Id && y.ChangedFields.Select(z => z.FieldDescriptorId).Contains(SlcPropertiesIds.Sections.PropertyInfo.Name.Id)));
			ValidateDomNames(toCreate.Concat(toUpdateNameValidation).ToList());

			var toCreateDomInstances = toCreate
				.Where(IsValid)
				.Select(x => x.GetInstanceWithChanges())
				.ToList();
			var toUpdateDomInstances = changeResults
				.Where(IsValid)
				.Select(x => new DomProperty(x.Instance))
				.ToList();
			CreateOrUpdateDomProperties(toCreateDomInstances.Concat(toUpdateDomInstances).ToList());
		}

		private void CreateOrUpdateDomProperties(ICollection<DomProperty> domProperties)
		{
			if (domProperties == null)
			{
				throw new ArgumentNullException(nameof(domProperties));
			}

			if (domProperties.Count == 0)
			{
				return;
			}

			planApi.DomHelpers.SlcPropertiesHelper.DomHelper.DomInstances.TryCreateOrUpdateInBatches(domProperties.Select(x => x.ToInstance()), out var domResult);

			foreach (var id in domResult.UnsuccessfulIds)
			{
				ReportError(id.Id);

				if (domResult.TraceDataPerItem.TryGetValue(id, out var traceData))
				{
					var mediaOpsTraceData = new MediaOpsTraceData();
					mediaOpsTraceData.Add(new MediaOpsErrorData() { ErrorMessage = traceData.ToString() });
				}
			}

			ReportSuccess(domResult.SuccessfulItems.Select(x => new DomProperty(x)));
		}

		private void Delete(ICollection<Property> apiProperties, PropertyDeleteOptions options)
		{
			if (apiProperties == null)
			{
				throw new ArgumentNullException(nameof(apiProperties));
			}

			if (apiProperties.Count == 0)
			{
				return;
			}

			ValidateStateForDeleteAction(apiProperties);
			ValidatePropertiesAreNotInUse(apiProperties, options);

			var lockResult = planApi.LockManager.LockAndExecute(apiProperties.Where(IsValid).ToList(), DeleteLocked);
			ReportError(lockResult);
		}

		private void DeleteLocked(ICollection<Property> apiProperties)
		{
			if (apiProperties == null)
			{
				throw new ArgumentNullException(nameof(apiProperties));
			}

			if (apiProperties.Count == 0)
			{
				return;
			}

			if (apiProperties.Any(x => !IsValid(x)))
			{
				throw new ArgumentException($"Not all provided properties are valid", nameof(apiProperties));
			}

			var instancesToDelete = apiProperties.Select(x => x.OriginalInstance.ToInstance());
			planApi.DomHelpers.SlcPropertiesHelper.DomHelper.DomInstances.TryDeleteInBatches(instancesToDelete, out var domResult);

			foreach (var id in domResult.UnsuccessfulIds)
			{
				ReportError(id.Id);

				if (domResult.TraceDataPerItem.TryGetValue(id, out var traceData))
				{
					var mediaOpsTraceData = new MediaOpsTraceData();
					mediaOpsTraceData.Add(new MediaOpsErrorData { ErrorMessage = traceData.ToString() });
				}
			}

			ReportSuccess(instancesToDelete.Where(x => domResult.SuccessfulIds.Contains(x.ID)).Select(x => new DomProperty(x)));
		}

		private void ValidateIdsNotInUse(ICollection<Property> apiProperties)
		{
			if (apiProperties == null)
			{
				throw new ArgumentNullException(nameof(apiProperties));
			}

			if (apiProperties.Count == 0)
			{
				return;
			}

			var objectsRequiringValidation = apiProperties.Where(x => x.IsNew && x.HasUserDefinedId).ToList();
			if (objectsRequiringValidation.Count == 0)
			{
				return;
			}

			var objectsWithDuplicateIds = objectsRequiringValidation
				.GroupBy(o => o.Id)
				.Where(g => g.Count() > 1)
				.SelectMany(g => g)
				.ToList();

			foreach (var property in objectsWithDuplicateIds)
			{
				var error = new PropertyDuplicateIdError
				{
					ErrorMessage = $"Property '{property.Name}' has a duplicate ID.",
					Id = property.Id,
				};

				ReportError(property.Id, error);

				objectsRequiringValidation.Remove(property);
			}

			foreach (var foundInstance in planApi.DomHelpers.SlcPropertiesHelper.GetPropertiesInstances(objectsRequiringValidation.Select(x => x.Id)))
			{
				planApi.Logger.Information(this, $"ID is already in use by a Properties instance.", [foundInstance.ID.Id]);

				var error = new PropertyIdInUseError
				{
					ErrorMessage = "ID is already in use.",
					Id = foundInstance.ID.Id,
				};

				ReportError(foundInstance.ID.Id, error);
			}
		}

		private void ValidateNames(ICollection<Property> apiProperties)
		{
			if (apiProperties == null)
			{
				throw new ArgumentNullException(nameof(apiProperties));
			}

			if (apiProperties.Count == 0)
			{
				return;
			}

			var objectsRequiringValidation = apiProperties.ToList();

			foreach (var property in objectsRequiringValidation.Where(x => !InputValidator.IsNonEmptyText(x.Name)).ToArray())
			{
				var error = new PropertyInvalidNameError
				{
					ErrorMessage = "Name cannot be empty.",
					Id = property.Id,
				};

				ReportError(property.Id, error);

				objectsRequiringValidation.Remove(property);
			}

			foreach (var property in objectsRequiringValidation.Where(x => !InputValidator.HasValidTextLength(x.Name)).ToArray())
			{
				var error = new PropertyInvalidNameError
				{
					ErrorMessage = $"Name exceeds maximum length of {InputValidator.DefaultMaxTextLength} characters.",
					Id = property.Id,
					Name = property.Name,
				};

				ReportError(property.Id, error);

				objectsRequiringValidation.Remove(property);
			}

			var objectsWithDuplicateNames = objectsRequiringValidation
				.GroupBy(property => property.Name)
				.Where(g => g.Count() > 1)
				.SelectMany(x => x)
				.ToList();

			foreach (var property in objectsWithDuplicateNames)
			{
				var error = new PropertyDuplicateNameError
				{
					ErrorMessage = $"Property '{property.Name}' has a duplicate name.",
					Id = property.Id,
					Name = property.Name,
				};

				ReportError(property.Id, error);
			}
		}

		private void ValidateDomNames(ICollection<Property> apiProperties)
		{
			if (apiProperties == null)
			{
				throw new ArgumentNullException(nameof(apiProperties));
			}

			if (apiProperties.Count == 0)
			{
				return;
			}

			FilterElement<DomInstance> Filter(string name) =>
				DomInstanceExposers.DomDefinitionId.Equal(SlcPropertiesIds.Definitions.Property.Id)
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcPropertiesIds.Sections.PropertyInfo.Name).Equal(name));

			var domPropertiesByName = planApi.DomHelpers.SlcPropertiesHelper.GetProperties(apiProperties.Select(x => x.Name), Filter)
				.GroupBy(x => x.PropertyInfo.Name)
				.ToDictionary(x => x.Key, x => (IReadOnlyCollection<DomProperty>)x.ToList());

			foreach (var property in apiProperties)
			{
				if (!domPropertiesByName.TryGetValue(property.Name, out var domProperties))
				{
					continue;
				}

				var existing = domProperties.Where(x => x.ID.Id != property.Id).ToList();
				if (existing.Count == 0)
				{
					continue;
				}

				planApi.Logger.Information(this, $"Name '{property.Name}' is already in use by DOM property/properties with ID(s)", [existing.Select(x => x.ID.Id).ToArray()]);

				var error = new PropertyNameExistsError
				{
					ErrorMessage = "Name is already in use.",
					Id = property.Id,
					Name = property.Name,
				};

				ReportError(property.Id, error);
			}
		}

		private void ValidateSectionNames(ICollection<Property> apiProperties)
		{
			if (apiProperties == null)
			{
				throw new ArgumentNullException(nameof(apiProperties));
			}

			if (apiProperties.Count == 0)
			{
				return;
			}

			var objectsRequiringValidation = apiProperties.ToList();

			foreach (var property in objectsRequiringValidation.Where(x => !InputValidator.IsNonEmptyText(x.SectionName)).ToArray())
			{
				var error = new PropertyInvalidSectionNameError
				{
					ErrorMessage = "Section name cannot be empty.",
					Id = property.Id,
				};

				ReportError(property.Id, error);

				objectsRequiringValidation.Remove(property);
			}

			foreach (var property in objectsRequiringValidation.Where(x => !InputValidator.HasValidTextLength(x.SectionName)).ToArray())
			{
				var error = new PropertyInvalidSectionNameError
				{
					ErrorMessage = $"Section name exceeds maximum length of {InputValidator.DefaultMaxTextLength} characters.",
					Id = property.Id,
					Name = property.SectionName,
				};

				ReportError(property.Id, error);

				objectsRequiringValidation.Remove(property);
			}
		}

		private void ValidateScopes(ICollection<Property> apiProperties)
		{
			if (apiProperties == null)
			{
				throw new ArgumentNullException(nameof(apiProperties));
			}

			if (apiProperties.Count == 0)
			{
				return;
			}

			var objectsRequiringValidation = apiProperties.ToList();

			foreach (var property in objectsRequiringValidation.Where(x => !InputValidator.IsNonEmptyText(x.Scope)).ToArray())
			{
				var error = new PropertyInvalidScopeError
				{
					ErrorMessage = "Scope cannot be empty.",
					Id = property.Id,
				};

				ReportError(property.Id, error);

				objectsRequiringValidation.Remove(property);
			}

			foreach (var property in objectsRequiringValidation.Where(x => !InputValidator.HasValidTextLength(x.Scope)).ToArray())
			{
				var error = new PropertyInvalidScopeError
				{
					ErrorMessage = $"Scope exceeds maximum length of {InputValidator.DefaultMaxTextLength} characters.",
					Id = property.Id,
					Scope = property.Scope,
				};

				ReportError(property.Id, error);

				objectsRequiringValidation.Remove(property);
			}
		}

		private void ValidateStringProperties(ICollection<StringProperty> apiProperties)
		{
			if (apiProperties == null)
			{
				throw new ArgumentNullException(nameof(apiProperties));
			}

			if (apiProperties.Count == 0)
			{
				return;
			}

			foreach (var property in apiProperties)
			{
				if (property.SizeLimit <= 0)
				{
					var error = new PropertyInvalidStringSizeLimitError
					{
						ErrorMessage = "Size limit must be greater than 0.",
						SizeLimit = property.SizeLimit,
						Id = property.Id,
					};

					ReportError(property.Id, error);
					continue;
				}

				if (property.SizeLimit > 8000)
				{
					var error = new PropertyInvalidStringSizeLimitError
					{
						ErrorMessage = "Size limit cannot exceed 8000.",
						SizeLimit = property.SizeLimit,
						Id = property.Id,
					};

					ReportError(property.Id, error);
					continue;
				}

				if (!string.IsNullOrEmpty(property.DefaultValue)
					&& !InputValidator.HasValidTextLength(property.DefaultValue, property.SizeLimit))
				{
					var error = new PropertyInvalidStringDefaultValueError
					{
						ErrorMessage = $"Default value exceeds maximum length of {property.SizeLimit} characters.",
						Id = property.Id,
						DefaultValue = property.DefaultValue,
					};

					ReportError(property.Id, error);
				}
			}
		}

		private void ValidateDiscreteProperties(ICollection<DiscreteProperty> apiProperties)
		{
			if (apiProperties == null)
			{
				throw new ArgumentNullException(nameof(apiProperties));
			}

			if (apiProperties.Count == 0)
			{
				return;
			}

			foreach (var property in apiProperties)
			{
				if (property.Discretes.Count == 0)
				{
					var error = new PropertyNoDiscretesError
					{
						ErrorMessage = "Empty discretes list is not allowed.",
						Id = property.Id,
					};

					ReportError(property.Id, error);
					continue;
				}

				var discretesWithInvalidLength = property.Discretes.Where(d => !InputValidator.HasValidTextLength(d)).ToList();
				if (discretesWithInvalidLength.Count > 0)
				{
					var error = new PropertyDiscreteInvalidLengthError
					{
						ErrorMessage = $"{discretesWithInvalidLength.Count} discrete(s) exceed(s) the maximum length of {InputValidator.DefaultMaxTextLength} characters.",
						Id = property.Id,
						InvalidDiscretes = discretesWithInvalidLength,
					};

					ReportError(property.Id, error);
					continue;
				}

				var duplicateDiscretes = property.Discretes
					.GroupBy(x => x.Trim())
					.Where(g => g.Count() > 1)
					.SelectMany(g => g)
					.ToList();

				if (duplicateDiscretes.Count != 0)
				{
					var error = new PropertyDuplicateDiscretesError
					{
						ErrorMessage = $"The property defines the following duplicate discretes: {String.Join(", ", duplicateDiscretes)}.",
						Id = property.Id,
						Discretes = duplicateDiscretes,
					};

					ReportError(property.Id, error);
				}
			}
		}

		private void ValidateFileProperties(ICollection<FileProperty> apiProperties)
		{
			if (apiProperties == null)
			{
				throw new ArgumentNullException(nameof(apiProperties));
			}

			if (apiProperties.Count == 0)
			{
				return;
			}

			throw new NotImplementedException();
		}

		private void ValidateStateForDeleteAction(ICollection<Property> apiProperties)
		{
			if (apiProperties == null)
			{
				throw new ArgumentNullException(nameof(apiProperties));
			}

			if (apiProperties.Count == 0)
			{
				return;
			}

			foreach (var property in apiProperties.Where(x => x.IsNew))
			{
				var error = new PropertyInvalidStateError
				{
					ErrorMessage = $"A property that was not saved cannot be removed.",
					Id = property.Id,
				};

				ReportError(property.Id, error);
			}
		}

		private void ValidatePropertiesAreNotInUse(ICollection<Property> apiProperties, PropertyDeleteOptions options)
		{
			if (apiProperties == null)
			{
				throw new ArgumentNullException(nameof(apiProperties));
			}

			if (apiProperties.Count == 0)
			{
				return;
			}

			var filter = new ORFilterElement<PropertyValueCollection>(apiProperties
				.Select(x => PropertyValueCollectionExposers.PropertyValues.PropertyId.Equal(x.Id))
				.ToArray());
			var collectionsUsingProperty = planApi.PropertyValueCollections.Read(filter);
			var collectionsByPropertyId = collectionsUsingProperty
				.SelectMany(c => c.PropertyValues.Select(v => new { Collection = c, PropertyId = v.PropertyId }))
				.GroupBy(x => x.PropertyId)
				.ToDictionary(g => g.Key, g => (IReadOnlyCollection<PropertyValueCollection>)g.Select(x => x.Collection).ToList());

			foreach (var property in apiProperties)
			{
				if (!collectionsByPropertyId.TryGetValue(property.Id, out var collections))
				{
					continue;
				}

				if (options.ForceDelete)
				{
					planApi.Logger.Warning(this, $"Property '{property.Name}' ({property.Id}) is in use by {collections.Count} collection(s), but will be deleted anyway because ForceDelete option is enabled.");
					continue;
				}

				var error = new PropertyInUseError
				{
					ErrorMessage = $"Property '{property.Name}' is in use by {collections.Count} collection(s).",
					Id = property.Id,
					CollectionIds = collections.Select(x => x.Id).ToList(),
				};

				ReportError(property.Id, error);
			}
		}

		private ICollection<DomChangeResults> GetPropertiesWithChanges(ICollection<Property> apiProperties)
		{
			return GetItemsWithChanges<Property, DomProperty>(
				apiProperties,
				p => p.OriginalInstance,
				p => p.GetInstanceWithChanges(),
				ids => planApi.DomHelpers.SlcPropertiesHelper.GetProperties(ids),
				p => new PropertyNotFoundError { ErrorMessage = $"Property with ID '{p.Id}' no longer exists.", Id = p.Id },
				(p, msg) => new PropertyValueAlreadyChangedError { ErrorMessage = msg, Id = p.Id })
				.ToList();
		}
	}
}
