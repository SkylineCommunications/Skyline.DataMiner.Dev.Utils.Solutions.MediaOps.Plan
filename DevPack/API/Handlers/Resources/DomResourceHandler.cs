namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.Logging;
    using Skyline.DataMiner.Core.DataMinerSystem.Common;
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.ActivityHelper;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;
    using Skyline.DataMiner.Utils.DOM.Extensions;
    using DomResource = Storage.DOM.SlcResource_Studio.ResourceInstance;
    using DomResourcePool = Storage.DOM.SlcResource_Studio.ResourcepoolInstance;

    internal class DomResourceHandler : ApiObjectValidator
    {
        private readonly MediaOpsPlanApi planApi;

        private readonly HashSet<Guid> resourceIdsWithCoreChanges = new HashSet<Guid>();

        private IDictionary<Guid, ResourcePool> resourcePoolsById = new Dictionary<Guid, ResourcePool>();

        private DomResourceHandler(MediaOpsPlanApi planApi)
        {
            this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
        }

        internal static bool TryCreateOrUpdate(MediaOpsPlanApi planApi, IEnumerable<Resource> apiResources, out BulkCreateOrUpdateResult<Guid> result)
        {
            var handler = new DomResourceHandler(planApi);
            handler.CreateOrUpdate(apiResources);

            result = new BulkCreateOrUpdateResult<Guid>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

            return !result.HasFailures();
        }

        internal static bool TryDeprecate(MediaOpsPlanApi planApi, IEnumerable<Resource> apiResources, out BulkCreateOrUpdateResult<Guid> result)
        {
            var handler = new DomResourceHandler(planApi);
            handler.TransitionToDeprecated(apiResources);

            result = new BulkCreateOrUpdateResult<Guid>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);
            return !result.HasFailures();
        }

        internal static bool TryDelete(MediaOpsPlanApi planApi, IEnumerable<Resource> apiResources, out BulkDeleteResult<Guid> result)
        {
            var handler = new DomResourceHandler(planApi);
            handler.Delete(apiResources);

            result = new BulkDeleteResult<Guid>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

            return !result.HasFailures();
        }

        internal static void TransitionToComplete(MediaOpsPlanApi planApi, Resource apiResource)
        {
            var handler = new DomResourceHandler(planApi);
            handler.TransitionToComplete(apiResource);
        }

        internal static void ConvertToUnmanagedResource(MediaOpsPlanApi planApi, Resource resource)
        {
            var handler = new DomResourceHandler(planApi);
            handler.ConvertToUnmanagedResource(resource);
        }

        internal static void ConvertToVirtualFunctionResource(MediaOpsPlanApi planApi, Resource resource, ResourceVirtualFunctionLinkConfiguration configuration)
        {
            var handler = new DomResourceHandler(planApi);
            handler.ConvertToVirtualFunctionResource(resource, configuration);
        }

        internal static void ConvertToServiceResource(MediaOpsPlanApi planApi, Resource resource, ResourceServiceLinkConfiguration configuration)
        {
            var handler = new DomResourceHandler(planApi);
            handler.ConvertToServiceResource(resource, configuration);
        }

        internal static void ConvertToElementResource(MediaOpsPlanApi planApi, Resource resource, ResourceElementLinkConfiguration configuration)
        {
            var handler = new DomResourceHandler(planApi);
            handler.ConvertToElementResource(resource, configuration);
        }

        private void TransitionToComplete(Resource apiResource)
        {
            // Clear Errors
            ClearErrors(planApi, apiResource, ResourceErrors.ExecuteAction_MarkCompleteException);

            // Create CORE Resource
            var result = CoreResourceHandler.CreateOrUpdate(planApi, [apiResource.OriginalInstance]);
            result.ThrowOnFailure();

            // Save link with CORE Resource
            CreateOrUpdate([apiResource.OriginalInstance]);

            // Transition DOM Resource to Complete
            planApi.DomHelpers.SlcResourceStudioHelper.TransitionResourceToComplete(apiResource.Id);
        }

        private void ClearErrors(MediaOpsPlanApi planApi, Resource apiResource, ErrorDefinition errorDefinition)
        {
            var domResource = planApi.DomHelpers.SlcResourceStudioHelper.GetResources([apiResource.Id]).First();
            domResource.ClearError(errorDefinition.ErrorCode);
            CreateOrUpdate([domResource]);
        }

        private void CreateOrUpdate(IEnumerable<Resource> apiResources)
        {
            if (apiResources == null)
            {
                throw new ArgumentNullException(nameof(apiResources));
            }

            if (!apiResources.Any())
            {
                return;
            }

            var toCreate = apiResources.Where(x => x.IsNew).ToList();
            var toUpdate = apiResources.Except(toCreate).ToList();

            ValidateIdsNotInUse(toCreate);
            ValidateCapabilities(apiResources);
            ValidateResourceProperties(apiResources);
            ValidateNames(apiResources);

            var validResources = apiResources.Where(IsValid).ToList();
            var lockResult = planApi.LockManager.LockAndExecute(validResources, CreateOrUpdateCoreResources);
            ReportError(lockResult);
        }

        private void CreateOrUpdateCoreResources(ICollection<Resource> resources)
        {
            if (resources == null)
            {
                throw new ArgumentNullException(nameof(resources));
            }

            if (resources.Any(x => !IsValid(x)))
            {
                throw new ArgumentException($"Not all provided resources are valid", nameof(resources));
            }

            var resourcesToCreate = resources.Where(x => x.IsNew).ToList();
            var resourcesToUpdate = resources.Except(resourcesToCreate).ToList();

            var changeResults = ActivityHelper.Track(nameof(DomResourceHandler), nameof(GetResourcesWithChanges), act => GetResourcesWithChanges(resourcesToUpdate));

            var toUpdateNameValidation = resourcesToUpdate.Where(x => changeResults.Any(y => y.Instance.ID.Id == x.Id && y.ChangedFields.Select(z => z.FieldDescriptorId).Contains(SlcResource_StudioIds.Sections.ResourceInfo.Name.Id)));
            ActivityHelper.Track(nameof(DomResourceHandler), nameof(ValidateDomNames), act => ValidateDomNames(resourcesToCreate.Concat(toUpdateNameValidation)));

            var toCreatePoolValidation = resourcesToCreate.Where(IsValid);
            var toUpdatePoolValidation = resourcesToUpdate.Where(x => changeResults.Any(y => y.Instance.ID.Id == x.Id && y.ChangedFields.Select(z => z.FieldDescriptorId).Contains(SlcResource_StudioIds.Sections.ResourceInternalProperties.Pool_Ids.Id)));
            ValidatePoolAssignments(toCreatePoolValidation.Concat(toUpdatePoolValidation));

            var resourcesWithCapabilityChanges = resourcesToUpdate.Where(x =>
                IsValid(x)
                && x.State != ResourceState.Deprecated
                && changeResults.Any(y => y.Instance.ID.Id == x.Id
                && (y.AddedSections.Select(z => z.SectionDefinitionId).Contains(SlcResource_StudioIds.Sections.ResourceCapabilities.Id.Id)
                || y.RemovedSections.Select(z => z.SectionDefinitionId).Contains(SlcResource_StudioIds.Sections.ResourceCapabilities.Id.Id)
                || y.ChangedFields.Select(z => z.SectionDefinitionId).Contains(SlcResource_StudioIds.Sections.ResourceCapabilities.Id.Id))));

            foreach (var resource in resourcesWithCapabilityChanges)
            {
                MarkAsResourceWithCoreChanges(resource);
            }

            var toCreateDomInstances = resourcesToCreate
                .Where(IsValid)
                .Select(x => x.GetInstanceWithChanges())
                .ToList();

            var toUpdateDomInstances = changeResults
                .Where(IsValid)
                .Select(x => new DomResource(x.Instance))
                .ToList();

            CreateOrUpdate(toCreateDomInstances.Concat(toUpdateDomInstances));
        }

        private void CreateOrUpdate(IEnumerable<DomResource> domResources)
        {
            if (domResources == null)
            {
                throw new ArgumentNullException(nameof(domResources));
            }

            if (!domResources.Any())
            {
                return;
            }

            var domResourcesById = domResources.ToDictionary(x => x.ID.Id);

            if (resourceIdsWithCoreChanges.Count != 0)
            {
                var domResourcesWithCoreChanges = domResources.Where(x => resourceIdsWithCoreChanges.Contains(x.ID.Id));
                UpdateCaches(domResourcesWithCoreChanges);

                CoreResourceHandler.TryCreateOrUpdate(planApi, domResourcesWithCoreChanges, out var coreResult);

                foreach (var id in coreResult.UnsuccessfulIds)
                {
                    ReportError(id);

                    if (coreResult.TraceDataPerItem.TryGetValue(id, out var traceData))
                    {
                        PassTraceData(id, traceData);
                    }

                    domResourcesById.Remove(id);
                }
            }

            planApi.DomHelpers.SlcResourceStudioHelper.DomHelper.DomInstances.TryCreateOrUpdateInBatches(domResourcesById.Values.Select(x => x.ToInstance()), out var domResult);

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

        private void TransitionToDeprecated(IEnumerable<Resource> apiResources)
        {
            if (apiResources == null)
            {
                throw new ArgumentNullException(nameof(apiResources));
            }

            if (!apiResources.Any())
            {
                return;
            }

            // Todo: add checks to see if resource is in use by jobs, etc.
            ValidateStateForDeprecateAction(apiResources);

            // Update CORE resources
            var resourcesToDeprecate = apiResources.Where(x => !TraceDataPerItem.Keys.Contains(x.Id)).ToList();

            CoreResourceHandler.TryDeprecate(planApi, resourcesToDeprecate.Select(x => x.OriginalInstance), out var coreResult);

            foreach (var id in coreResult.UnsuccessfulIds)
            {
                ReportError(id);

                if (coreResult.TraceDataPerItem.TryGetValue(id, out var traceData))
                {
                    PassTraceData(id, traceData);
                }
            }

            // Transition DOM resources to Deprecate state
            foreach (var id in coreResult.SuccessfulIds)
            {
                try
                {
                    planApi.DomHelpers.SlcResourceStudioHelper.TransitionResourceToDeprecated(id);

                    ReportSuccess(id);
                }
                catch (Exception ex)
                {
                    ReportError(id, new MediaOpsErrorData() { ErrorMessage = ex.ToString() });
                }
            }
        }

        private void Delete(IEnumerable<Resource> apiResources)
        {
            if (apiResources == null)
            {
                throw new ArgumentNullException(nameof(apiResources));
            }

            if (!apiResources.Any())
            {
                return;
            }

            var newResources = apiResources.Where(x => x.IsNew).ToList();
            newResources.ForEach(x =>
            {
                var error = new ResourceConfigurationError
                {
                    ErrorReason = ResourceConfigurationError.Reason.InvalidState,
                    ErrorMessage = $"A resource that was not saved cannot be removed.",
                };

                ReportError(x.Id, error);
            });

            apiResources = apiResources.Except(newResources).ToList();

            ValidateStateForDeleteAction(apiResources);

            var resourcesToDelete = apiResources.Where(IsValid).ToList();
            var lockResult = planApi.LockManager.LockAndExecute(resourcesToDelete, DeleteCoreResources);
            ReportError(lockResult);
        }

        private void DeleteCoreResources(ICollection<Resource> resources)
        {
            CoreResourceHandler.TryDelete(planApi, resources.Select(x => x.OriginalInstance), out var coreResult);

            foreach (var id in coreResult.UnsuccessfulIds)
            {
                ReportError(id);

                if (coreResult.TraceDataPerItem.TryGetValue(id, out var traceData))
                {
                    PassTraceData(id, traceData);
                }
            }

            planApi.DomHelpers.SlcResourceStudioHelper.DomHelper.DomInstances.TryDeleteInBatches(resources.Where(IsValid).Select(x => x.OriginalInstance.ToInstance()), out var domResult);

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

        private void UpdateCaches(IEnumerable<DomResource> domResources)
        {
            if (domResources == null)
            {
                throw new ArgumentNullException(nameof(domResources));
            }

            if (!domResources.Any())
            {
                return;
            }

            foreach (var domResource in domResources)
            {
                UpdateResourcePoolCache(domResource);
            }
        }

        private void UpdateResourcePoolCache(DomResource domResource)
        {
            if (resourcePoolsById == null)
            {
                return;
            }

            var domResourcePools = new List<DomResourcePool>();
            foreach (var poolId in domResource.ResourceInternalProperties.PoolIds)
            {
                if (!resourcePoolsById.TryGetValue(poolId, out var resourcePool))
                {
                    continue;
                }

                domResourcePools.Add(resourcePool.OriginalInstance);
            }

            if (domResourcePools.Count > 0)
            {
                domResource.SetCache(domResourcePools);

            }
        }

        private void ValidateIdsNotInUse(IEnumerable<Resource> apiResources)
        {
            if (apiResources == null)
            {
                throw new ArgumentNullException(nameof(apiResources));
            }

            if (!apiResources.Any())
            {
                return;
            }

            var resourcesRequiringValidation = apiResources.Where(x => x.IsNew && x.HasUserDefinedId).ToList();
            if (resourcesRequiringValidation.Count == 0)
            {
                return;
            }

            var resourcesWithDuplicateIds = resourcesRequiringValidation
                .GroupBy(resource => resource.Id)
                .Where(g => g.Count() > 1)
                .SelectMany(x => x)
                .ToList();

            foreach (var resource in resourcesWithDuplicateIds)
            {
                var error = new ResourceConfigurationError
                {
                    ErrorReason = ResourceConfigurationError.Reason.DuplicateId,
                    ErrorMessage = $"Resource '{resource.Name}' has a duplicate ID.",
                };

                ReportError(resource.Id, error);

                resourcesRequiringValidation.Remove(resource);
            }

            foreach (var foundInstance in planApi.DomHelpers.SlcResourceStudioHelper.GetResourceStudioInstances(resourcesRequiringValidation.Select(x => x.Id)))
            {
                planApi.Logger.LogInformation($"ID is already in use by a Resource Studio instance.", foundInstance.ID.Id);

                var error = new ResourceConfigurationError
                {
                    ErrorReason = ResourceConfigurationError.Reason.IdInUse,
                    ErrorMessage = "ID is already in use.",
                };

                ReportError(foundInstance.ID.Id, error);
            }
        }

        //private void ValidateStateForCompleteAction(IEnumerable<Resource> apiResources)
        //{
        //    if (apiResources == null)
        //    {
        //        throw new ArgumentNullException(nameof(apiResources));
        //    }

        //    if (!apiResources.Any())
        //    {
        //        return;
        //    }

        //    foreach (var resource in apiResources.Where(x => x.State != ResourceState.Draft))
        //    {
        //        var error = new ResourceConfigurationError
        //        {
        //            ErrorReason = ResourceConfigurationError.Reason.InvalidState,
        //            ErrorMessage = "Not allowed to complete a resource that is not in Draft state."
        //        };
        //        ReportError(resource.Id, error);
        //    }
        //}

        private void ValidateStateForDeprecateAction(IEnumerable<Resource> apiResources)
        {
            if (apiResources == null)
            {
                throw new ArgumentNullException(nameof(apiResources));
            }

            if (!apiResources.Any())
            {
                return;
            }

            foreach (var resource in apiResources.Where(x => x.State != ResourceState.Complete))
            {
                var error = new ResourceConfigurationError
                {
                    ErrorReason = ResourceConfigurationError.Reason.InvalidState,
                    ErrorMessage = "Not allowed to deprecate a resource that is not in Completed state."
                };
                ReportError(resource.Id, error);
            }
        }

        private void ValidateStateForDeleteAction(IEnumerable<Resource> apiResources)
        {
            if (apiResources == null)
            {
                throw new ArgumentNullException(nameof(apiResources));
            }

            if (!apiResources.Any())
            {
                return;
            }

            foreach (var resource in apiResources.Where(x => !new[] { ResourceState.Draft, ResourceState.Deprecated }.Contains(x.State)))
            {
                var error = new ResourceConfigurationError
                {
                    ErrorReason = ResourceConfigurationError.Reason.InvalidState,
                    ErrorMessage = "Not allowed to delete a resource that is not in Draft or Deprecated state."
                };
                ReportError(resource.Id, error);
            }
        }

        private void ValidateNames(IEnumerable<Resource> apiResources)
        {
            if (apiResources == null)
            {
                throw new ArgumentNullException(nameof(apiResources));
            }

            if (!apiResources.Any())
            {
                return;
            }

            var resourcesRequiringValidation = apiResources.ToList();

            foreach (var resource in resourcesRequiringValidation.Where(x => !InputValidator.ValidateEmptyText(x.Name)))
            {
                var error = new ResourceConfigurationError
                {
                    ErrorReason = ResourceConfigurationError.Reason.InvalidName,
                    ErrorMessage = "Name cannot be empty.",
                };

                ReportError(resource.Id, error);

                resourcesRequiringValidation.Remove(resource);
            }

            foreach (var resource in resourcesRequiringValidation.Where(x => !InputValidator.ValidateTextLength(x.Name)))
            {
                var error = new ResourceConfigurationError
                {
                    ErrorReason = ResourceConfigurationError.Reason.InvalidName,
                    ErrorMessage = "Name exceeds maximum length of 150 characters.",
                };

                ReportError(resource.Id, error);

                resourcesRequiringValidation.Remove(resource);
            }

            var resourcesWithDuplicateNames = resourcesRequiringValidation
                .GroupBy(resource => resource.Name)
                .Where(g => g.Count() > 1)
                .SelectMany(x => x)
                .ToList();

            foreach (var resource in resourcesWithDuplicateNames)
            {
                var error = new ResourceConfigurationError
                {
                    ErrorReason = ResourceConfigurationError.Reason.DuplicateName,
                    ErrorMessage = $"Resource '{resource.Name}' has a duplicate name.",
                };

                ReportError(resource.Id, error);
            }
        }

        private void ValidateDomNames(IEnumerable<Resource> apiResources)
        {
            if (apiResources == null)
            {
                throw new ArgumentNullException(nameof(apiResources));
            }

            if (!apiResources.Any())
            {
                return;
            }

            FilterElement<DomInstance> filter(string name) =>
                DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resource.Id)
                .AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInfo.Name).Equal(name));

            var domResourcesbyName = planApi.DomHelpers.SlcResourceStudioHelper.GetResources(apiResources.Select(x => x.Name), filter)
                .GroupBy(x => x.Name)
                .ToDictionary(x => x.Key, x => (IReadOnlyCollection<DomResource>)x.ToList());

            foreach (var resource in apiResources)
            {
                if (!domResourcesbyName.TryGetValue(resource.Name, out var domResources))
                {
                    MarkAsResourceWithCoreChanges(resource);
                    continue;
                }

                var existingResources = domResources.Where(x => x.ID.Id != resource.Id).ToList();
                if (existingResources.Count == 0)
                {
                    MarkAsResourceWithCoreChanges(resource);
                    continue;
                }

                planApi.Logger.LogInformation($"Name '{resource.Name}' is already in use by DOM resource(s) with ID(s)", existingResources.Select(x => x.ID.Id).ToArray());

                var error = new ResourceConfigurationError
                {
                    ErrorReason = ResourceConfigurationError.Reason.NameExists,
                    ErrorMessage = "Name is already in use.",
                };

                ReportError(resource.Id, error);
            }
        }

        private void ValidatePoolAssignments(IEnumerable<Resource> apiResources)
        {
            if (apiResources == null)
            {
                throw new ArgumentNullException(nameof(apiResources));
            }

            if (!apiResources.Any())
            {
                return;
            }

            var poolIds = apiResources
                .SelectMany(x => x.AssignedResourcePoolIds)
                .Distinct()
                .ToList();
            resourcePoolsById = planApi.ResourcePools.Read(poolIds);

            foreach (var resource in apiResources)
            {
                var hasError = false;

                foreach (var poolId in resource.AssignedResourcePoolIds)
                {
                    if (!resourcePoolsById.TryGetValue(poolId, out _))
                    {
                        var error = new ResourceConfigurationError
                        {
                            ErrorReason = ResourceConfigurationError.Reason.InvalidAssignedPool,
                            ErrorMessage = $"Resource Pool with ID '{poolId}' not found.",
                        };

                        ReportError(resource.Id, error);
                        hasError = true;
                    }
                }

                if (!hasError)
                {
                    MarkAsResourceWithCoreChanges(resource);
                }
            }
        }

        private void ValidateCapabilities(IEnumerable<Resource> apiResources)
        {
            if (apiResources == null)
            {
                throw new ArgumentNullException(nameof(apiResources));
            }

            if (!apiResources.Any())
            {
                return;
            }

            var capabilityIds = apiResources
                .SelectMany(x => x.Capabilities)
                .Select(x => x.Id)
                .Distinct()
                .ToList();
            var capabilitiesById = planApi.Capabilities.Read(capabilityIds);

            foreach (var resource in apiResources)
            {
                foreach (var capabilitySettings in resource.Capabilities)
                {
                    if (capabilitySettings.Id == Guid.Empty)
                    {
                        var error = new InvalidResourceCapabilitySettingsError
                        {
                            ErrorMessage = "Capability ID cannot be empty.",
                        };

                        ReportError(resource.Id, error);
                        continue;
                    }

                    if (!capabilitiesById.TryGetValue(capabilitySettings.Id, out var capability))
                    {
                        var error = new InvalidResourceCapabilitySettingsError
                        {
                            ErrorMessage = $"Capability with ID '{capabilitySettings.Id}' not found.",
                            CapabilityId = capabilitySettings.Id,
                        };

                        ReportError(resource.Id, error);
                        continue;
                    }

                    if (capabilitySettings.Discretes.Count == 0)
                    {
                        var error = new InvalidResourceCapabilitySettingsError
                        {
                            ErrorMessage = "At least one discrete value must be specified for the capability.",
                            CapabilityId = capabilitySettings.Id,
                        };

                        ReportError(resource.Id, error);
                        continue;
                    }

                    foreach (var discreteValue in capabilitySettings.Discretes)
                    {
                        if (!capability.Discretes.Contains(discreteValue))
                        {
                            var error = new InvalidResourceCapabilitySettingsError
                            {
                                ErrorMessage = $"Discrete value '{discreteValue}' is not valid for capability '{capability.Name}'.",
                                CapabilityId = capabilitySettings.Id,
                            };

                            ReportError(resource.Id, error);
                        }
                    }
                }
            }
        }

        private void ValidateResourceProperties(IEnumerable<Resource> apiResources)
        {
            if (apiResources == null)
            {
                throw new ArgumentNullException(nameof(apiResources));
            }

            if (!apiResources.Any())
            {
                return;
            }

            var propertyIds = apiResources
                .SelectMany(x => x.Properties)
                .Select(x => x.Id)
                .Distinct()
                .ToList();
            var propertiesById = planApi.Properties.Read(propertyIds);

            foreach (var resource in apiResources)
            {
                foreach (var propertySettings in resource.Properties)
                {
                    if (propertySettings.Id == Guid.Empty)
                    {
                        var error = new InvalidResourcePropertySettingsError
                        {
                            ErrorMessage = "Property ID cannot be empty.",
                        };

                        ReportError(resource.Id, error);
                    }
                    else if (!propertiesById.TryGetValue(propertySettings.Id, out _))
                    {
                        var error = new InvalidResourcePropertySettingsError
                        {
                            ErrorMessage = $"Property with ID '{propertySettings.Id}' not found.",
                        };

                        ReportError(resource.Id, error);
                    }
                }
            }
        }

        private IEnumerable<DomChangeResults> GetResourcesWithChanges(IEnumerable<Resource> apiResources)
        {
            if (apiResources == null)
            {
                throw new ArgumentNullException(nameof(apiResources));
            }

            if (!apiResources.Any())
            {
                return [];
            }

            return GetResourcesWithChangesIterator(apiResources);
        }

        private IEnumerable<DomChangeResults> GetResourcesWithChangesIterator(IEnumerable<Resource> apiResources)
        {
            var resourcesRequiringValidation = apiResources.Where(x => !x.IsNew && x.HasChanges).ToList();
            if (resourcesRequiringValidation.Count == 0)
            {
                yield break;
            }

            var storedDomResourcesById = planApi.DomHelpers.SlcResourceStudioHelper.GetResources(resourcesRequiringValidation.Select(x => x.Id)).ToDictionary(x => x.ID.Id);
            foreach (var resource in resourcesRequiringValidation)
            {
                if (!storedDomResourcesById.TryGetValue(resource.Id, out var stored))
                {
                    var error = new ResourceConfigurationError
                    {
                        ErrorReason = ResourceConfigurationError.Reason.NotFound,
                        ErrorMessage = $"Resource with ID '{resource.Id}' no longer exists."
                    };

                    ReportError(resource.Id, error);

                    continue;
                }

                var changeResult = DomChangeHandler.HandleChanges(resource.OriginalInstance, resource.GetInstanceWithChanges(), stored);
                if (changeResult.HasErrors)
                {
                    foreach (var errorDetails in changeResult.Errors)
                    {
                        var error = new ResourceConfigurationError
                        {
                            ErrorReason = ResourceConfigurationError.Reason.ValueAlreadyChanged,
                            ErrorMessage = errorDetails.Message,
                        };

                        ReportError(resource.Id, error);
                    }

                    continue;
                }

                yield return changeResult;
            }
        }

        private void MarkAsResourceWithCoreChanges(Resource resource)
        {
            if (resource == null)
            {
                throw new ArgumentNullException(nameof(resource));
            }

            if (resource.State != ResourceState.Complete)
            {
                return;
            }

            resourceIdsWithCoreChanges.Add(resource.Id);
        }

        private void ConvertToUnmanagedResource(Resource resource)
        {
            var domResource = planApi.DomHelpers.SlcResourceStudioHelper.GetResources([resource.Id]).FirstOrDefault();
            if (domResource == null)
            {
                ReportError(resource.Id, new ResourceConfigurationError
                {
                    ErrorReason = ResourceConfigurationError.Reason.NotFound,
                    ErrorMessage = $"Resource with ID '{resource.Id}' not found."
                });

                return;
            }

            domResource.ResourceInfo.Type = SlcResource_StudioIds.Enums.Type.Unmanaged;
            domResource.ResourceInternalProperties.ResourceMetadata = String.Empty;

            CreateOrUpdate([domResource]);
        }

        private void ConvertToVirtualFunctionResource(Resource resource, ResourceVirtualFunctionLinkConfiguration configuration)
        {
            if (!CoreResourceHandler.TryValidateVirtualFunctionConfiguration(planApi, configuration, out var error))
            {
                ReportError(resource.Id, error);
                return;
            }

            var domResource = planApi.DomHelpers.SlcResourceStudioHelper.GetResources([resource.Id]).FirstOrDefault();
            if (domResource == null)
            {
                ReportError(resource.Id, new ResourceConfigurationError
                {
                    ErrorReason = ResourceConfigurationError.Reason.NotFound,
                    ErrorMessage = $"Resource with ID '{resource.Id}' not found."
                });

                return;
            }

            domResource.ResourceInfo.Type = SlcResource_StudioIds.Enums.Type.VirtualFunction;
            domResource.ResourceInternalProperties.Metadata = new ResourceMetadata
            {
                LinkedElementInfo = new DmsElementId(configuration.AgentId, configuration.ElementId).Value,
                LinkedFunctionId = configuration.FunctionId,
                LinkedFunctionTableIndex = configuration.FunctionTableIndex,
            };

            CreateOrUpdate([domResource]);
        }

        private void ConvertToServiceResource(Resource resource, ResourceServiceLinkConfiguration configuration)
        {
            if (!CoreResourceHandler.TryValidateServiceConfiguration(planApi, configuration, out var error))
            {
                ReportError(resource.Id, error);
                return;
            }

            var domResource = planApi.DomHelpers.SlcResourceStudioHelper.GetResources([resource.Id]).FirstOrDefault();
            if (domResource == null)
            {
                ReportError(resource.Id, new ResourceConfigurationError
                {
                    ErrorReason = ResourceConfigurationError.Reason.NotFound,
                    ErrorMessage = $"Resource with ID '{resource.Id}' not found."
                });

                return;
            }

            domResource.ResourceInfo.Type = SlcResource_StudioIds.Enums.Type.Service;
            domResource.ResourceInternalProperties.Metadata = new ResourceMetadata
            {
                LinkedServiceInfo = new DmsServiceId(configuration.AgentId, configuration.ServiceId).Value,
            };

            CreateOrUpdate([domResource]);
        }

        private void ConvertToElementResource(Resource resource, ResourceElementLinkConfiguration configuration)
        {
            if (!CoreResourceHandler.TryValidateElementConfiguration(planApi, configuration, out var error))
            {
                ReportError(resource.Id, error);
                return;
            }

            var domResource = planApi.DomHelpers.SlcResourceStudioHelper.GetResources([resource.Id]).FirstOrDefault();
            if (domResource == null)
            {
                ReportError(resource.Id, new ResourceConfigurationError
                {
                    ErrorReason = ResourceConfigurationError.Reason.NotFound,
                    ErrorMessage = $"Resource with ID '{resource.Id}' not found."
                });

                return;
            }

            domResource.ResourceInfo.Type = SlcResource_StudioIds.Enums.Type.Element;
            domResource.ResourceInternalProperties.Metadata = new ResourceMetadata
            {
                LinkedElementInfo = new DmsElementId(configuration.AgentId, configuration.ElementId).Value,
            };

            CreateOrUpdate([domResource]);
        }
    }
}
