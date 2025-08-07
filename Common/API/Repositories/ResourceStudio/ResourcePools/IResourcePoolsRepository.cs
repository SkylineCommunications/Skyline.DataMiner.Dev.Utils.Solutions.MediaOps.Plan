namespace Skyline.DataMiner.MediaOps.Plan.API
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Defines methods for managing <see cref="ResourcePool"/> objects, including state transitions.
    /// </summary>
    public interface IResourcePoolsRepository : ICrudRepository<ResourcePool>, ICounterRepository<ResourcePool>
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
        /// Deletes the specified <see cref="ResourcePool"/> using the provided <see cref="ResourcePoolDeleteOptions"/>.
        /// </summary>
        /// <param name="resourcepool">The resource pool to delete.</param>
        /// <param name="options">Options specifying how the resource pool and its resources should be deleted.</param>
        void Delete(ResourcePool resourcepool, ResourcePoolDeleteOptions options);

        /// <summary>
        /// Determines whether the specified <see cref="ResourcePool"/> contains any deprecated resources.
        /// </summary>
        /// <param name="resourcePool">The resource pool to check for deprecated resources.</param>
        /// <returns><c>true</c> if the resource pool contains deprecated resources; otherwise, <c>false</c>.</returns>
        bool HasDeprecatedResources(ResourcePool resourcePool);

        /// <summary>
        /// Gets the number of deprecated resources in the specified <see cref="ResourcePool"/>.
        /// </summary>
        /// <param name="resourcePool">The resource pool to count deprecated resources in.</param>
        /// <returns>The count of deprecated resources in the resource pool.</returns>
        long DeprecatedResourceCount(ResourcePool resourcePool);

        /// <summary>
        /// Determines whether the specified <see cref="ResourcePool"/> contains any resources.
        /// </summary>
        /// <param name="resourcePool">The resource pool to check for resources.</param>
        /// <returns><c>true</c> if the resource pool contains resources; otherwise, <c>false</c>.</returns>
        bool HasResources(ResourcePool resourcePool);

        /// <summary>
        /// Gets the total number of resources in the specified <see cref="ResourcePool"/>.
        /// </summary>
        /// <param name="resourcePool">The resource pool to count resources in.</param>
        /// <returns>The count of resources in the resource pool.</returns>
        long ResourceCount(ResourcePool resourcePool);
    }
}
