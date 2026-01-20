namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;

    using Skyline.DataMiner.Solutions.MediaOps.Plan.Exceptions;

    /// <summary>
    /// Defines methods for managing <see cref="ResourcePool"/> objects, including state transitions.
    /// </summary>
    public interface IResourcePoolsRepository : IRepository<ResourcePool>
    {
        /// <summary>
        /// Moves the specified <see cref="ResourcePool"/> to the desired state.
        /// </summary>
        /// <param name="resourcePool">The resource pool to move.</param>
        /// <param name="desiredState">The state to move the resource pool to.</param>
        void MoveTo(ResourcePool resourcePool, ResourcePoolState desiredState);

        /// <summary>
        /// Moves the resource pool with the specified identifier to the desired state.
        /// </summary>
        /// <param name="resourcePoolId">The unique identifier of the resource pool to move.</param>
        /// <param name="desiredState">The state to move the resource pool to.</param>
        void MoveTo(Guid resourcePoolId, ResourcePoolState desiredState);

        /// <summary>
        /// Deprecates the specified <see cref="ResourcePool"/> using the provided <see cref="ResourcePoolDeprecateOptions"/>.
        /// </summary>
        /// <param name="resourcePool">The resource pool to deprecate.</param>
        /// <param name="options">Options specifying hot the resource pool and its resources should be deprecated.</param>
        void Deprecate(ResourcePool resourcePool, ResourcePoolDeprecateOptions options);

        /// <summary>
        /// Deletes the specified <see cref="ResourcePool"/> using the provided <see cref="ResourcePoolDeleteOptions"/>.
        /// </summary>
        /// <param name="resourcePool">The resource pool to delete.</param>
        /// <param name="options">Options specifying how the resource pool and its resources should be deleted.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="resourcePool"/> is <c>null</c>.</exception>
        /// <exception cref="MediaOpsException">Thrown when the deletion operation fails for the specified resource pool.</exception>
        void Delete(ResourcePool resourcePool, ResourcePoolDeleteOptions options);

        /// <summary>
        /// Deletes the specified resource pool from the repository using the provided <see cref="ResourcePoolDeleteOptions"/>.
        /// </summary>
        /// <param name="resourcePoolId">The unique identifier of the resource pool to delete.</param>
        /// <param name="options">Options specifying how the resource pool and its resources should be deleted.</param>
        /// <exception cref="MediaOpsException">Thrown when the deletion operation fails for the specified resource pool.</exception>
        void Delete(Guid resourcePoolId, ResourcePoolDeleteOptions options);

        /// <summary>
        /// Deletes the specified resource pools from the repository using the provided <see cref="ResourcePoolDeleteOptions"/>.
        /// </summary>
        /// <param name="resourcePools">The resource pools to delete.</param>
        /// <param name="options">Options specifying how the resource pools and its resources should be deleted.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="resourcePools"/> is <c>null</c>.</exception>
        /// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk deletion operation fails for one or more resource pools.</exception>
        void Delete(IEnumerable<ResourcePool> resourcePools, ResourcePoolDeleteOptions options);

        /// <summary>
        /// Deletes resource pools with the specified identifiers from the repository using the provided <see cref="ResourcePoolDeleteOptions"/>.
        /// </summary>
        /// <param name="resourcePoolIds">The unique identifiers of the resource pools to delete.</param>
        /// <param name="options">Options specifying how the resource pools and its resources should be deleted.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="resourcePoolIds"/> is <c>null</c>.</exception>
        /// <exception cref="MediaOpsBulkException{Guid}">Thrown when the bulk deletion operation fails for one or more resource pools.</exception>
        void Delete(IEnumerable<Guid> resourcePoolIds, ResourcePoolDeleteOptions options);

        /// <summary>
        /// Retrieves a collection of resource pools associated with the specified resource.
        /// </summary>
        /// <param name="resource">The resource for which to retrieve the associated resource pools. Cannot be <see langword="null"/>.</param>
        /// <returns>An enumerable collection of <see cref="ResourcePool"/> objects associated with the specified resource. If no
        /// resource pools are associated, returns an empty collection.</returns>
        IEnumerable<ResourcePool> GetResourcePools(Resource resource);

        /// <summary>
        /// Retrieves a mapping of resources to their associated resource pools.
        /// </summary>
        /// <remarks>The method does not guarantee the order of resources or resource pools in the
        /// returned dictionary.</remarks>
        /// <param name="resources">A collection of resources for which to retrieve the associated resource pools.</param>
        /// <returns>A read-only dictionary where each key is a resource from the input collection,  and the value is an
        /// enumerable of resource pools associated with that resource. If a resource has no associated pools, it will
        /// not appear in the dictionary.</returns>
        IReadOnlyDictionary<Resource, IEnumerable<ResourcePool>> GetPoolsPerResource(IEnumerable<Resource> resources);

        /// <summary>
        /// Retrieves a mapping of resource pools to their respective parent resource pools.
        /// </summary>
        /// <remarks>The method does not modify the input collection or the resource pools themselves. The
        /// caller should ensure that the input collection is not null or empty to avoid unexpected results.</remarks>
        /// <param name="resourcePools">A collection of resource pools for which to retrieve parent-child relationships.</param>
        /// <returns>A read-only dictionary where each key is a resource pool from the input collection, and the value is an
        /// enumerable of its parent resource pools. If a resource pool has no parents, its value will be an empty
        /// enumerable.</returns>
        IReadOnlyDictionary<ResourcePool, IEnumerable<ResourcePool>> GetParentPoolLinks(IEnumerable<ResourcePool> resourcePools);

        /// <summary>
        /// Assigns the specified resources to the given resource pool.
        /// </summary>
        /// <param name="resourcePool">The resource pool to which the resources will be assigned.</param>
        /// <param name="resources">The collection of resources to assign to the pool.</param>
        void AssignResourcesToPool(ResourcePool resourcePool, IEnumerable<Resource> resources);

        /// <summary>
        /// Assigns the specified resources to the given resource pool.
        /// </summary>
        /// <param name="resourcePoolId">The unique identifier of the resource pool to which the resources will be assigned.</param>
        /// <param name="resources">The collection of resources to assign to the pool.</param>
        void AssignResourcesToPool(Guid resourcePoolId, IEnumerable<Resource> resources);

        /// <summary>
        /// Removes the specified resources from the given resource pool.
        /// </summary>
        /// <param name="resourcePool">The resource pool from which the resources will be unassigned.</param>
        /// <param name="resources">The collection of resources to remove from the pool.</param>
        void UnassignResourcesFromPool(ResourcePool resourcePool, IEnumerable<Resource> resources);

        /// <summary>
        /// Removes the specified resources from the given resource pool.
        /// </summary>
        /// <param name="resourcePoolId">The unique identifier of the resource pool from which the resources will be unassigned.</param>
        /// <param name="resources">The collection of resources to remove from the pool.</param>
        void UnassignResourcesFromPool(Guid resourcePoolId, IEnumerable<Resource> resources);
    }
}
