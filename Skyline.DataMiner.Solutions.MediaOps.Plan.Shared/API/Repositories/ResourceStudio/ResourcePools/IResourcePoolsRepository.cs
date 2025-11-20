namespace Skyline.DataMiner.Solutions.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Defines methods for managing <see cref="ResourcePool"/> objects, including state transitions.
    /// </summary>
    public interface IResourcePoolsRepository : ICrudRepository<ResourcePool>
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
        void Delete(ResourcePool resourcePool, ResourcePoolDeleteOptions options);

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
    }
}
