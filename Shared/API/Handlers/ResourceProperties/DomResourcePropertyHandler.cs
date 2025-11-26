namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Utils.DOM.Extensions;

    using DomResourceProperty = Storage.DOM.SlcResource_Studio.ResourcepropertyInstance;

    internal class DomResourcePropertyHandler : ApiObjectValidator<Guid>
    {
        private readonly MediaOpsPlanApi planApi;

        private DomResourcePropertyHandler(MediaOpsPlanApi planApi)
        {
            this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
        }

        internal static BulkCreateOrUpdateResult<Guid> CreateOrUpdate(MediaOpsPlanApi planApi, IEnumerable<ResourceProperty> apiResourceProperties)
        {
            var handler = new DomResourcePropertyHandler(planApi);
            handler.CreateOrUpdate(apiResourceProperties);

            var result = new BulkCreateOrUpdateResult<Guid>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);
            result.ThrowOnFailure();

            return result;
        }

        internal static bool TryCreateOrUpdate(MediaOpsPlanApi planApi, IEnumerable<ResourceProperty> apiResourceProperties, out BulkCreateOrUpdateResult<Guid> result)
        {
            var handler = new DomResourcePropertyHandler(planApi);
            handler.CreateOrUpdate(apiResourceProperties);

            result = new BulkCreateOrUpdateResult<Guid>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

            return !result.HasFailures();
        }

        internal static BulkDeleteResult<Guid> Delete(MediaOpsPlanApi planApi, IEnumerable<ResourceProperty> apiResourceProperties)
        {
            var handler = new DomResourcePropertyHandler(planApi);
            handler.Delete(apiResourceProperties);

            var result = new BulkDeleteResult<Guid>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);
            result.ThrowOnFailure();

            return result;
        }

        internal static bool TryDelete(MediaOpsPlanApi planApi, IEnumerable<ResourceProperty> apiResourceProperties, out BulkDeleteResult<Guid> result)
        {
            var handler = new DomResourcePropertyHandler(planApi);
            handler.Delete(apiResourceProperties);

            result = new BulkDeleteResult<Guid>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

            return !result.HasFailures();
        }

        private void CreateOrUpdate(IEnumerable<ResourceProperty> apiResourceProperties)
        {
            if (apiResourceProperties == null)
            {
                throw new ArgumentNullException(nameof(apiResourceProperties));
            }

            if (!apiResourceProperties.Any())
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

            // Todo: lock DOM instances
            var changeResults = GetPropertiesWithChanges(toUpdate.Where(x => !TraceDataPerItem.Keys.Contains(x.Id)));

            var toCreateNameValidation = toCreate.Where(x => !TraceDataPerItem.Keys.Contains(x.Id));
            var toUpdateNameValidation = toUpdate.Where(x => changeResults.Any(y => y.Instance.ID.Id == x.Id && y.ChangedFields.Select(z => z.FieldDescriptorId).Contains(SlcResource_StudioIds.Sections.ResourcePoolInfo.Name.Id)));
            ValidateNames(toCreateNameValidation.Concat(toUpdateNameValidation));

            var toCreateDomInstances = toCreate
                .Where(x => !TraceDataPerItem.Keys.Contains(x.Id))
                .Select(x => x.GetInstanceWithChanges())
                .ToList();

            var toUpdateDomInstances = changeResults
                .Where(x => !TraceDataPerItem.Keys.Contains(x.Instance.ID.Id))
                .Select(x => new DomResourceProperty(x.Instance))
                .ToList();

            CreateOrUpdate(toCreateDomInstances.Concat(toUpdateDomInstances));
        }

        private void CreateOrUpdate(IEnumerable<DomResourceProperty> domResourceProperties)
        {
            if (domResourceProperties == null)
            {
                throw new ArgumentNullException(nameof(domResourceProperties));
            }

            if (!domResourceProperties.Any())
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

            ReportSuccess(domResult.SuccessfulIds.Select(x => x.Id));
        }

        private void Delete(IEnumerable<ResourceProperty> apiResourceProperties)
        {
            if (apiResourceProperties == null)
            {
                throw new ArgumentNullException(nameof(apiResourceProperties));
            }

            if (!apiResourceProperties.Any())
            {
                return;
            }

            var newProperties = apiResourceProperties.Where(x => x.IsNew).ToList();
            newProperties.ForEach(x =>
            {
                var error = new ResourcePropertyConfigurationError
                {
                    ErrorReason = ResourcePropertyConfigurationError.Reason.InvalidState,
                    ErrorMessage = $"A resource that was not saved cannot be removed.",
                };

                ReportError(x.Id, error);
            });

            // Todo: add check is property is in use

            var propertiesToDelete = apiResourceProperties.Except(newProperties).ToList();
            planApi.DomHelpers.SlcResourceStudioHelper.DomHelper.DomInstances.TryDeleteInBatches(propertiesToDelete.Select(x => x.OriginalInstance.ToInstance()), out var domResult);

            foreach (var id in domResult.UnsuccessfulIds)
            {
                ReportError(id.Id);

                if (domResult.TraceDataPerItem.TryGetValue(id, out var traceData))
                {
                    var mediaOpsTraceData = new MediaOpsTraceData();
                    mediaOpsTraceData.Add(new MediaOpsErrorData { ErrorMessage = traceData.ToString() });
                }
            }

            ReportSuccess(domResult.SuccessfulIds.Select(x => x.Id));
        }

        private void ValidateIdsNotInUse(IEnumerable<ResourceProperty> apiResourceProperties)
        {
            if (apiResourceProperties == null)
            {
                throw new ArgumentNullException(nameof(apiResourceProperties));
            }

            if (!apiResourceProperties.Any())
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
                var error = new ResourcePropertyConfigurationError
                {
                    ErrorReason = ResourcePropertyConfigurationError.Reason.DuplicateId,
                    ErrorMessage = $"Resource property '{property.Name}' has a duplicate ID.",
                };

                ReportError(property.Id, error);

                propertiesRequiringValidation.Remove(property);
            }

            foreach (var foundInstance in planApi.DomHelpers.SlcResourceStudioHelper.GetResourceStudioInstances(propertiesRequiringValidation.Select(x => x.Id)))
            {
                planApi.Logger.LogInformation($"ID is already in use by a Resource Studio instance.", foundInstance.ID.Id);

                var error = new ResourcePropertyConfigurationError
                {
                    ErrorReason = ResourcePropertyConfigurationError.Reason.IdInUse,
                    ErrorMessage = "ID is already in use.",
                };

                ReportError(foundInstance.ID.Id, error);
            }
        }

        private void ValidateNames(IEnumerable<ResourceProperty> apiResourceProperties)
        {
            if (apiResourceProperties == null)
            {
                throw new ArgumentNullException(nameof(apiResourceProperties));
            }

            if (!apiResourceProperties.Any())
            {
                return;
            }

            var propertiesRequiringValidation = apiResourceProperties.ToList();

            foreach (var property in propertiesRequiringValidation.Where(x => !InputValidator.ValidateEmptyText(x.Name)))
            {
                var error = new ResourcePropertyConfigurationError
                {
                    ErrorReason = ResourcePropertyConfigurationError.Reason.InvalidName,
                    ErrorMessage = "Name cannot be empty.",
                };

                ReportError(property.Id, error);

                propertiesRequiringValidation.Remove(property);
            }

            foreach (var property in propertiesRequiringValidation.Where(x => !InputValidator.ValidateTextLength(x.Name)))
            {
                var error = new ResourcePropertyConfigurationError
                {
                    ErrorReason = ResourcePropertyConfigurationError.Reason.InvalidName,
                    ErrorMessage = $"Name exceeds maximum length of {InputValidator.DefaultMaxTextLength} characters.",
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
                var error = new ResourcePropertyConfigurationError
                {
                    ErrorReason = ResourcePropertyConfigurationError.Reason.DuplicateName,
                    ErrorMessage = $"Resource property '{property.Name}' has a duplicate name.",
                };

                ReportError(property.Id, error);

                propertiesRequiringValidation.Remove(property);
            }

            FilterElement<DomInstance> filter(string name) =>
                DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resourceproperty.Id)
                .AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.PropertyInfo.PropertyName).Equal(name));

            var domPropertiesByName = planApi.DomHelpers.SlcResourceStudioHelper.GetResourceProperties(propertiesRequiringValidation.Select(x => x.Name), filter)
                .GroupBy(x => x.Name)
                .ToDictionary(x => x.Key, x => (IReadOnlyCollection<DomResourceProperty>)x.ToList());

            foreach (var property in propertiesRequiringValidation)
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

                planApi.Logger.LogInformation($"Name '{property.Name}' is already ins use by DOM resource property/properties with ID(s)", existingProperties.Select(x => x.ID.Id).ToArray());

                var error = new ResourcePropertyConfigurationError
                {
                    ErrorReason = ResourcePropertyConfigurationError.Reason.NameExists,
                    ErrorMessage = "Name is already in use.",
                };

                ReportError(property.Id, error);
            }
        }

        private IEnumerable<DomChangeResults> GetPropertiesWithChanges(IEnumerable<ResourceProperty> apiResourceProperties)
        {
            if (apiResourceProperties == null)
            {
                throw new ArgumentNullException(nameof(apiResourceProperties));
            }

            if (!apiResourceProperties.Any())
            {
                return [];
            }

            return GetPropertiesWithChangesIterator(apiResourceProperties);
        }

        private IEnumerable<DomChangeResults> GetPropertiesWithChangesIterator(IEnumerable<ResourceProperty> apiResourceProperties)
        {
            var propertiesRequiringValidation = apiResourceProperties.Where(x => !x.IsNew && x.HasChanges).ToList();
            if (propertiesRequiringValidation.Count == 0)
            {
                yield break;
            }

            var storedDomReesourcePropertiesById = planApi.DomHelpers.SlcResourceStudioHelper.GetResourceProperties(propertiesRequiringValidation.Select(x => x.Id)).ToDictionary(x => x.ID.Id);
            foreach (var property in propertiesRequiringValidation)
            {
                if (!storedDomReesourcePropertiesById.TryGetValue(property.Id, out var stored))
                {
                    var error = new ResourcePropertyConfigurationError
                    {
                        ErrorReason = ResourcePropertyConfigurationError.Reason.NotFound,
                        ErrorMessage = $"Resource property with ID '{property.Id}' no longer exists."
                    };

                    ReportError(property.Id, error);

                    continue;
                }

                var changeResult = DomChangeHandler.HandleChanges(property.OriginalInstance, property.GetInstanceWithChanges(), stored);
                if (changeResult.HasErrors)
                {
                    foreach (var errorDetails in changeResult.Errors)
                    {
                        var error = new ResourcePropertyConfigurationError
                        {
                            ErrorReason = ResourcePropertyConfigurationError.Reason.ValueAlreadyChanged,
                            ErrorMessage = errorDetails.Message,
                        };

                        ReportError(property.Id, error);
                    }
                }

                yield return changeResult;
            }
        }
    }
}
