namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Net.Sections;
    using Skyline.DataMiner.SDM;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.ActivityHelper;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
    using SLDataGateway.API.Types.Querying;

    using StorageResourceStudio = Storage.DOM.SlcResource_Studio;

    /// <summary>
    /// Provides repository operations for managing <see cref="ResourcePool"/> objects.
    /// </summary>
    internal class ResourcePoolsRepository : Repository, IResourcePoolsRepository
    {
        private readonly ResourcePoolPoolFilterTranslator filterTranslator = new ResourcePoolPoolFilterTranslator();

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourcePoolsRepository"/> class.
        /// </summary>
        /// <param name="planApi">The MediaOps Plan API instance.</param>
        public ResourcePoolsRepository(MediaOpsPlanApi planApi) : base(planApi)
        {
        }

        /// <summary>
        /// Assigns the specified resources to the given resource pool.
        /// </summary>
        /// <param name="resourcePool">The resource pool to which the resources will be assigned.</param>
        /// <param name="resources">The collection of resources to assign to the pool.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="resourcePool"/> or <paramref name="resources"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the resources collection contains a <c>null</c> resource.</exception>
        public void AssignResourcesToPool(ResourcePool resourcePool, IEnumerable<Resource> resources)
        {
            if (resourcePool == null)
            {
                throw new ArgumentNullException(nameof(resourcePool));
            }

            AssignResourcesToPool(resourcePool.Id, resources);
        }

        /// <summary>
        /// Assigns the specified resources to the given resource pool.
        /// </summary>
        /// <param name="resourcePoolId">The unique identifier of the resource pool to which the resources will be assigned.</param>
        /// <param name="resources">The collection of resources to assign to the pool.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="resourcePoolId"/> is <see cref="Guid.Empty"/> or when the resources collection contains a <c>null</c> resource.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="resources"/> is <c>null</c>.</exception>
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

        /// <summary>
        /// Gets the number of resource pools that match the specified filter.
        /// </summary>
        /// <param name="filter">The filter criteria to apply when counting resource pools.</param>
        /// <returns>The count of resource pools matching the filter.</returns>
        public long Count(FilterElement<ResourcePool> filter)
        {
            var domFilter = filterTranslator.Translate(filter);
            return PlanApi.DomHelpers.SlcResourceStudioHelper.CountResourceStudioInstances(domFilter);
        }

        /// <summary>
        /// Gets the total number of resource pools in the repository.
        /// </summary>
        /// <returns>The total count of resource pools.</returns>
        public long Count()
        {
            return Count(new TRUEFilterElement<ResourcePool>());
        }

        /// <summary>
        /// Gets the number of resource pools that match the specified query.
        /// </summary>
        /// <param name="query">The query criteria to apply when counting resource pools.</param>
        /// <returns>The count of resource pools matching the query.</returns>
        public long Count(IQuery<ResourcePool> query)
        {
            return Count(query.Filter);
        }

        /// <summary>
        /// Creates a new resource pool in the repository.
        /// </summary>
        /// <param name="apiObject">The resource pool to create.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObject"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when attempting to create an existing resource pool.</exception>
        /// <exception cref="MediaOpsException">Thrown when the creation operation fails.</exception>
        public void Create(ResourcePool apiObject)
        {
            PlanApi.Logger.LogInformation("Creating new ResourcePool...");

            if (apiObject == null)
            {
                throw new ArgumentNullException(nameof(apiObject));
            }

            ActivityHelper.Track(nameof(ResourcePoolsRepository), nameof(Create), act =>
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
            });
        }

        /// <summary>
        /// Creates multiple new resource pools in the repository.
        /// </summary>
        /// <param name="apiObjects">The collection of resource pools to create.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when attempting to create existing resource pools.</exception>
        /// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk creation operation fails.</exception>
        public void Create(IEnumerable<ResourcePool> apiObjects)
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

            if (!DomResourcePoolHandler.TryCreateOrUpdate(PlanApi, apiObjects.ToList(), out var result))
            {
                throw new MediaOpsBulkException<Guid>(result);
            }
        }

        /// <summary>
        /// Creates new resource pools or updates existing ones in the repository.
        /// </summary>
        /// <param name="apiObjects">The collection of resource pools to create or update.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
        /// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk operation fails.</exception>
        public void CreateOrUpdate(IEnumerable<ResourcePool> apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            ActivityHelper.Track(nameof(ResourcePoolsRepository), nameof(CreateOrUpdate), act =>
            {
                if (!DomResourcePoolHandler.TryCreateOrUpdate(PlanApi, apiObjects?.ToList(), out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var resourceIds = result.SuccessfulIds;
                act?.AddTag("Created Resource Pools", String.Join(", ", resourceIds));
                act?.AddTag("Created Resource Pools Count", resourceIds.Count);
            });
        }

        /// <summary>
        /// Deletes the specified resource pools from the repository.
        /// </summary>
        /// <param name="apiObjects">The resource pools to delete.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
        public void Delete(IEnumerable<ResourcePool> apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            Delete(apiObjects.Select(x => x.Id).ToArray());
        }

        /// <summary>
        /// Deletes resource pools with the specified identifiers from the repository.
        /// </summary>
        /// <param name="apiObjectIds">The unique identifiers of the resource pools to delete.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjectIds"/> is <c>null</c>.</exception>
        /// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk deletion operation fails.</exception>
        public void Delete(IEnumerable<Guid> apiObjectIds)
        {
            if (apiObjectIds == null)
            {
                throw new ArgumentNullException(nameof(apiObjectIds));
            }

            var resourcePoolsToDelete = Read(apiObjectIds);

            ActivityHelper.Track(nameof(CapabilitiesRepository), nameof(Delete), act =>
            {
                if (!DomResourcePoolHandler.TryDelete(PlanApi, resourcePoolsToDelete?.ToList(), out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var capabilityIds = result.SuccessfulIds;
                act?.AddTag("Removed Capabilities", String.Join(", ", capabilityIds));
                act?.AddTag("Removed Capabilities Count", capabilityIds.Count());
            });
        }

        /// <summary>
        /// Deletes the specified <see cref="ResourcePool"/> using the provided <see cref="ResourcePoolDeleteOptions"/>.
        /// </summary>
        /// <param name="resourcePool">The resource pool to delete.</param>
        /// <param name="options">Options specifying how the resource pool and its resources should be deleted.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="resourcePool"/> is <c>null</c>.</exception>
        /// <exception cref="MediaOpsException">Thrown when the deletion operation fails.</exception>
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

        /// <summary>
        /// Deletes the specified resource pool from the repository.
        /// </summary>
        /// <param name="oToDelete">The resource pool to delete.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="oToDelete"/> is <c>null</c>.</exception>
        public void Delete(ResourcePool oToDelete)
        {
            if (oToDelete == null)
            {
                throw new ArgumentNullException(nameof(oToDelete));
            }

            Delete([oToDelete]);
        }

        /// <summary>
        /// Deletes the specified resource pool from the repository.
        /// </summary>
        /// <param name="apiObjectId">The unique identifier of the resource pool to delete.</param>
        public void Delete(Guid apiObjectId)
        {
            Delete([apiObjectId]);
        }

        /// <summary>
        /// Deprecates the specified <see cref="ResourcePool"/> using the provided <see cref="ResourcePoolDeprecateOptions"/>.
        /// </summary>
        /// <param name="resourcePool">The resource pool to deprecate.</param>
        /// <param name="options">Options specifying how the resource pool and its resources should be deprecated.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="resourcePool"/> is <c>null</c>.</exception>
        /// <exception cref="MediaOpsException">Thrown when the deprecation operation fails.</exception>
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

        /// <summary>
        /// Retrieves a mapping of resource pools to their respective parent resource pools.
        /// </summary>
        /// <param name="resourcePools">A collection of resource pools for which to retrieve parent-child relationships.</param>
        /// <returns>A read-only dictionary where each key is a resource pool from the input collection, and the value is an enumerable of its parent resource pools.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="resourcePools"/> is <c>null</c>.</exception>
        public IReadOnlyDictionary<ResourcePool, IEnumerable<ResourcePool>> GetParentPoolLinks(IEnumerable<ResourcePool> resourcePools)
        {
            if (resourcePools == null)
            {
                throw new ArgumentNullException(nameof(resourcePools));
            }

            var apiPoolsById = resourcePools.ToDictionary(x => x.Id);

            var poolFilters = resourcePools
                .Select(x => DomInstanceExposers.FieldValues
                    .DomInstanceField(StorageResourceStudio.SlcResource_StudioIds.Sections.ResourcePoolLinks.LinkedResourcePool)
                    .Equal(x.Id))
                .ToArray();

            var filter = new ORFilterElement<DomInstance>(poolFilters)
                .AND(DomInstanceExposers.DomDefinitionId.Equal(StorageResourceStudio.SlcResource_StudioIds.Definitions.Resourcepool.Id));

            var domPools = PlanApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(filter);
            var parentApiPoolsById = domPools.Select(x => new ResourcePool(x)).ToDictionary(x => x.Id);

            var parentPoolsPerPool = resourcePools.ToDictionary(
                pool => pool,
                pool =>
                    domPools
                        .Where(domPool => domPool.ResourcePoolLinks
                        .Any(link => link.LinkedResourcePool.Value == pool.Id))
                        .Select(domPool =>
                        {
                            if (apiPoolsById.ContainsKey(domPool.ID.Id))
                            {
                                return apiPoolsById[domPool.ID.Id];
                            }
                            else
                            {
                                return parentApiPoolsById[domPool.ID.Id];
                            }
                        })
            );

            return parentPoolsPerPool;
        }

        /// <summary>
        /// Retrieves a mapping of resources to their associated resource pools.
        /// </summary>
        /// <param name="resources">A collection of resources for which to retrieve the associated resource pools.</param>
        /// <returns>A read-only dictionary where each key is a resource from the input collection, and the value is an enumerable of resource pools associated with that resource.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="resources"/> is <c>null</c>.</exception>
        public IReadOnlyDictionary<Resource, IEnumerable<ResourcePool>> GetPoolsPerResource(IEnumerable<Resource> resources)
        {
            if (resources == null)
            {
                throw new ArgumentNullException(nameof(resources));
            }

            var poolsPerId = Read(resources.SelectMany(x => x.OriginalInstance.ResourceInternalProperties.PoolIds).Distinct()).ToDictionary(x => x.Id);

            var resourcePoolsPerResource = new Dictionary<Resource, IEnumerable<ResourcePool>>();
            foreach (var resource in resources)
            {
                if (resourcePoolsPerResource.ContainsKey(resource))
                {
                    // Filter out duplicate resources provided through argument
                    continue;
                }

                List<ResourcePool> pools = new List<ResourcePool>();
                foreach (var poolId in resource.OriginalInstance.ResourceInternalProperties.PoolIds)
                {
                    if (poolsPerId.TryGetValue(poolId, out ResourcePool pool))
                    {
                        pools.Add(pool);
                    }
                }

                resourcePoolsPerResource.Add(resource, pools);
            }

            return resourcePoolsPerResource;
        }

        /// <summary>
        /// Retrieves a collection of resource pools associated with the specified resource.
        /// </summary>
        /// <param name="resource">The resource for which to retrieve the associated resource pools.</param>
        /// <returns>An enumerable collection of <see cref="ResourcePool"/> objects associated with the specified resource.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="resource"/> is <c>null</c>.</exception>
        public IEnumerable<ResourcePool> GetResourcePools(Resource resource)
        {
            if (resource == null)
            {
                throw new ArgumentNullException(nameof(resource));
            }

            return PlanApi.DomHelpers.SlcResourceStudioHelper.GetPoolsByResource(resource.Id)
                .Select(x => new ResourcePool(x));
        }

        /// <summary>
        /// Determines whether the specified resource pool contains any resources.
        /// </summary>
        /// <param name="resourcePool">The resource pool to check.</param>
        /// <returns><c>true</c> if the resource pool has resources; otherwise, <c>false</c>.</returns>
        public bool HasResources(ResourcePool resourcePool)
        {
            if (resourcePool == null)
            {
                throw new ArgumentNullException(nameof(resourcePool));
            }

            return PlanApi.Resources.Count(ResourceExposers.ResourcePoolIds.Contains(resourcePool.Id)) > 0;
        }

        /// <summary>
        /// Moves the specified <see cref="ResourcePool"/> to the desired state.
        /// </summary>
        /// <param name="resourcePool">The resource pool to move.</param>
        /// <param name="desiredState">The state to move the resource pool to.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="resourcePool"/> is <c>null</c>.</exception>
        public void MoveTo(ResourcePool resourcePool, ResourcePoolState desiredState)
        {
            if (resourcePool == null)
            {
                throw new ArgumentNullException(nameof(resourcePool));
            }

            MoveTo(resourcePool.Id, desiredState);
        }

        /// <summary>
        /// Moves the resource pool with the specified identifier to the desired state.
        /// </summary>
        /// <param name="resourcePoolId">The unique identifier of the resource pool to move.</param>
        /// <param name="desiredState">The state to move the resource pool to.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="resourcePoolId"/> is <see cref="Guid.Empty"/>.</exception>
        /// <exception cref="MediaOpsException">Thrown when the state transition is not supported or fails.</exception>
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
                    var error = new ResourcePoolInvalidStateError
                    {
                        ErrorMessage = $"Move to state '{desiredState}' is not supported.",
                        Id = resourcePoolId,
                    };

                    throw new MediaOpsException(error);
                }

                action(resourcePoolId);
            });
        }

        /// <summary>
        /// Reads a single resource pool by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the resource pool.</param>
        /// <returns>The resource pool with the specified identifier, or <c>null</c> if not found.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="id"/> is <see cref="Guid.Empty"/>.</exception>
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
                var resourcePool = Read(ResourcePoolExposers.Id.Equal(id)).FirstOrDefault();

                if (resourcePool == null)
                {
                    act?.AddTag("Hit", false);
                    return null;
                }

                act?.AddTag("Hit", true);
                return resourcePool;
            });
        }

        /// <summary>
        /// Reads multiple resource pools by their unique identifiers.
        /// </summary>
        /// <param name="ids">A collection of unique identifiers.</param>
        /// <returns>An enumerable collection of resource pools matching the specified identifiers.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="ids"/> is <c>null</c>.</exception>
        public IEnumerable<ResourcePool> Read(IEnumerable<Guid> ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            if (!ids.Any())
            {
                return Array.Empty<ResourcePool>();
            }

            return Read(new ORFilterElement<ResourcePool>(ids.Select(x => ResourcePoolExposers.Id.Equal(x)).ToArray()));
        }

        /// <summary>
        /// Reads resource pools that match the specified filter.
        /// </summary>
        /// <param name="filter">The filter criteria to apply when reading resource pools.</param>
        /// <returns>An enumerable collection of resource pools matching the filter.</returns>
        public IEnumerable<ResourcePool> Read(FilterElement<ResourcePool> filter)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            return ActivityHelper.Track(nameof(ResourcePoolsRepository), nameof(Read), act =>
            {
                var domFilter = filterTranslator.Translate(filter);
                IEnumerable<ResourcePool> Iterator()
                {
                    foreach (var domResourcePool in PlanApi.DomHelpers.SlcResourceStudioHelper.GetResourcePools(domFilter))
                    {
                        yield return new ResourcePool(domResourcePool);
                    }
                }

                return Iterator();
            });
        }

        /// <summary>
        /// Reads all resource pools from the repository.
        /// </summary>
        /// <returns>An enumerable collection of all resource pools.</returns>
        public IEnumerable<ResourcePool> Read()
        {
            return Read(new TRUEFilterElement<ResourcePool>());
        }

        /// <summary>
        /// Reads resource pools that match the specified query.
        /// </summary>
        /// <param name="query">The query criteria to apply when reading resource pools.</param>
        /// <returns>An enumerable collection of resource pools matching the query.</returns>
        public IEnumerable<ResourcePool> Read(IQuery<ResourcePool> query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            return Read(query.Filter);
        }

        /// <summary>
        /// Reads all resource pools in pages.
        /// </summary>
        /// <returns>An enumerable collection of pages, where each page contains a collection of resource pools.</returns>
        public IEnumerable<IPagedResult<ResourcePool>> ReadPaged()
        {
            return ReadPaged(new TRUEFilterElement<ResourcePool>());
        }

        /// <summary>
        /// Reads resource pools that match the specified filter in pages.
        /// </summary>
        /// <param name="filter">The filter criteria to apply when reading resource pools.</param>
        /// <returns>An enumerable collection of pages, where each page contains resource pools matching the filter.</returns>
        public IEnumerable<IPagedResult<ResourcePool>> ReadPaged(FilterElement<ResourcePool> filter)
        {
            return ReadPaged(filter, MediaOpsPlanApi.DefaultPageSize);
        }

        /// <summary>
        /// Reads resource pools that match the specified query in pages.
        /// </summary>
        /// <param name="query">The query criteria to apply when reading resource pools.</param>
        /// <returns>An enumerable collection of pages, where each page contains resource pools matching the query.</returns>
        public IEnumerable<IPagedResult<ResourcePool>> ReadPaged(IQuery<ResourcePool> query)
        {
            return ReadPaged(query.Filter);
        }

        /// <summary>
        /// Reads resource pools that match the specified filter in pages with a custom page size.
        /// </summary>
        /// <param name="filter">The filter criteria to apply when reading resource pools.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>An enumerable collection of pages, where each page contains up to the specified number of resource pools matching the filter.</returns>
        public IEnumerable<IPagedResult<ResourcePool>> ReadPaged(FilterElement<ResourcePool> filter, int pageSize)
        {
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            if (pageSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be greater than zero.");
            }

            var pageNumber = 0;
            var paramFilter = filterTranslator.Translate(filter);
            var items = PlanApi.DomHelpers.SlcResourceStudioHelper.GetResourcePoolsPaged(paramFilter, pageSize);
            var enumerator = items.GetEnumerator();
            var hasNext = enumerator.MoveNext();

            while (hasNext)
            {
                var page = enumerator.Current;
                hasNext = enumerator.MoveNext();
                yield return new PagedResult<ResourcePool>(page.Select(x => new ResourcePool(x)), pageNumber++, pageSize, hasNext);
            }
        }

        /// <summary>
        /// Reads resource pools that match the specified query in pages with a custom page size.
        /// </summary>
        /// <param name="query">The query criteria to apply when reading resource pools.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>An enumerable collection of pages, where each page contains up to the specified number of resource pools matching the query.</returns>
        public IEnumerable<IPagedResult<ResourcePool>> ReadPaged(IQuery<ResourcePool> query, int pageSize)
        {
            return ReadPaged(query.Filter, pageSize);
        }

        /// <summary>
        /// Reads all resource pools in pages.
        /// </summary>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>An enumerable collection of pages, where each page contains a collection of resource pools.</returns>
        public IEnumerable<IPagedResult<ResourcePool>> ReadPaged(int pageSize)
        {
            return ReadPaged(new TRUEFilterElement<ResourcePool>(), MediaOpsPlanApi.DefaultPageSize);
        }

        /// <summary>
        /// Gets the count of resources in the specified resource pool.
        /// </summary>
        /// <param name="resourcePool">The resource pool for which to count resources.</param>
        /// <returns>The number of resources in the pool.</returns>
        public long ResourceCount(ResourcePool resourcePool)
        {
            if (resourcePool == null)
            {
                throw new ArgumentNullException(nameof(resourcePool));
            }

            return PlanApi.Resources.Count(ResourceExposers.ResourcePoolIds.Contains(resourcePool.Id));
        }

        /// <summary>
        /// Removes the specified resources from the given resource pool.
        /// </summary>
        /// <param name="resourcePool">The resource pool from which the resources will be unassigned.</param>
        /// <param name="resources">The collection of resources to remove from the pool.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="resourcePool"/> or <paramref name="resources"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the resources collection contains a <c>null</c> resource.</exception>
        public void UnassignResourcesFromPool(ResourcePool resourcePool, IEnumerable<Resource> resources)
        {
            if (resourcePool == null)
            {
                throw new ArgumentNullException(nameof(resourcePool));
            }

            UnassignResourcesFromPool(resourcePool.Id, resources);
        }

        /// <summary>
        /// Removes the specified resources from the given resource pool.
        /// </summary>
        /// <param name="resourcePoolId">The unique identifier of the resource pool from which the resources will be unassigned.</param>
        /// <param name="resources">The collection of resources to remove from the pool.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="resourcePoolId"/> is <see cref="Guid.Empty"/> or when the resources collection contains a <c>null</c> resource.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="resources"/> is <c>null</c>.</exception>
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

        /// <summary>
        /// Updates an existing resource pool in the repository.
        /// </summary>
        /// <param name="apiObject">The resource pool to update.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObject"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when attempting to update a new resource pool that doesn't exist yet.</exception>
        /// <exception cref="MediaOpsException">Thrown when the update operation fails.</exception>
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

        /// <summary>
        /// Updates multiple existing resource pools in the repository.
        /// </summary>
        /// <param name="apiObjects">The collection of resource pools to update.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when attempting to update new resource pools that don't exist yet.</exception>
        /// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk update operation fails.</exception>
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

            if (!DomResourcePoolHandler.TryCreateOrUpdate(PlanApi, apiObjects.ToList(), out var result))
            {
                throw new MediaOpsBulkException<Guid>(result);
            }
        }

        /// <summary>
        /// Handles moving the resource pool to the Complete state.
        /// </summary>
        /// <param name="resourcePoolId">The unique identifier of the resource pool to transition.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="resourcePoolId"/> is <see cref="Guid.Empty"/>.</exception>
        /// <exception cref="MediaOpsException">Thrown when the resource pool doesn't exist or the completion operation fails.</exception>
        private void HandleMoveToCompleteAction(Guid resourcePoolId)
        {
            if (resourcePoolId == Guid.Empty)
            {
                throw new ArgumentException("Resource pool ID cannot be empty.", nameof(resourcePoolId));
            }

            var resourcePool = Read(resourcePoolId)
                ?? throw new MediaOpsException(
                    new ResourcePoolNotFoundError()
                    {
                        ErrorMessage = $"Resource pool with ID '{resourcePoolId}' does not exist.",
                        Id = resourcePoolId
                    });

            if (!DomResourcePoolHandler.TryComplete(PlanApi, [resourcePool], out var result))
            {
                throw new MediaOpsException(result.TraceDataPerItem[resourcePool.Id]);
            }
        }

        /// <summary>
        /// Handles moving the resource pool to the Deprecated state.
        /// </summary>
        /// <param name="resourcePoolId">The unique identifier of the resource pool to transition.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="resourcePoolId"/> is <see cref="Guid.Empty"/>.</exception>
        /// <exception cref="MediaOpsException">Thrown when the resource pool doesn't exist or the deprecation operation fails.</exception>
        private void HandleMoveToDeprecatedAction(Guid resourcePoolId)
        {
            if (resourcePoolId == Guid.Empty)
            {
                throw new ArgumentException("Resource pool ID cannot be empty.", nameof(resourcePoolId));
            }

            var resourcePool = Read(resourcePoolId)
                ?? throw new MediaOpsException(
                    new ResourcePoolNotFoundError()
                    {
                        ErrorMessage = $"Resource pool with ID '{resourcePoolId}' does not exist.",
                        Id = resourcePoolId
                    });

            if (!DomResourcePoolHandler.TryDeprecate(PlanApi, [resourcePool], out var result))
            {
                throw new MediaOpsException(result.TraceDataPerItem[resourcePool.Id]);
            }
        }
    }
}
