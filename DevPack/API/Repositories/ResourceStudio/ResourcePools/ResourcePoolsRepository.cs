namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.ActivityHelper;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

    internal partial class ResourcePoolsRepository : DomRepository<ResourcePool>, IResourcePoolsRepository
    {
        public Guid Create(ResourcePool apiObject)
        {
            PlanApi.Logger.LogInformation("Creating new ResourcePool...");

            if (apiObject == null)
            {
                throw new ArgumentNullException(nameof(apiObject));
            }

            return ActivityHelper.Track(nameof(ResourcePoolsRepository), nameof(Create), act =>
            {
                if (!apiObject.IsNew)
                {
                    throw new InvalidOperationException("Not possible to use method Create for existing resource pool. Use CreateOrUpdate or Update instead.");
                }

                if (!DomResourcePoolHandler.TryCreateOrUpdate(PlanApi, [apiObject], out var result))
                {
                    throw new MediaOpsException(result.TraceDataPerItem[apiObject.Id]);
                }

                var resourcePoolId = result.SuccessfulIds.First();
                act?.AddTag("ResourcePoolId", resourcePoolId);

                return resourcePoolId;
            });
        }

        public IEnumerable<Guid> Create(IEnumerable<ResourcePool> apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            var existingResourcePools = apiObjects.Where(x => !x.IsNew).ToList();
            if (existingResourcePools.Any())
            {
                throw new InvalidOperationException("Not possible to use method Create for existing resource pools. Use CreateOrUpdate or Update instead.");
            }

            if (!DomResourcePoolHandler.TryCreateOrUpdate(PlanApi, apiObjects, out var result))
            {
                throw new MediaOpsBulkException<Guid>(result);
            }

            return result.SuccessfulIds;
        }

        public IEnumerable<Guid> CreateOrUpdate(IEnumerable<ResourcePool> apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            return ActivityHelper.Track(nameof(ResourcePoolsRepository), nameof(CreateOrUpdate), act =>
            {
                if (!DomResourcePoolHandler.TryCreateOrUpdate(PlanApi, apiObjects, out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var resourceIds = result.SuccessfulIds;
                act?.AddTag("Created Resource Pools", String.Join(", ", resourceIds));
                act?.AddTag("Created Resource Pools Count", resourceIds.Count);

                return resourceIds;
            });
        }

        public void Deprecate(ResourcePool resourcePool, ResourcePoolDeprecateOptions options)
        {
            if (resourcePool == null)
            {
                throw new ArgumentNullException(nameof(resourcePool));
            }

            if (!DomResourcePoolHandler.TryDeprecate(PlanApi, [resourcePool], out var result, options))
            {
                throw new MediaOpsException(result.TraceDataPerItem[resourcePool.Id]);
            }
        }

        public void Delete(params ResourcePool[] apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            Delete(apiObjects.Select(x => x.Id).ToArray());
        }

        public void Delete(params Guid[] apiObjectIds)
        {
            if (apiObjectIds == null)
            {
                throw new ArgumentNullException(nameof(apiObjectIds));
            }

            var resourcePoolsToDelete = Read(apiObjectIds).Values;

            if (!DomResourcePoolHandler.TryDelete(PlanApi, resourcePoolsToDelete, out var result))
            {
                throw new MediaOpsBulkException<Guid>(result);
            }
        }

        public void Delete(ResourcePool resourcePool, ResourcePoolDeleteOptions options)
        {
            if (resourcePool == null)
            {
                throw new ArgumentNullException(nameof(resourcePool));
            }

            if (!DomResourcePoolHandler.TryDelete(PlanApi, [resourcePool], out var result, options))
            {
                throw new MediaOpsException(result.TraceDataPerItem[resourcePool.Id]);
            }
        }

        public void MoveTo(ResourcePool resourcePool, ResourcePoolState desiredState)
        {
            if (resourcePool == null)
            {
                throw new ArgumentNullException(nameof(resourcePool));
            }

            MoveTo(resourcePool.Id, desiredState);
        }

        public void MoveTo(Guid resourcePoolId, ResourcePoolState desiredState)
        {
            PlanApi.Logger.LogInformation("Moving ResourcePool {resourcePoolId} to {desiredState}...", resourcePoolId, desiredState);

            if (resourcePoolId == Guid.Empty)
            {
                throw new ArgumentException("Resource pool ID cannot be empty.", nameof(resourcePoolId));
            }

            ActivityHelper.Track(nameof(ResourcePoolsRepository), nameof(MoveTo), act =>
            {
                act?.AddTag("ResourcePoolId", resourcePoolId);
                act?.AddTag("DesiredState", desiredState);

                var actionMethods = new Dictionary<ResourcePoolState, Action<Guid>>
                {
                    [ResourcePoolState.Complete] = HandleMoveToCompleteAction,
                    [ResourcePoolState.Deprecated] = HandleMoveToDeprecatedAction,
                };

                if (!actionMethods.TryGetValue(desiredState, out var action))
                {
                    throw new MediaOpsException($"Move to state '{desiredState}' is not supported.");
                }

                action(resourcePoolId);
            });
        }

        public void Update(ResourcePool apiObject)
        {
            if (apiObject == null)
            {
                throw new ArgumentNullException(nameof(apiObject));
            }

            ActivityHelper.Track(nameof(ResourcePoolsRepository), nameof(Update), act =>
            {
                if (!apiObject.HasChanges)
                {
                    act?.AddTag("NoChanges", true);
                    return;
                }

                if (apiObject.IsNew)
                {
                    throw new InvalidOperationException("Not possible to use method Update for a new resource pool. Use CreateOrUpdate or Create instead.");
                }

                if (!DomResourcePoolHandler.TryCreateOrUpdate(PlanApi, [apiObject], out var result))
                {
                    throw new MediaOpsException(result.TraceDataPerItem[apiObject.Id]);
                }
            });
        }

        public void Update(IEnumerable<ResourcePool> apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            var newResourcePools = apiObjects.Where(x => x.IsNew);
            if (newResourcePools.Any())
            {
                throw new InvalidOperationException("Not possible to use method Update for new resource pools. Use Create or CreateOrUpdate instead.");
            }

            if (!DomResourcePoolHandler.TryCreateOrUpdate(PlanApi, apiObjects, out var result))
            {
                throw new MediaOpsBulkException<Guid>(result);
            }
        }

        public void AssignResourcesToPool(ResourcePool resourcePool, IEnumerable<Resource> resources)
        {
            if (resourcePool == null)
            {
                throw new ArgumentNullException(nameof(resourcePool));
            }

            AssignResourcesToPool(resourcePool.Id, resources);
        }

        public void AssignResourcesToPool(Guid resourcePoolId, IEnumerable<Resource> resources)
        {
            if (resourcePoolId == Guid.Empty)
            {
                throw new ArgumentException("Resource pool ID cannot be empty.", nameof(resourcePoolId));
            }

            if (resources == null)
            {
                throw new ArgumentNullException(nameof(resources));
            }

            if (!resources.Any())
            {
                return;
            }

            if (resources.Any(x => x == null))
            {
                throw new ArgumentException("The collection contains a null resource.", nameof(resources));
            }

            foreach (var resource in resources)
            {
                resource.AssignToPool(resourcePoolId);
            }

            PlanApi.Resources.Update(resources);
        }

        public void UnassignResourcesFromPool(ResourcePool resourcePool, IEnumerable<Resource> resources)
        {
            if (resourcePool == null)
            {
                throw new ArgumentNullException(nameof(resourcePool));
            }

            UnassignResourcesFromPool(resourcePool.Id, resources);
        }

        public void UnassignResourcesFromPool(Guid resourcePoolId, IEnumerable<Resource> resources)
        {
            if (resourcePoolId == Guid.Empty)
            {
                throw new ArgumentException("Resource pool ID cannot be empty.", nameof(resourcePoolId));
            }

            if (resources == null)
            {
                throw new ArgumentNullException(nameof(resources));
            }

            if (!resources.Any())
            {
                return;
            }

            if (resources.Any(x => x == null))
            {
                throw new ArgumentException("The collection contains a null resource.", nameof(resources));
            }

            foreach (var resource in resources)
            {
                resource.UnassignFromPool(resourcePoolId);
            }

            PlanApi.Resources.Update(resources);
        }

        private void HandleMoveToCompleteAction(Guid resourcePoolId)
        {
            if (resourcePoolId == Guid.Empty)
            {
                throw new ArgumentException("Resource pool ID cannot be empty.", nameof(resourcePoolId));
            }

            var resourcePool = Read(resourcePoolId) ?? throw new MediaOpsException($"Resource pool with ID '{resourcePoolId}' does not exist.");

            if (!DomResourcePoolHandler.TryComplete(PlanApi, [resourcePool], out var result))
            {
                throw new MediaOpsException(result.TraceDataPerItem[resourcePool.Id]);
            }
        }

        private void HandleMoveToDeprecatedAction(Guid resourcePoolId)
        {
            if (resourcePoolId == Guid.Empty)
            {
                throw new ArgumentException("Resource pool ID cannot be empty.", nameof(resourcePoolId));
            }

            var resourcePool = Read(resourcePoolId) ?? throw new MediaOpsException($"Resource pool with ID '{resourcePoolId}' does not exist.");

            if (!DomResourcePoolHandler.TryDeprecate(PlanApi, [resourcePool], out var result))
            {
                throw new MediaOpsException(result.TraceDataPerItem[resourcePool.Id]);
            }
        }
    }
}
