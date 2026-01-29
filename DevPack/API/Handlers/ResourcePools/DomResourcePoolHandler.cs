namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.API.Handlers.Orchestration;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;
    using Skyline.DataMiner.Utils.DOM.Extensions;

    using DomResourcePool = Storage.DOM.SlcResource_Studio.ResourcepoolInstance;

    internal class DomResourcePoolHandler : ApiObjectValidator
    {
        private readonly MediaOpsPlanApi planApi;

        private readonly HashSet<Guid> poolIdsWithCoreChanges = new HashSet<Guid>();

        private readonly HashSet<ResourcePool> referencedApiResourcePoolsToUpdate = new HashSet<ResourcePool>();

        private DomResourcePoolHandler(MediaOpsPlanApi planApi)
        {
            this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
        }

        internal static bool TryCreateOrUpdate(MediaOpsPlanApi planApi, ICollection<ResourcePool> apiResourcePools, out BulkOperationResult<Guid> result)
        {
            var handler = new DomResourcePoolHandler(planApi);
            handler.CreateOrUpdate(apiResourcePools);

            result = new BulkOperationResult<Guid>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

            return !result.HasFailures;
        }

        internal static bool TryComplete(MediaOpsPlanApi planApi, ICollection<ResourcePool> apiResourcePools, out BulkOperationResult<Guid> result)
        {
            var handler = new DomResourcePoolHandler(planApi);
            handler.TransitionToCompleted(apiResourcePools);

            result = new BulkOperationResult<Guid>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

            return !result.HasFailures;
        }

        internal static bool TryDeprecate(MediaOpsPlanApi planApi, ICollection<ResourcePool> apiResourcePools, out BulkOperationResult<Guid> result, ResourcePoolDeprecateOptions options = null)
        {
            var handler = new DomResourcePoolHandler(planApi);
            handler.TransitionToDeprecated(apiResourcePools, options ?? ResourcePoolDeprecateOptions.GetDefaults());

            result = new BulkOperationResult<Guid>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

            return !result.HasFailures;
        }

        internal static bool TryDelete(MediaOpsPlanApi planApi, ICollection<ResourcePool> apiResourcePools, out BulkOperationResult<Guid> result, ResourcePoolDeleteOptions options = null)
        {
            var handler = new DomResourcePoolHandler(planApi);
            handler.Delete(apiResourcePools, options ?? ResourcePoolDeleteOptions.GetDefaults());

            result = new BulkOperationResult<Guid>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

            return !result.HasFailures;
        }

        private void CreateOrUpdate(ICollection<ResourcePool> apiResourcePools)
        {
            if (apiResourcePools == null)
            {
                throw new ArgumentNullException(nameof(apiResourcePools));
            }

            if (apiResourcePools.Count == 0)
            {
                return;
            }

            var toCreate = apiResourcePools.Where(x => x.IsNew).ToList();
            var toUpdate = apiResourcePools.Except(toCreate).ToList();

            ValidateIdsNotInUse(toCreate);
            ValidateStateForUpdateAction(toUpdate);

            ValidatePoolLinks(apiResourcePools);
            ValidateCapabilities(apiResourcePools);
            ValidateNames(apiResourcePools);

            // Create or update DOM resource pools
            var validResourcePools = apiResourcePools.Where(IsValid).ToList();
            var resourcePoolchangeResults = planApi.LockManager.LockAndExecute(validResourcePools, CreateOrUpdateCoreResourcePools);
            ReportError(resourcePoolchangeResults);

            // Update resource pool capabilities if needed
            var poolsToUpdateWithCapabilities = toUpdate.Where(x =>
            IsValid(x)
            && x.State != ResourcePoolState.Draft
            && resourcePoolchangeResults.ActionResults.Any(y => y.Instance.ID.Id == x.Id
                    && (y.AddedSections.Select(z => z.SectionDefinitionId).Contains(SlcResource_StudioIds.Sections.ResourcePoolCapabilities.Id.Id)
                        || y.RemovedSections.Select(z => z.SectionDefinitionId).Contains(SlcResource_StudioIds.Sections.ResourcePoolCapabilities.Id.Id)
                        || y.ChangedFields.Select(z => z.SectionDefinitionId).Contains(SlcResource_StudioIds.Sections.ResourcePoolCapabilities.Id.Id)))).ToList();

            var lockResult = planApi.LockManager.LockAndExecute(poolsToUpdateWithCapabilities, UpdateCoreResources);
            ReportError(lockResult);
        }

        private ICollection<DomChangeResults> CreateOrUpdateCoreResourcePools(ICollection<ResourcePool> resourcePools)
        {
            if (resourcePools == null)
            {
                throw new ArgumentNullException(nameof(resourcePools));
            }

            if (resourcePools.Any(x => !IsValid(x)))
            {
                throw new ArgumentException($"Not all provided resource pools are valid", nameof(resourcePools));
            }

            var resourcePoolsToCreate = resourcePools.Where(x => x.IsNew).ToList();
            var resourcePoolsToUpdate = resourcePools.Except(resourcePoolsToCreate).ToList();

            var changeResults = GetPoolsWithChanges(resourcePoolsToUpdate);

            var toUpdateNameValidation = resourcePoolsToUpdate.Where(x => changeResults.Any(y => y.Instance.ID.Id == x.Id && y.ChangedFields.Select(z => z.FieldDescriptorId).Contains(SlcResource_StudioIds.Sections.ResourcePoolInfo.Name.Id))).ToList();
            ValidateDomNames(resourcePoolsToCreate.Concat(toUpdateNameValidation).ToList());

            ValidateCategories(resourcePools.Where(IsValid).ToList());

            CreateOrUpdateOrchestrationSettings(resourcePools.Where(IsValid).ToList());

            var toCreateDomInstances = resourcePoolsToCreate
                .Where(IsValid)
                .Select(x => x.GetInstanceWithChanges())
                .ToList();

            var toUpdateDomInstances = changeResults
                .Where(IsValid)
                .Select(x => new DomResourcePool(x.Instance))
                .ToList();

            CreateOrUpdateDomResourcePools(toCreateDomInstances.Concat(toUpdateDomInstances).ToList());
            return changeResults;
        }

        private void CreateOrUpdateOrchestrationSettings(ICollection<ResourcePool> resourcePools)
        {
            if (resourcePools == null)
            {
                throw new ArgumentNullException(nameof(resourcePools));
            }

            if (resourcePools.Any(x => !IsValid(x)))
            {
                throw new ArgumentException($"Not all provided resource pools are valid", nameof(resourcePools));
            }

            var resourcePoolIdByOrchestrationSettingsId = resourcePools.ToDictionary(x => x.OrchestrationSettings.Id, x => x.Id);

            DomOrchestrationSettingsHandler.TryCreateOrUpdate(planApi, resourcePools.Select(x => x.OrchestrationSettings).ToList(), out var domResult);

            foreach (var id in domResult.UnsuccessfulIds)
            {
                if (!resourcePoolIdByOrchestrationSettingsId.TryGetValue(id, out var resourcePoolId))
                {
                    planApi.Logger.Error(this, $"Failed to find resource pool ID for orchestration settings ID", [id]);
                    continue;
                }

                ReportError(resourcePoolId);

                if (domResult.TraceDataPerItem.TryGetValue(id, out var traceData))
                {
                    PassTraceData(resourcePoolId, traceData);
                }
            }
        }

        private void CreateOrUpdateDomResourcePools(ICollection<DomResourcePool> domResourcePools)
        {
            if (domResourcePools == null)
            {
                throw new ArgumentNullException(nameof(domResourcePools));
            }

            if (domResourcePools.Count == 0)
            {
                return;
            }

            var domPoolsById = domResourcePools.ToDictionary(x => x.ID.Id);

            if (poolIdsWithCoreChanges.Count != 0)
            {
                CoreResourcePoolHandler.TryCreateOrUpdate(planApi, domResourcePools.Where(x => poolIdsWithCoreChanges.Contains(x.ID.Id)).ToList(), out var coreResult);

                foreach (var id in coreResult.UnsuccessfulIds)
                {
                    ReportError(id);

                    if (coreResult.TraceDataPerItem.TryGetValue(id, out var traceData))
                    {
                        PassTraceData(id, traceData);
                    }

                    domPoolsById.Remove(id);
                }
            }

            planApi.DomHelpers.SlcResourceStudioHelper.DomHelper.DomInstances.TryCreateOrUpdateInBatches(domPoolsById.Values.Select(x => x.ToInstance()), out var domResult);

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

            ReportSuccess(domResult.SuccessfulIds.Select(x => x.Id));
        }

        private void TransitionToCompleted(ICollection<ResourcePool> apiResourcePools)
        {
            if (apiResourcePools == null)
            {
                throw new ArgumentNullException(nameof(apiResourcePools));
            }

            if (apiResourcePools.Count == 0)
            {
                return;
            }

            ValidateStateForCompleteAction(apiResourcePools);

            // Create CORE resource pools
            var poolsToCreate = apiResourcePools
                .Where(x => !TraceDataPerItem.Keys.Contains(x.Id))
                .Select(x => x.OriginalInstance)
                .ToList();
            if (poolsToCreate.Count == 0)
            {
                return;
            }

            CoreResourcePoolHandler.TryCreateOrUpdate(planApi, poolsToCreate, out var coreResult);
            foreach (var id in coreResult.UnsuccessfulIds)
            {
                ReportError(id);

                if (coreResult.TraceDataPerItem.TryGetValue(id, out var traceData))
                {
                    PassTraceData(id, traceData);
                }
            }

            // Save link with CORE resource pools
            var poolsToSave = poolsToCreate.Where(x => coreResult.SuccessfulIds.Contains(x.ID.Id)).ToList();
            planApi.DomHelpers.SlcResourceStudioHelper.DomHelper.DomInstances.TryCreateOrUpdateInBatches(poolsToSave.Select(x => x.ToInstance()), out var domResult);
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

            // Transition DOM resource pools to Complete state
            foreach (var domInstanceId in domResult.SuccessfulIds)
            {
                try
                {
                    planApi.DomHelpers.SlcResourceStudioHelper.DomHelper.DomInstances.DoStatusTransition(domInstanceId, SlcResource_StudioIds.Behaviors.Resourcepool_Behavior.Transitions.Draft_To_Complete);

                    ReportSuccess(domInstanceId.Id);
                }
                catch (Exception ex)
                {
                    ReportError(domInstanceId.Id, new MediaOpsErrorData() { ErrorMessage = ex.ToString() });
                }
            }

            // Update CORE resources
            var toUpdateCoreResources = apiResourcePools
                .Where(x => !TraceDataPerItem.Keys.Contains(x.Id))
                .ToList();
            UpdateCoreResources(toUpdateCoreResources);
        }

        private void TransitionToDeprecated(ICollection<ResourcePool> apiResourcePools, ResourcePoolDeprecateOptions options)
        {
            if (apiResourcePools == null)
            {
                throw new ArgumentNullException(nameof(apiResourcePools));
            }

            if (apiResourcePools.Count == 0)
            {
                return;
            }

            ValidateStateForDeprecateAction(apiResourcePools);
            ValidateWorkflowUsage(apiResourcePools.Where(IsValid).ToArray());

            var poolsToDeprecate = apiResourcePools.Where(IsValid).ToList();
            if (options.AllowResourceDeprecation)
            {
                DeprecatePoolResources(poolsToDeprecate);
            }

            foreach (var pool in poolsToDeprecate)
            {
                try
                {
                    planApi.DomHelpers.SlcResourceStudioHelper.DomHelper.DomInstances.DoStatusTransition(pool.OriginalInstance.ID, SlcResource_StudioIds.Behaviors.Resourcepool_Behavior.Transitions.Complete_To_Deprecated);

                    ReportSuccess(pool.Id);
                }
                catch (Exception ex)
                {
                    ReportError(pool.Id, new MediaOpsErrorData() { ErrorMessage = ex.ToString() });
                }
            }
        }

        private void Delete(ICollection<ResourcePool> apiResourcePools, ResourcePoolDeleteOptions options)
        {
            if (apiResourcePools == null)
            {
                throw new ArgumentNullException(nameof(apiResourcePools));
            }

            if (apiResourcePools.Count == 0)
            {
                return;
            }

            ValidateStateForDeleteAction(apiResourcePools);

            var poolsToDelete = apiResourcePools.Where(IsValid).ToList();
            RemovePoolFromParentPoolLinks(poolsToDelete);
            HandleDeleteOptions(poolsToDelete, options);
            UnassignResourcesFromPool(poolsToDelete);

            var deleteCorePoolsLockResult = planApi.LockManager.LockAndExecute(poolsToDelete, DeleteCoreResourcePools); // Returns pools that require updates after referenced pools have been removed.
            ReportError(deleteCorePoolsLockResult);

            planApi.ResourcePools.Update(deleteCorePoolsLockResult.ActionResults);
        }

        private ICollection<ResourcePool> DeleteCoreResourcePools(ICollection<ResourcePool> poolsToDelete)
        {
            DeleteOrchestrationSettings(poolsToDelete.Where(IsValid).ToList());

            var domPoolsById = poolsToDelete.ToDictionary(x => x.Id, x => x.OriginalInstance);

            CoreResourcePoolHandler.TryDelete(planApi, domPoolsById.Values, out var coreResult);

            foreach (var id in coreResult.UnsuccessfulIds)
            {
                ReportError(id);

                if (coreResult.TraceDataPerItem.TryGetValue(id, out var traceData))
                {
                    PassTraceData(id, traceData);
                }

                domPoolsById.Remove(id);
            }

            planApi.DomHelpers.SlcResourceStudioHelper.DomHelper.DomInstances.TryDeleteInBatches(domPoolsById.Values.Select(x => x.ToInstance()), out var domResult);

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

            ReportSuccess(domResult.SuccessfulIds.Select(x => x.Id));

            // Return affected pools that require updates
            return referencedApiResourcePoolsToUpdate.Where(x => !domResult.SuccessfulIds.Select(y => y.Id).Contains(x.Id)).ToList();
        }

        private void DeleteOrchestrationSettings(ICollection<ResourcePool> resourcePools)
        {
            if (resourcePools == null)
            {
                throw new ArgumentNullException(nameof(resourcePools));
            }

            if (resourcePools.Any(x => !IsValid(x)))
            {
                throw new ArgumentException($"Not all provided resource pools are valid", nameof(resourcePools));
            }

            DomOrchestrationSettingsHandler.TryDelete(planApi, resourcePools.Select(x => x.OrchestrationSettings).ToList(), out _);
        }

        private void DeprecatePoolResources(ICollection<ResourcePool> apiResourcePools)
        {
            var resourcesPerPoolCollection = planApi.Resources.GetResourcesPerPool(apiResourcePools, ResourceState.Complete);
            var poolsPerResourceCollection = planApi.ResourcePools.GetPoolsPerResource(resourcesPerPoolCollection.Values.SelectMany(x => x).Distinct(new DefaultApiObjectComparer()).Cast<Resource>());

            var resourcesToDeprecate = new List<Resource>();
            foreach (var resourcesPerPool in resourcesPerPoolCollection)
            {
                var poolResourcesToDeprecate = resourcesPerPoolCollection.Values
                    .SelectMany(x => x)
                    .Distinct(new DefaultApiObjectComparer())
                    .Cast<Resource>()
                    .Where(x => poolsPerResourceCollection.TryGetValue(x, out var pools) && pools.Count() == 1 && pools.First().Id == resourcesPerPool.Key.Id)
                    .ToList();

                resourcesToDeprecate.AddRange(poolResourcesToDeprecate);
            }

            try
            {
                planApi.Resources.Deprecate(resourcesToDeprecate);
            }
            catch (MediaOpsBulkException<Guid> ex)
            {
                foreach (var traceData in ex.Result.TraceDataPerItem)
                {
                    foreach (var error in traceData.Value.ErrorData)
                    {
                        ReportError(traceData.Key, error);
                    }
                }
            }
        }

        private void RemovePoolFromParentPoolLinks(ICollection<ResourcePool> apiResourcePools)
        {
            var parentLinksPerPoolCollection = planApi.ResourcePools.GetParentPoolLinks(apiResourcePools);

            foreach (var pool in apiResourcePools)
            {
                if (parentLinksPerPoolCollection.TryGetValue(pool, out var parentPools))
                {
                    foreach (var parentPool in parentPools)
                    {
                        parentPool.RemoveLinkedResourcePool(pool);

                        referencedApiResourcePoolsToUpdate.Add(parentPool);
                    }
                }
            }
        }

        private void HandleDeleteOptions(ICollection<ResourcePool> apiResourcePools, ResourcePoolDeleteOptions options)
        {
            if (options.DeleteDraftResources)
            {
                DeletePoolResources(apiResourcePools, ResourceState.Draft);
            }

            if (options.DeleteDeprecatedResources)
            {
                DeletePoolResources(apiResourcePools, ResourceState.Deprecated);
            }
        }

        private void DeletePoolResources(ICollection<ResourcePool> apiResourcePools, ResourceState state)
        {
            var resourcesPerPoolCollection = planApi.Resources.GetResourcesPerPool(apiResourcePools, state);
            var poolsPerResourceCollection = planApi.ResourcePools.GetPoolsPerResource(resourcesPerPoolCollection.Values.SelectMany(x => x).Distinct(new DefaultApiObjectComparer()).Cast<Resource>());

            var resourcesToDelete = new List<Resource>();
            foreach (var resourcesPerPool in resourcesPerPoolCollection)
            {
                var poolResourcesToDelete = resourcesPerPoolCollection.Values
                    .SelectMany(x => x)
                    .Distinct(new DefaultApiObjectComparer())
                    .Cast<Resource>()
                    .Where(x => poolsPerResourceCollection.TryGetValue(x, out var pools) && pools.Count() == 1 && pools.First().Id == resourcesPerPool.Key.Id)
                    .ToList();

                resourcesToDelete.AddRange(poolResourcesToDelete);
            }

            planApi.Resources.Delete(resourcesToDelete.Select(x => x.Id).ToArray());
        }

        private void UnassignResourcesFromPool(ICollection<ResourcePool> apiResourcePools)
        {
            // todo: implement logic to unassign resources from the given resource pools
        }

        private void UpdateCoreResources(ICollection<ResourcePool> apiResourcePools)
        {
            if (apiResourcePools == null)
            {
                throw new ArgumentNullException(nameof(apiResourcePools));
            }

            if (apiResourcePools.Count == 0)
            {
                return;
            }

            var resources = planApi.Resources.GetResourcesPerPool(apiResourcePools, ResourceState.Complete)
                .Values
                .SelectMany(x => x)
                .Distinct(new DefaultApiObjectComparer())
                .Cast<Resource>();

            CoreResourceHandler.TryCreateOrUpdate(planApi, resources.Select(x => x.OriginalInstance).ToList(), out var coreResult);

            foreach (var traceData in coreResult.TraceDataPerItem)
            {
                foreach (var error in traceData.Value.ErrorData)
                {
                    ReportError(traceData.Key, error);
                }
            }
        }

        private void ValidateIdsNotInUse(ICollection<ResourcePool> apiResourcePools)
        {
            if (apiResourcePools == null)
            {
                throw new ArgumentNullException(nameof(apiResourcePools));
            }

            if (apiResourcePools.Count == 0)
            {
                return;
            }

            var poolsRequiringValidation = apiResourcePools.Where(x => x.IsNew && x.HasUserDefinedId).ToList();
            if (poolsRequiringValidation.Count == 0)
            {
                return;
            }

            var poolsWithDuplicateIds = poolsRequiringValidation
                .GroupBy(pool => pool.Id)
                .Where(g => g.Count() > 1)
                .SelectMany(x => x)
                .ToList();

            foreach (var pool in poolsWithDuplicateIds)
            {
                var error = new ResourcePoolDuplicateIdError
                {
                    ErrorMessage = $"Resource pool '{pool.Name}' has a duplicate ID.",
                    Id = pool.Id,
                };

                ReportError(pool.Id, error);

                poolsRequiringValidation.Remove(pool);
            }

            foreach (var foundInstance in planApi.DomHelpers.SlcResourceStudioHelper.GetResourceStudioInstances(poolsRequiringValidation.Select(x => x.Id)))
            {
                planApi.Logger.Information(this, $"ID is already in use by a Resource Studio instance.", [foundInstance.ID.Id]);

                var error = new ResourcePoolIdInUseError
                {
                    ErrorMessage = "ID is already in use.",
                    Id = foundInstance.ID.Id,
                };

                ReportError(foundInstance.ID.Id, error);
            }
        }

        private void ValidateStateForUpdateAction(ICollection<ResourcePool> apiResourcePools)
        {
            if (apiResourcePools == null)
            {
                throw new ArgumentNullException(nameof(apiResourcePools));
            }

            if (apiResourcePools.Count == 0)
            {
                return;
            }

            foreach (var pool in apiResourcePools.Where(x => x.State == ResourcePoolState.Deprecated))
            {
                var error = new ResourcePoolInvalidStateError
                {
                    ErrorMessage = "Not allowed to update a resource pool in Deprecated state.",
                    Id = pool.Id,
                };
                ReportError(pool.Id, error);
            }
        }

        private void ValidateStateForCompleteAction(ICollection<ResourcePool> apiResourcePools)
        {
            if (apiResourcePools == null)
            {
                throw new ArgumentNullException(nameof(apiResourcePools));
            }

            if (apiResourcePools.Count == 0)
            {
                return;
            }

            foreach (var pool in apiResourcePools.Where(x => x.State != ResourcePoolState.Draft))
            {
                var error = new ResourcePoolInvalidStateError
                {
                    ErrorMessage = "Not allowed to complete a resource pool that is not in Draft state.",
                    Id = pool.Id,
                };
                ReportError(pool.Id, error);
            }
        }

        private void ValidateStateForDeprecateAction(ICollection<ResourcePool> apiResourcePools)
        {
            if (apiResourcePools == null)
            {
                throw new ArgumentNullException(nameof(apiResourcePools));
            }

            if (apiResourcePools.Count == 0)
            {
                return;
            }

            foreach (var pool in apiResourcePools.Where(x => x.State != ResourcePoolState.Complete))
            {
                var error = new ResourcePoolInvalidStateError
                {
                    ErrorMessage = "Not allowed to deprecate a resource pool that is not in Completed state.",
                    Id = pool.Id,
                };
                ReportError(pool.Id, error);
            }
        }

        private void ValidateStateForDeleteAction(ICollection<ResourcePool> apiResourcePools)
        {
            if (apiResourcePools == null)
            {
                throw new ArgumentNullException(nameof(apiResourcePools));
            }

            if (apiResourcePools.Count == 0)
            {
                return;
            }

            foreach (var pool in apiResourcePools.Where(x => !new[] { ResourcePoolState.Draft, ResourcePoolState.Deprecated }.Contains(x.State)))
            {
                var error = new ResourcePoolInvalidStateError
                {
                    ErrorMessage = "Not allowed to delete a resource pool that is not in Draft or Deprecated state.",
                    Id = pool.Id,
                };
                ReportError(pool.Id, error);
            }
        }

        private void ValidateNames(ICollection<ResourcePool> apiResourcePools)
        {
            if (apiResourcePools == null)
            {
                throw new ArgumentNullException(nameof(apiResourcePools));
            }

            if (apiResourcePools.Count == 0)
            {
                return;
            }

            var poolsRequiringValidation = apiResourcePools.ToList();

            foreach (var pool in poolsRequiringValidation.Where(x => !InputValidator.IsNonEmptyText(x.Name)).ToArray())
            {
                var error = new ResourcePoolInvalidNameError
                {
                    ErrorMessage = "Name cannot be empty.",
                    Id = pool.Id,
                };

                ReportError(pool.Id, error);

                poolsRequiringValidation.Remove(pool);
            }

            foreach (var pool in poolsRequiringValidation.Where(x => !InputValidator.HasValidTextLength(x.Name)).ToArray())
            {
                var error = new ResourcePoolInvalidNameError
                {
                    ErrorMessage = $"Name exceeds maximum length of {InputValidator.DefaultMaxTextLength} characters.",
                    Id = pool.Id,
                    Name = pool.Name,
                };

                ReportError(pool.Id, error);

                poolsRequiringValidation.Remove(pool);
            }

            var poolsWithDuplicateNames = poolsRequiringValidation
                .GroupBy(pool => pool.Name)
                .Where(g => g.Count() > 1)
                .SelectMany(x => x)
                .ToList();

            foreach (var pool in poolsWithDuplicateNames)
            {
                var error = new ResourcePoolDuplicateNameError
                {
                    ErrorMessage = $"Resource pool '{pool.Name}' has a duplicate name.",
                    Id = pool.Id,
                    Name = pool.Name,
                };

                ReportError(pool.Id, error);

                poolsRequiringValidation.Remove(pool);
            }
        }

        private void ValidateDomNames(ICollection<ResourcePool> apiResourcePools)
        {
            FilterElement<DomInstance> filter(string name) =>
                DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resourcepool.Id)
                .AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourcePoolInfo.Name).Equal(name));

            var domPoolsbyName = planApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(apiResourcePools.Select(x => x.Name), filter)
                .GroupBy(x => x.Name)
                .ToDictionary(x => x.Key, x => (IReadOnlyCollection<DomResourcePool>)x.ToList());

            foreach (var pool in apiResourcePools)
            {
                if (!domPoolsbyName.TryGetValue(pool.Name, out var domPools))
                {
                    MarkAsPoolWithCoreChanges(pool);
                    continue;
                }

                var existingPools = domPools.Where(x => x.ID.Id != pool.Id).ToList();
                if (existingPools.Count == 0)
                {
                    MarkAsPoolWithCoreChanges(pool);
                    continue;
                }

                planApi.Logger.Information(this, $"Name '{pool.Name}' is already in use by DOM resource pool(s) with ID(s)", [existingPools.Select(x => x.ID.Id).ToArray()]);

                var error = new ResourcePoolNameExistsError
                {
                    ErrorMessage = "Name is already in use.",
                    Id = pool.Id,
                    Name = pool.Name,
                };

                ReportError(pool.Id, error);
            }
        }

        private void ValidatePoolLinks(ICollection<ResourcePool> apiResourcePools)
        {
            if (apiResourcePools == null)
            {
                throw new ArgumentNullException(nameof(apiResourcePools));
            }

            if (apiResourcePools.Count == 0)
            {
                return;
            }

            var linkedResourcePoolIds = apiResourcePools
                .SelectMany(x => x.LinkedResourcePools)
                .Select(x => x.LinkedResourcePoolId)
                .Distinct()
                .ToList();
            var domPoolsById = planApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(linkedResourcePoolIds).ToDictionary(x => x.ID.Id);

            foreach (var pool in apiResourcePools)
            {
                foreach (var link in pool.LinkedResourcePools)
                {
                    if (link.LinkedResourcePoolId == Guid.Empty)
                    {
                        var error = new ResourcePoolEmptyPoolLinkError
                        {
                            ErrorMessage = "Linked resource pool ID cannot be empty.",
                            Id = pool.Id,
                        };

                        ReportError(pool.Id, error);
                    }
                    else if (link.LinkedResourcePoolId == pool.Id)
                    {
                        var error = new ResourcePoolSelfReferencePoolLinkError
                        {
                            ErrorMessage = "A resource pool cannot link to itself.",
                            Id = pool.Id,
                        };

                        ReportError(pool.Id, error);
                    }
                    else if (!domPoolsById.TryGetValue(link.LinkedResourcePoolId, out _))
                    {
                        var error = new ResourcePoolNotFoundPoolLinkError
                        {
                            ErrorMessage = $"Linked resource pool with ID '{link.LinkedResourcePoolId}' {(link.IsNew ? "does not exist" : "no longer exists")}.",
                            Id = pool.Id,
                            LinkedResourcePoolId = link.LinkedResourcePoolId,
                        };

                        ReportError(pool.Id, error);
                    }
                }
            }
        }

        private void ValidateCapabilities(ICollection<ResourcePool> apiResourcePools)
        {
            if (apiResourcePools == null)
            {
                throw new ArgumentNullException(nameof(apiResourcePools));
            }

            if (apiResourcePools.Count == 0)
            {
                return;
            }

            var capabilityIds = apiResourcePools
                .SelectMany(x => x.Capabilities)
                .Select(x => x.Id)
                .Distinct()
                .ToList();
            var capabilitiesById = planApi.Capabilities.Read(capabilityIds).ToDictionary(x => x.Id);

            foreach (var pool in apiResourcePools)
            {
                var duplicateSettings = pool.Capabilities
                    .GroupBy(x => x.Id)
                    .Where(g => g.Count() > 1)
                    .ToDictionary(x => x.Key, x => x.Count());

                foreach (var kvp in duplicateSettings)
                {
                    var error = new ResourcePoolInvalidCapabilitySettingsError
                    {
                        Id = pool.Id,
                        CapabilityId = kvp.Key,
                        ErrorMessage = $"Capability with ID '{kvp.Key}' is defined {kvp.Value} times. Duplicate capability settings are not allowed.",
                    };

                    ReportError(pool.Id, error);
                }

                if (duplicateSettings.Count > 0)
                {
                    continue;
                }

                foreach (var capabilitySettings in pool.Capabilities)
                {
                    if (capabilitySettings.Id == Guid.Empty)
                    {
                        var error = new ResourcePoolInvalidCapabilitySettingsError
                        {
                            ErrorMessage = "Capability ID cannot be empty.",
                        };

                        ReportError(pool.Id, error);
                        continue;
                    }

                    if (!capabilitiesById.TryGetValue(capabilitySettings.Id, out var capability))
                    {
                        var error = new ResourcePoolInvalidCapabilitySettingsError
                        {
                            ErrorMessage = $"Capability with ID '{capabilitySettings.Id}' not found.",
                            CapabilityId = capabilitySettings.Id,
                        };

                        ReportError(pool.Id, error);
                        continue;
                    }

                    if (capabilitySettings.Discretes.Count == 0)
                    {
                        var error = new ResourcePoolInvalidCapabilitySettingsError
                        {
                            ErrorMessage = "At least one discrete value must be specified for the capability.",
                            CapabilityId = capabilitySettings.Id,
                        };

                        ReportError(pool.Id, error);
                        continue;
                    }

                    foreach (var discreteValue in capabilitySettings.Discretes)
                    {
                        if (!capability.Discretes.Contains(discreteValue))
                        {
                            var error = new ResourcePoolInvalidCapabilitySettingsError
                            {
                                ErrorMessage = $"Discrete value '{discreteValue}' is not valid for capability '{capability.Name}'.",
                                CapabilityId = capabilitySettings.Id,
                            };

                            ReportError(pool.Id, error);
                        }
                    }
                }
            }
        }

        private void ValidateCategories(ICollection<ResourcePool> apiResourcePools)
        {
            if (apiResourcePools == null)
            {
                throw new ArgumentNullException(nameof(apiResourcePools));
            }

            if (apiResourcePools.Count == 0 || apiResourcePools.All(x => x.CategoryId == null))
            {
                return;
            }

            var resourcePoolScope = planApi.Categories.Scopes.Read("Resource Pools");
            if (resourcePoolScope == null)
            {
                foreach (var pool in apiResourcePools)
                {
                    if (pool.CategoryId != null)
                    {
                        var error = new ResourcePoolCategoryScopeNotFoundError
                        {
                            ErrorMessage = "Category scope 'Resource Pools' could not found.",
                        };

                        ReportError(pool.Id, error);
                    }
                }

                return;
            }

            var categoryIds = Enumerable.ToHashSet(planApi.Categories.Categories.GetByScope(resourcePoolScope).Select(x => x.ID.ToString()));

            foreach (var pool in apiResourcePools)
            {
                if (pool.CategoryId == null)
                {
                    continue;
                }

                if (!categoryIds.Contains(pool.CategoryId))
                {
                    var error = new ResourcePoolCategoryNotFoundError
                    {
                        ErrorMessage = $"Category with ID '{pool.CategoryId}' could not found in Scope 'Resource Pools'.",
                        CategoryId = pool.CategoryId,
                        Id = pool.Id,
                    };

                    ReportError(pool.Id, error);
                }
            }
        }

        private void ValidateWorkflowUsage(ICollection<ResourcePool> apiResourcePools)
        {
            PassTraceData(SlcWorkflowResourcePoolUsageValidator.Validate(planApi, apiResourcePools));
        }

        private ICollection<DomChangeResults> GetPoolsWithChanges(ICollection<ResourcePool> apiResourcePools)
        {
            if (apiResourcePools == null)
            {
                throw new ArgumentNullException(nameof(apiResourcePools));
            }

            var changeResults = new List<DomChangeResults>();
            if (apiResourcePools.Count == 0)
            {
                return changeResults;
            }

            var poolsRequiringValidation = apiResourcePools.Where(x => !x.IsNew && x.HasChanges).ToList();
            if (poolsRequiringValidation.Count == 0)
            {
                return changeResults;
            }

            var storedDomResourcePoolsById = planApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(poolsRequiringValidation.Select(x => x.Id)).ToDictionary(x => x.ID.Id);
            foreach (var pool in poolsRequiringValidation)
            {
                if (!storedDomResourcePoolsById.TryGetValue(pool.Id, out var stored))
                {
                    var error = new ResourcePoolNotFoundError
                    {
                        ErrorMessage = $"Resource pool with ID '{pool.Id}' no longer exists.",
                        Id = pool.Id,
                    };

                    ReportError(pool.Id, error);

                    continue;
                }

                var changeResult = DomChangeHandler.HandleChanges(pool.OriginalInstance, pool.GetInstanceWithChanges(), stored);
                if (changeResult.HasErrors)
                {
                    foreach (var errorDetails in changeResult.Errors)
                    {
                        var error = new ResourcePoolValueAlreadyChangedError
                        {
                            ErrorMessage = errorDetails.Message,
                            Id = pool.Id,
                        };

                        ReportError(pool.Id, error);
                    }

                    continue;
                }

                changeResults.Add(changeResult);
            }

            return changeResults;
        }

        private void MarkAsPoolWithCoreChanges(ResourcePool resourcePool)
        {
            if (resourcePool == null)
            {
                throw new ArgumentNullException(nameof(resourcePool));
            }

            if (resourcePool.State != ResourcePoolState.Complete)
            {
                return;
            }

            poolIdsWithCoreChanges.Add(resourcePool.Id);
        }
    }
}
