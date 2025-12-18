namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Microsoft.Extensions.Logging;

    using Skyline.DataMiner.Net.Apps.DataMinerObjectModel;
    using Skyline.DataMiner.Net.Messages.SLDataGateway;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.ActivityHelper;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Extensions;
    using Skyline.DataMiner.Solutions.MediaOps.Plan.Storage.DOM.SlcResource_Studio;

    using SLDataGateway.API.Types.Querying;

    /// <summary>
    /// Provides repository operations for managing <see cref="Resource"/> objects.
    /// </summary>
    internal class ResourcesRepository : Repository, IResourcesRepository
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResourcesRepository"/> class.
        /// </summary>
        /// <param name="planApi">The MediaOps Plan API instance.</param>
        public ResourcesRepository(MediaOpsPlanApi planApi) : base(planApi)
        {
        }

        /// <summary>
        /// Converts the specified <see cref="Resource"/> to an <see cref="ElementResource"/>.
        /// </summary>
        /// <param name="resource">The resource to convert.</param>
        /// <param name="configuration">The configuration for the element link.</param>
        /// <returns>The converted <see cref="ElementResource"/>.</returns>
        /// <exception cref="MediaOpsException">Thrown when the resource is not in Draft state or conversion fails.</exception>
        public ElementResource ConvertToElementResource(Resource resource, ResourceElementLinkConfiguration configuration)
        {
            if (resource.State != ResourceState.Draft)
            {
                var error = new ResourceInvalidStateError()
                {
                    ErrorMessage = $"Resource {resource.Name} is not in Draft state. Cannot convert to ElementResource.",
                    Id = resource.Id,
                };

                throw new MediaOpsException(error);
            }

            if (resource is ElementResource elementResource)
            {
                return elementResource;
            }

            DomResourceHandler.ConvertToElementResource(PlanApi, resource, configuration);
            return (ElementResource)Read(resource.Id);
        }

        /// <summary>
        /// Converts the resource with the specified identifier to an <see cref="ElementResource"/>.
        /// </summary>
        /// <param name="resourceId">The unique identifier of the resource to convert.</param>
        /// <param name="configuration">The configuration for the element link.</param>
        /// <returns>The converted <see cref="ElementResource"/>.</returns>
        /// <exception cref="ResourceNotFoundException">Thrown when the resource with the specified identifier is not found.</exception>
        /// <exception cref="MediaOpsException">Thrown when the resource is not in Draft state or conversion fails.</exception>
        public ElementResource ConvertToElementResource(Guid resourceId, ResourceElementLinkConfiguration configuration)
        {
            var resource = Read(resourceId)
                ?? throw new MediaOpsException(
                    new ResourceNotFoundError()
                    {
                        ErrorMessage = $"Unable to find resource with ID {resourceId}",
                        Id = resourceId,
                    });

            return ConvertToElementResource(resource, configuration);
        }

        /// <summary>
        /// Converts the specified <see cref="Resource"/> to a <see cref="ServiceResource"/>.
        /// </summary>
        /// <param name="resource">The resource to convert.</param>
        /// <param name="configuration">The configuration for the service link.</param>
        /// <returns>The converted <see cref="ServiceResource"/>.</returns>
        /// <exception cref="MediaOpsException">Thrown when the resource is not in Draft state or conversion fails.</exception>
        public ServiceResource ConvertToServiceResource(Resource resource, ResourceServiceLinkConfiguration configuration)
        {
            if (resource.State != ResourceState.Draft)
            {
                var error = new ResourceInvalidStateError()
                {
                    ErrorMessage = $"Resource {resource.Name} is not in Draft state. Cannot convert to ServiceResource.",
                    Id = resource.Id,
                };

                throw new MediaOpsException(error);
            }

            if (resource is ServiceResource serviceResource)
            {
                return serviceResource;
            }

            DomResourceHandler.ConvertToServiceResource(PlanApi, resource, configuration);
            return (ServiceResource)Read(resource.Id);
        }

        /// <summary>
        /// Converts the resource with the specified identifier to a <see cref="ServiceResource"/>.
        /// </summary>
        /// <param name="resourceId">The unique identifier of the resource to convert.</param>
        /// <param name="configuration">The configuration for the service link.</param>
        /// <returns>The converted <see cref="ServiceResource"/>.</returns>
        /// <exception cref="ResourceNotFoundException">Thrown when the resource with the specified identifier is not found.</exception>
        /// <exception cref="MediaOpsException">Thrown when the resource is not in Draft state or conversion fails.</exception>
        public ServiceResource ConvertToServiceResource(Guid resourceId, ResourceServiceLinkConfiguration configuration)
        {
            var resource = Read(resourceId)
                ?? throw new MediaOpsException(
                    new ResourceNotFoundError()
                    {
                        ErrorMessage = $"Unable to find resource with ID {resourceId}",
                        Id = resourceId
                    });

            return ConvertToServiceResource(resource, configuration);
        }

        /// <summary>
        /// Converts the specified <see cref="Resource"/> to an <see cref="UnmanagedResource"/>.
        /// </summary>
        /// <param name="resource">The resource to convert.</param>
        /// <returns>The converted <see cref="UnmanagedResource"/>.</returns>
        /// <exception cref="MediaOpsException">Thrown when the resource is not in Draft state or conversion fails.</exception>
        public UnmanagedResource ConvertToUnmanagedResource(Resource resource)
        {
            if (resource.State != ResourceState.Draft)
            {
                var error = new ResourceInvalidStateError()
                {
                    ErrorMessage = $"Resource {resource.Name} is not in Draft state. Cannot convert to UnmanagedResource.",
                    Id = resource.Id,
                };

                throw new MediaOpsException(error);
            }

            if (resource is UnmanagedResource unmanagedResource)
            {
                return unmanagedResource;
            }

            DomResourceHandler.ConvertToUnmanagedResource(PlanApi, resource);
            return (UnmanagedResource)Read(resource.Id);
        }

        /// <summary>
        /// Converts the resource with the specified identifier to an <see cref="UnmanagedResource"/>.
        /// </summary>
        /// <param name="resourceId">The unique identifier of the resource to convert.</param>
        /// <returns>The converted <see cref="UnmanagedResource"/>.</returns>
        /// <exception cref="ResourceNotFoundException">Thrown when the resource with the specified identifier is not found.</exception>
        /// <exception cref="MediaOpsException">Thrown when the resource is not in Draft state or conversion fails.</exception>
        public UnmanagedResource ConvertToUnmanagedResource(Guid resourceId)
        {
            var resource = Read(resourceId)
                ?? throw new MediaOpsException(
                    new ResourceNotFoundError()
                    {
                        ErrorMessage = $"Unable to find resource with ID {resourceId}",
                        Id = resourceId,
                    });

            return ConvertToUnmanagedResource(resource);
        }

        /// <summary>
        /// Converts the specified <see cref="Resource"/> to a <see cref="VirtualFunctionResource"/>.
        /// </summary>
        /// <param name="resource">The resource to convert.</param>
        /// <param name="configuration">The configuration for the virtual function link.</param>
        /// <returns>The converted <see cref="VirtualFunctionResource"/>.</returns>
        /// <exception cref="MediaOpsException">Thrown when the resource is not in Draft state or conversion fails.</exception>
        public VirtualFunctionResource ConvertToVirtualFunctionResource(Resource resource, ResourceVirtualFunctionLinkConfiguration configuration)
        {
            if (resource.State != ResourceState.Draft)
            {
                var error = new ResourceInvalidStateError()
                {
                    ErrorMessage = $"Resource {resource.Name} is not in Draft state. Cannot convert to VirtualFunctionResource.",
                    Id = resource.Id,
                };

                throw new MediaOpsException(error);
            }

            if (resource is VirtualFunctionResource virtualFunctionResource)
            {
                return virtualFunctionResource;
            }

            DomResourceHandler.ConvertToVirtualFunctionResource(PlanApi, resource, configuration);
            return (VirtualFunctionResource)Read(resource.Id);
        }

        /// <summary>
        /// Converts the resource with the specified identifier to a <see cref="VirtualFunctionResource"/>.
        /// </summary>
        /// <param name="resourceId">The unique identifier of the resource to convert.</param>
        /// <param name="configuration">The configuration for the virtual function link.</param>
        /// <returns>The converted <see cref="VirtualFunctionResource"/>.</returns>
        /// <exception cref="ResourceNotFoundException">Thrown when the resource with the specified identifier is not found.</exception>
        /// <exception cref="MediaOpsException">Thrown when the resource is not in Draft state or conversion fails.</exception>
        public VirtualFunctionResource ConvertToVirtualFunctionResource(Guid resourceId, ResourceVirtualFunctionLinkConfiguration configuration)
        {
            var resource = Read(resourceId)
                ?? throw new MediaOpsException(
                    new ResourceNotFoundError()
                    {
                        ErrorMessage = $"Unable to find resource with ID {resourceId}",
                        Id = resourceId
                    });

            return ConvertToVirtualFunctionResource(resource, configuration);
        }

        /// <summary>
        /// Gets the total number of resources in the repository.
        /// </summary>
        /// <returns>The total count of resources.</returns>
        public long Count()
        {
            return PlanApi.DomHelpers.SlcResourceStudioHelper.CountResourceStudioInstances(
                DomInstanceExposers.DomDefinitionId.Equal(SlcResource_StudioIds.Definitions.Resource.Id));
        }

        /// <summary>
        /// Gets the number of resources that match the specified filter.
        /// </summary>
        /// <param name="filter">The filter criteria to apply when counting resources.</param>
        /// <returns>The count of resources matching the filter.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public long Count(FilterElement<Resource> filter)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the number of resources that match the specified query.
        /// </summary>
        /// <param name="query">The query criteria to apply when counting resources.</param>
        /// <returns>The count of resources matching the query.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public long Count(IQuery<Resource> query)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a new resource in the repository.
        /// </summary>
        /// <param name="apiObject">The resource to create.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObject"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when attempting to create an existing resource.</exception>
        /// <exception cref="MediaOpsException">Thrown when the creation operation fails.</exception>
        public void Create(Resource apiObject)
        {
            if (apiObject == null)
            {
                throw new ArgumentNullException(nameof(apiObject));
            }

            PlanApi.Logger.LogInformation($"Creating new Resource {apiObject.Name}...");

            ActivityHelper.Track(nameof(ResourcesRepository), nameof(Create), act =>
            {
                if (!apiObject.IsNew)
                {
                    throw new InvalidOperationException("Not possible to use method Create for existing resources. Use CreateOrUpdate or Update instead.");
                }

                if (!DomResourceHandler.TryCreateOrUpdate(PlanApi, [apiObject], out var result))
                {
                    throw new MediaOpsException(result.TraceDataPerItem[apiObject.Id]);
                }

                var resourceId = result.SuccessfulIds.First();
                act?.AddTag("ResourceId", resourceId);
            });
        }

        /// <summary>
        /// Creates multiple new resources in the repository.
        /// </summary>
        /// <param name="apiObjects">The collection of resources to create.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when attempting to create existing resources.</exception>
        /// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk creation operation fails.</exception>
        public void Create(IEnumerable<Resource> apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            ActivityHelper.Track(nameof(ResourcesRepository), nameof(Create), act =>
            {
                var existingResources = apiObjects.Where(x => !x.IsNew);
                if (existingResources.Any())
                {
                    throw new InvalidOperationException("Not possible to use method Create for existing resources. Use CreateOrUpdate or Update instead.");
                }

                if (!DomResourceHandler.TryCreateOrUpdate(PlanApi, apiObjects.ToList(), out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var resourceIds = result.SuccessfulIds;
                act?.AddTag("ResourceIds", String.Join(", ", resourceIds));
            });
        }

        /// <summary>
        /// Creates new resources or updates existing ones in the repository.
        /// </summary>
        /// <param name="apiObjects">The collection of resources to create or update.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
        /// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk operation fails.</exception>
        public void CreateOrUpdate(IEnumerable<Resource> apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            ActivityHelper.Track(nameof(ResourcesRepository), nameof(CreateOrUpdate), act =>
            {
                if (!DomResourceHandler.TryCreateOrUpdate(PlanApi, apiObjects?.ToList(), out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var resourceIds = result.SuccessfulIds;
                act?.AddTag("Created or Updated Resources", String.Join(", ", resourceIds));
                act?.AddTag("Created or Updated Resources Count", resourceIds.Count);
            });
        }

        /// <summary>
        /// Deletes the specified resources from the repository.
        /// </summary>
        /// <param name="apiObjects">The resources to delete.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
        public void Delete(params Resource[] apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            Delete(apiObjects.Select(x => x.Id).ToArray());
        }

        /// <summary>
        /// Deletes resources with the specified identifiers from the repository.
        /// </summary>
        /// <param name="apiObjectIds">The unique identifiers of the resources to delete.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjectIds"/> is <c>null</c>.</exception>
        /// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk deletion operation fails.</exception>
        public void Delete(params Guid[] apiObjectIds)
        {
            if (apiObjectIds == null)
            {
                throw new ArgumentNullException(nameof(apiObjectIds));
            }

            PlanApi.Logger.LogInformation("Deleting Resources {resourceIds}...", String.Join(", ", apiObjectIds));

            var resourcesToDelete = Read(apiObjectIds);

            ActivityHelper.Track(nameof(ResourcesRepository), nameof(Delete), act =>
            {
                if (!DomResourceHandler.TryDelete(PlanApi, resourcesToDelete?.ToList(), out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var resourceIds = result.SuccessfulIds;
                act?.AddTag("Removed Resources", String.Join(", ", resourceIds));
                act?.AddTag("Removed Resources Count", resourceIds.Count);
            });
        }

        /// <summary>
        /// Marks the specified resources as deprecated, indicating that they are no longer recommended for use.
        /// </summary>
        /// <param name="resources">A collection of resources to be marked as deprecated.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="resources"/> is <c>null</c>.</exception>
        /// <exception cref="MediaOpsBulkException{Guid}">Thrown when the deprecation operation fails.</exception>
        public void Deprecate(IEnumerable<Resource> resources)
        {
            if (resources == null)
            {
                throw new ArgumentNullException(nameof(resources));
            }

            ActivityHelper.Track(nameof(ResourcesRepository), nameof(Deprecate), act =>
            {
                if (!DomResourceHandler.TryDeprecate(PlanApi, resources?.ToList(), out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var resourceIds = result.SuccessfulIds;
                act?.AddTag("Deprecated Resources", String.Join(", ", resourceIds));
                act?.AddTag("Deprecated Resources Count", resourceIds.Count);
            });
        }

        /// <summary>
        /// Retrieves all resources in the specified resource pool.
        /// </summary>
        /// <param name="resourcePool">The resource pool for which to retrieve resources.</param>
        /// <returns>An enumerable collection of resources in the pool.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="resourcePool"/> is <c>null</c>.</exception>
        public IEnumerable<Resource> GetResourcesInPool(ResourcePool resourcePool)
        {
            if (resourcePool == null)
            {
                throw new ArgumentNullException(nameof(resourcePool));
            }

            return Resource.InstantiateResources(PlanApi, PlanApi.DomHelpers.SlcResourceStudioHelper.GetResourcesByPool(resourcePool.Id));
        }

        /// <summary>
        /// Retrieves all resources in the specified resource pool that are in the specified state.
        /// </summary>
        /// <param name="resourcePool">The resource pool for which to retrieve resources.</param>
        /// <param name="state">The state to filter resources by.</param>
        /// <returns>An enumerable collection of resources in the pool with the specified state.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="resourcePool"/> is <c>null</c>.</exception>
        public IEnumerable<Resource> GetResourcesInPool(ResourcePool resourcePool, ResourceState state)
        {
            if (resourcePool == null)
            {
                throw new ArgumentNullException(nameof(resourcePool));
            }

            var filter = DomInstanceExposers.FieldValues.DomInstanceField(SlcResource_StudioIds.Sections.ResourceInternalProperties.Pool_Ids)
                .Contains(Convert.ToString(resourcePool.Id))
                .AND(DomInstanceExposers.StatusId.Equal(SlcResource_StudioIds.Behaviors.Resource_Behavior.Statuses.ToValue(EnumExtensions.MapEnum<ResourceState, SlcResource_StudioIds.Behaviors.Resource_Behavior.StatusesEnum>(state))));

            return Resource.InstantiateResources(PlanApi, PlanApi.DomHelpers.SlcResourceStudioHelper.GetResources(filter));
        }

        /// <summary>
        /// Retrieves a mapping of resource pools to their contained resources.
        /// </summary>
        /// <param name="resourcePools">A collection of resource pools for which to retrieve resources.</param>
        /// <returns>A read-only dictionary where each key is a resource pool and the value is an enumerable of resources in that pool.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="resourcePools"/> is <c>null</c>.</exception>
        public IReadOnlyDictionary<ResourcePool, IEnumerable<Resource>> GetResourcesPerPool(IEnumerable<ResourcePool> resourcePools)
        {
            if (resourcePools == null)
            {
                throw new ArgumentNullException(nameof(resourcePools));
            }

            var domResources = PlanApi.DomHelpers.SlcResourceStudioHelper.GetAllResourcesInPools(resourcePools.Select(x => x.Id));
            var apiResourcesById = Resource.InstantiateResources(PlanApi, domResources).ToDictionary(x => x.Id);

            var resourcesPerPool = resourcePools.ToDictionary(
                pool => pool,
                pool =>
                    domResources
                        .Where(x => x.ResourceInternalProperties.PoolIds.Contains(pool.Id))
                        .Select(x => apiResourcesById[x.ID.Id])
            );

            return resourcesPerPool;
        }

        /// <summary>
        /// Retrieves a mapping of resource pools to their contained resources that are in the specified state.
        /// </summary>
        /// <param name="resourcePools">A collection of resource pools for which to retrieve resources.</param>
        /// <param name="state">The state to filter resources by.</param>
        /// <returns>A read-only dictionary where each key is a resource pool and the value is an enumerable of resources in that pool with the specified state.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="resourcePools"/> is <c>null</c>.</exception>
        public IReadOnlyDictionary<ResourcePool, IEnumerable<Resource>> GetResourcesPerPool(IEnumerable<ResourcePool> resourcePools, ResourceState state)
        {
            if (resourcePools == null)
            {
                throw new ArgumentNullException(nameof(resourcePools));
            }

            var resourceFilters = resourcePools
                .Select(x => DomInstanceExposers.FieldValues
                    .DomInstanceField(SlcResource_StudioIds.Sections.ResourceInternalProperties.Pool_Ids)
                    .Contains(Convert.ToString(x.Id)))
                .ToArray();

            var filter = new ORFilterElement<DomInstance>(resourceFilters)
                .AND(DomInstanceExposers.StatusId.Equal(SlcResource_StudioIds.Behaviors.Resource_Behavior.Statuses.ToValue(EnumExtensions.MapEnum<ResourceState, SlcResource_StudioIds.Behaviors.Resource_Behavior.StatusesEnum>(state))));

            var domResources = PlanApi.DomHelpers.SlcResourceStudioHelper.GetResources(filter);
            var apiResourcesById = Resource.InstantiateResources(PlanApi, domResources).ToDictionary(x => x.Id);

            var resourcesPerPool = resourcePools.ToDictionary(
                pool => pool,
                pool =>
                    domResources
                        .Where(x => x.ResourceInternalProperties.PoolIds.Contains(pool.Id))
                        .Select(x => apiResourcesById[x.ID.Id])
            );

            return resourcesPerPool;
        }

        /// <summary>
        /// Determines whether the specified resource pool contains any resources.
        /// </summary>
        /// <param name="resourcePool">The resource pool to check.</param>
        /// <returns><c>true</c> if the resource pool has resources; otherwise, <c>false</c>.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public bool HasResources(ResourcePool resourcePool)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Moves the specified <see cref="Resource"/> to the desired state.
        /// </summary>
        /// <param name="resource">The resource to move.</param>
        /// <param name="desiredState">The state to move the resource to.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="resource"/> is <c>null</c>.</exception>
        public void MoveTo(Resource resource, ResourceState desiredState)
        {
            if (resource == null)
            {
                throw new ArgumentNullException(nameof(resource));
            }

            MoveTo(resource.Id, desiredState);
        }

        /// <summary>
        /// Moves the resource with the specified identifier to the desired state.
        /// </summary>
        /// <param name="resourceId">The unique identifier of the resource to move.</param>
        /// <param name="desiredState">The state to move the resource to.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="resourceId"/> is <see cref="Guid.Empty"/>.</exception>
        /// <exception cref="MediaOpsException">Thrown when the resource is not found, the state transition is not supported, or the operation fails.</exception>
        public void MoveTo(Guid resourceId, ResourceState desiredState)
        {
            PlanApi.Logger.LogInformation("Moving Resource {resourceId} to {desiredState}...", resourceId, desiredState);

            if (resourceId == Guid.Empty)
            {
                throw new ArgumentException("Resource ID cannot be empty.", nameof(resourceId));
            }

            var resource = Read(resourceId);
            if (resource == null)
            {
                throw new MediaOpsException(
                    new ResourceNotFoundError()
                    {
                        ErrorMessage = $"Unable to find resource with ID {resourceId}",
                        Id = resourceId
                    });
            }

            ActivityHelper.Track(nameof(ResourcesRepository), nameof(MoveTo), act =>
            {
                act?.AddTag("ResourceId", resourceId);
                act?.AddTag("DesiredState", desiredState);

                var actionMethods = new Dictionary<ResourceState, Action<Resource>>
                {
                    [ResourceState.Complete] = HandleMoveToCompleteAction,
                    [ResourceState.Deprecated] = HandleMoveToDeprecatedAction,
                };

                if (!actionMethods.TryGetValue(desiredState, out var action))
                {
                    var error = new ResourceInvalidStateError()
                    {
                        ErrorMessage = $"Move to state '{desiredState}' is not supported.",
                        Id = resource.Id,
                    };

                    throw new MediaOpsException(error);
                }

                action(resource);
            });
        }

        /// <summary>
        /// Reads a single resource by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the resource.</param>
        /// <returns>The resource with the specified identifier, or <c>null</c> if not found.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="id"/> is <see cref="Guid.Empty"/>.</exception>
        public Resource Read(Guid id)
        {
            PlanApi.Logger.LogInformation($"Reading Resource with ID: {id}...");

            if (id == Guid.Empty)
            {
                throw new ArgumentException(nameof(id));
            }

            return ActivityHelper.Track(nameof(ResourcesRepository), nameof(Read), act =>
            {
                act?.AddTag("ResourceId", id);
                var filter = DomInstanceExposers.DomDefinitionId.Equal(Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Definitions.Resource.Id)
                        .AND(DomInstanceExposers.Id.Equal(id));
                var domResource = PlanApi.DomHelpers.SlcResourceStudioHelper.GetResources(filter)
                    .FirstOrDefault();

                if (domResource == null)
                {
                    act?.AddTag("Hit", false);
                    return null;
                }

                act?.AddTag("Hit", true);

                return Resource.InstantiateResources(PlanApi, [domResource]).FirstOrDefault();
            });
        }

        /// <summary>
        /// Reads multiple resources by their unique identifiers.
        /// </summary>
        /// <param name="ids">A collection of unique identifiers.</param>
        /// <returns>An enumerable collection of resources matching the specified identifiers.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="ids"/> is <c>null</c>.</exception>
        public IEnumerable<Resource> Read(IEnumerable<Guid> ids)
        {
            if (ids == null)
            {
                throw new ArgumentNullException(nameof(ids));
            }

            return ActivityHelper.Track(nameof(ResourcesRepository), nameof(Read), act =>
            {
                act?.AddTag("ResourceIds", String.Join(", ", ids));
                act?.AddTag("ResourceIds Count", ids.Count());

                var resources = PlanApi.DomHelpers.SlcResourceStudioHelper.GetResources(ids);
                return Resource.InstantiateResources(PlanApi, resources);
            });
        }

        /// <summary>
        /// Reads all resources from the repository.
        /// </summary>
        /// <returns>An enumerable collection of all resources.</returns>
        public IEnumerable<Resource> Read()
        {
            return ActivityHelper.Track(nameof(ResourcesRepository), nameof(Read), act =>
            {
                var filter = DomInstanceExposers.DomDefinitionId.Equal(Storage.DOM.SlcResource_Studio.SlcResource_StudioIds.Definitions.Resource.Id);
                return Resource.InstantiateResources(PlanApi, PlanApi.DomHelpers.SlcResourceStudioHelper.GetResources(filter));
            });
        }

        /// <summary>
        /// Reads resources that match the specified filter.
        /// </summary>
        /// <param name="filter">The filter criteria to apply when reading resources.</param>
        /// <returns>An enumerable collection of resources matching the filter.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public IEnumerable<Resource> Read(FilterElement<Resource> filter)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reads resources that match the specified query.
        /// </summary>
        /// <param name="query">The query criteria to apply when reading resources.</param>
        /// <returns>An enumerable collection of resources matching the query.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public IEnumerable<Resource> Read(IQuery<Resource> query)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reads all resources in pages.
        /// </summary>
        /// <returns>An enumerable collection of pages, where each page contains a collection of resources.</returns>
        public IEnumerable<IEnumerable<Resource>> ReadPaged()
        {
            return PlanApi.DomHelpers.SlcResourceStudioHelper.GetAllResourcesPaged()
                .Select(page => Resource.InstantiateResources(PlanApi, page));
        }

        /// <summary>
        /// Reads all resources in pages.
        /// </summary>
        /// <returns>An enumerable collection of pages, where each page contains a collection of resources.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        IEnumerable<IPagedResult<Resource>> IPageableRepository<Resource>.ReadPaged()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reads resources that match the specified filter in pages.
        /// </summary>
        /// <param name="filter">The filter criteria to apply when reading resources.</param>
        /// <returns>An enumerable collection of pages, where each page contains resources matching the filter.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public IEnumerable<IPagedResult<Resource>> ReadPaged(FilterElement<Resource> filter)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reads resources that match the specified query in pages.
        /// </summary>
        /// <param name="query">The query criteria to apply when reading resources.</param>
        /// <returns>An enumerable collection of pages, where each page contains resources matching the query.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public IEnumerable<IPagedResult<Resource>> ReadPaged(IQuery<Resource> query)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reads resources that match the specified filter in pages with a custom page size.
        /// </summary>
        /// <param name="filter">The filter criteria to apply when reading resources.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>An enumerable collection of pages, where each page contains up to the specified number of resources matching the filter.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public IEnumerable<IPagedResult<Resource>> ReadPaged(FilterElement<Resource> filter, int pageSize)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reads resources that match the specified query in pages with a custom page size.
        /// </summary>
        /// <param name="query">The query criteria to apply when reading resources.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>An enumerable collection of pages, where each page contains up to the specified number of resources matching the query.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public IEnumerable<IPagedResult<Resource>> ReadPaged(IQuery<Resource> query, int pageSize)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the count of resources in the specified resource pool.
        /// </summary>
        /// <param name="resourcePool">The resource pool for which to count resources.</param>
        /// <returns>The number of resources in the pool.</returns>
        /// <exception cref="NotImplementedException">This method is not yet implemented.</exception>
        public long ResourceCount(ResourcePool resourcePool)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Attempts to convert the specified <see cref="Resource"/> to an <see cref="ElementResource"/>.
        /// </summary>
        /// <param name="resource">The resource to convert.</param>
        /// <param name="configuration">The configuration for the element link.</param>
        /// <param name="elementResource">When this method returns, contains the converted <see cref="ElementResource"/>, if the conversion succeeded; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the conversion succeeded; otherwise, <c>false</c>.</returns>
        public bool TryConvertToElementResource(Resource resource, ResourceElementLinkConfiguration configuration, out ElementResource elementResource)
        {
            elementResource = null;

            try
            {
                elementResource = ConvertToElementResource(resource, configuration);
                return true;
            }
            catch (Exception e)
            {
                PlanApi.Logger.LogWarning($"Unable to convert Resource {resource.Name} [{resource.Id}] to an Element Resource because of: {e}");
                return false;
            }
        }

        /// <summary>
        /// Attempts to convert the resource with the specified identifier to an <see cref="ElementResource"/>.
        /// </summary>
        /// <param name="resourceId">The unique identifier of the resource to convert.</param>
        /// <param name="configuration">The configuration for the element link.</param>
        /// <param name="elementResource">When this method returns, contains the converted <see cref="ElementResource"/>, if the conversion succeeded; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the conversion succeeded; otherwise, <c>false</c>.</returns>
        public bool TryConvertToElementResource(Guid resourceId, ResourceElementLinkConfiguration configuration, out ElementResource elementResource)
        {
            elementResource = null;

            try
            {
                elementResource = ConvertToElementResource(resourceId, configuration);
                return true;
            }
            catch (Exception e)
            {
                PlanApi.Logger.LogWarning($"Unable to convert Resource with ID {resourceId} to an Element Resource because of: {e}");
                return false;
            }
        }

        /// <summary>
        /// Attempts to convert the specified <see cref="Resource"/> to a <see cref="ServiceResource"/>.
        /// </summary>
        /// <param name="resource">The resource to convert.</param>
        /// <param name="configuration">The configuration for the service link.</param>
        /// <param name="serviceResource">When this method returns, contains the converted <see cref="ServiceResource"/>, if the conversion succeeded; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the conversion succeeded; otherwise, <c>false</c>.</returns>
        public bool TryConvertToServiceResource(Resource resource, ResourceServiceLinkConfiguration configuration, out ServiceResource serviceResource)
        {
            serviceResource = null;

            try
            {
                serviceResource = ConvertToServiceResource(resource, configuration);
                return true;
            }
            catch (Exception e)
            {
                PlanApi.Logger.LogWarning($"Unable to convert Resource {resource.Name} [{resource.Id}] to a Service Resource because of: {e}");
                return false;
            }
        }

        /// <summary>
        /// Attempts to convert the resource with the specified identifier to a <see cref="ServiceResource"/>.
        /// </summary>
        /// <param name="resourceId">The unique identifier of the resource to convert.</param>
        /// <param name="configuration">The configuration for the service link.</param>
        /// <param name="serviceResource">When this method returns, contains the converted <see cref="ServiceResource"/>, if the conversion succeeded; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the conversion succeeded; otherwise, <c>false</c>.</returns>
        public bool TryConvertToServiceResource(Guid resourceId, ResourceServiceLinkConfiguration configuration, out ServiceResource serviceResource)
        {
            serviceResource = null;

            try
            {
                serviceResource = ConvertToServiceResource(resourceId, configuration);
                return true;
            }
            catch (Exception e)
            {
                PlanApi.Logger.LogWarning($"Unable to convert Resource with ID {resourceId} to a Service Resource because of: {e}");
                return false;
            }
        }

        /// <summary>
        /// Attempts to convert the specified <see cref="Resource"/> to an <see cref="UnmanagedResource"/>.
        /// </summary>
        /// <param name="resource">The resource to convert.</param>
        /// <param name="unmanagedResource">When this method returns, contains the converted <see cref="UnmanagedResource"/>, if the conversion succeeded; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the conversion succeeded; otherwise, <c>false</c>.</returns>
        public bool TryConvertToUnmanagedResource(Resource resource, out UnmanagedResource unmanagedResource)
        {
            unmanagedResource = null;

            try
            {
                unmanagedResource = ConvertToUnmanagedResource(resource);
                return true;
            }
            catch (Exception e)
            {
                PlanApi.Logger.LogWarning($"Unable to convert Resource {resource.Name} [{resource.Id}] to an Unmanaged Resource because of: {e}");
                return false;
            }
        }

        /// <summary>
        /// Attempts to convert the resource with the specified identifier to an <see cref="UnmanagedResource"/>.
        /// </summary>
        /// <param name="resourceId">The unique identifier of the resource to convert.</param>
        /// <param name="unmanagedResource">When this method returns, contains the converted <see cref="UnmanagedResource"/>, if the conversion succeeded; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the conversion succeeded; otherwise, <c>false</c>.</returns>
        public bool TryConvertToUnmanagedResource(Guid resourceId, out UnmanagedResource unmanagedResource)
        {
            unmanagedResource = null;

            try
            {
                unmanagedResource = ConvertToUnmanagedResource(resourceId);
                return true;
            }
            catch (Exception e)
            {
                PlanApi.Logger.LogWarning($"Unable to convert Resource with ID {resourceId} to an Unmanaged Resource because of: {e}");
                return false;
            }
        }

        /// <summary>
        /// Attempts to convert the specified <see cref="Resource"/> to a <see cref="VirtualFunctionResource"/>.
        /// </summary>
        /// <param name="resource">The resource to convert.</param>
        /// <param name="configuration">The configuration for the virtual function link.</param>
        /// <param name="virtualFunctionResource">When this method returns, contains the converted <see cref="VirtualFunctionResource"/>, if the conversion succeeded; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the conversion succeeded; otherwise, <c>false</c>.</returns>
        public bool TryConvertToVirtualFunctionResource(Resource resource, ResourceVirtualFunctionLinkConfiguration configuration, out VirtualFunctionResource virtualFunctionResource)
        {
            virtualFunctionResource = null;

            try
            {
                virtualFunctionResource = ConvertToVirtualFunctionResource(resource, configuration);
                return true;
            }
            catch (Exception e)
            {
                PlanApi.Logger.LogWarning($"Unable to convert Resource {resource.Name} [{resource.Id}] to a Virtual Function Resource because of: {e}");
                return false;
            }
        }

        /// <summary>
        /// Attempts to convert the resource with the specified identifier to a <see cref="VirtualFunctionResource"/>.
        /// </summary>
        /// <param name="resourceId">The unique identifier of the resource to convert.</param>
        /// <param name="configuration">The configuration for the virtual function link.</param>
        /// <param name="virtualFunctionResource">When this method returns, contains the converted <see cref="VirtualFunctionResource"/>, if the conversion succeeded; otherwise, <c>null</c>.</param>
        /// <returns><c>true</c> if the conversion succeeded; otherwise, <c>false</c>.</returns>
        public bool TryConvertToVirtualFunctionResource(Guid resourceId, ResourceVirtualFunctionLinkConfiguration configuration, out VirtualFunctionResource virtualFunctionResource)
        {
            virtualFunctionResource = null;

            try
            {
                virtualFunctionResource = ConvertToVirtualFunctionResource(resourceId, configuration);
                return true;
            }
            catch (Exception e)
            {
                PlanApi.Logger.LogWarning($"Unable to convert Resource with ID {resourceId} to a Virtual Function Resource because of: {e}");
                return false;
            }
        }

        /// <summary>
        /// Updates an existing resource in the repository.
        /// </summary>
        /// <param name="apiObject">The resource to update.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObject"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Thrown when attempting to update a new resource that doesn't exist yet.</exception>
        /// <exception cref="MediaOpsException">Thrown when the update operation fails.</exception>
        public void Update(Resource apiObject)
        {
            if (apiObject == null)
            {
                throw new ArgumentNullException(nameof(apiObject));
            }

            PlanApi.Logger.LogInformation($"Updating existing Resource {apiObject.Name}...");

            ActivityHelper.Track(nameof(ResourcesRepository), nameof(Update), act =>
            {
                if (!apiObject.HasChanges)
                {
                    act?.AddTag("NoChanges", true);
                    return;
                }

                if (apiObject.IsNew)
                {
                    throw new InvalidOperationException("Not possible to use method Update for new resources. Use Create or CreateOrUpdate instead.");
                }

                if (!DomResourceHandler.TryCreateOrUpdate(PlanApi, [apiObject], out var result))
                {
                    throw new MediaOpsException(result.TraceDataPerItem[apiObject.Id]);
                }

                var resourceId = result.SuccessfulIds.First();
                act?.AddTag("ResourceId", resourceId);
            });
        }

        /// <summary>
        /// Updates multiple existing resources in the repository.
        /// </summary>
        /// <param name="apiObjects">The collection of resources to update.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="apiObjects"/> is <c>null</c>.</exception>
        /// <exception cref="MediaOpsException">Thrown when attempting to update new resources or when the bulk update operation fails.</exception>
        public void Update(IEnumerable<Resource> apiObjects)
        {
            if (apiObjects == null)
            {
                throw new ArgumentNullException(nameof(apiObjects));
            }

            ActivityHelper.Track(nameof(ResourcesRepository), nameof(Update), act =>
            {
                var newResources = apiObjects.Where(x => x.IsNew);
                if (newResources.Any())
                {
                    throw new InvalidOperationException("Not possible to use method Update for new resources. Use Create or CreateOrUpdate instead.");
                }

                if (!DomResourceHandler.TryCreateOrUpdate(PlanApi, apiObjects.ToList(), out var result))
                {
                    throw new MediaOpsBulkException<Guid>(result);
                }

                var resourceIds = result.SuccessfulIds;
                act?.AddTag("ResourceIds", String.Join(", ", resourceIds));
            });
        }

        /// <summary>
        /// Handles moving the resource to the Complete state.
        /// </summary>
        /// <param name="resource">The resource to transition.</param>
        /// <exception cref="MediaOpsException">Thrown when the resource cannot be completed from its current state.</exception>
        private void HandleMoveToCompleteAction(Resource resource)
        {
            if (resource.State == ResourceState.Complete)
            {
                PlanApi.Logger.LogInformation("Resource {resource.Id} is already in Complete state. No action taken.", resource.Id);
                return;
            }

            if (resource.State != ResourceState.Draft)
            {
                var error = new ResourceInvalidStateError()
                {
                    ErrorMessage = "A resource can only be completed from Draft State.",
                    Id = resource.Id,
                };

                throw new MediaOpsException(error);
            }

            ActivityHelper.Track(nameof(DomResourceHandler), nameof(DomResourceHandler.TransitionToComplete), act =>
            {
                act?.AddTag("ResourceId", resource.Id);
                act?.AddTag("ResourceName", resource.Name);

                DomResourceHandler.TransitionToComplete(PlanApi, resource);
            });
        }

        /// <summary>
        /// Handles moving the resource to the Deprecated state.
        /// </summary>
        /// <param name="resource">The resource to transition.</param>
        /// <exception cref="MediaOpsException">Thrown when the resource cannot be deprecated from its current state or the deprecation fails.</exception>
        private void HandleMoveToDeprecatedAction(Resource resource)
        {
            if (resource.State == ResourceState.Deprecated)
            {
                PlanApi.Logger.LogInformation("Resource {resource.Id} is already in Deprecated state. No action taken.", resource.Id);
                return;
            }

            if (resource.State != ResourceState.Complete)
            {
                var error = new ResourceInvalidStateError()
                {
                    ErrorMessage = "A resource can only be deprecated from Complete State.",
                    Id = resource.Id,
                };

                throw new MediaOpsException(error);
            }

            ActivityHelper.Track(nameof(ResourcesRepository), nameof(HandleMoveToDeprecatedAction), act =>
            {
                act?.AddTag("ResourceId", resource.Id);
                act?.AddTag("ResourceName", resource.Name);

                if (!DomResourceHandler.TryDeprecate(PlanApi, [resource], out var result))
                {
                    throw new MediaOpsException(result.TraceDataPerItem[resource.Id]);
                }
            });
        }
    }
}
