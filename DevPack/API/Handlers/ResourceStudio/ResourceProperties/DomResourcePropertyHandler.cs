namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
	using Skyline.DataMiner.Net.Messages.SLDataGateway;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;
	using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;
	using Skyline.DataMiner.Utils.DOM.Extensions;

	using DomResourceProperty = Storage.DOM.SlcResource_Studio.ResourcepropertyInstance;

	internal class DomResourcePropertyHandler : DomInstanceApiObjectValidator<DomResourceProperty>
	{
		private readonly MediaOpsPlanApi planApi;

		private DomResourcePropertyHandler(MediaOpsPlanApi planApi)
		{
			this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
		}

		internal static bool TryCreateOrUpdate(MediaOpsPlanApi planApi, ICollection<ResourceProperty> apiResourceProperties, out DomInstanceBulkOperationResult<DomResourceProperty> result)
		{
			var handler = new DomResourcePropertyHandler(planApi);
			handler.CreateOrUpdate(apiResourceProperties);

			result = new DomInstanceBulkOperationResult<DomResourceProperty>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		internal static bool TryDelete(MediaOpsPlanApi planApi, ICollection<ResourceProperty> apiResourceProperties, out DomInstanceBulkOperationResult<DomResourceProperty> result)
		{
			var handler = new DomResourcePropertyHandler(planApi);
			handler.Delete(apiResourceProperties);

			result = new DomInstanceBulkOperationResult<DomResourceProperty>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

			return !result.HasFailures;
		}

		private void CreateOrUpdate(ICollection<ResourceProperty> apiResourceProperties)
		{
			if (apiResourceProperties == null)
			{
				throw new ArgumentNullException(nameof(apiResourceProperties));
			}

			if (apiResourceProperties.Count == 0)
			{
				return;
			}

			var toCreate = new List<ResourceProperty>();
			var toUpdate = new List<ResourceProperty>();
			foreach (var resourceProperty in apiResourceProperties)
			{
				if (resourceProperty.IsNew)
				{
					toCreate.Add(resourceProperty);
				}
				else
				{
					toUpdate.Add(resourceProperty);
				}
			}

			ValidateIdsNotInUse(toCreate);
			ValidateNames(apiResourceProperties);

			var validResourceProperties = apiResourceProperties.Where(IsValid).ToList();
			var lockResult = planApi.LockManager.LockAndExecute(validResourceProperties, CreateOrUpdateCoreResourceProperties);
			ReportError(lockResult);
		}

		private void CreateOrUpdateCoreResourceProperties(ICollection<ResourceProperty> validResourceProperties)
		{
			if (validResourceProperties == null)
			{
				throw new ArgumentNullException(nameof(validResourceProperties));
			}

			if (validResourceProperties.Any(x => !IsValid(x)))
			{
				throw new ArgumentException($"Not all provided resource properties are valid", nameof(validResourceProperties));
			}

			var resourcePropertiesToCreate = validResourceProperties.Where(x => x.IsNew).ToList();
			var resourcePropertiesToUpdate = validResourceProperties.Except(resourcePropertiesToCreate).ToList();

			var changeResults = GetPropertiesWithChanges(resourcePropertiesToUpdate);

			var toUpdateNameValidation = resourcePropertiesToUpdate.Where(x => changeResults.Any(y => y.Instance.ID.Id == x.Id && y.ChangedFields.Select(z => z.FieldDescriptorId).Contains(SlcResource_StudioIds.Sections.PropertyInfo.PropertyName.Id)));
			ValidateDomNames(resourcePropertiesToCreate.Concat(toUpdateNameValidation).ToList());

			var toCreateDomInstances = resourcePropertiesToCreate
				.Where(IsValid)
				.Select(x => x.GetInstanceWithChanges())
				.ToList();

			var toUpdateDomInstances = changeResults
				.Where(IsValid)
				.Select(x => new DomResourceProperty(x.Instance))
				.ToList();

			CreateOrUpdateDomResourceProperties(toCreateDomInstances.Concat(toUpdateDomInstances).ToList());
		}

		private void CreateOrUpdateDomResourceProperties(ICollection<DomResourceProperty> domResourceProperties)
		{
			if (domResourceProperties == null)
			{
				throw new ArgumentNullException(nameof(domResourceProperties));
			}

			if (domResourceProperties.Count == 0)
			{
				return;
			}

			planApi.DomHelpers.SlcResourceStudioHelper.DomHelper.DomInstances.TryCreateOrUpdateInBatches(domResourceProperties.Select(x => x.ToInstance()), out var domResult);

			foreach (var id in domResult.UnsuccessfulIds)
			{
				ReportError(id.Id);

				if (domResult.TraceDataPerItem.TryGetValue(id, out var traceData))
				{
					var mediaOpsTraceData = new MediaOpsTraceData();
					mediaOpsTraceData.Add(new MediaOpsErrorData() { ErrorMessage = traceData.ToString() });
				}
			}

			ReportSuccess(domResult.SuccessfulItems.Select(x => new DomResourceProperty(x)));
		}

		private void Delete(ICollection<ResourceProperty> apiResourceProperties)
		{
			if (apiResourceProperties == null)
			{
				throw new ArgumentNullException(nameof(apiResourceProperties));
			}

			if (apiResourceProperties.Count == 0)
			{
				return;
			}

			var newProperties = apiResourceProperties.Where(x => x.IsNew).ToList();
			newProperties.ForEach(x =>
			{
				var error = new ResourcePropertyInvalidStateError
				{
					ErrorMessage = $"A resource property that was not saved cannot be removed.",
					Id = x.Id,
				};

				ReportError(x.Id, error);
			});

			ValidatePropertiesAreNotInUse(apiResourceProperties.Except(newProperties).ToList());

			var propertiesToDelete = apiResourceProperties.Where(IsValid).ToList();
			var lockResult = planApi.LockManager.LockAndExecute(propertiesToDelete, DeleteCoreResourceProperties);
			ReportError(lockResult);
		}

		private void DeleteCoreResourceProperties(ICollection<ResourceProperty> propertiesToDelete)
		{
			if (propertiesToDelete == null)
			{
				throw new ArgumentNullException(nameof(propertiesToDelete));
			}

			if (propertiesToDelete.Count == 0)
			{
				return;
			}

			var instancesToDelete = propertiesToDelete.Select(x => x.OriginalInstance.ToInstance());
			planApi.DomHelpers.SlcResourceStudioHelper.DomHelper.DomInstances.TryDeleteInBatches(instancesToDelete, out var domResult);

			foreach (var id in domResult.UnsuccessfulIds)
			{
				ReportError(id.Id);

				if (domResult.TraceDataPerItem.TryGetValue(id, out var traceData))
				{
					var mediaOpsTraceData = new MediaOpsTraceData();
					mediaOpsTraceData.Add(new MediaOpsErrorData { ErrorMessage = traceData.ToString() });
				}
			}

			ReportSuccess(instancesToDelete.Where(x => domResult.SuccessfulIds.Contains(x.ID)).Select(x => new DomResourceProperty(x)));
		}

		private void ValidateIdsNotInUse(ICollection<ResourceProperty> apiResourceProperties)
		{
			if (apiResourceProperties == null)
			{
				throw new ArgumentNullException(nameof(apiResourceProperties));
			}

			if (apiResourceProperties.Count == 0)
			{
				return;
			}

			var propertiesRequiringValidation = apiResourceProperties.Where(x => x.IsNew && x.HasUserDefinedId).ToList();
			if (propertiesRequiringValidation.Count == 0)
			{
				return;
			}

			var propertiesWithDuplicateIds = propertiesRequiringValidation
				.GroupBy(property => property.Id)
				.Where(g => g.Count() > 1)
				.SelectMany(x => x)
				.ToList();

			foreach (var property in propertiesWithDuplicateIds)
			{
				var error = new ResourcePropertyDuplicateIdError
				{
					ErrorMessage = $"Resource property '{property.Name}' has a duplicate ID.",
					Id = property.Id,
				};

				ReportError(property.Id, error);

				propertiesRequiringValidation.Remove(property);
			}

			foreach (var foundInstance in planApi.DomHelpers.SlcResourceStudioHelper.GetResourceStudioInstances(propertiesRequiringValidation.Select(x => x.Id)))
			{
				planApi.Logger.Information(this, $"ID is already in use by a Resource Studio instance.", [foundInstance.ID.Id]);

				var error = new ResourcePropertyIdInUseError
				{
					ErrorMessage = "ID is already in use.",
					Id = foundInstance.ID.Id,
				};

				ReportError(foundInstance.ID.Id, error);
			}
		}

		private void ValidateNames(ICollection<ResourceProperty> apiResourceProperties)
		{
			if (apiResourceProperties == null)
			{
				throw new ArgumentNullException(nameof(apiResourceProperties));
			}

			if (apiResourceProperties.Count == 0)
			{
				return;
			}

			var propertiesRequiringValidation = apiResourceProperties.ToList();

			foreach (var property in propertiesRequiringValidation.Where(x => !InputValidator.IsNonEmptyText(x.Name)).ToArray())
			{
				var error = new ResourcePropertyInvalidNameError
				{
					ErrorMessage = "Name cannot be empty.",
					Id = property.Id,
				};

				ReportError(property.Id, error);

				propertiesRequiringValidation.Remove(property);
			}

			foreach (var property in propertiesRequiringValidation.Where(x => !InputValidator.HasValidTextLength(x.Name)).ToArray())
			{
				var error = new ResourcePropertyInvalidNameError
				{
					ErrorMessage = $"Name exceeds maximum length of {InputValidator.DefaultMaxTextLength} characters.",
					Id = property.Id,
					Name = property.Name,
				};

				ReportError(property.Id, error);

				propertiesRequiringValidation.Remove(property);
			}

			var propertiesWithDuplicateNames = propertiesRequiringValidation
				.GroupBy(property => property.Name)
				.Where(g => g.Count() > 1)
				.SelectMany(x => x)
				.ToList();

			foreach (var property in propertiesWithDuplicateNames)
			{
				var error = new ResourcePropertyDuplicateNameError
				{
					ErrorMessage = $"Resource property '{property.Name}' has a duplicate name.",
					Id = property.Id,
					Name = property.Name,
				};

				ReportError(property.Id, error);
			}
		}

		private void ValidateDomNames(ICollection<ResourceProperty> apiResourceProperties)
		{
			if (apiResourceProperties == null)
			{
				throw new ArgumentNullException(nameof(apiResourceProperties));
			}

			if (apiResourceProperties.Count == 0)
			{
				return;
			}

			FilterElement<DomInstance> Filter(string name) =>
				DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resourceproperty.Id)
				.AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.PropertyInfo.PropertyName).Equal(name));

			var domPropertiesByName = planApi.DomHelpers.SlcResourceStudioHelper.GetResourceProperties(apiResourceProperties.Select(x => x.Name), Filter)
				.GroupBy(x => x.PropertyInfo.PropertyName)
				.ToDictionary(x => x.Key, x => (IReadOnlyCollection<DomResourceProperty>)x.ToList());

			foreach (var property in apiResourceProperties)
			{
				if (!domPropertiesByName.TryGetValue(property.Name, out var domProperties))
				{
					continue;
				}

				var existingProperties = domProperties.Where(x => x.ID.Id != property.Id).ToList();
				if (existingProperties.Count == 0)
				{
					continue;
				}

				planApi.Logger.Information(this, $"Name '{property.Name}' is already in use by DOM resource property/properties with ID(s)", [existingProperties.Select(x => x.ID.Id).ToArray()]);

				var error = new ResourcePropertyNameExistsError
				{
					ErrorMessage = "Name is already in use.",
					Id = property.Id,
					Name = property.Name,
				};

				ReportError(property.Id, error);
			}
		}

		private void ValidatePropertiesAreNotInUse(ICollection<ResourceProperty> apiResourceProperties)
		{
			if (apiResourceProperties == null)
			{
				throw new ArgumentNullException(nameof(apiResourceProperties));
			}

			if (apiResourceProperties.Count == 0)
			{
				return;
			}

			var filter = new ORFilterElement<Resource>(apiResourceProperties
				.Select(x => ResourceExposers.Properties.PropertyId.Equal(x.Id))
				.ToArray());

			var resourcesImplementingProperties = planApi.Resources.Read(filter);

			var resourcesByPropertyId = resourcesImplementingProperties
				.SelectMany(r => r.Properties.Select(p => new { PropertyId = p.Id, Resource = r }))
				.GroupBy(x => x.PropertyId)
				.ToDictionary(x => x.Key, x => x.Select(y => y.Resource).ToList());

			foreach (var property in apiResourceProperties)
			{
				if (!resourcesByPropertyId.TryGetValue(property.Id, out var resources))
				{
					continue;
				}

				var error = new ResourcePropertyInUseError
				{
					ErrorMessage = $"Resource property '{property.Name}' is in use by {resources.Count} resource(s).",
					Id = property.Id,
					ResourceIds = resources.Select(x => x.Id).ToList(),
				};

				ReportError(property.Id, error);
			}
		}

		private IEnumerable<DomChangeResults> GetPropertiesWithChanges(ICollection<ResourceProperty> apiResourceProperties)
		{
			if (apiResourceProperties == null)
			{
				throw new ArgumentNullException(nameof(apiResourceProperties));
			}

			if (apiResourceProperties.Count == 0)
			{
				return [];
			}

			return GetPropertiesWithChangesIterator(apiResourceProperties);
		}

		private IEnumerable<DomChangeResults> GetPropertiesWithChangesIterator(ICollection<ResourceProperty> apiResourceProperties)
		{
			var propertiesRequiringValidation = apiResourceProperties.Where(x => !x.IsNew && x.HasChanges).ToList();
			if (propertiesRequiringValidation.Count == 0)
			{
				yield break;
			}

			var storedDomResourcePropertiesById = planApi.DomHelpers.SlcResourceStudioHelper.GetResourceProperties(propertiesRequiringValidation.Select(x => x.Id)).ToDictionary(x => x.ID.Id);
			foreach (var property in propertiesRequiringValidation)
			{
				if (!storedDomResourcePropertiesById.TryGetValue(property.Id, out var stored))
				{
					var error = new ResourcePropertyNotFoundError
					{
						ErrorMessage = $"Resource property with ID '{property.Id}' no longer exists.",
						Id = property.Id,
					};

					ReportError(property.Id, error);

					continue;
				}

				var changeResult = DomChangeHandler.HandleChanges(property.OriginalInstance, property.GetInstanceWithChanges(), stored);
				if (changeResult.HasErrors)
				{
					foreach (var errorDetails in changeResult.Errors)
					{
						var error = new ResourcePropertyValueAlreadyChangedError
						{
							ErrorMessage = errorDetails.Message,
							Id = property.Id,
						};

						ReportError(property.Id, error);
					}
				}

				yield return changeResult;
			}
		}
	}
}
