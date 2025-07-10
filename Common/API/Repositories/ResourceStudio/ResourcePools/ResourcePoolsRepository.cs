namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using Skyline.DataMiner.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.MediaOps.Plan.Extensions;
    using Skyline.DataMiner.MediaOps.Plan.Storage;
    using Skyline.DataMiner.MediaOps.Plan.Storage.DOM;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Net.Sections;
    using Skyline.DataMiner.Utils.DOM.Extensions;

    using DomResourcePool = Storage.DOM.SlcResource_Studio.ResourcepoolInstance;
    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

    internal class ResourcePoolsRepository : RepositoryBase<ResourcePool>, IResourcePoolsRepository
    {
        public ResourcePoolsRepository(MediaOpsPlanApi planApi) : base(planApi)
        {
        }

        public Guid Create(ResourcePool apiObject)
        {
            using (var act = MediaOpsPlanApi.ActivitySource.StartActivity(nameof(Create), ActivityKind.Server))
            {
                try
                {
                    if (apiObject == null)
                    {
                        throw new ArgumentNullException(nameof(apiObject));
                    }

                    if (!apiObject.IsNew)
                    {
                        throw new MediaOpsException("Not possible to use method Create for existing resource pool. Use CreateOrUpdate or Update instead.");
                    }

                    if (!DomResourcePoolHandler.TryCreateOrUpdate(PlanApi, [apiObject], out var result))
                    {
                        throw new MediaOpsException(result.TraceDataPerItem[apiObject.Id]);
                    }

                    var resourcePoolId = result.SuccessfulIds[0];
                    act.AddTag("ResourcePoolId", resourcePoolId);

                    return resourcePoolId;
                }
                catch (Exception exception)
                {
                    act.AddException(exception);
                    throw;
                }
            }
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
            using (var act = MediaOpsPlanApi.ActivitySource.StartActivity(nameof(MoveTo), ActivityKind.Server))
            {
                act.AddTag("ResourcePoolId", resourcePoolId);
                act.AddTag("DesiredState", desiredState);

                try
                {
                    if (resourcePoolId == Guid.Empty)
                    {
                        throw new ArgumentException("Resource pool ID cannot be empty.", nameof(resourcePoolId));
                    }

                    var actionMethods = new Dictionary<ResourcePoolState, Action<Guid>>
                    {
                        [ResourcePoolState.Complete] = HandleMoveToCompleteAction,
                    };

                    if (!actionMethods.TryGetValue(desiredState, out var action))
                    {
                        throw new MediaOpsException($"Move to state '{desiredState}' is not supported.");
                    }

                    action(resourcePoolId);
                }
                catch (Exception exception)
                {
                    act.AddException(exception);
                    throw;
                }
            }
        }

        public ResourcePool Read(Guid id)
        {
            using (var act = MediaOpsPlanApi.ActivitySource.StartActivity())
            {
                act.AddTag("ResourcePoolId", id);

                try
                {
                    if (id == Guid.Empty)
                    {
                        throw new ArgumentNullException(nameof(id));
                    }

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
                }
                catch (Exception exception)
                {
                    act.AddException(exception);
                    throw;
                }
            }
        }

        public IDictionary<Guid, ResourcePool> Read(IEnumerable<Guid> ids)
        {
            using (var act = MediaOpsPlanApi.ActivitySource.StartActivity())
            {
                act.AddTag("RequestedResourcePoolCount", ids.Count());

                try
                {
                    if (ids == null)
                    {
                        throw new ArgumentNullException(nameof(ids));
                    }

                    if (!ids.Any())
                    {
                        return new Dictionary<Guid, ResourcePool>();
                    }

                    var retrievedPools = PlanApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(ids).SafeToDictionary(x => x.ID.Id, x => new ResourcePool(x));

                    act.AddTag("RetrievedResourcePoolCount", retrievedPools.Count);
                    return retrievedPools;
                }
                catch (Exception exception)
                {
                    act.AddException(exception);
                    throw;
                }
            }
        }

        public IEnumerable<ResourcePool> Read(FilterElement<ResourcePool> filter)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ResourcePool> ReadAll()
        {
            using (var act = MediaOpsPlanApi.ActivitySource.StartActivity())
            {
                var filter = DomInstanceExposers.DomDefinitionId.Equal(StorageResourceStudio.SlcResource_StudioIds.Definitions.Resourcepool.Id);
                foreach (var domResourcePool in PlanApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(filter))
                {
                    yield return new ResourcePool(domResourcePool);
                }
            }
        }

        public IEnumerable<IEnumerable<ResourcePool>> ReadAllPage()
        {
            throw new NotImplementedException();
        }

        public void Update(ResourcePool apiObject)
        {
            using (var act = MediaOpsPlanApi.ActivitySource.StartActivity())
            {
                try
                {
                    if (apiObject == null)
                    {
                        throw new ArgumentNullException(nameof(apiObject));
                    }

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
                }
                catch (Exception exception)
                {
                    act.AddException(exception);
                    throw;
                }
            }
        }

        public void Update(IEnumerable<ResourcePool> apiObjects)
        {
            throw new NotImplementedException();
        }

        internal DomResourcePool GetDomResourcePool(Guid domResourcePoolId)
        {
            using (var act = MediaOpsPlanApi.ActivitySource.StartActivity())
            {
                act.AddTag("ResourcePoolId", domResourcePoolId);

                try
                {
                    if (domResourcePoolId == Guid.Empty)
                    {
                        throw new ArgumentException("Resource pool ID cannot be empty.", nameof(domResourcePoolId));
                    }

                    return PlanApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(DomInstanceExposers.Id.Equal(domResourcePoolId)).FirstOrDefault();
                }
                catch (Exception exception)
                {
                    act.AddException(exception);
                    throw;
                }
            }
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

        private void ValidateNameInUseInDom(string name, Guid domResourcePoolId)
        {
            var existingDomResourcePool = PlanApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(DomInstanceExposers.FieldValues.DomInstanceField(StorageResourceStudio.SlcResource_StudioIds.Sections.ResourcePoolInfo.Name).Equal(name))
                .FirstOrDefault(x => x.ID.Id != domResourcePoolId && x.Status != StorageResourceStudio.SlcResource_StudioIds.Behaviors.Resourcepool_Behavior.StatusesEnum.Draft);
            if (existingDomResourcePool != null)
            {
                PlanApi.Logger.Information(this, $"Name '{name}' is already in use by a DOM resource pool with ID '{existingDomResourcePool.ID.Id}'.");
                throw new MediaOpsException(new ResourcePoolConfigurationError
                {
                    ErrorReason = ResourcePoolConfigurationError.Reason.NameExists,
                    ErrorMessage = "Name is already in use.",
                });
            }
        }
    }
}
