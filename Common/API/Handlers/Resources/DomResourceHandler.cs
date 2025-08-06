namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.Logging;
    using Skyline.DataMiner.Core.DataMinerSystem.Common;
    using Skyline.DataMiner.MediaOps.Plan.ActivityHelper;
    using Skyline.DataMiner.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.MediaOps.Plan.Extensions;
    using Skyline.DataMiner.MediaOps.Plan.Storage.DOM;
    using Skyline.DataMiner.MediaOps.Plan.Storage.DOM.SlcResource_Studio;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Utils.DOM.Extensions;
    using DomResource = Storage.DOM.SlcResource_Studio.ResourceInstance;

    internal class DomResourceHandler : ApiObjectValidator<Guid>
    {
        private readonly MediaOpsPlanApi planApi;

        private readonly HashSet<Guid> resourceIdsWithCoreChanges = new HashSet<Guid>();

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

        internal static void TransitionToDeprecated(MediaOpsPlanApi planApi, Resource apiResource)
        {
            var handler = new DomResourceHandler(planApi);
            handler.TransitionToDeprecated(apiResource);
        }

        internal static long CountAll(MediaOpsPlanApi planApi)
        {
            var handler = new DomResourceHandler(planApi);
            return handler.CountAll();
        }

        private long CountAll()
        {
            return planApi.DomHelpers.SlcResourceStudioHelper.CountResourceStudioInstances(
                DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resource.Id));
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
            planApi.DomHelpers.SlcResourceStudioHelper.TransitionToComplete(apiResource.Id);
        }

        private void TransitionToDeprecated(Resource apiResource)
        {
            CoreResourceHandler.DeprecateResource(planApi, apiResource.OriginalInstance);
            planApi.DomHelpers.SlcResourceStudioHelper.TransitionToDeprecated(apiResource.Id);
        }

        private void ClearErrors(MediaOpsPlanApi planApi, Resource apiResource, ErrorDefinition errorDefinition)
        {
            var domResource = planApi.DomHelpers.SlcResourceStudioHelper.GetResources([apiResource.Id]).First();
            domResource.ClearError(errorDefinition.ErrorCode);
            CreateOrUpdate([domResource]);
            apiResource.UpdateInstance(domResource);
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

            var toCreate = new List<Resource>();
            var toUpdate = new List<Resource>();
            foreach (var resources in apiResources)
            {
                if (resources.IsNew)
                {
                    toCreate.Add(resources);
                }
                else
                {
                    toUpdate.Add(resources);
                }
            }

            ValidateIdsNotInUse(toCreate);
            ValidateState(toUpdate);

            // Todo: lock DOM instances
            var changeResults = ActivityHelper.Track(nameof(DomResourceHandler), nameof(GetResourcesWithChanges), act => GetResourcesWithChanges(toUpdate.Where(x => !TraceDataPerItem.Keys.Contains(x.Id))));

            var toCreateNameValidation = toCreate.Where(x => !TraceDataPerItem.Keys.Contains(x.Id));
            var toUpdateNameValidation = toUpdate.Where(x => changeResults.Any(y => y.Instance.ID.Id == x.Id && y.ChangedFieldDescriptorIds.Contains(SlcResource_StudioIds.Sections.ResourceInfo.Name.Id)));
            ActivityHelper.Track(nameof(DomResourceHandler), nameof(ValidateNames), act => ValidateNames(toCreateNameValidation.Concat(toUpdateNameValidation)));

            var toCreateDomInstances = toCreate
                .Where(x => !TraceDataPerItem.Keys.Contains(x.Id))
                .Select(x => x.GetInstanceWithChanges())
                .ToList();

            var toUpdateDomInstances = changeResults
                .Where(x => !TraceDataPerItem.Keys.Contains(x.Instance.ID.Id))
                .Select(x => new DomResource(x.Instance))
                .ToList();

            CreateOrUpdate(toCreateDomInstances.Concat(toUpdateDomInstances));

            var createdDomResources = planApi.DomHelpers.SlcResourceStudioHelper.GetResources(SuccessfulItems);
            foreach (var resource in apiResources.Where(x => SuccessfulItems.Contains(x.Id)))
            {
                resource.UpdateInstance(createdDomResources.Single(x => x.ID.Id.Equals(resource.Id)));
            }
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
                CoreResourceHandler.TryCreateOrUpdate(planApi, domResources.Where(x => resourceIdsWithCoreChanges.Contains(x.ID.Id)), out var coreResult);

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

            var invalidStateResources = apiResources.Where(x => x.State != ResourceState.Draft && x.State != ResourceState.Deprecated).ToList();
            invalidStateResources.ForEach(x =>
            {
                var error = new ResourceConfigurationError
                {
                    ErrorReason = ResourceConfigurationError.Reason.InvalidState,
                    ErrorMessage = $"A resource in State {x.State} cannot be removed.",
                };

                ReportError(x.Id, error);
            });

            apiResources = apiResources.Except(invalidStateResources).ToList();

            var resourcesToDelete = apiResources.ToDictionary(x => x.Id);
            CoreResourceHandler.TryDelete(planApi, resourcesToDelete.Values.Select(x => x.OriginalInstance), out var coreResult);

            foreach (var id in coreResult.UnsuccessfulIds)
            {
                ReportError(id);

                if (coreResult.TraceDataPerItem.TryGetValue(id, out var traceData))
                {
                    PassTraceData(id, traceData);
                }

                resourcesToDelete.Remove(id);
            }

            planApi.DomHelpers.SlcResourceStudioHelper.DomHelper.DomInstances.TryDeleteInBatches(resourcesToDelete.Values.Select(x => x.OriginalInstance.ToInstance()), out var domResult);

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

        private void ValidateState(IEnumerable<Resource> apiResources)
        {
            if (apiResources == null)
            {
                throw new ArgumentNullException(nameof(apiResources));
            }

            if (!apiResources.Any())
            {
                return;
            }

            foreach (var pool in apiResources.Where(x => x.State == ResourceState.Deprecated))
            {
                var error = new ResourceConfigurationError
                {
                    ErrorReason = ResourceConfigurationError.Reason.InvalidState,
                    ErrorMessage = "Not allowed to update a resource in Deprecated state."
                };

                ReportError(pool.Id, error);
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

                resourcesRequiringValidation.Remove(resource);
            }

            FilterElement<DomInstance> filter(string name) =>
                DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resource.Id)
                .AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInfo.Name).Equal(name)
                .AND(DomInstanceExposers.StatusId.NotEqual(SlcResource_StudioIds.Behaviors.Resourcepool_Behavior.Statuses.Draft)));

            var domResourcesbyName = planApi.DomHelpers.SlcResourceStudioHelper.GetResources(resourcesRequiringValidation.Select(x => x.Name), filter)
                .GroupBy(x => x.Name)
                .ToDictionary(x => x.Key, x => (IReadOnlyCollection<DomResource>)x.ToList());

            foreach (var resource in resourcesRequiringValidation)
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
                    foreach (var errorMessage in changeResult.Errors)
                    {
                        var error = new ResourceConfigurationError
                        {
                            ErrorReason = ResourceConfigurationError.Reason.ValueAlreadyChanged,
                            ErrorMessage = errorMessage
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
            domResource.ResourceInternalProperties.ResourceMetadata = new ResourceMetadata
            {
                LinkedElementInfo = new DmsElementId(configuration.AgentId, configuration.ElementId).Value,
                LinkedFunctionId = configuration.FunctionId,
                LinkedFunctionTableIndex = configuration.FunctionTableIndex,
            }.Serialize();

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
            domResource.ResourceInternalProperties.ResourceMetadata = new ResourceMetadata
            {
                LinkedServiceInfo = new DmsServiceId(configuration.AgentId, configuration.ServiceId).Value,
            }.Serialize();

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
            domResource.ResourceInternalProperties.ResourceMetadata = new ResourceMetadata
            {
                LinkedElementInfo = new DmsElementId(configuration.AgentId, configuration.ElementId).Value,
            }.Serialize();

            CreateOrUpdate([domResource]);
        }
    }
}
