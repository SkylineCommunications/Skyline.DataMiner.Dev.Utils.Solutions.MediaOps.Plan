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
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Utils.DOM.Extensions;

    using DomResourcePool = Storage.DOM.SlcResource_Studio.ResourcepoolInstance;

    internal class DomResourcePoolHandler : ApiObjectValidator<Guid>
    {
        private readonly MediaOpsPlanApi planApi;

        private readonly HashSet<Guid> poolIdsWithCoreChanges = new HashSet<Guid>();

        private DomResourcePoolHandler(MediaOpsPlanApi planApi)
        {
            this.planApi = planApi ?? throw new ArgumentNullException(nameof(planApi));
        }

        public static BulkCreateOrUpdateResult<Guid> CreateOrUpdate(MediaOpsPlanApi planApi, IEnumerable<ResourcePool> apiResourcePools)
        {
            var handler = new DomResourcePoolHandler(planApi);
            handler.CreateOrUpdate(apiResourcePools);

            var result = new BulkCreateOrUpdateResult<Guid>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);
            result.ThrowOnFailure();

            return result;
        }

        public static bool TryCreateOrUpdate(MediaOpsPlanApi planApi, IEnumerable<ResourcePool> apiResourcePools, out BulkCreateOrUpdateResult<Guid> result)
        {
            var handler = new DomResourcePoolHandler(planApi);
            handler.CreateOrUpdate(apiResourcePools);

            result = new BulkCreateOrUpdateResult<Guid>(handler.SuccessfulItems, handler.UnsuccessfulItems, handler.TraceDataPerItem);

            return !result.HasFailures();
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
            ValidateState(toUpdate);

            // Todo: lock DOM instances
            var changeResults = GetPoolsWithChanges(toUpdate.Where(x => !TraceDataPerItem.Keys.Contains(x.Id)));

            var toCreateNameValidation = toCreate.Where(x => !TraceDataPerItem.Keys.Contains(x.Id));
            var toUpdateNameValidation = toUpdate.Where(x => changeResults.Any(y => y.Instance.ID.Id == x.Id && y.ChangedFieldDescriptorIds.Contains(SlcResource_StudioIds.Sections.ResourcePoolInfo.Name.Id)));
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
                }
            }

            ReportSuccess(domResult.SuccessfulIds.Select(x => x.Id));
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

        private void ValidateState(IEnumerable<ResourcePool> apiResourcePools)
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
                    ErrorMessage = "Name exceeds maximum length of 150 characters.",
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
                    foreach (var errorMessage in changeResult.Errors)
                    {
                        var error = new ResourcePoolConfigurationError
                        {
                            ErrorReason = ResourcePoolConfigurationError.Reason.ValueAlreadyChanged,
                            ErrorMessage = errorMessage
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
    }
}
