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

        internal static bool TryCreateOrUpdate(MediaOpsPlanApi planApi, ICollection<Resource> apiResources, out BulkOperationResult<Guid> result)
        {
            var handler = new DomResourceHandler(planApi);
            handler.CreateOrUpdate(apiResources);

            result = new BulkOperationResult<Guid>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

            return !result.HasFailures;
        }

        internal static bool TryDeprecate(MediaOpsPlanApi planApi, ICollection<Resource> apiResources, out BulkOperationResult<Guid> result)
        {
            var handler = new DomResourceHandler(planApi);
            handler.TransitionToDeprecated(apiResources);

            result = new BulkOperationResult<Guid>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);
            return !result.HasFailures;
        }

        internal static bool TryDelete(MediaOpsPlanApi planApi, ICollection<Resource> apiResources, out BulkOperationResult<Guid> result)
        {
            var handler = new DomResourceHandler(planApi);
            handler.Delete(apiResources);

            result = new BulkOperationResult<Guid>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

            return !result.HasFailures;
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

        internal static void ConvertToVirtualFunctionResource(MediaOpsPlanApi planApi, Resource resource, ResourceVirtualFunctionLinkSetting setting)
        {
            var handler = new DomResourceHandler(planApi);
            handler.ConvertToVirtualFunctionResource(resource, setting);
        }

        internal static void ConvertToServiceResource(MediaOpsPlanApi planApi, Resource resource, ResourceServiceLinkSetting setting)
        {
            var handler = new DomResourceHandler(planApi);
            handler.ConvertToServiceResource(resource, setting);
        }

        internal static void ConvertToElementResource(MediaOpsPlanApi planApi, Resource resource, ResourceElementLinkSetting setting)
        {
            var handler = new DomResourceHandler(planApi);
            handler.ConvertToElementResource(resource, setting);
        }

        private void TransitionToComplete(Resource apiResource)
        {
            // Clear Errors
            ClearErrors(planApi, apiResource, ResourceErrors.ExecuteAction_MarkCompleteException);

            // Create CORE Resource
            if (!CoreResourceHandler.TryCreateOrUpdate(planApi, [apiResource.OriginalInstance], out var result))
            {
                result.ThrowSingleException(apiResource.Id);
            }

            // Save link with CORE Resource
            CreateOrUpdateDomResources([apiResource.OriginalInstance]);

            // Transition DOM Resource to Complete
            planApi.DomHelpers.SlcResourceStudioHelper.TransitionResourceToComplete(apiResource.Id);
        }

        private void ClearErrors(MediaOpsPlanApi planApi, Resource apiResource, ErrorDefinition errorDefinition)
        {
            var domResource = planApi.DomHelpers.SlcResourceStudioHelper.GetResources([apiResource.Id]).First();
            domResource.ClearError(errorDefinition.ErrorCode);
            CreateOrUpdateDomResources([domResource]);
        }

        private void CreateOrUpdate(ICollection<Resource> apiResources)
        {
            if (apiResources == null)
            {
                throw new ArgumentNullException(nameof(apiResources));
            }

            if (apiResources.Count == 0)
            {
                return;
            }

            var toCreate = apiResources.Where(x => x.IsNew).ToList();

            ValidateIdsNotInUse(toCreate);
            ValidateCapacities(apiResources);
            ValidateCapabilities(apiResources);
            ValidateResourceProperties(apiResources);
            ValidateNames(apiResources);
            ValidateConcurrency(apiResources);
            ValidateConnectionManagement(apiResources);

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
            ActivityHelper.Track(nameof(DomResourceHandler), nameof(ValidateDomNames), act => ValidateDomNames(resourcesToCreate.Concat(toUpdateNameValidation).ToList()));

            var toCreatePoolValidation = resourcesToCreate.Where(IsValid);
            var toUpdatePoolValidation = resourcesToUpdate.Where(x => changeResults.Any(y => y.Instance.ID.Id == x.Id && y.ChangedFields.Select(z => z.FieldDescriptorId).Contains(SlcResource_StudioIds.Sections.ResourceInternalProperties.Pool_Ids.Id)));
            ValidatePoolAssignments(toCreatePoolValidation.Concat(toUpdatePoolValidation).ToList());

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

            var resourcesWithCapacityChanges = resourcesToUpdate.Where(x =>
                IsValid(x)
                && x.State != ResourceState.Deprecated
                && changeResults.Any(y => y.Instance.ID.Id == x.Id
                && (y.AddedSections.Select(z => z.SectionDefinitionId).Contains(SlcResource_StudioIds.Sections.ResourceCapacities.Id.Id)
                        || y.RemovedSections.Select(z => z.SectionDefinitionId).Contains(SlcResource_StudioIds.Sections.ResourceCapacities.Id.Id)
                        || y.ChangedFields.Select(z => z.SectionDefinitionId).Contains(SlcResource_StudioIds.Sections.ResourceCapacities.Id.Id))));

            foreach (var resource in resourcesWithCapacityChanges)
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

            CreateOrUpdateDomResources(toCreateDomInstances.Concat(toUpdateDomInstances).ToList());
        }

        private void CreateOrUpdateDomResources(ICollection<DomResource> domResources)
        {
            if (domResources == null)
            {
                throw new ArgumentNullException(nameof(domResources));
            }

            if (domResources.Count == 0)
            {
                return;
            }

            var domResourcesById = domResources.ToDictionary(x => x.ID.Id);

            if (resourceIdsWithCoreChanges.Count != 0)
            {
                var domResourcesWithCoreChanges = domResources.Where(x => resourceIdsWithCoreChanges.Contains(x.ID.Id)).ToList();
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

        private void TransitionToDeprecated(ICollection<Resource> apiResources)
        {
            if (apiResources == null)
            {
                throw new ArgumentNullException(nameof(apiResources));
            }

            if (apiResources.Count == 0)
            {
                return;
            }

            // Todo: add checks to see if resource is in use by jobs, etc.
            ValidateStateForDeprecateAction(apiResources);

            // Update CORE resources
            var resourcesToDeprecate = apiResources.Where(x => !TraceDataPerItem.Keys.Contains(x.Id)).ToList();

            CoreResourceHandler.TryDeprecate(planApi, resourcesToDeprecate.Select(x => x.OriginalInstance).ToList(), out var coreResult);

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

        private void Delete(ICollection<Resource> apiResources)
        {
            if (apiResources == null)
            {
                throw new ArgumentNullException(nameof(apiResources));
            }

            if (apiResources.Count == 0)
            {
                return;
            }

            var newResources = apiResources.Where(x => x.IsNew).ToList();
            newResources.ForEach(x =>
            {
                var error = new ResourceInvalidStateError
                {
                    ErrorMessage = $"A resource that was not saved cannot be removed.",
                    Id = x.Id,
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
            CoreResourceHandler.TryDelete(planApi, resources.Select(x => x.OriginalInstance).ToList(), out var coreResult);

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

        private void UpdateCaches(ICollection<DomResource> domResources)
        {
            if (domResources == null)
            {
                throw new ArgumentNullException(nameof(domResources));
            }

            if (domResources.Count == 0)
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

        private void ValidateIdsNotInUse(ICollection<Resource> apiResources)
        {
            if (apiResources == null)
            {
                throw new ArgumentNullException(nameof(apiResources));
            }

            if (apiResources.Count == 0)
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
                var error = new ResourceDuplicateIdError
                {
                    ErrorMessage = $"Resource '{resource.Name}' has a duplicate ID.",
                    Id = resource.Id,
                };

                ReportError(resource.Id, error);

                resourcesRequiringValidation.Remove(resource);
            }

            foreach (var foundInstance in planApi.DomHelpers.SlcResourceStudioHelper.GetResourceStudioInstances(resourcesRequiringValidation.Select(x => x.Id)))
            {
                planApi.Logger.LogInformation($"ID is already in use by a Resource Studio instance.", foundInstance.ID.Id);

                var error = new ResourceIdInUseError
                {
                    ErrorMessage = "ID is already in use.",
                    Id = foundInstance.ID.Id,
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

        private void ValidateStateForDeprecateAction(ICollection<Resource> apiResources)
        {
            if (apiResources == null)
            {
                throw new ArgumentNullException(nameof(apiResources));
            }

            if (apiResources.Count == 0)
            {
                return;
            }

            foreach (var resource in apiResources.Where(x => x.State != ResourceState.Complete))
            {
                var error = new ResourceInvalidStateError
                {
                    ErrorMessage = "Not allowed to deprecate a resource that is not in Completed state.",
                    Id = resource.Id,
                };
                ReportError(resource.Id, error);
            }
        }

        private void ValidateStateForDeleteAction(ICollection<Resource> apiResources)
        {
            if (apiResources == null)
            {
                throw new ArgumentNullException(nameof(apiResources));
            }

            if (apiResources.Count == 0)
            {
                return;
            }

            foreach (var resource in apiResources.Where(x => !new[] { ResourceState.Draft, ResourceState.Deprecated }.Contains(x.State)))
            {
                var error = new ResourceInvalidStateError
                {
                    ErrorMessage = "Not allowed to delete a resource that is not in Draft or Deprecated state.",
                    Id = resource.Id,
                };
                ReportError(resource.Id, error);
            }
        }

        private void ValidateNames(ICollection<Resource> apiResources)
        {
            if (apiResources == null)
            {
                throw new ArgumentNullException(nameof(apiResources));
            }

            if (apiResources.Count == 0)
            {
                return;
            }

            var resourcesRequiringValidation = apiResources.ToList();

            foreach (var resource in resourcesRequiringValidation.Where(x => !InputValidator.IsNonEmptyText(x.Name)).ToArray())
            {
                var error = new ResourceInvalidNameError
                {
                    ErrorMessage = "Name cannot be empty.",
                    Id = resource.Id,
                };

                ReportError(resource.Id, error);

                resourcesRequiringValidation.Remove(resource);
            }

            foreach (var resource in resourcesRequiringValidation.Where(x => !InputValidator.HasValidTextLength(x.Name)).ToArray())
            {
                var error = new ResourceInvalidNameError
                {
                    ErrorMessage = $"Name exceeds maximum length of {InputValidator.DefaultMaxTextLength} characters.",
                    Id = resource.Id,
                    Name = resource.Name,
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
                var error = new ResourceDuplicateNameError
                {
                    ErrorMessage = $"Resource '{resource.Name}' has a duplicate name.",
                    Id = resource.Id,
                    Name = resource.Name,
                };

                ReportError(resource.Id, error);
            }
        }

        private void ValidateDomNames(ICollection<Resource> apiResources)
        {
            if (apiResources == null)
            {
                throw new ArgumentNullException(nameof(apiResources));
            }

            if (apiResources.Count == 0)
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

                var error = new ResourceNameExistsError
                {
                    ErrorMessage = "Name is already in use.",
                    Id = resource.Id,
                    Name = resource.Name,
                };

                ReportError(resource.Id, error);
            }
        }

        private void ValidateConcurrency(ICollection<Resource> apiResources)
        {
            if (apiResources == null)
            {
                throw new ArgumentNullException(nameof(apiResources));
            }

            foreach (var apiResource in apiResources)
            {
                if (apiResource.Concurrency < 1)
                {
                    var error = new ResourceInvalidConcurrencyError
                    {
                        ErrorMessage = "Concurrency must be greater than or equal to 1.",
                        Id = apiResource.Id,
                    };

                    ReportError(apiResource.Id, error);
                }
            }
        }

        private void ValidatePoolAssignments(ICollection<Resource> apiResources)
        {
            if (apiResources == null)
            {
                throw new ArgumentNullException(nameof(apiResources));
            }

            if (apiResources.Count == 0)
            {
                return;
            }

            var poolIds = apiResources
                .SelectMany(x => x.ResourcePoolIds)
                .Distinct()
                .ToList();
            resourcePoolsById = planApi.ResourcePools.Read(poolIds).ToDictionary(x => x.Id);

            foreach (var resource in apiResources)
            {
                var hasError = false;

                foreach (var poolId in resource.ResourcePoolIds)
                {
                    if (!resourcePoolsById.TryGetValue(poolId, out _))
                    {
                        var error = new ResourceInvalidAssignedPoolError
                        {
                            ErrorMessage = $"Resource Pool with ID '{poolId}' not found.",
                            Id = resource.Id,
                            ResourcePoolId = poolId,
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

        private void ValidateCapacities(ICollection<Resource> apiResources)
        {
            if (apiResources == null)
            {
                throw new ArgumentNullException(nameof(apiResources));
            }

            if (apiResources.Count == 0)
            {
                return;
            }

            var capacityIds = apiResources
                .SelectMany(x => x.Capacities)
                .Select(x => x.Id)
                .Distinct()
                .ToList();
            var capacitiesById = planApi.Capacities.Read(capacityIds).ToDictionary(x => x.Id);

            foreach (var resource in apiResources)
            {
                var duplicateSettings = resource.Capacities
                    .GroupBy(x => x.Id)
                    .Where(g => g.Count() > 1)
                    .ToDictionary(x => x.Key, x => x.Count());

                foreach (var kvp in duplicateSettings)
                {
                    var error = new ResourceInvalidCapacitySettingsError
                    {
                        Id = resource.Id,
                        CapacityId = kvp.Key,
                        ErrorMessage = $"Capacity with ID '{kvp.Key}' is defined {kvp.Value} times. Duplicate capacity settings are not allowed.",
                    };

                    ReportError(resource.Id, error);
                }

                if (duplicateSettings.Count > 0)
                {
                    continue;
                }

                foreach (var capacitySetting in resource.Capacities)
                {
                    if (capacitySetting.Id == Guid.Empty)
                    {
                        var error = new ResourceInvalidCapacitySettingsError
                        {
                            ErrorMessage = "Capacity ID cannot be empty.",
                        };

                        ReportError(resource.Id, error);
                        continue;
                    }

                    if (!capacitiesById.TryGetValue(capacitySetting.Id, out var capacity))
                    {
                        var error = new ResourceInvalidCapacitySettingsError
                        {
                            ErrorMessage = $"Capacity with ID '{capacitySetting.Id}' not found.",
                            CapacityId = capacitySetting.Id,
                        };

                        ReportError(resource.Id, error);
                        continue;
                    }

                    PassTraceData(ResourceCapacitySettingValidator.Validate(resource.Id, capacity, capacitySetting));
                }
            }
        }

        private void ValidateCapabilities(ICollection<Resource> apiResources)
        {
            if (apiResources == null)
            {
                throw new ArgumentNullException(nameof(apiResources));
            }

            if (apiResources.Count == 0)
            {
                return;
            }

            var capabilityIds = apiResources
                .SelectMany(x => x.Capabilities)
                .Select(x => x.Id)
                .Distinct()
                .ToList();
            var capabilitiesById = planApi.Capabilities.Read(capabilityIds).ToDictionary(x => x.Id);

            foreach (var resource in apiResources)
            {
                var duplicateSettings = resource.Capabilities
                    .GroupBy(x => x.Id)
                    .Where(g => g.Count() > 1)
                    .ToDictionary(x => x.Key, x => x.Count());

                foreach (var kvp in duplicateSettings)
                {
                    var error = new ResourceInvalidCapabilitySettingsError
                    {
                        Id = resource.Id,
                        CapabilityId = kvp.Key,
                        ErrorMessage = $"Capability with ID '{kvp.Key}' is defined {kvp.Value} times. Duplicate capability settings are not allowed.",
                    };

                    ReportError(resource.Id, error);
                }

                if (duplicateSettings.Count > 0)
                {
                    continue;
                }

                foreach (var capabilitySetting in resource.Capabilities)
                {
                    if (capabilitySetting.Id == Guid.Empty)
                    {
                        var error = new ResourceInvalidCapabilitySettingsError
                        {
                            ErrorMessage = "Capability ID cannot be empty.",
                        };

                        ReportError(resource.Id, error);
                        continue;
                    }

                    if (!capabilitiesById.TryGetValue(capabilitySetting.Id, out var capability))
                    {
                        var error = new ResourceInvalidCapabilitySettingsError
                        {
                            ErrorMessage = $"Capability with ID '{capabilitySetting.Id}' not found.",
                            CapabilityId = capabilitySetting.Id,
                        };

                        ReportError(resource.Id, error);
                        continue;
                    }

                    if (capabilitySetting.Discretes.Count == 0)
                    {
                        var error = new ResourceInvalidCapabilitySettingsError
                        {
                            ErrorMessage = "At least one discrete value must be specified for the capability.",
                            CapabilityId = capabilitySetting.Id,
                        };

                        ReportError(resource.Id, error);
                        continue;
                    }

                    foreach (var discreteValue in capabilitySetting.Discretes)
                    {
                        if (!capability.Discretes.Contains(discreteValue))
                        {
                            var error = new ResourceInvalidCapabilitySettingsError
                            {
                                ErrorMessage = $"Discrete value '{discreteValue}' is not valid for capability '{capability.Name}'.",
                                CapabilityId = capabilitySetting.Id,
                            };

                            ReportError(resource.Id, error);
                        }
                    }
                }
            }
        }

        private void ValidateResourceProperties(ICollection<Resource> apiResources)
        {
            if (apiResources == null)
            {
                throw new ArgumentNullException(nameof(apiResources));
            }

            if (apiResources.Count == 0)
            {
                return;
            }

            var propertyIds = apiResources
                .SelectMany(x => x.Properties)
                .Select(x => x.Id)
                .Distinct()
                .ToList();
            var propertiesById = planApi.ResourceProperties.Read(propertyIds).ToDictionary(x => x.Id);

            foreach (var resource in apiResources)
            {
                var duplicateSettings = resource.Properties
                    .GroupBy(x => x.Id)
                    .Where(g => g.Count() > 1)
                    .ToDictionary(x => x.Key, x => x.Count());

                foreach (var kvp in duplicateSettings)
                {
                    var error = new ResourceInvalidPropertySettingsError
                    {
                        Id = resource.Id,
                        PropertyId = kvp.Key,
                        ErrorMessage = $"Property with ID '{kvp.Key}' is defined {kvp.Value} times. Duplicate property settings are not allowed.",
                    };

                    ReportError(resource.Id, error);
                }

                if (duplicateSettings.Count > 0)
                {
                    continue;
                }

                foreach (var propertySetting in resource.Properties)
                {
                    if (propertySetting.Id == Guid.Empty)
                    {
                        var error = new ResourceInvalidPropertySettingsError
                        {
                            Id = resource.Id,
                            PropertyId = propertySetting.Id,
                            ErrorMessage = "Property ID cannot be empty.",
                        };

                        ReportError(resource.Id, error);
                        continue;
                    }

                    if (!propertiesById.TryGetValue(propertySetting.Id, out _))
                    {
                        var error = new ResourceInvalidPropertySettingsError
                        {
                            Id = resource.Id,
                            PropertyId = propertySetting.Id,
                            ErrorMessage = $"Property with ID '{propertySetting.Id}' not found.",
                        };

                        ReportError(resource.Id, error);
                        continue;
                    }

                    if (!InputValidator.HasValidTextLength(propertySetting.Value))
                    {
                        var error = new ResourceInvalidPropertySettingsError
                        {
                            Id = resource.Id,
                            PropertyId = propertySetting.Id,
                            ErrorMessage = $"Property value length is limited to {InputValidator.DefaultMaxTextLength} characters.",
                        };

                        ReportError(resource.Id, error);
                    }
                }
            }
        }

        private void ValidateConnectionManagement(ICollection<Resource> apiResources)
        {

            if (apiResources == null)
            {
                throw new ArgumentNullException(nameof(apiResources));
            }

            if (apiResources.Count == 0)
            {
                return;
            }

            var virtualSignalGroupIds = apiResources
                .SelectMany(x => new[] { x.VirtualSignalGroupInputId, x.VirtualSignalGroupOutputId })
                .Where(x => x != Guid.Empty)
                .Distinct()
                .ToList();

            var virtualSignalGroupsById = planApi.LiveApi.VirtualSignalGroups.Read(virtualSignalGroupIds);

            foreach (var resource in apiResources)
            {
                if (resource.VirtualSignalGroupInputId != Guid.Empty && !virtualSignalGroupsById.TryGetValue(resource.VirtualSignalGroupInputId, out _))
                {
                    var error = new ResourceInvalidVirtualSignalGroupError
                    {
                        ErrorMessage = $"Virtual Signal Group with ID '{resource.VirtualSignalGroupInputId}' not found.",
                        Id = resource.Id,
                        VirtualSignalGroupId = resource.VirtualSignalGroupInputId,
                    };

                    ReportError(resource.Id, error);
                }

                if (resource.VirtualSignalGroupOutputId != Guid.Empty && !virtualSignalGroupsById.TryGetValue(resource.VirtualSignalGroupOutputId, out _))
                {
                    var error = new ResourceInvalidVirtualSignalGroupError
                    {
                        ErrorMessage = $"Virtual Signal Group with ID '{resource.VirtualSignalGroupOutputId}' not found.",
                        Id = resource.Id,
                        VirtualSignalGroupId = resource.VirtualSignalGroupOutputId,
                    };

                    ReportError(resource.Id, error);
                }
            }
        }

        private ICollection<DomChangeResults> GetResourcesWithChanges(ICollection<Resource> apiResources)
        {
            if (apiResources == null)
            {
                throw new ArgumentNullException(nameof(apiResources));
            }

            if (apiResources.Count == 0)
            {
                return [];
            }

            var resourcesRequiringValidation = apiResources.Where(x => !x.IsNew && x.HasChanges).ToList();
            if (resourcesRequiringValidation.Count == 0)
            {
                return Array.Empty<DomChangeResults>();
            }

            List<DomChangeResults> changeResults = new List<DomChangeResults>();
            var storedDomResourcesById = planApi.DomHelpers.SlcResourceStudioHelper.GetResources(resourcesRequiringValidation.Select(x => x.Id)).ToDictionary(x => x.ID.Id);
            foreach (var resource in resourcesRequiringValidation)
            {
                if (!storedDomResourcesById.TryGetValue(resource.Id, out var stored))
                {
                    var error = new ResourceNotFoundError
                    {
                        ErrorMessage = $"Resource with ID '{resource.Id}' no longer exists.",
                        Id = resource.Id,
                    };

                    ReportError(resource.Id, error);

                    continue;
                }

                var changeResult = DomChangeHandler.HandleChanges(resource.OriginalInstance, resource.GetInstanceWithChanges(), stored);
                if (changeResult.HasErrors)
                {
                    foreach (var errorDetails in changeResult.Errors)
                    {
                        var error = new ResourceValueAlreadyChangedError
                        {
                            ErrorMessage = errorDetails.Message,
                            Id = resource.Id,
                        };

                        ReportError(resource.Id, error);
                    }

                    continue;
                }

                changeResults.Add(changeResult);
            }

            return changeResults;
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
                ReportError(resource.Id, new ResourceNotFoundError
                {
                    ErrorMessage = $"Resource with ID '{resource.Id}' not found.",
                    Id = resource.Id,
                });

                return;
            }

            domResource.ResourceInfo.Type = SlcResource_StudioIds.Enums.Type.Unmanaged;
            domResource.ResourceInternalProperties.ResourceMetadata = String.Empty;

            CreateOrUpdateDomResources([domResource]);
        }

        private void ConvertToVirtualFunctionResource(Resource resource, ResourceVirtualFunctionLinkSetting setting)
        {
            if (!CoreResourceHandler.TryValidateVirtualFunctionConfiguration(planApi, setting, out var error))
            {
                error.Id = resource.Id;
                ReportError(resource.Id, error);
                return;
            }

            var domResource = planApi.DomHelpers.SlcResourceStudioHelper.GetResources([resource.Id]).FirstOrDefault();
            if (domResource == null)
            {
                ReportError(resource.Id, new ResourceNotFoundError
                {
                    ErrorMessage = $"Resource with ID '{resource.Id}' not found.",
                    Id = resource.Id,
                });

                return;
            }

            domResource.ResourceInfo.Type = SlcResource_StudioIds.Enums.Type.VirtualFunction;
            domResource.ResourceInternalProperties.Metadata = new ResourceMetadata
            {
                LinkedElementInfo = new DmsElementId(setting.AgentId, setting.ElementId).Value,
                LinkedFunctionId = setting.FunctionId,
                LinkedFunctionTableIndex = setting.FunctionTableIndex,
            };

            CreateOrUpdateDomResources([domResource]);
        }

        private void ConvertToServiceResource(Resource resource, ResourceServiceLinkSetting setting)
        {
            if (!CoreResourceHandler.TryValidateServiceConfiguration(planApi, setting, out var error))
            {
                error.Id = resource.Id;
                ReportError(resource.Id, error);
                return;
            }

            var domResource = planApi.DomHelpers.SlcResourceStudioHelper.GetResources([resource.Id]).FirstOrDefault();
            if (domResource == null)
            {
                ReportError(resource.Id, new ResourceNotFoundError
                {
                    ErrorMessage = $"Resource with ID '{resource.Id}' not found.",
                    Id = resource.Id,
                });

                return;
            }

            domResource.ResourceInfo.Type = SlcResource_StudioIds.Enums.Type.Service;
            domResource.ResourceInternalProperties.Metadata = new ResourceMetadata
            {
                LinkedServiceInfo = new DmsServiceId(setting.AgentId, setting.ServiceId).Value,
            };

            CreateOrUpdateDomResources([domResource]);
        }

        private void ConvertToElementResource(Resource resource, ResourceElementLinkSetting setting)
        {
            if (!CoreResourceHandler.TryValidateElementConfiguration(planApi, setting, out var error))
            {
                error.Id = resource.Id;
                ReportError(resource.Id, error);
                return;
            }

            var domResource = planApi.DomHelpers.SlcResourceStudioHelper.GetResources([resource.Id]).FirstOrDefault();
            if (domResource == null)
            {
                ReportError(resource.Id, new ResourceNotFoundError
                {
                    ErrorMessage = $"Resource with ID '{resource.Id}' not found.",
                    Id = resource.Id,
                });

                return;
            }

            domResource.ResourceInfo.Type = SlcResource_StudioIds.Enums.Type.Element;
            domResource.ResourceInternalProperties.Metadata = new ResourceMetadata
            {
                LinkedElementInfo = new DmsElementId(setting.AgentId, setting.ElementId).Value,
            };

            CreateOrUpdateDomResources([domResource]);
        }
    }
}
