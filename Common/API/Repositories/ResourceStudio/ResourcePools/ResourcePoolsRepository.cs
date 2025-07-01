namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Skyline.DataMiner.MediaOps.Plan.API.Validators;
    using Skyline.DataMiner.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.MediaOps.Plan.Extensions;
    using Skyline.DataMiner.MediaOps.Plan.Storage.DOM;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Helper;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Net.Sections;
    using Skyline.DataMiner.Utils.DOM.Extensions;

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

            if (!apiObject.IsNew)
            {
                throw new MediaOpsException("Not possible to use method Create for existing resource pool. Use CreateOrUpdate or Update instead.");
            }

            if (apiObject.HasUserDefinedId)
            {
                ValidateIdNotInUse(apiObject.Id);
            }

            ValidateName(apiObject.Name);

            var domResourcePool = apiObject.GetInstanceWithChanges();
            domResourcePool.Save(PlanApi.DomHelpers.SlcResourceStudioHelper.DomHelper);

            return domResourcePool.ID.Id;
        }

        public IEnumerable<Guid> Create(IEnumerable<ResourcePool> apiObjects)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Guid> CreateOrUpdate(IEnumerable<ResourcePool> apiObjects)
        {
            throw new NotImplementedException(); ;
        }

        public void Delete(params ResourcePool[] objectApis)
        {
            if (objectApis == null)
            {
                throw new ArgumentNullException(nameof(objectApis));
            }

            PlanApi.DomHelpers.SlcResourceStudioHelper.DomHelper.DomInstances.DeleteInBatches(objectApis.Select(x => x.OriginalInstance.ToInstance()).ToList());
        }

        public void Delete(params Guid[] objectIds)
        {
            if (objectIds == null)
            {
                throw new ArgumentNullException(nameof(objectIds));
            }

            PlanApi.DomHelpers.SlcResourceStudioHelper.DomHelper.DomInstances.DeleteInBatches(objectIds.Select(x => new DomResourcePool(x).ToInstance()).ToList());
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
                return null;
            }

            return new ResourcePool(domResourcePool);
        }

        public IDictionary<Guid, ResourcePool> Read(IEnumerable<Guid> ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            FilterElement<DomInstance> createFilter(Guid id) =>
                DomInstanceExposers.DomDefinitionId.Equal(StorageResourceStudio.SlcResource_StudioIds.Definitions.Resourcepool.Id)
                .AND(DomInstanceExposers.Id.Equal(id));

            return FilterQueryExecutor.RetrieveFilteredItems(
                ids.Distinct(),
                x => createFilter(x),
                x => PlanApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(x))
                .SafeToDictionary(x => x.ID.Id, x => new ResourcePool(x));
        }

        public IEnumerable<ResourcePool> Read(FilterElement<ResourcePool> filter)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ResourcePool> ReadAll()
        {
            var filter = DomInstanceExposers.DomDefinitionId.Equal(StorageResourceStudio.SlcResource_StudioIds.Definitions.Resourcepool.Id);
            foreach (var domResourcePool in PlanApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(filter))
            {
                yield return new ResourcePool(domResourcePool);
            }
        }

        public IEnumerable<IEnumerable<ResourcePool>> ReadAllPage()
        {
            throw new NotImplementedException();
        }

        public void Update(ResourcePool apiObject)
        {
            if (apiObject == null)
            {
                throw new ArgumentNullException(nameof(apiObject));
            }

            if (!apiObject.HasChanges)
            {
                return;
            }

            if (apiObject.IsNew)
            {
                throw new MediaOpsException("Not possible to use method Update for a new resource pool. Use CreateOrUpdate or Create instead.");
            }

            if (apiObject.State == ResourcePoolState.Deprecated)
            {
                throw new MediaOpsException("Not allowed to update a resource pool in Deprecated state.");
            }

#warning lock DOM instance
            // todo: lock DOM instance

            var storedInstance = PlanApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(DomInstanceExposers.DomDefinitionId.Equal(StorageResourceStudio.SlcResource_StudioIds.Definitions.Resourcepool.Id)
                .AND(DomInstanceExposers.Id.Equal(apiObject.Id))).FirstOrDefault();
            if (storedInstance == null)
            {
                throw new MediaOpsException($"Resource pool with ID '{apiObject.Id}' no longer exists.");
            }


            var changeResult = DomChangeHandler.HandleChanges(apiObject.OriginalInstance, apiObject.GetInstanceWithChanges(), storedInstance);
            if (changeResult.HasErrors)
            {
                foreach (var error in changeResult.Errors)
                {
                    TraceData.Add(new ResourcePoolConfigurationError
                    {
                        ErrorReason = ResourcePoolConfigurationError.Reason.ValueAlreadyChanged,
                        ErrorMessage = error,
                    });
                }

                throw new MediaOpsException(TraceData);
            }

            var domResourcePool = new DomResourcePool(changeResult.Instance);

            bool hasCoreChanges = false;
            foreach (var id in changeResult.ChangedFieldDescriptorIds)
            {
                if (id == StorageResourceStudio.SlcResource_StudioIds.Sections.ResourcePoolInfo.Name.Id)
                {
                    ValidateName(domResourcePool.ResourcePoolInfo.Name);

                    if (apiObject.State != ResourcePoolState.Draft)
                    {
                        ValidateNameInUseInDom(domResourcePool.ResourcePoolInfo.Name, apiObject.Id);
                    }

                    hasCoreChanges = true;
                }
            }
            
            if (hasCoreChanges && apiObject.State == ResourcePoolState.Complete)
            {
                CreateOrUpdateCore(domResourcePool);
            }

            domResourcePool.Save(PlanApi.DomHelpers.SlcResourceStudioHelper.DomHelper);

            // Todo: unlock DOM instance
        }

        public void Update(IEnumerable<ResourcePool> apiObjects)
        {
            throw new NotImplementedException();
        }

        internal DomResourcePool GetDomResourcePool(Guid domResourcePoolId)
        {
            return PlanApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(DomInstanceExposers.Id.Equal(domResourcePoolId)).FirstOrDefault();
        }

        internal CoreResourcePool GetCoreResourcePool(Guid coreResourcePoolId)
        {
            if (coreResourcePoolId == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(coreResourcePoolId));
            }

            var coreResourcePoolsById = GetCoreResourcePools(new[] { coreResourcePoolId });
            if (!coreResourcePoolsById.TryGetValue(coreResourcePoolId, out var coreResourcePool))
            {
                return null;
            }

            return coreResourcePool;
        }

        internal IReadOnlyDictionary<Guid, CoreResourcePool> GetCoreResourcePools(IEnumerable<Guid> coreResourcePoolIds)
        {
            if (coreResourcePoolIds == null)
            {
                throw new ArgumentNullException(nameof(coreResourcePoolIds));
            }

            if (!coreResourcePoolIds.Any())
            {
                return new Dictionary<Guid, CoreResourcePool>();
            }

            var coreResourcePools = new List<CoreResourcePool>();
            foreach (var batch in coreResourcePoolIds.Where(x => x != Guid.Empty).Distinct().Batch(500))
            {
                coreResourcePools.AddRange(PlanApi.CoreHelpers.ResourceManagerHelper.GetResourcePools(batch.Select(x => new CoreResourcePool(x)).ToArray()));
            }

            return coreResourcePools.ToDictionary(x => x.ID);
        }

        internal IReadOnlyDictionary<string, IReadOnlyCollection<CoreResourcePool>> GetCoreResourcePools(IEnumerable<string> coreResourcePoolNames)
        {
            if (coreResourcePoolNames == null)
            {
                throw new ArgumentNullException(nameof(coreResourcePoolNames));
            }

            if (!coreResourcePoolNames.Any())
            {
                return new Dictionary<string, IReadOnlyCollection<CoreResourcePool>>();
            }

            var coreResourcePools = new List<CoreResourcePool>();
            foreach (var batch in coreResourcePoolNames.Where(x => !string.IsNullOrEmpty(x)).Distinct().Batch(500))
            {
                coreResourcePools.AddRange(PlanApi.CoreHelpers.ResourceManagerHelper.GetResourcePools(batch.Select(x => new CoreResourcePool { Name = x}).ToArray()));
            }

            return coreResourcePools
                .GroupBy(x => x.Name)
                .ToDictionary(x => x.Key, x => (IReadOnlyCollection<CoreResourcePool>)x.ToList());
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

            var coreResourcePool = new CoreResourcePool(Guid.NewGuid())
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

        private void CreateOrUpdateCore(IEnumerable<DomResourcePool> domResourcePools)
        {
            if (domResourcePools == null)
            {
                throw new ArgumentNullException(nameof(domResourcePools));
            }

            var coreResourcePoolsById = GetCoreResourcePools(domResourcePools
                .Where(x => x.ResourcePoolInternalProperties.ResourcePoolId != Guid.Empty)
                .Select(x => x.ResourcePoolInternalProperties.ResourcePoolId)
                .Distinct());



            /*
             * 
             * Get existing core resource pools
             * Check for if names needs to be validated
             * 
             * 
             */
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
