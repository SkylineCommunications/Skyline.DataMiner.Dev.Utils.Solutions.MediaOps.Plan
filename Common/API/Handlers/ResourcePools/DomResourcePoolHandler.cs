namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.MediaOps.Plan.Extensions;
    using Skyline.DataMiner.MediaOps.Plan.Storage.DOM;
    using Skyline.DataMiner.MediaOps.Plan.Storage.DOM.SlcResource_Studio;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Utils.DOM.Extensions;

    using DomResourcePool = Storage.DOM.SlcResource_Studio.ResourcepoolInstance;

    internal class DomResourcePoolHandler : ApiObjectValidator<Guid>
    {
        private readonly MediaOpsPlanApi planApi;

        private readonly HashSet<Guid> poolIdsWithCoreChanges = new HashSet<Guid>();

        private readonly HashSet<ResourcePool> referencedApiResourcePoolsToUpdate = new HashSet<ResourcePool>();

        private DomResourcePoolHandler(MediaOpsPlanApi planApi)
        {
            this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
        }

        internal static BulkCreateOrUpdateResult<Guid> CreateOrUpdate(MediaOpsPlanApi planApi, IEnumerable<ResourcePool> apiResourcePools)
        {
            var handler = new DomResourcePoolHandler(planApi);
            handler.CreateOrUpdate(apiResourcePools);

            var result = new BulkCreateOrUpdateResult<Guid>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);
            result.ThrowOnFailure();

            return result;
        }

        internal static bool TryCreateOrUpdate(MediaOpsPlanApi planApi, IEnumerable<ResourcePool> apiResourcePools, out BulkCreateOrUpdateResult<Guid> result)
        {
            var handler = new DomResourcePoolHandler(planApi);
            handler.CreateOrUpdate(apiResourcePools);

            result = new BulkCreateOrUpdateResult<Guid>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

            return !result.HasFailures();
        }

        internal static bool TryComplete(MediaOpsPlanApi planApi, IEnumerable<ResourcePool> apiResourcePools, out BulkCreateOrUpdateResult<Guid> result)
        {
            var handler = new DomResourcePoolHandler(planApi);
            handler.TransitionToCompleted(apiResourcePools);

            result = new BulkCreateOrUpdateResult<Guid>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

            return !result.HasFailures();
        }

        internal static bool TryDeprecate(MediaOpsPlanApi planApi, IEnumerable<ResourcePool> apiResourcePools, out BulkCreateOrUpdateResult<Guid> result, ResourcePoolDeprecateOptions options = null)
        {
            var handler = new DomResourcePoolHandler(planApi);
            handler.TransitionToDeprecated(apiResourcePools, options ?? ResourcePoolDeprecateOptions.GetDefaults());

            result = new BulkCreateOrUpdateResult<Guid>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

            return !result.HasFailures();
        }

        internal static bool TryDelete(MediaOpsPlanApi planApi, IEnumerable<ResourcePool> apiResourcePools, out BulkCreateOrUpdateResult<Guid> result, ResourcePoolDeleteOptions options = null)
        {
            var handler = new DomResourcePoolHandler(planApi);
            handler.Delete(apiResourcePools, options ?? ResourcePoolDeleteOptions.GetDefaults());

            result = new BulkCreateOrUpdateResult<Guid>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

            return !result.HasFailures();
        }

        internal static long CountAll(MediaOpsPlanApi planApi)
        {
            var handler = new DomResourcePoolHandler(planApi);
            return handler.CountAll();
        }

        private void CreateOrUpdate(IEnumerable<ResourcePool> apiResourcePools)
        {
            if (apiResourcePools == null)
            {
                throw new ArgumentNullException(nameof(apiResourcePools));
            }

            if (!apiResourcePools.Any())
            {
                return;
            }

            var toCreate = new List<ResourcePool>();
            var toUpdate = new List<ResourcePool>();
            foreach (var resourcePool in apiResourcePools)
            {
                if (resourcePool.IsNew)
                {
                    toCreate.Add(resourcePool);
                }
                else
                {
                    toUpdate.Add(resourcePool);
                }
            }

            ValidateIdsNotInUse(toCreate);
            ValidateStateForUpdateAction(toUpdate);

            // Todo: lock DOM instances
            var changeResults = GetPoolsWithChanges(toUpdate.Where(x => !TraceDataPerItem.Keys.Contains(x.Id)));

            var toCreateNameValidation = toCreate.Where(x => !TraceDataPerItem.Keys.Contains(x.Id));
            var toUpdateNameValidation = toUpdate.Where(x => changeResults.Any(y => y.Instance.ID.Id == x.Id && y.ChangedFields.Select(z => z.FieldDescriptorId).Contains(SlcResource_StudioIds.Sections.ResourcePoolInfo.Name.Id)));
            ValidateNames(toCreateNameValidation.Concat(toUpdateNameValidation));

            var toCreateDomInstances = toCreate
                .Where(x => !TraceDataPerItem.Keys.Contains(x.Id))
                .Select(x => x.GetInstanceWithChanges())
                .ToList();

            var toUpdateDomInstances = changeResults
                .Where(x => !TraceDataPerItem.Keys.Contains(x.Instance.ID.Id))
                .Select(x => new DomResourcePool(x.Instance))
                .ToList();

            CreateOrUpdate(toCreateDomInstances.Concat(toUpdateDomInstances));
        }

        private void CreateOrUpdate(IEnumerable<DomResourcePool> domResourcePools)
        {
            if (domResourcePools == null)
            {
                throw new ArgumentNullException(nameof(domResourcePools));
            }

            if (!domResourcePools.Any())
            {
                return;
            }

            var domPoolsById = domResourcePools.ToDictionary(x => x.ID.Id);

            if (poolIdsWithCoreChanges.Count != 0)
            {
                CoreResourcePoolHandler.TryCreateOrUpdate(planApi, domResourcePools.Where(x => poolIdsWithCoreChanges.Contains(x.ID.Id)), out var coreResult);

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

        private void TransitionToCompleted(IEnumerable<ResourcePool> apiResourcePools)
        {
            if (apiResourcePools == null)
            {
                throw new ArgumentNullException(nameof(apiResourcePools));
            }

            if (!apiResourcePools.Any())
            {
                return;
            }

            ValidateStateForCompleteAction(apiResourcePools);
            ValidateNames(apiResourcePools.Where(x => !TraceDataPerItem.Keys.Contains(x.Id)));

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
        }

        private void TransitionToDeprecated(IEnumerable<ResourcePool> apiResourcePools, ResourcePoolDeprecateOptions options)
        {
            if (apiResourcePools == null)
            {
                throw new ArgumentNullException(nameof(apiResourcePools));
            }

            if (!apiResourcePools.Any())
            {
                return;
            }

            ValidateStateForDeprecateAction(apiResourcePools);

            var poolsToDeprecate = apiResourcePools.Where(x => !TraceDataPerItem.Keys.Contains(x.Id)).ToList();
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
                    throw;
                }
            }
        }

        private void Delete(IEnumerable<ResourcePool> apiResourcePools, ResourcePoolDeleteOptions options)
        {
            if (apiResourcePools == null)
            {
                throw new ArgumentNullException(nameof(apiResourcePools));
            }

            if (!apiResourcePools.Any())
            {
                return;
            }

            ValidateStateForDeleteAction(apiResourcePools);

            var poolsToDelete = apiResourcePools.Where(x => !TraceDataPerItem.Keys.Contains(x.Id)).ToList();
            RemovePoolFromParentPoolLinks(poolsToDelete);
            HandleDeleteOptions(poolsToDelete, options);
            UnassignResourcesFromPool(poolsToDelete);

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

            var poolsToUpdate = referencedApiResourcePoolsToUpdate.Where(x => !domResult.SuccessfulIds.Select(y => y.Id).Contains(x.Id)).ToList();
            planApi.ResourcePools.Update(poolsToUpdate);
        }

        private void DeprecatePoolResources(IEnumerable<ResourcePool> apiResourcePools)
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

            // todo: use bulk deprecate call when available in resource repository (ADO34081)
            foreach (var resource in resourcesToDeprecate)
            {
                try
                {
                    planApi.Resources.MoveTo(resource, ResourceState.Deprecated);
                    ReportSuccess(resource.Id);
                }
                catch (Exception ex)
                {
                    ReportError(resource.Id, new MediaOpsErrorData() { ErrorMessage = ex.ToString() });
                }
            }
        }

        private void RemovePoolFromParentPoolLinks(IEnumerable<ResourcePool> apiResourcePools)
        {
            var parentLinksPerPoolCollection = planApi.ResourcePools.GetParentPoolLinks(apiResourcePools);

            foreach (var pool in apiResourcePools)
            {
                if (parentLinksPerPoolCollection.TryGetValue(pool, out var parentPools))
                {
                    foreach (var parentPool in parentPools)
                    {
                        parentPool.RemoveResourcePoolLink(pool);

                        referencedApiResourcePoolsToUpdate.Add(parentPool);
                    }
                }
            }
        }

        private void HandleDeleteOptions(IEnumerable<ResourcePool> apiResourcePools, ResourcePoolDeleteOptions options)
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

        private void DeletePoolResources(IEnumerable<ResourcePool> apiResourcePools, ResourceState state)
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

        private void UnassignResourcesFromPool(IEnumerable<ResourcePool> apiResourcePools)
        {
            // todo: implement logic to unassign resources from the given resource pools
        }

        private void ValidateIdsNotInUse(IEnumerable<ResourcePool> apiResourcePools)
        {
            if (apiResourcePools == null)
            {
                throw new ArgumentNullException(nameof(apiResourcePools));
            }

            if (!apiResourcePools.Any())
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
                var error = new ResourcePoolConfigurationError
                {
                    ErrorReason = ResourcePoolConfigurationError.Reason.DuplicateId,
                    ErrorMessage = $"Resource pool '{pool.Name}' has a duplicate ID.",
                };

                ReportError(pool.Id, error);

                poolsRequiringValidation.Remove(pool);
            }

            foreach (var foundInstance in planApi.DomHelpers.SlcResourceStudioHelper.GetResourceStudioInstances(poolsRequiringValidation.Select(x => x.Id)))
            {
                planApi.Logger.LogInformation($"ID is already in use by a Resource Studio instance.", foundInstance.ID.Id);

                var error = new ResourcePoolConfigurationError
                {
                    ErrorReason = ResourcePoolConfigurationError.Reason.IdInUse,
                    ErrorMessage = "ID is already in use.",
                };

                ReportError(foundInstance.ID.Id, error);
            }
        }

        private void ValidateStateForUpdateAction(IEnumerable<ResourcePool> apiResourcePools)
        {
            if (apiResourcePools == null)
            {
                throw new ArgumentNullException(nameof(apiResourcePools));
            }

            if (!apiResourcePools.Any())
            {
                return;
            }

            foreach (var pool in apiResourcePools.Where(x => x.State == ResourcePoolState.Deprecated))
            {
                var error = new ResourcePoolConfigurationError
                {
                    ErrorReason = ResourcePoolConfigurationError.Reason.InvalidState,
                    ErrorMessage = "Not allowed to update a resource pool in Deprecated state."
                };
                ReportError(pool.Id, error);
            }
        }

        private void ValidateStateForCompleteAction(IEnumerable<ResourcePool> apiResourcePools)
        {
            if (apiResourcePools == null)
            {
                throw new ArgumentNullException(nameof(apiResourcePools));
            }

            if (!apiResourcePools.Any())
            {
                return;
            }

            foreach (var pool in apiResourcePools.Where(x => x.State != ResourcePoolState.Draft))
            {
                var error = new ResourcePoolConfigurationError
                {
                    ErrorReason = ResourcePoolConfigurationError.Reason.InvalidState,
                    ErrorMessage = "Not allowed to complete a resource pool that is not in Draft state."
                };
                ReportError(pool.Id, error);
            }
        }

        private void ValidateStateForDeprecateAction(IEnumerable<ResourcePool> apiResourcePools)
        {
            if (apiResourcePools == null)
            {
                throw new ArgumentNullException(nameof(apiResourcePools));
            }

            if (!apiResourcePools.Any())
            {
                return;
            }

            foreach (var pool in apiResourcePools.Where(x => x.State != ResourcePoolState.Complete))
            {
                var error = new ResourcePoolConfigurationError
                {
                    ErrorReason = ResourcePoolConfigurationError.Reason.InvalidState,
                    ErrorMessage = "Not allowed to deprecate a resource pool that is not in Completed state."
                };
                ReportError(pool.Id, error);
            }
        }

        private void ValidateStateForDeleteAction(IEnumerable<ResourcePool> apiResourcePools)
        {
            if (apiResourcePools == null)
            {
                throw new ArgumentNullException(nameof(apiResourcePools));
            }

            if (!apiResourcePools.Any())
            {
                return;
            }

            foreach (var pool in apiResourcePools.Where(x => !new[] { ResourcePoolState.Draft, ResourcePoolState.Deprecated }.Contains(x.State)))
            {
                var error = new ResourcePoolConfigurationError
                {
                    ErrorReason = ResourcePoolConfigurationError.Reason.InvalidState,
                    ErrorMessage = "Not allowed to delete a resource pool that is not in Draft or Deprecated state."
                };
                ReportError(pool.Id, error);
            }
        }

        private void ValidateNames(IEnumerable<ResourcePool> apiResourcePools)
        {
            if (apiResourcePools == null)
            {
                throw new ArgumentNullException(nameof(apiResourcePools));
            }

            if (!apiResourcePools.Any())
            {
                return;
            }

            var poolsRequiringValidation = apiResourcePools.ToList();

            foreach (var pool in poolsRequiringValidation.Where(x => !InputValidator.ValidateEmptyText(x.Name)))
            {
                var error = new ResourcePoolConfigurationError
                {
                    ErrorReason = ResourcePoolConfigurationError.Reason.InvalidName,
                    ErrorMessage = "Name cannot be empty.",
                };

                ReportError(pool.Id, error);

                poolsRequiringValidation.Remove(pool);
            }

            foreach (var pool in poolsRequiringValidation.Where(x => !InputValidator.ValidateTextLength(x.Name)))
            {
                var error = new ResourcePoolConfigurationError
                {
                    ErrorReason = ResourcePoolConfigurationError.Reason.InvalidName,
                    ErrorMessage = $"Name exceeds maximum length of {InputValidator.DefaultMaxTextLength} characters.",
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
                var error = new ResourcePoolConfigurationError
                {
                    ErrorReason = ResourcePoolConfigurationError.Reason.DuplicateName,
                    ErrorMessage = $"Resource pool '{pool.Name}' has a duplicate name.",
                };

                ReportError(pool.Id, error);

                poolsRequiringValidation.Remove(pool);
            }

            FilterElement<DomInstance> filter(string name) =>
                DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resourcepool.Id)
                .AND(DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourcePoolInfo.Name).Equal(name)
                .AND(DomInstanceExposers.StatusId.NotEqual(SlcResource_StudioIds.Behaviors.Resourcepool_Behavior.Statuses.Draft)));

            var domPoolsbyName = planApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(poolsRequiringValidation.Select(x => x.Name), filter)
                .GroupBy(x => x.Name)
                .ToDictionary(x => x.Key, x => (IReadOnlyCollection<DomResourcePool>)x.ToList());

            foreach (var pool in poolsRequiringValidation)
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

                planApi.Logger.LogInformation($"Name '{pool.Name}' is already in use by DOM resource pool(s) with ID(s)", existingPools.Select(x => x.ID.Id).ToArray());

                var error = new ResourcePoolConfigurationError
                {
                    ErrorReason = ResourcePoolConfigurationError.Reason.NameExists,
                    ErrorMessage = "Name is already in use.",
                };

                ReportError(pool.Id, error);
            }
        }

        private IEnumerable<DomChangeResults> GetPoolsWithChanges(IEnumerable<ResourcePool> apiResourcePools)
        {
            if (apiResourcePools == null)
            {
                throw new ArgumentNullException(nameof(apiResourcePools));
            }

            if (!apiResourcePools.Any())
            {
                return [];
            }

            return GetPoolsWithChangesIterator(apiResourcePools);
        }

        private IEnumerable<DomChangeResults> GetPoolsWithChangesIterator(IEnumerable<ResourcePool> apiResourcePools)
        {
            var poolsRequiringValidation = apiResourcePools.Where(x => !x.IsNew && x.HasChanges).ToList();
            if (poolsRequiringValidation.Count == 0)
            {
                yield break;
            }

            var storedDomResourcePoolsById = planApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(poolsRequiringValidation.Select(x => x.Id)).ToDictionary(x => x.ID.Id);
            foreach (var pool in poolsRequiringValidation)
            {
                if (!storedDomResourcePoolsById.TryGetValue(pool.Id, out var stored))
                {
                    var error = new ResourcePoolConfigurationError
                    {
                        ErrorReason = ResourcePoolConfigurationError.Reason.NotFound,
                        ErrorMessage = $"Resource pool with ID '{pool.Id}' no longer exists."
                    };

                    ReportError(pool.Id, error);

                    continue;
                }

                var changeResult = DomChangeHandler.HandleChanges(pool.OriginalInstance, pool.GetInstanceWithChanges(), stored);
                if (changeResult.HasErrors)
                {
                    foreach (var errorDetails in changeResult.Errors)
                    {
                        var error = new ResourcePoolConfigurationError
                        {
                            ErrorReason = ResourcePoolConfigurationError.Reason.ValueAlreadyChanged,
                            ErrorMessage = errorDetails.Message,
                        };

                        ReportError(pool.Id, error);
                    }

                    continue;
                }

                yield return changeResult;
            }
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

        private long CountAll()
        {
            return planApi.DomHelpers.SlcResourceStudioHelper.DomHelper.DomInstances
                .Count(DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resourcepool.Id));
        }
    }
}
