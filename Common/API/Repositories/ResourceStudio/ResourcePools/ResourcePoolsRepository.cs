namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.MediaOps.Plan.ActivityHelper;
    using Skyline.DataMiner.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.MediaOps.Plan.Extensions;
    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Net.Sections;

    using SLDataGateway.API.Types.Querying;

    using DomResourcePool = Storage.DOM.SlcResource_Studio.ResourcepoolInstance;
    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

    internal class ResourcePoolsRepository : Repository<ResourcePool>, IResourcePoolsRepository
    {
        public ResourcePoolsRepository(MediaOpsPlanApi planApi) : base(planApi)
        {
        }

        public long Count(FilterElement<ResourcePool> filter)
        {
            throw new NotImplementedException();
        }

        public long CountAll()
        {
            return DomResourcePoolHandler.CountAll(PlanApi);
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
            throw new NotImplementedException();
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

        public void Delete(params ResourcePool[] apiObjects)
        {
            throw new NotImplementedException();
        }

        public void Delete(params Guid[] apiObjectIds)
        {
            throw new NotImplementedException();
        }

        public void Delete(ResourcePool resourcepool, ResourcePoolDeleteOptions options)
        {
            throw new NotImplementedException();
        }

        public long DeprecatedResourceCount(ResourcePool resourcePool)
        {
            throw new NotImplementedException();
        }

        public bool HasDeprecatedResources(ResourcePool resourcePool)
        {
            throw new NotImplementedException();
        }

        public bool HasResources(ResourcePool resourcePool)
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

        public ResourcePool Read(Guid id)
        {
            PlanApi.Logger.LogInformation("Reading ResourcePool with ID: {id}...", id);

            if (id == Guid.Empty)
            {
                throw new ArgumentNullException(nameof(id));
            }

            return ActivityHelper.Track(nameof(ResourcePoolsRepository), nameof(Read), act =>
            {
               act?.AddTag("ResourcePoolId", id);
                var filter = DomInstanceExposers.DomDefinitionId.Equal(StorageResourceStudio.SlcResource_StudioIds.Definitions.Resourcepool.Id)
                        .AND(DomInstanceExposers.Id.Equal(id));
                var domResourcePool = PlanApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(filter)
                    .FirstOrDefault();

                if (domResourcePool == null)
                {
                   act?.AddTag("Hit", false);
                    return null;
                }

               act?.AddTag("Hit", true);
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
               act?.AddTag("RequestedResourcePoolCount", ids.Count());

                if (!ids.Any())
                {
                    return new Dictionary<Guid, ResourcePool>();
                }

                var retrievedPools = PlanApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(ids).SafeToDictionary(x => x.ID.Id, x => new ResourcePool(x));

               act?.AddTag("RetrievedResourcePoolCount", retrievedPools.Count);
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

        public long ResourceCount(ResourcePool resourcePool)
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
            throw new NotImplementedException();
        }

        public IQueryable<ResourcePool> Query()
        {
            return new ApiRepositoryQuery<ResourcePool>(QueryProvider);
        }

        public IQueryable<IEnumerable<ResourcePool>> QueryPaged()
        {
            throw new NotImplementedException();
        }

        protected internal override FilterElement<DomInstance> CreateFilter(string fieldName, Comparer comparer, object value)
        {
            switch (fieldName)
            {
                case nameof(ResourcePool.State):
                    return FilterElementFactory.Create(DomInstanceExposers.StatusId, comparer, TranslateResourcePoolState((ResourcePoolState)value));
            }

            return base.CreateFilter(fieldName, comparer, value);
        }

        protected internal override IOrderByElement CreateOrderBy(string fieldName, SortOrder sortOrder, bool naturalSort = false)
        {
            switch (fieldName)
            {
                case nameof(ResourcePool.State):
                    return OrderByElementFactory.Create(DomInstanceExposers.StatusId, sortOrder, naturalSort);
            }

            return base.CreateOrderBy(fieldName, sortOrder, naturalSort);
        }

        internal override IEnumerable<ResourcePool> Read(IQuery<DomInstance> query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            var domFilter = AddDomDefinitionFilter(query.Filter, StorageResourceStudio.SlcResource_StudioIds.Definitions.Resourcepool);

            query = query.WithFilter(domFilter);

            var domInstances = PlanApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(query);

            return domInstances.Select(x => new ResourcePool(x));
        }

        internal override long Count(FilterElement<DomInstance> domFilter)
        {
            return PlanApi.DomHelpers.SlcResourceStudioHelper.CountResourceStudioInstances(AddDomDefinitionFilter(domFilter, StorageResourceStudio.SlcResource_StudioIds.Definitions.Resourcepool));
        }

        internal DomResourcePool GetDomResourcePool(Guid domResourcePoolId)
        {
            if (domResourcePoolId == Guid.Empty)
            {
                throw new ArgumentException("Resource pool ID cannot be empty.", nameof(domResourcePoolId));
            }

            return ActivityHelper.Track(nameof(ResourcePoolsRepository), nameof(GetDomResourcePool), act =>
            {
               act?.AddTag("ResourcePoolId", domResourcePoolId);
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

        /// <summary>
        /// Translates the ResourceState enum to the state in DOM.
        /// </summary>
        /// <param name="state">State to be translated.</param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">If the provided state is not supported.</exception>
        private string TranslateResourcePoolState(ResourcePoolState state)
        {
            return state switch
            {
                ResourcePoolState.Draft => StorageResourceStudio.SlcResource_StudioIds.Behaviors.Resourcepool_Behavior.Statuses.Draft,
                ResourcePoolState.Complete => StorageResourceStudio.SlcResource_StudioIds.Behaviors.Resourcepool_Behavior.Statuses.Complete,
                ResourcePoolState.Deprecated => StorageResourceStudio.SlcResource_StudioIds.Behaviors.Resourcepool_Behavior.Statuses.Deprecated,
                _ => throw new NotSupportedException($"Resource Pool state '{state}' is not supported.")
            };
        }
    }
}
