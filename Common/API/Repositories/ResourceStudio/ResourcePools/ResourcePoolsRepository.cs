namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.MediaOps.Plan.API.Validators;
    using Skyline.DataMiner.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;

    using CoreResourcePool = Skyline.DataMiner.Net.Messages.ResourcePool;
    using DomResourcePool = Storage.DOM.SlcResource_Studio.ResourcepoolInstance;
    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

    internal class ResourcePoolsRepository : RepositoryBase<ResourcePool>, IResourcePoolsRepository
    {
        public ResourcePoolsRepository(MediaOpsPlanApi planApi) : base(planApi)
        {
        }

        public Guid Create(ResourcePool apiObject)
        {
            if (apiObject == null)
            {
                throw new ArgumentNullException(nameof(apiObject));
            }

            if (apiObject.Id != Guid.Empty)
            {
                ValidateIdNotInUse(apiObject.Id);
            }

            ValidateName(apiObject.Name);

            var domResourcePool = (apiObject.Id != Guid.Empty) ? new DomResourcePool(apiObject.Id) : new DomResourcePool();
            domResourcePool.ResourcePoolInfo.Name = apiObject.Name;

            domResourcePool.Save(PlanApi.DomHelpers.SlcResourceStudioHelper.DomHelper);

            return domResourcePool.ID.Id;
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
            if (resourcePoolId == Guid.Empty)
            {
                throw new ArgumentException(nameof(resourcePoolId));
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

        public ResourcePool Read(Guid id)
        {
            throw new NotImplementedException();
        }

        public IDictionary<Guid, ResourcePool> Read(IEnumerable<Guid> ids)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ResourcePool> Read(FilterElement<ResourcePool> filter)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ResourcePool> ReadAll()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IEnumerable<ResourcePool>> ReadAllPage()
        {
            throw new NotImplementedException();
        }

        public Guid Update(ResourcePool apiObject)
        {
            throw new NotImplementedException();
        }

        internal DomResourcePool GetDomResourcePool(Guid domResourcePoolId)
        {
            return PlanApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(DomInstanceExposers.Id.Equal(domResourcePoolId)).FirstOrDefault();
        }

        internal CoreResourcePool GetCoreResourcePool(Guid coreResourcePoolId)
        {
            return PlanApi.CoreHelpers.ResourceManagerHelper.GetResourcePool(coreResourcePoolId);
        }

        private void HandleMoveToCompleteAction(Guid resourcePoolId)
        {
            var domResourcePool = GetDomResourcePool(resourcePoolId);

            if (domResourcePool.Status != StorageResourceStudio.SlcResource_StudioIds.Behaviors.Resourcepool_Behavior.StatusesEnum.Draft)
            {
                throw new MediaOpsException("Move to state Complete is only allowed for resource pools in Draft state.");
            }

            ValidateNameInUseInDom(domResourcePool.ResourcePoolInfo.Name, domResourcePool.ID.Id);

            CreateOrUpdateCore(domResourcePool);

            domResourcePool.Save(PlanApi.DomHelpers.SlcResourceStudioHelper.DomHelper);

            PlanApi.DomHelpers.SlcResourceStudioHelper.DomHelper.DomInstances.DoStatusTransition(domResourcePool, StorageResourceStudio.SlcResource_StudioIds.Behaviors.Resourcepool_Behavior.Transitions.Draft_To_Complete);
        }

        private void CreateOrUpdateCore(DomResourcePool domResourcePool)
        {
            if (domResourcePool.ResourcePoolInternalProperties.ResourcePoolId == Guid.Empty)
            {
                CreateCore(domResourcePool);
            }
            else
            {
                UpdateCore(domResourcePool);
            }
        }

        private void CreateCore(DomResourcePool domResourcePool)
        {
            ValidateNameInUseInCore(domResourcePool.ResourcePoolInfo.Name, domResourcePool.ResourcePoolInternalProperties.ResourcePoolId);

            var coreResourcePool = new CoreResourcePool
            {
                Name = domResourcePool.ResourcePoolInfo.Name,
            };

            coreResourcePool = PlanApi.CoreHelpers.ResourceManagerHelper.AddOrUpdateResourcePools(coreResourcePool)[0];
            domResourcePool.ResourcePoolInternalProperties.ResourcePoolId = coreResourcePool.ID;
        }

        private void UpdateCore(DomResourcePool domResourcePool)
        {
            var coreResourcePool = GetCoreResourcePool(domResourcePool.ResourcePoolInternalProperties.ResourcePoolId);
            if (coreResourcePool == null)
            {
                CreateCore(domResourcePool);
                return;
            }

            ValidateNameInUseInCore(domResourcePool.ResourcePoolInfo.Name, domResourcePool.ResourcePoolInternalProperties.ResourcePoolId);

            coreResourcePool.Name = domResourcePool.ResourcePoolInfo.Name;

            PlanApi.CoreHelpers.ResourceManagerHelper.AddOrUpdateResourcePools(coreResourcePool);
        }

        private void ValidateName(string name)
        {
            if (!InputValidator.ValidateEmptyText(name))
            {
                throw new MediaOpsException(new ResourcePoolConfigurationError
                {
                    ErrorReason = ResourcePoolConfigurationError.Reason.InvalidName,
                    ErrorMessage = "Name cannot be empty."
                });
            }

            if (!InputValidator.ValidateTextLength(name, 150))
            {
                throw new MediaOpsException(new ResourcePoolConfigurationError
                {
                    ErrorReason = ResourcePoolConfigurationError.Reason.InvalidName,
                    ErrorMessage = "Name cannot contain more than 150 characters."
                });
            }
        }

        private void ValidateIdNotInUse(Guid domResourcePoolId)
        {
            var foundInstances = PlanApi.DomHelpers.SlcResourceStudioHelper.CountResourceStudioInstances(DomInstanceExposers.Id.Equal(domResourcePoolId));
            if (foundInstances != 0)
            {
                PlanApi.Logger.Information(this, $"ID '{domResourcePoolId}' is already in use by a Resource Studio instance");
                throw new MediaOpsException(new ResourcePoolConfigurationError
                {
                    ErrorReason = ResourcePoolConfigurationError.Reason.IdInUse,
                    ErrorMessage = "ID is already in use."
                });
            }
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

        private void ValidateNameInUseInCore(string name, Guid coreResourcePoolId)
        {
            var existingCoreResourcePool = PlanApi.CoreHelpers.ResourceManagerHelper.GetResourcePools(new CoreResourcePool { Name = name })
                .FirstOrDefault(x => x.ID != coreResourcePoolId);
            if (existingCoreResourcePool != null)
            {
                PlanApi.Logger.Information(this, $"Name '{name}' is already in use by a CORE resource pool with ID '{existingCoreResourcePool.ID}'.");
                throw new MediaOpsException(new ResourcePoolConfigurationError
                {
                    ErrorReason = ResourcePoolConfigurationError.Reason.NameExists,
                    ErrorMessage = "Name is already in use.",
                });
            }
        }
    }
}
