namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.MediaOps.Plan.ActivityHelper;
    using Microsoft.Extensions.Logging;
    using Skyline.DataMiner.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.MediaOps.Plan.Extensions;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Net.Sections;

    using DomResourcePool = Storage.DOM.SlcResource_Studio.ResourcepoolInstance;
    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

    internal class ResourcePoolsRepository : RepositoryBase<ResourcePool>, IResourcePoolsRepository
    {
        public ResourcePoolsRepository(MediaOpsPlanApi planApi) : base(planApi)
        {
        }

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
                    throw new MediaOpsException("Not possible to use method Create for existing resource pool. Use CreateOrUpdate or Update instead.");
                }

                if (!DomResourcePoolHandler.TryCreateOrUpdate(PlanApi, [apiObject], out var result))
                {
                    throw new MediaOpsException(result.TraceDataPerItem[apiObject.Id]);
                }

                var resourcePoolId = result.SuccessfulIds.First();
                act.AddTag("ResourcePoolId", resourcePoolId);

                return resourcePoolId;
            });
        }

        public IEnumerable<Guid> Create(IEnumerable<ResourcePool> apiObjects)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Guid> CreateOrUpdate(IEnumerable<ResourcePool> apiObjects)
        {
            throw new NotImplementedException();
        }

        public void Delete(params ResourcePool[] objectApis)
        {
            throw new NotImplementedException();
        }

        public void Delete(params Guid[] objectIds)
        {
            throw new NotImplementedException();
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
                act.AddTag("ResourcePoolId", resourcePoolId);
                act.AddTag("DesiredState", desiredState);

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

        public ResourcePool Read(Guid id)
        {
            PlanApi.Logger.LogInformation("Reading ResourcePool with ID: {id}...", id);

            if (id == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(id));
            }

            return ActivityHelper.Track(nameof(ResourcePoolsRepository), nameof(Read), act =>
            {
                act.AddTag("ResourcePoolId", id);
                var filter = DomInstanceExposers.DomDefinitionId.Equal(StorageResourceStudio.SlcResource_StudioIds.Definitions.Resourcepool.Id)
                        .AND(DomInstanceExposers.Id.Equal(id));
                var domResourcePool = PlanApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(filter)
                    .FirstOrDefault();

                if (domResourcePool == null)
                {
                    act.AddTag("Hit", false);
                    return null;
                }

                act.AddTag("Hit", true);
                return new ResourcePool(domResourcePool);
            });
        }

        public IDictionary<Guid, ResourcePool> Read(IEnumerable<Guid> ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            return ActivityHelper.Track(nameof(ResourcePoolsRepository), nameof(Read), act =>
            {
                act.AddTag("RequestedResourcePoolCount", ids.Count());

                if (!ids.Any())
                {
                    return new Dictionary<Guid, ResourcePool>();
                }

                var retrievedPools = PlanApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(ids).SafeToDictionary(x => x.ID.Id, x => new ResourcePool(x));

                act.AddTag("RetrievedResourcePoolCount", retrievedPools.Count);
                return retrievedPools;
            });
        }

        public IEnumerable<ResourcePool> Read(FilterElement<ResourcePool> filter)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ResourcePool> ReadAll()
        {
            return ActivityHelper.Track(nameof(ResourcePoolsRepository), nameof(ReadAll), act =>
            {
                var filter = DomInstanceExposers.DomDefinitionId.Equal(StorageResourceStudio.SlcResource_StudioIds.Definitions.Resourcepool.Id);
                IEnumerable<ResourcePool> Iterator()
                {
                    foreach (var domResourcePool in PlanApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(filter))
                    {
                        yield return new ResourcePool(domResourcePool);
                    }
                }

                return Iterator();
            });
        }

        public IEnumerable<IEnumerable<ResourcePool>> ReadAllPaged()
        {
            throw new NotImplementedException();
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
                    act.AddTag("NoChanges", true);
                    return;
                }

                if (apiObject.IsNew)
                {
                    throw new MediaOpsException("Not possible to use method Update for a new resource pool. Use CreateOrUpdate or Create instead.");
                }

                if (!DomResourcePoolHandler.TryCreateOrUpdate(PlanApi, [apiObject], out var result))
                {
                    throw new MediaOpsException(result.TraceDataPerItem[apiObject.Id]);
                }
            });
        }

        public void Update(IEnumerable<ResourcePool> apiObjects)
        {
            throw new NotImplementedException();
        }

        internal DomResourcePool GetDomResourcePool(Guid domResourcePoolId)
        {
            if (domResourcePoolId == Guid.Empty)
            {
                throw new ArgumentException("Resource pool ID cannot be empty.", nameof(domResourcePoolId));
            }

            return ActivityHelper.Track(nameof(ResourcePoolsRepository), nameof(GetDomResourcePool), act =>
            {
                act.AddTag("ResourcePoolId", domResourcePoolId);
                return PlanApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(DomInstanceExposers.Id.Equal(domResourcePoolId)).FirstOrDefault();
            });
        }

        private void HandleMoveToCompleteAction(Guid resourcePoolId)
        {
            if (resourcePoolId == Guid.Empty)
            {
                throw new ArgumentException("Resource pool ID cannot be empty.", nameof(resourcePoolId));
            }

            var domResourcePool = GetDomResourcePool(resourcePoolId);

            if (domResourcePool.Status != StorageResourceStudio.SlcResource_StudioIds.Behaviors.Resourcepool_Behavior.StatusesEnum.Draft)
            {
                throw new MediaOpsException("Move to state Complete is only allowed for resource pools in Draft state.");
            }

            ValidateNameInUseInDom(domResourcePool.ResourcePoolInfo.Name, domResourcePool.ID.Id);

            CoreResourcePoolHandler.CreateOrUpdate(PlanApi, [domResourcePool]);

            domResourcePool.Save(PlanApi.DomHelpers.SlcResourceStudioHelper.DomHelper);

            PlanApi.DomHelpers.SlcResourceStudioHelper.DomHelper.DomInstances.DoStatusTransition(domResourcePool, StorageResourceStudio.SlcResource_StudioIds.Behaviors.Resourcepool_Behavior.Transitions.Draft_To_Complete);
        }

        private void HandleMoveToDeprecatedAction(Guid guid)
        {
            throw new NotImplementedException();
        }

        private void ValidateNameInUseInDom(string name, Guid domResourcePoolId)
        {
            var existingPools = PlanApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(
                DomInstanceExposers.DomDefinitionId.Equal(StorageResourceStudio.SlcResource_StudioIds.Definitions.Resourcepool.Id)
                .AND(DomInstanceExposers.FieldValues.DomInstanceField(StorageResourceStudio.SlcResource_StudioIds.Sections.ResourcePoolInfo.Name).Equal(name))
                .AND(DomInstanceExposers.StatusId.NotEqual(StorageResourceStudio.SlcResource_StudioIds.Behaviors.Resourcepool_Behavior.Statuses.Draft)))
                .Where(x => x.ID.Id != domResourcePoolId)
                .ToList();
            if (existingPools.Count != 0)
            {
                PlanApi.Logger.LogInformation("Name '{name}' is already in use by a DOM resource pool with ID '{domResourcePoolId}'.", name, domResourcePoolId);
                throw new MediaOpsException(new ResourcePoolConfigurationError
                {
                    ErrorReason = ResourcePoolConfigurationError.Reason.NameExists,
                    ErrorMessage = "Name is already in use.",
                });
            }
        }
    }
}
